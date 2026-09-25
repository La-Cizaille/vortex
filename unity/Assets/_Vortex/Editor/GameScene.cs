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
using Vortex.Core.Commands;

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

            if (File.Exists(ScenePath))
            {
                ListInBuild();
            }
        }

        // The game scene first in the build, under its current id (a regenerated scene gets a new one); scenes that no
        // longer exist are dropped from the list.
        private static void ListInBuild()
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            EditorBuildSettingsScene[] wanted = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(current.Where(s => s.path != ScenePath && File.Exists(s.path)))
                .ToArray();
            bool same = wanted.Length == current.Length
                && wanted.Zip(current, (a, b) => a.path == b.path && a.guid == b.guid && a.enabled == b.enabled).All(equal => equal);
            if (!same)
            {
                EditorBuildSettings.scenes = wanted;
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
            UiBuilder.Box(UiBuilder.Part<Image>(root.transform, "Fond", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), SeatBackground, receivesPointer: true);
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

            root.GetComponent<SeatDisplay>().Assign(name, hp, shield, statuses, overcharge, rounds, attack, defense, highlight, leader, root.GetComponent<CanvasGroup>(), growOnHover: 1.3f);
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

            // Pointer events on 3D objects (cards), next to those of the interface.
            camera.gameObject.AddComponent<PhysicsRaycaster>();
            var light = new GameObject("Lumière", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(55f, -30f, 0f);
            Transform ships = new GameObject("Vaisseaux").transform;
            Transform centre = new GameObject("Centre de la table").transform;
            Transform cards = new GameObject("Cartes 3D").transform;
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Interface, reference resolution 1920 x 1080 (INTERFACE.md 2), drawn by the camera at 10 units: the 3D cards,
            // nearer (ADR-0017), show in front of its panels. The foreground layer, drawn last, stays above the cards.
            Canvas canvas = UiBuilder.Canvas("Interface", RenderMode.ScreenSpaceCamera, camera, 0);
            canvas.planeDistance = 10f;
            Transform ui = canvas.transform;
            Canvas front = UiBuilder.Canvas("Premier plan", RenderMode.ScreenSpaceOverlay, null, 10);
            RectTransform foreground = UiBuilder.Part<RectTransform>(front.transform, "Centre", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            RectTransform opponents = UiBuilder.Part<RectTransform>(ui, "Adversaires", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            (MarketDisplay market, Button recycleAttack, Button recycleDefense, Button endMarket) = BuildMarket(ui);
            (SeatDisplay player, ActionButton[] actions, Button combo, Button overcharge, Graphic overchargeGlow) = BuildPlayerPanel(ui);
            RoundBanner banner = BuildBanner(ui);
            GameLogDisplay log = BuildLog(ui);
            PlaybackControls playback = BuildPlayback(ui);
            (Button endTurn, TMP_Text endTurnLabel) = UiBuilder.Button(ui, "Fin de tour", new Vector2(1f, 0f), new Vector2(-240f, 20f), new Vector2(220f, 104f));
            endTurnLabel.fontSize = 26f;
            endTurnLabel.fontStyle = FontStyles.Bold;
            CommandPanel commands = BuildChoicePanel(ui, "Coups (mode test)", new Vector2(1f, 0f), new Vector2(-20f, 132f), 520f, 3);

            // Foreground: the decision window, the help bubble and the aim line stay above the 3D cards.
            CommandPanel decision = BuildChoicePanel(front.transform, "Décision", new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), 720f, 3);
            HelpBubble help = BuildHelp(front.transform);
            Image aimLine = UiBuilder.Fixed<Image>(front.transform, "Visée", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 8f));
            aimLine.rectTransform.pivot = new Vector2(0f, 0.5f);
            aimLine.color = new Color32(255, 214, 102, 220);
            aimLine.raycastTarget = false;
            aimLine.gameObject.SetActive(false);

            var controls = new GameObject("Commandes", typeof(PlayerControls)).GetComponent<PlayerControls>();
            controls.Assign(
                actions,
                combo,
                combo.GetComponent<Image>(),
                combo.GetComponentInChildren<TMP_Text>(),
                endTurn,
                endTurn.GetComponent<Image>(),
                endTurnLabel,
                endMarket,
                endMarket.GetComponentInChildren<TMP_Text>(),
                recycleAttack,
                recycleDefense,
                overcharge,
                overchargeGlow,
                decision,
                help,
                aimLine.rectTransform,
                (RectTransform)player.transform,
                (RectTransform)market.transform);

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
            director.AssignControls(controls, AssetDatabase.LoadAssetAtPath<IconCatalog>(ThemeAssets.IconsPath));

            CardZoom zoom = new GameObject("Zoom", typeof(CardZoom)).GetComponent<CardZoom>();
            director.AssignCards(cards, zoom, foreground);
        }

        // A window of choices: a title and one button per choice, three per row; it grows upwards from its anchor. The
        // test-mode panel (every allowed move) and the decision window are built with it.
        private static CommandPanel BuildChoicePanel(Transform parent, string name, Vector2 anchor, Vector2 position, float width, int columns)
        {
            Image root = UiBuilder.Box(UiBuilder.Fixed<Image>(parent, name, anchor, position, new Vector2(width, 100f)), Panel, receivesPointer: true);
            root.rectTransform.pivot = new Vector2(anchor.x, 0f);
            VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 10);
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            root.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text title = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(root.transform, "Titre", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 18f, FontStyles.Bold, TextAlignmentOptions.Left);
            GridLayoutGroup grid = UiBuilder.Part<GridLayoutGroup>(root.transform, "Boutons", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            float cell = (width - 20f - ((columns - 1) * 8f)) / columns;
            grid.cellSize = new Vector2(cell, 38f);
            grid.spacing = new Vector2(8f, 5f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            (Button template, TMP_Text label) = UiBuilder.Button(root.transform, "Modèle de bouton", Vector2.zero, Vector2.zero, new Vector2(cell, 38f));
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

        // The help shown next to what the pointer is on: a small panel whose height follows its text.
        private static HelpBubble BuildHelp(Transform parent)
        {
            Image bubble = UiBuilder.Box(UiBuilder.Fixed<Image>(parent, "Aide", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 60f)), new Color(0.02f, 0.03f, 0.07f, 0.95f));
            bubble.rectTransform.pivot = new Vector2(0.5f, 0f);
            VerticalLayoutGroup layout = bubble.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            bubble.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TMP_Text text = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(bubble.transform, "Texte", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 17f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            HelpBubble help = bubble.gameObject.AddComponent<HelpBubble>();
            help.Assign(bubble.rectTransform, text);
            bubble.gameObject.SetActive(false);
            return help;
        }

        // The two black markets in the middle: attack on the left, defense on the right (INTERFACE.md 3.3), with their
        // buttons at their level (ARB-71): "Recycler" in each half's header, "Passer le marché" above the middle.
        private static (MarketDisplay Market, Button RecycleAttack, Button RecycleDefense, Button EndMarket) BuildMarket(Transform ui)
        {
            // 1180 wide: the panels of the opponents at the ends of the arc stay clear of it.
            Image root = UiBuilder.Box(UiBuilder.Fixed<Image>(ui, "Marché noir", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(1180f, 210f)), new Color(0f, 0f, 0f, 0.35f));
            (RectTransform attackRow, TMP_Text attackLabel, TMP_Text attackDeck, Button recycleAttack) = MarketHalf(root.transform, "ATK", 0f, 0.5f);
            (RectTransform defenseRow, TMP_Text defenseLabel, TMP_Text defenseDeck, Button recycleDefense) = MarketHalf(root.transform, "DEF", 0.5f, 1f);
            (Button endMarket, TMP_Text endLabel) = UiBuilder.Button(root.transform, "Passer le marché", new Vector2(0.5f, 1f), new Vector2(0f, 46f), new Vector2(220f, 40f));
            endLabel.fontSize = 18f;
            MarketDisplay market = root.gameObject.AddComponent<MarketDisplay>();
            market.Assign(attackRow, defenseRow, attackLabel, defenseLabel, attackDeck, defenseDeck, 133f);
            return (market, recycleAttack, recycleDefense, endMarket);
        }

        private static (RectTransform Row, TMP_Text Label, TMP_Text Deck, Button Recycle) MarketHalf(Transform parent, string name, float from, float to)
        {
            RectTransform half = UiBuilder.Part<RectTransform>(parent, name, new Vector2(from, 0f), new Vector2(to, 1f), Vector2.zero, Vector2.zero);
            TMP_Text label = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(half, "Titre", new Vector2(0f, 1f), new Vector2(16f, -6f), new Vector2(200f, 28f)), 22f, FontStyles.Bold, TextAlignmentOptions.Left);
            TMP_Text deck = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(half, "Pioche", Vector2.one, new Vector2(-16f, -8f), new Vector2(200f, 24f)), 16f, FontStyles.Normal, TextAlignmentOptions.Right);
            deck.color = Muted;
            (Button recycle, TMP_Text recycleLabel) = UiBuilder.Button(half, "Recycler", new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(140f, 30f));
            recycleLabel.fontSize = 16f;
            HorizontalLayoutGroup row = UiBuilder.Part<HorizontalLayoutGroup>(half, "Cartes", Vector2.zero, Vector2.one, new Vector2(10f, 8f), new Vector2(-10f, -38f));
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return ((RectTransform)row.transform, label, deck, recycle);
        }

        // The player's own seat at the bottom: modifiers on each side of the ship, figures under it (INTERFACE.md 3.2), the
        // crew actions in a half-circle above the ship (3.4), the combo above the attack card, the overcharge token
        // armable by a tap (ARB-67).
        private static (SeatDisplay Seat, ActionButton[] Actions, Button Combo, Button Overcharge, Graphic OverchargeGlow) BuildPlayerPanel(Transform ui)
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
            Image glow = Disc(stats.transform, "Surcharge armée", new Vector2(0.5f, 1f), new Vector2(86f, -70f), 32f);
            glow.color = new Color32(255, 214, 102, 255);
            glow.enabled = false;
            Image overcharge = Disc(stats.transform, "Surcharge", new Vector2(0.5f, 1f), new Vector2(90f, -74f), 24f);
            overcharge.raycastTarget = true;
            Button overchargeButton = overcharge.gameObject.AddComponent<Button>();
            TMP_Text statuses = UiBuilder.Label(UiBuilder.Fixed<TextMeshProUGUI>(root, "Effets", new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(560f, 28f)), 16f, FontStyles.Normal, TextAlignmentOptions.Center);
            statuses.color = Muted;
            GameObject leader = Tag(root, new Vector2(0.5f, 0f), new Vector2(0f, 160f));

            (Button combo, TMP_Text comboLabel) = UiBuilder.Button(root, "Combo", new Vector2(0.5f, 0f), new Vector2(-340f, 244f), new Vector2(150f, 40f));
            comboLabel.fontStyle = FontStyles.Bold;

            // The ship stands a quarter of the screen up (GameDirector), that is 270 units above the panel's bottom.
            var ship = new Vector2(0f, 270f);
            CrewAction[] order = { CrewAction.Attack, CrewAction.Sabotage, CrewAction.RerollShield, CrewAction.Overcharge, CrewAction.DefensivePosture };
            float[] angles = { 160f, 125f, 90f, 55f, 20f };
            var actions = new ActionButton[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                float angle = angles[i] * Mathf.Deg2Rad;
                Vector2 place = ship + (new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 125f);
                actions[i] = BuildAction(root, order[i], place);
            }

            SeatDisplay seat = root.gameObject.AddComponent<SeatDisplay>();
            seat.Assign(name, hp, shield, statuses, overcharge, rounds, attack, defense, highlight, leader, root.GetComponent<CanvasGroup>());
            return (seat, actions, combo, overchargeButton, glow);
        }

        // One action: a disc with the pictogram, or its short name while the icon is missing.
        private static ActionButton BuildAction(Transform parent, CrewAction action, Vector2 centre)
        {
            Image disc = UiBuilder.Fixed<Image>(parent, "Action " + action, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(56f, 56f));
            disc.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            disc.rectTransform.anchoredPosition = centre;
            disc.sprite = UiBuilder.Disc;
            disc.color = new Color(0.1f, 0.12f, 0.2f, 0.95f);
            disc.raycastTarget = true;
            CanvasGroup group = disc.gameObject.AddComponent<CanvasGroup>();
            Image icon = UiBuilder.Part<Image>(disc.transform, "Pictogramme", Vector2.zero, Vector2.one, new Vector2(8f, 8f), new Vector2(-8f, -8f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            TMP_Text shortName = UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(disc.transform, "Nom court", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 16f, FontStyles.Bold, TextAlignmentOptions.Center);
            ActionButton button = disc.gameObject.AddComponent<ActionButton>();
            button.Assign(action, icon, shortName, group);
            return button;
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
