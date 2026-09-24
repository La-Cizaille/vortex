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

        /// <summary>The card, a 3D object (ADR-0017).</summary>
        public const string CardPrefabPath = "Assets/_Vortex/Prefabs/Card.prefab";

        /// <summary>Body of the cards (tinted with their technology colour).</summary>
        public const string CardBodyMaterialPath = "Assets/_Vortex/Theme/Materials/CardBody.mat";

        /// <summary>Face parts of the cards: background and illustration.</summary>
        public const string CardFaceMaterialPath = "Assets/_Vortex/Theme/Materials/CardFace.mat";

        /// <summary>Back of the cards.</summary>
        public const string CardBackMaterialPath = "Assets/_Vortex/Theme/Materials/CardBack.mat";

        /// <summary>Disc of the Torment badge.</summary>
        public const string CardBadgeMaterialPath = "Assets/_Vortex/Theme/Materials/CardBadge.mat";

        private const string TextMeshProSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        /// <summary>Creates every missing theme asset.</summary>
        public static void EnsureAll()
        {
            EnsureTextMeshPro();
            EnsureFolder(ArtImportRules.CardsFolder, "Illustrations des cartes, des événements et des technologies : un fichier par identifiant, nommé comme lui (A_005.png, EVT_TROU_NOIR.png, TECH_BLUE.png). Voir docs/CONTRIBUTING.md.");
            EnsureFolder(ArtImportRules.ShipsFolder, "Modèles des vaisseaux (.fbx), exportés depuis art-src/ships avec tools/blender/export_unity.py, puis associés à un siège dans Theme/ShipCatalog. Voir docs/ASSETS.md.");
            EnsureFolder(ArtImportRules.IconsFolder, "Icônes (PNG transparents) : actions d'équipage, icônes du texte des cartes, technologies, jetons. Noms et tailles : docs/ASSETS.md.");
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

            Material body = EnsureMaterial(CardBodyMaterialPath, Color.white);
            Material face = EnsureMaterial(CardFaceMaterialPath, Color.white);
            Material back = EnsureMaterial(CardBackMaterialPath, new Color32(30, 34, 62, 255));
            Material badgeColour = EnsureMaterial(CardBadgeMaterialPath, new Color32(150, 40, 60, 255));

            // A 1 x 1.4 card, 0.02 thick (ADR-0017). The front faces -Z, like Unity's quads and TextMeshPro texts, so a
            // card turned like the camera shows its front to it. Everything visible is under "Visuel", so that the card
            // can be hidden without being destroyed; the box collider takes pointer events.
            var size = new Vector2(1f, 1.4f);
            const float Thickness = 0.02f;
            var root = new GameObject("Card", typeof(CardDisplay), typeof(BoxCollider));
            var area = root.GetComponent<BoxCollider>();
            area.size = new Vector3(size.x, size.y, Thickness);
            Transform visual = new GameObject("Visuel").transform;
            visual.SetParent(root.transform, false);

            // The body: a box for now, which a modelled card (rounded corners, bevel) can replace in the prefab.
            Renderer frame = Primitive(PrimitiveType.Cube, visual, "Corps", Vector3.zero, Vector3.zero, new Vector3(size.x, size.y, Thickness), body);
            Renderer background = Primitive(PrimitiveType.Quad, visual, "Fond", new Vector3(0f, 0f, -0.011f), Vector3.zero, new Vector3(0.94f, 1.34f, 1f), face);
            const float ArtWidth = 0.86f;
            const float ArtHeight = ArtWidth * 9f / 16f;
            Renderer art = Primitive(PrimitiveType.Quad, visual, "Illustration", new Vector3(0f, 0.65f - (ArtHeight / 2f), -0.012f), Vector3.zero, new Vector3(ArtWidth, ArtHeight, 1f), face);
            Primitive(PrimitiveType.Quad, visual, "Dos", new Vector3(0f, 0f, 0.011f), new Vector3(0f, 180f, 0f), new Vector3(0.96f, 1.36f, 1f), back);

            TMP_Text title = Text3D(visual, "Nom", new Vector2(0f, 0.1f), new Vector2(0.86f, 0.1f), 0.4f, 1f, FontStyles.Bold, TextAlignmentOptions.Center);
            title.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_Text caption = Text3D(visual, "Type", new Vector2(0f, 0.01f), new Vector2(0.86f, 0.07f), 0.3f, 0.55f, FontStyles.Normal, TextAlignmentOptions.Center);
            TMP_Text text = Text3D(visual, "Texte", new Vector2(0f, -0.34f), new Vector2(0.84f, 0.56f), 0.25f, 0.62f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            TMP_Text id = Text3D(visual, "Identifiant", new Vector2(0.24f, -0.655f), new Vector2(0.4f, 0.05f), 0.2f, 0.4f, FontStyles.Normal, TextAlignmentOptions.Right);

            // Torment tokens: a disc with the count, over the top right corner, large enough to read on a small card.
            var badge = new GameObject("Tourments");
            badge.transform.SetParent(visual, false);
            badge.transform.localPosition = new Vector3(0.42f, 0.62f, -0.02f);
            Primitive(PrimitiveType.Cylinder, badge.transform, "Disque", Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.3f, 0.004f, 0.3f), badgeColour);
            TMP_Text torments = Text3D(badge.transform, "Nombre", Vector2.zero, new Vector2(0.3f, 0.26f), 0.5f, 1.6f, FontStyles.Bold, TextAlignmentOptions.Center);
            torments.transform.localPosition = new Vector3(0f, 0f, -0.006f);
            badge.SetActive(false);

            root.GetComponent<CardDisplay>().Assign(frame, background, art, title, caption, text, id, badge, torments, visual.gameObject, area, size);
            Directory.CreateDirectory(Path.GetDirectoryName(CardPrefabPath)!);
            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log("Created " + CardPrefabPath);
        }

        // Unlit materials: the card reads the same whatever the lighting of the scene. The designer can switch them to
        // a lit shader (Universal Render Pipeline/Lit), which takes the same colour and texture properties.
        private static Material EnsureMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            ProjectAssets.Create(material, path);
            return material;
        }

        private static Renderer Primitive(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 rotation, Vector3 scale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localEulerAngles = rotation;
            part.transform.localScale = scale;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }

        // A world-space TextMeshPro label, sized by its rectangle; auto-sizing keeps long texts inside it.
        private static TMP_Text Text3D(Transform parent, string name, Vector2 position, Vector2 size, float minSize, float maxSize, FontStyles style, TextAlignmentOptions alignment)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            TextMeshPro label = holder.AddComponent<TextMeshPro>();
            var shape = (RectTransform)holder.transform;
            shape.sizeDelta = size;
            shape.localPosition = new Vector3(position.x, position.y, -0.013f);
            label.enableAutoSizing = true;
            label.fontSizeMin = minSize;
            label.fontSizeMax = maxSize;
            label.fontStyle = style;
            label.alignment = alignment;
            return label;
        }
    }
}
