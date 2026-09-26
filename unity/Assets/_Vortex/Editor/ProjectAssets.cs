using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Events;

namespace Vortex.Editor
{
    /// <summary>
    /// Creates the project's base assets when they are missing (menu, or batch mode with
    /// -executeMethod Vortex.Editor.ProjectAssets.EnsureAll). Existing assets are never overwritten: once created,
    /// they belong to the designer.
    /// </summary>
    public static class ProjectAssets
    {
        /// <summary>The game content asset (references the engine's content files).</summary>
        public const string ContentPath = "Assets/_Vortex/Content/GameContent.asset";

        /// <summary>The feedback profile used by the game scene.</summary>
        public const string ProfilePath = "Assets/_Vortex/Presentation/Feedback/DefaultFeedbackProfile.asset";

        /// <summary>The dice shown for a roll.</summary>
        public const string DiceTrayPath = "Assets/_Vortex/Prefabs/DiceTray.prefab";

        /// <summary>The feedback of dice rolls.</summary>
        public const string DicePath = "Assets/_Vortex/Presentation/Feedback/Dice.asset";

        private const string PausePath = "Assets/_Vortex/Presentation/Feedback/Pause.asset";
        private const string FeedbackFolder = "Assets/_Vortex/Presentation/Feedback/";
        private const string DataFolder = "Packages/com.vortex.core/Runtime/Data/";

        /// <summary>Creates every missing base asset.</summary>
        [MenuItem("Vortex/Développement/Créer les assets de base manquants")]
        public static void EnsureAll()
        {
            EnsureContent();
            EnsureFeedbackProfile();
            ThemeAssets.EnsureAll();
            EnsureDice();
            EnsureAnimations();
            GalleryScene.Ensure();
            GameScene.Ensure();
            MenuScene.Ensure();
            BuildSceneList.Update();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureContent()
        {
            if (AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath) != null)
            {
                return;
            }

            var content = ScriptableObject.CreateInstance<GameContent>();
            content.Assign(Data(CardsFile.FileName), Data(EventsFile.FileName), Data(TechnologiesFile.FileName), Data(GameConfig.FileName));
            Create(content, ContentPath);
        }

        private static void EnsureFeedbackProfile()
        {
            if (AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProfilePath) != null)
            {
                return;
            }

            PauseFeedback pause = AssetDatabase.LoadAssetAtPath<PauseFeedback>(PausePath);
            if (pause == null)
            {
                pause = ScriptableObject.CreateInstance<PauseFeedback>();
                pause.Configure(0.15f);
                Create(pause, PausePath);
            }

            var profile = ScriptableObject.CreateInstance<FeedbackProfile>();
            profile.Configure(pause);
            Create(profile, ProfilePath);
        }

        // The dice tray and its feedback. When the feedback is created, it is also given to dice rolls in the profile
        // (unless the profile already plays something for them); later, the profile belongs to the designer.
        private static void EnsureDice()
        {
            GameObject trayAsset = AssetDatabase.LoadAssetAtPath<GameObject>(DiceTrayPath);
            DiceTray tray = trayAsset != null ? trayAsset.GetComponent<DiceTray>() : BuildDiceTray();
            if (AssetDatabase.LoadAssetAtPath<DiceFeedback>(DicePath) != null)
            {
                return;
            }

            var dice = ScriptableObject.CreateInstance<DiceFeedback>();
            dice.Configure(tray, FeedbackAnchor.Foreground, 0.6f, 0.8f);
            Create(dice, DicePath);
            FeedbackProfile profile = AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProfilePath);
            bool attackDice = profile.MapIfMissing(GameEventType.DiceRolled, dice);
            bool effectDie = profile.MapIfMissing(GameEventType.DieRolled, dice);
            if (attackDice || effectDie)
            {
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
        }

        // The first animations of the table (ANIMATIONS.md, lot 1): each feedback is created once and given to its event
        // in the profile unless the profile already plays something for it; afterwards both belong to the designer.
        private static void EnsureAnimations()
        {
            FeedbackProfile profile = AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProfilePath);
            bool changed = Animation<AimFeedback>(profile, "Aim", GameEventType.AttackDeclared);
            changed |= Animation<LaserFeedback>(profile, "Laser", GameEventType.AttackResolved);
            changed |= Animation<DeflectFeedback>(profile, "Deflect", GameEventType.AttackRedirected);
            changed |= Animation<CriticalFeedback>(profile, "Critical", GameEventType.CriticalHit);
            changed |= Animation<DodgeFeedback>(profile, "Dodge", GameEventType.HpLossPrevented);
            changed |= Animation<ShieldPulseFeedback>(profile, "ShieldPulse", GameEventType.ShieldChanged);
            changed |= Animation<TormentFeedback>(profile, "Torment", GameEventType.TormentPlaced, GameEventType.TormentsRemoved);
            changed |= Animation<CardFlightFeedback>(profile, "CardFlight", GameEventType.MarketCardTaken, GameEventType.CardStolen, GameEventType.CardActivated);
            changed |= Animation<EventFeedback>(profile, "RoundEvent", GameEventType.EventRevealed);
            changed |= Animation<KnockbackFeedback>(profile, "Knockback", GameEventType.HpLost);
            changed |= Animation<ThrusterFeedback>(profile, "Thrusters", GameEventType.TechnologyActivated);
            changed |= Animation<ExplosionFeedback>(profile, "Explosion", GameEventType.PlayerEliminated);
            if (changed)
            {
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssetIfDirty(profile);
            }
        }

        private static bool Animation<T>(FeedbackProfile profile, string name, params GameEventType[] types)
            where T : FeedbackAsset
        {
            string path = FeedbackFolder + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return false;
            }

            T feedback = ScriptableObject.CreateInstance<T>();
            Create(feedback, path);
            bool mapped = false;
            foreach (GameEventType type in types)
            {
                mapped |= profile.MapIfMissing(type, feedback);
            }

            return mapped;
        }

        // A dark rounded tray; each die is a white diamond (a d8 seen from above) with its value, then the total.
        private static DiceTray BuildDiceTray()
        {
            var root = new GameObject("DiceTray", typeof(RectTransform), typeof(Image), typeof(DiceTray));
            UiBuilder.Box(root.GetComponent<Image>(), new Color(0.03f, 0.04f, 0.08f, 0.9f));
            HorizontalLayoutGroup row = root.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(18, 18, 14, 14);
            row.spacing = 16f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            ContentSizeFitter fit = root.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform die = UiBuilder.Part<RectTransform>(root.transform, "Dé", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(90f, 90f));
            LayoutElement dieSize = die.gameObject.AddComponent<LayoutElement>();
            dieSize.preferredWidth = 90f;
            dieSize.preferredHeight = 90f;
            Image shape = UiBuilder.Box(UiBuilder.Fixed<Image>(die, "Forme", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64f, 64f)), new Color32(242, 242, 245, 255));
            shape.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            TMP_Text value = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(die, "Valeur", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 40f, FontStyles.Bold, TextAlignmentOptions.Center);
            value.color = new Color32(26, 28, 40, 255);
            die.gameObject.SetActive(false);

            TMP_Text total = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(root.transform, "Total", Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(120f, 90f)), 40f, FontStyles.Bold, TextAlignmentOptions.Center);
            LayoutElement totalSize = total.gameObject.AddComponent<LayoutElement>();
            totalSize.preferredWidth = 120f;
            totalSize.preferredHeight = 90f;

            root.GetComponent<DiceTray>().Assign(die, total);
            Directory.CreateDirectory(Path.GetDirectoryName(DiceTrayPath)!);
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, DiceTrayPath);
            Object.DestroyImmediate(root);
            Debug.Log("Created " + DiceTrayPath);
            return saved.GetComponent<DiceTray>();
        }

        private static TextAsset Data(string file)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder + file);
            if (asset == null)
            {
                throw new FileNotFoundException("Engine content file not found: " + DataFolder + file);
            }

            return asset;
        }

        /// <summary>Saves a new asset, creating its folder if needed.</summary>
        internal static void Create(Object asset, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log("Created " + path);
        }
    }
}
