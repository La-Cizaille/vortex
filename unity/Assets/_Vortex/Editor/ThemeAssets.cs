using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;
using Vortex.Core.Content;
using Object = UnityEngine.Object;

namespace Vortex.Editor
{
    /// <summary>
    /// Creates the theme assets when they are missing (through <see cref="ProjectAssets.EnsureAll"/>) and keeps the card
    /// art catalog in step with the art folder. Existing assets are never overwritten: they belong to the designer.
    /// </summary>
    public static class ThemeAssets
    {
        /// <summary>Colours, fonts, text icons and playback speed.</summary>
        public const string ThemePath = "Assets/_Vortex/Theme/ThemeSettings.asset";

        /// <summary>Illustrations by content id.</summary>
        public const string CardArtPath = "Assets/_Vortex/Theme/CardArtCatalog.asset";

        /// <summary>Ship of each seat.</summary>
        public const string ShipsPath = "Assets/_Vortex/Theme/ShipCatalog.asset";

        /// <summary>Interface texts.</summary>
        public const string TextsPath = "Assets/_Vortex/Content/TextTable.asset";

        /// <summary>Layout of a card.</summary>
        public const string CardPrefabPath = "Assets/_Vortex/Prefabs/Card.prefab";

        private const string TextMeshProSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        /// <summary>Creates every missing theme asset.</summary>
        public static void EnsureAll()
        {
            EnsureTextMeshPro();
            EnsureFolder(ArtImportRules.CardsFolder, "Illustrations des cartes, des événements et des technologies : un fichier par identifiant, nommé comme lui (A_005.png, EVT_TROU_NOIR.png, TECH_BLUE.png). Voir docs/CONTRIBUTING.md.");
            EnsureFolder(ArtImportRules.ShipsFolder, "Modèles des vaisseaux (.fbx), à associer à un siège dans Theme/ShipCatalog. Voir docs/CONTRIBUTING.md.");
            Ensure<ThemeSettings>(ThemePath);
            Ensure<CardArtCatalog>(CardArtPath);
            Ensure<ShipCatalog>(ShipsPath);
            EnsureTexts();
            EnsureCardPrefab();
            SyncCardArt();
        }

        /// <summary>
        /// Rebuilds the card art catalog from the art folder. A file whose name is not a content id is reported and
        /// left out. Does nothing while the catalog or the content is not imported yet (first import of the project).
        /// </summary>
        public static void SyncCardArt()
        {
            CardArtCatalog catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CardArtPath);
            GameContent content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            if (catalog == null || content == null)
            {
                return;
            }

            HashSet<string> ids = ContentIds(content.LoadData());
            var art = new List<(string Id, Sprite Sprite)>();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtImportRules.CardsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string id = Path.GetFileNameWithoutExtension(path);
                if (!ids.Contains(id))
                {
                    Debug.LogWarning(path + " : « " + id + " » n'est l'identifiant d'aucune carte, d'aucun événement ni d'aucune technologie (voir docs/CARDS.md). Fichier ignoré.");
                    continue;
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning(path + " n'est pas importé comme sprite (réglage « Texture Type » de l'import). Fichier ignoré.");
                    continue;
                }

                art.Add((id, sprite));
            }

            if (catalog.Replace(art))
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
        }

        /// <summary>Lists the content ids that still show a generated placeholder.</summary>
        [MenuItem("Vortex/Habillage/Vérifier les illustrations")]
        public static void ReportMissingArt()
        {
            SyncCardArt();
            CardArtCatalog catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CardArtPath);
            GameContent content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            HashSet<string> ids = ContentIds(content.LoadData());
            List<string> missing = ids.Where(id => catalog.Find(id) == null).OrderBy(id => id, StringComparer.Ordinal).ToList();
            Debug.Log("Illustrations : " + (ids.Count - missing.Count) + " sur " + ids.Count + "."
                + (missing.Count == 0 ? string.Empty : " Visuel provisoire pour : " + string.Join(", ", missing) + "."));
        }

        /// <summary>Every content id that can have an illustration: modifiers, events and technologies.</summary>
        public static HashSet<string> ContentIds(GameData data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return new HashSet<string>(
                data.Modifiers.Select(c => c.Id).Concat(data.Events.Select(e => e.Id)).Concat(data.Technologies.Select(t => t.Id)),
                StringComparer.Ordinal);
        }

        private static void EnsureTextMeshPro()
        {
            // The font, shaders and settings TextMeshPro needs are versioned in Assets/TextMesh Pro. They come from the
            // pinned uGUI package; Unity's importer only queues that import in batch mode, so it is not automated here.
            if (!File.Exists(TextMeshProSettingsPath))
            {
                throw new InvalidOperationException(
                    "TextMeshPro resources are missing (" + TextMeshProSettingsPath + "). Import them with Window > TextMeshPro > "
                    + "Import TMP Essential Resources, then run this menu again.");
            }
        }

        private static void EnsureFolder(string folder, string readme)
        {
            string file = folder + "/README.md";
            if (!File.Exists(file))
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(file, readme + "\n");
                AssetDatabase.ImportAsset(file);
            }
        }

        private static void Ensure<T>(string path)
            where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
            {
                ProjectAssets.Create(ScriptableObject.CreateInstance<T>(), path);
            }
        }

        private static void EnsureTexts()
        {
            TextTable table = AssetDatabase.LoadAssetAtPath<TextTable>(TextsPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<TextTable>();
                table.AddMissing(TextKeys.Defaults);
                ProjectAssets.Create(table, TextsPath);
            }
            else if (table.AddMissing(TextKeys.Defaults))
            {
                EditorUtility.SetDirty(table);
                AssetDatabase.SaveAssetIfDirty(table);
            }
        }

        private static void EnsureCardPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath) != null)
            {
                return;
            }

            // A 250 x 350 card: frame, inner background, illustration on top, then name, kind, text and id.
            var root = new GameObject("Card", typeof(RectTransform), typeof(Image), typeof(CardDisplay));
            ((RectTransform)root.transform).sizeDelta = new Vector2(250f, 350f);
            Image frame = root.GetComponent<Image>();
            Image background = UiBuilder.Part<Image>(root.transform, "Fond", Vector2.zero, Vector2.one, new Vector2(6f, 6f), new Vector2(-6f, -6f));
            Image art = UiBuilder.Part<Image>(background.transform, "Illustration", new Vector2(0f, 1f), Vector2.one, new Vector2(8f, -128f), new Vector2(-8f, -8f));
            TMP_Text title = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(background.transform, "Nom", new Vector2(0f, 1f), Vector2.one, new Vector2(8f, -162f), new Vector2(-8f, -132f)), 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            TMP_Text caption = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(background.transform, "Type", new Vector2(0f, 1f), Vector2.one, new Vector2(8f, -182f), new Vector2(-8f, -162f)), 13f, FontStyles.Normal, TextAlignmentOptions.Center);
            TMP_Text body = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(background.transform, "Texte", Vector2.zero, Vector2.one, new Vector2(10f, 24f), new Vector2(-10f, -186f)), 15f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            body.enableAutoSizing = true;
            body.fontSizeMin = 9f;
            body.fontSizeMax = 15f;
            TMP_Text id = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(background.transform, "Identifiant", Vector2.zero, new Vector2(1f, 0f), new Vector2(8f, 4f), new Vector2(-8f, 22f)), 11f, FontStyles.Normal, TextAlignmentOptions.BottomRight);

            // A long name shrinks to stay on one line instead of running over the caption.
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.enableAutoSizing = true;
            title.fontSizeMin = 12f;
            title.fontSizeMax = 20f;

            // Torment tokens: a disc with the count, over the top right corner of the illustration. It is large, so
            // that it stays readable when the card is shown small on a seat panel.
            Image badge = UiBuilder.Fixed<Image>(root.transform, "Tourments", Vector2.one, new Vector2(4f, 4f), new Vector2(96f, 96f));
            badge.sprite = UiBuilder.Disc;
            badge.color = new Color32(150, 40, 60, 255);
            badge.raycastTarget = false;
            TMP_Text torments = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(badge.transform, "Nombre", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 56f, FontStyles.Bold, TextAlignmentOptions.Center);
            badge.gameObject.SetActive(false);

            // Only the frame receives pointer events; the parts inside are never hit-tested.
            background.raycastTarget = false;
            art.raycastTarget = false;
            root.GetComponent<CardDisplay>().Assign(frame, background, art, title, caption, body, id, badge.gameObject, torments);

            Directory.CreateDirectory(Path.GetDirectoryName(CardPrefabPath)!);
            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("Created " + CardPrefabPath);
        }
    }
}
