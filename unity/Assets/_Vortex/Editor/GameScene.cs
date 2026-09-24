using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;

namespace Vortex.Editor
{
    /// <summary>
    /// Creates the game scene and the opponent panel prefab when they are missing (through
    /// <see cref="ProjectAssets.EnsureAll"/>), laid out as in INTERFACE.md section 3: the round banner at the top, the
    /// opponents in an arc, the black market in the middle, the player's ship and panel at the bottom, the log and the
    /// playback controls in the corners. Once created, the layout belongs to the designer.
    /// </summary>
    public static class GameScene
    {
        /// <summary>Path of the scene.</summary>
        public const string ScenePath = "Assets/_Vortex/Scenes/Game.unity";

        /// <summary>Path of the opponent panel.</summary>
        public const string SeatPanelPath = "Assets/_Vortex/Prefabs/SeatPanel.prefab";

        private static readonly Color Panel = new Color(0.04f, 0.05f, 0.1f, 0.82f);

        // Opaque, so that the turn frame behind a seat shows only around it.
        private static readonly Color SeatBackground = new Color(0.05f, 0.06f, 0.11f, 1f);
        private static readonly Color Muted = new Color32(150, 156, 175, 255);

        /// <summary>Creates the opponent panel and the scene if they do not exist, and lists the scene in the build.</summary>
        public static void Ensure()
        {
            EnsureSeatPanel();
            if (!File.Exists(ScenePath))
            {
                SceneFiles.Create(ScenePath, Build);
            }

            if (File.Exists(ScenePath) && EditorBuildSettings.scenes.All(s => s.path != ScenePath))
            {
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(ScenePath, true)).ToArray();
            }
        }

        private static void EnsureSeatPanel()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SeatPanelPath) != null)
            {
                return;
            }

            // 300 x 170, hanging from the top centre: the panel sits under its ship (ScreenAnchor).
            var root = new GameObject("SeatPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(SeatDisplay));
            var shape = (RectTransform)root.transform;
            shape.sizeDelta = new Vector2(300f, 170f);
            shape.pivot = new Vector2(0.5f, 1f);

            Image highlight = UiBuilder.Box(UiBuilder.Part<Image>(root.transform, "Cadre", Vector2.zero, Vector2.one, new Vector2(-4f, -4f), new Vector2(4f, 4f)), Color.white);
            UiBuilder.Box(UiBuilder.Part<Image>(root.transform, "Fond", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), SeatBackground);
            TMP_Text name = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root.transform, "Nom", new Vector2(0f, 1f), new Vector2(12f, -8f), new Vector2(150f, 26f)), 20f, FontStyles.Bold, TextAlignmentOptions.Left);
            TMP_Text hp = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root.transform, "PV", new Vector2(0f, 1f), new Vector2(12f, -38f), new Vector2(150f, 24f)), 18f, FontStyles.Normal, TextAlignmentOptions.Left);
            TMP_Text shield = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root.transform, "Bouclier", new Vector2(0f, 1f), new Vector2(12f, -62f), new Vector2(150f, 24f)), 18f, FontStyles.Normal, TextAlignmentOptions.Left);
            Image[] rounds = Enumerable.Range(0, 4)
                .Select(i => Disc(root.transform, "Technologie " + (i + 1), new Vector2(0f, 1f), new Vector2(12f + (i * 24f), -94f), 18f))
                .ToArray();
            Image overcharge = Disc(root.transform, "Surcharge", new Vector2(0f, 1f), new Vector2(126f, -94f), 18f);
            RectTransform attack = UiBuilder.Fixed<RectTransform>(root.transform, "ATK", Vector2.one, new Vector2(-74f, -10f), new Vector2(58f, 81f));
            RectTransform defense = UiBuilder.Fixed<RectTransform>(root.transform, "DEF", Vector2.one, new Vector2(-10f, -10f), new Vector2(58f, 81f));
            TMP_Text statuses = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(root.transform, "Effets", Vector2.zero, new Vector2(1f, 0f), new Vector2(12f, 8f), new Vector2(-12f, 56f)), 14f, FontStyles.Normal, TextAlignmentOptions.BottomLeft);
            statuses.color = Muted;
            GameObject leader = Tag(root.transform, new Vector2(0.5f, 1f), new Vector2(0f, 30f));

            root.GetComponent<SeatDisplay>().Assign(name, hp, shield, statuses, overcharge, rounds, attack, defense, highlight, leader, root.GetComponent<CanvasGroup>());
            Directory.CreateDirectory(Path.GetDirectoryName(SeatPanelPath)!);
            PrefabUtility.SaveAsPrefabAsset(root, SeatPanelPath);
            Object.DestroyImmediate(root);
            Debug.Log("Created " + SeatPanelPath);
        }

        private static void Build()
        {
            // Scene: a fixed camera looking down on the table (ADR-0015), a light, the ships' parent and the centre.
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 45f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 9f, -11f), Quaternion.Euler(38f, 0f, 0f));
            var light = new GameObject("Lumière", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            Transform ships = new GameObject("Vaisseaux").transform;
            Transform centre = new GameObject("Centre de la table").transform;
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Interface, reference resolution 1920 x 1080 (INTERFACE.md 2).
            var canvas = new GameObject("Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform ui = canvas.transform;

            RectTransform opponents = UiBuilder.Part<RectTransform>(ui, "Adversaires", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            MarketDisplay market = BuildMarket(ui);
            SeatDisplay player = BuildPlayerPanel(ui);
            RoundBanner banner = BuildBanner(ui);
            GameLogDisplay log = BuildLog(ui);
            PlaybackControls playback = BuildPlayback(ui);
            CommandPanel commands = BuildCommandPanel(ui);

            var director = new GameObject("Partie", typeof(GameDirector)).GetComponent<GameDirector>();
            director.Assign(
                AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath),
                AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath),
                AssetDatabase.LoadAssetAtPath<CardArtCatalog>(ThemeAssets.CardArtPath),
                AssetDatabase.LoadAssetAtPath<ShipCatalog>(ThemeAssets.ShipsPath),
                AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath),
                AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProjectAssets.ProfilePath),
                AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath).GetComponent<CardDisplay>());
            director.AssignTable(
                AssetDatabase.LoadAssetAtPath<GameObject>(SeatPanelPath).GetComponent<SeatDisplay>(),
                opponents,
                player,
                market,
                banner,
                log,
                playback,
                ships,
                centre,
                camera);
            director.AssignTestMode(commands);
        }

        // Test mode: one button per legal move, in the free space right of the player's ship, above the playback
        // buttons; it grows upwards.
        private static CommandPanel BuildCommandPanel(Transform ui)
        {
            Image root = UiBuilder.Box(UiBuilder.Fixed<Image>(ui, "Coups (mode test)", new Vector2(1f, 0f), new Vector2(-20f, 132f), new Vector2(520f, 100f)), Panel, receivesPointer: true);
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text title = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(root.transform, "Titre", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 20f, FontStyles.Bold, TextAlignmentOptions.Left);
            title.fontSize = 18f;
            GridLayoutGroup grid = UiBuilder.Part<GridLayoutGroup>(root.transform, "Boutons", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            grid.cellSize = new Vector2(162f, 38f);
            grid.spacing = new Vector2(8f, 5f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;

            (Button template, TMP_Text label) = UiBuilder.Button(root.transform, "Modèle de bouton", Vector2.zero, Vector2.zero, new Vector2(162f, 38f));
            label.fontSize = 15f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 15f;
            // Inactive, so the layout skips it; its active clones are laid out in the grid.
            template.gameObject.SetActive(false);

            CommandPanel panel = root.gameObject.AddComponent<CommandPanel>();
            panel.Assign(title, (RectTransform)grid.transform, template);
            return panel;
        }

        // The two black markets in the middle: attack on the left, defense on the right (INTERFACE.md 3.3).
        private static MarketDisplay BuildMarket(Transform ui)
        {
            // 1180 wide: the panels of the opponents at the ends of the arc stay clear of it.
            Image root = UiBuilder.Box(UiBuilder.Fixed<Image>(ui, "Marché noir", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1180f, 210f)), new Color(0f, 0f, 0f, 0.35f));
            (RectTransform attackRow, TMP_Text attackLabel, TMP_Text attackDeck) = MarketHalf(root.transform, "ATK", 0f, 0.5f);
            (RectTransform defenseRow, TMP_Text defenseLabel, TMP_Text defenseDeck) = MarketHalf(root.transform, "DEF", 0.5f, 1f);
            MarketDisplay market = root.gameObject.AddComponent<MarketDisplay>();
            market.Assign(attackRow, defenseRow, attackLabel, defenseLabel, attackDeck, defenseDeck, 0.38f);
            return market;
        }

        private static (RectTransform Row, TMP_Text Label, TMP_Text Deck) MarketHalf(Transform parent, string name, float from, float to)
        {
            RectTransform half = UiBuilder.Part<RectTransform>(parent, name, new Vector2(from, 0f), new Vector2(to, 1f), Vector2.zero, Vector2.zero);
            TMP_Text label = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(half, "Titre", new Vector2(0f, 1f), new Vector2(16f, -6f), new Vector2(200f, 28f)), 22f, FontStyles.Bold, TextAlignmentOptions.Left);
            TMP_Text deck = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(half, "Pioche", Vector2.one, new Vector2(-16f, -8f), new Vector2(200f, 24f)), 16f, FontStyles.Normal, TextAlignmentOptions.Right);
            deck.color = Muted;
            HorizontalLayoutGroup row = UiBuilder.Part<HorizontalLayoutGroup>(half, "Cartes", Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -38f));
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return ((RectTransform)row.transform, label, deck);
        }

        // The player's own seat at the bottom: modifiers on each side of the ship, figures under it (INTERFACE.md 3.2).
        private static SeatDisplay BuildPlayerPanel(Transform ui)
        {
            RectTransform root = UiBuilder.Fixed<RectTransform>(ui, "Joueur", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1100f, 320f));
            root.gameObject.AddComponent<CanvasGroup>();
            RectTransform attack = UiBuilder.Fixed<RectTransform>(root, "ATK", new Vector2(0.5f, 0f), new Vector2(-340f, 20f), new Vector2(150f, 210f));
            RectTransform defense = UiBuilder.Fixed<RectTransform>(root, "DEF", new Vector2(0.5f, 0f), new Vector2(340f, 20f), new Vector2(150f, 210f));

            Image highlight = UiBuilder.Box(UiBuilder.Fixed<Image>(root, "Cadre", new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(408f, 118f)), Color.white);
            Image stats = UiBuilder.Box(UiBuilder.Fixed<Image>(root, "Chiffres", new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(400f, 110f)), SeatBackground);
            TMP_Text name = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(stats.transform, "Nom", new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(380f, 26f)), 20f, FontStyles.Bold, TextAlignmentOptions.Center);
            TMP_Text hp = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(stats.transform, "PV", new Vector2(0.5f, 1f), new Vector2(-95f, -34f), new Vector2(180f, 32f)), 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            TMP_Text shield = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(stats.transform, "Bouclier", new Vector2(0.5f, 1f), new Vector2(95f, -34f), new Vector2(180f, 32f)), 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            Image[] rounds = Enumerable.Range(0, 4)
                .Select(i => Disc(stats.transform, "Technologie " + (i + 1), new Vector2(0.5f, 1f), new Vector2(-54f + (i * 30f), -74f), 24f))
                .ToArray();
            Image overcharge = Disc(stats.transform, "Surcharge", new Vector2(0.5f, 1f), new Vector2(90f, -74f), 24f);
            TMP_Text statuses = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root, "Effets", new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(560f, 28f)), 16f, FontStyles.Normal, TextAlignmentOptions.Center);
            statuses.color = Muted;
            GameObject leader = Tag(root, new Vector2(0.5f, 0f), new Vector2(0f, 160f));

            SeatDisplay seat = root.gameObject.AddComponent<SeatDisplay>();
            seat.Assign(name, hp, shield, statuses, overcharge, rounds, attack, defense, highlight, leader, root.GetComponent<CanvasGroup>());
            return seat;
        }

        // Round, event, doom countdown and result, at the top centre (INTERFACE.md 3.8).
        private static RoundBanner BuildBanner(Transform ui)
        {
            Image root = UiBuilder.Box(UiBuilder.Fixed<Image>(ui, "Bandeau", new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(560f, 112f)), Panel);
            TMP_Text round = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root.transform, "Manche", new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(540f, 36f)), 30f, FontStyles.Bold, TextAlignmentOptions.Center);
            TMP_Text roundEvent = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root.transform, "Événement", new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(540f, 30f)), 22f, FontStyles.Normal, TextAlignmentOptions.Center);
            TMP_Text doom = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root.transform, "Fin des temps", new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(540f, 26f)), 18f, FontStyles.Normal, TextAlignmentOptions.Center);
            doom.color = Muted;
            TMP_Text outcome = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root.transform, "Résultat", new Vector2(0.5f, 0f), new Vector2(0f, -56f), new Vector2(1000f, 48f)), 36f, FontStyles.Bold, TextAlignmentOptions.Center);
            outcome.color = new Color32(255, 214, 102, 255);
            RoundBanner banner = root.gameObject.AddComponent<RoundBanner>();
            banner.Assign(round, roundEvent, doom, outcome);
            return banner;
        }

        // The folding game log, bottom left (INTERFACE.md 3.8).
        private static GameLogDisplay BuildLog(Transform ui)
        {
            RectTransform root = UiBuilder.Part<RectTransform>(ui, "Journal", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            (Button toggle, TMP_Text toggleLabel) = UiBuilder.Button(root, "Bouton", Vector2.zero, new Vector2(20f, 20f), new Vector2(160f, 48f));
            Image panel = UiBuilder.Box(UiBuilder.Fixed<Image>(root, "Panneau", Vector2.zero, new Vector2(20f, 76f), new Vector2(500f, 300f)), Panel);
            TMP_Text lines = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(panel.transform, "Lignes", Vector2.zero, Vector2.one, new Vector2(14f, 10f), new Vector2(-14f, -10f)), 15f, FontStyles.Normal, TextAlignmentOptions.BottomLeft);
            GameLogDisplay log = root.gameObject.AddComponent<GameLogDisplay>();
            log.Assign(panel.gameObject, lines, toggle, toggleLabel);
            return log;
        }

        // Speed and skip, bottom right (INTERFACE.md 3.8); the end turn button will join them (M4.5).
        private static PlaybackControls BuildPlayback(Transform ui)
        {
            RectTransform root = UiBuilder.Part<RectTransform>(ui, "Lecture", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            (Button speed, TMP_Text speedLabel) = UiBuilder.Button(root, "Vitesse", new Vector2(1f, 0f), new Vector2(-20f, 76f), new Vector2(200f, 48f));
            (Button skip, TMP_Text skipLabel) = UiBuilder.Button(root, "Passer", new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(200f, 48f));
            PlaybackControls playback = root.gameObject.AddComponent<PlaybackControls>();
            playback.Assign(speed, speedLabel, skip, skipLabel);
            return playback;
        }

        private static Image Disc(Transform parent, string name, Vector2 anchor, Vector2 position, float size)
        {
            Image disc = UiBuilder.Fixed<Image>(parent, name, anchor, position, new Vector2(size, size));
            disc.sprite = UiBuilder.Disc;
            disc.raycastTarget = false;
            return disc;
        }

        // The "leader" tag of the leader bounty option, hidden until a seat is the sole HP leader.
        private static GameObject Tag(Transform parent, Vector2 anchor, Vector2 position)
        {
            Image tag = UiBuilder.Box(UiBuilder.Fixed<Image>(parent, "Leader", anchor, position, new Vector2(110f, 26f)), new Color32(255, 214, 102, 255));
            TMP_Text label = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(tag.transform, "Texte", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 16f, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = new Color32(40, 30, 10, 255);
            tag.gameObject.SetActive(false);
            return tag.gameObject;
        }
    }
}
