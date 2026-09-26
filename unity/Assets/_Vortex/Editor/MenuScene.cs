using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Menus;
using Vortex.Client.Theme;

namespace Vortex.Editor
{
    /// <summary>
    /// Creates the menu scene when it is missing (through <see cref="ProjectAssets.EnsureAll"/>), laid out as in
    /// INTERFACE.md section 5: the home screen, the local game menu with its development menu, and the options. The game
    /// opens on it. Once created, the layout belongs to the designer.
    /// </summary>
    public static class MenuScene
    {
        /// <summary>Path of the scene.</summary>
        public const string ScenePath = "Assets/_Vortex/Scenes/Menu.unity";

        /// <summary>Seats offered by the local game menu (the engine supports 2 to 5).</summary>
        private const int Seats = 5;

        /// <summary>Creates the scene if it does not exist.</summary>
        public static void Ensure()
        {
            if (!File.Exists(ScenePath))
            {
                SceneFiles.Create(ScenePath, Build);
            }
        }

        private static void Build()
        {
            var camera = new GameObject("Camera", typeof(Camera)).GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            _ = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            Canvas canvas = UiBuilder.Canvas("Menus", RenderMode.ScreenSpaceOverlay, null, 0);
            Transform ui = canvas.transform;

            // Home (docs/DIRECTION_ARTISTIQUE.md 6.5): a wall of old posters in the dark, and in front, the one that matters:
            // a band of blood with the title, the pitch, then the choices.
            RectTransform home = UiBuilder.Part<RectTransform>(ui, "Accueil", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            OldPoster(home, "Affiche 1", new Vector2(-560f, 60f), new Vector2(520f, 760f), 0.16f);
            OldPoster(home, "Affiche 2", new Vector2(600f, -30f), new Vector2(560f, 820f), 0.12f);
            Image column = MenuBuilder.Window(home, "Choix", new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), 600f);
            column.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(40, 40, 0, 36);
            Image band = UiBuilder.Part<Image>(column.transform, "Bandeau", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            band.color = UiBuilder.Blood;
            band.gameObject.AddComponent<LayoutElement>().preferredHeight = 170f;
            TMP_Text title = UiBuilder.Font(UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(band.transform, "Titre", Vector2.zero, Vector2.one, new Vector2(0f, 10f), Vector2.zero), 120f, FontStyles.Normal, TextAlignmentOptions.Center), MenuBuilder.Theme.TitleFont);
            title.color = UiBuilder.Bone;
            title.characterSpacing = 8f;
            TMP_Text tagline = MenuBuilder.Line(column.transform, "Accroche", 22f);
            tagline.fontStyle = FontStyles.Bold;
            Button localGame = MenuBuilder.Button(column.transform, "Partie locale", 76f, fontSize: 30f);
            Button findGame = MenuBuilder.Button(column.transform, "Trouver une partie", 76f, fontSize: 30f);
            Button options = MenuBuilder.Button(column.transform, "Options", 76f, fontSize: 30f);
            Button social = MenuBuilder.Button(column.transform, "Social", 76f, fontSize: 30f);
            Button quit = MenuBuilder.Button(column.transform, "Quitter", 76f, fontSize: 30f);

            LocalGameMenu local = BuildLocalGame(ui);
            OptionsMenu optionsMenu = MenuBuilder.Options(ui);

            MainMenu menu = new GameObject("Menu", typeof(MainMenu)).GetComponent<MainMenu>();
            menu.Assign(
                AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath),
                AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath),
                AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath),
                camera);
            menu.AssignLayout(home.gameObject, title, localGame, findGame, options, social, quit, local, optionsMenu);
            menu.AssignTagline(tagline);
        }

        // The local game: the number of players, one row per seat, then back, development and start.
        private static LocalGameMenu BuildLocalGame(Transform parent)
        {
            Image window = MenuBuilder.Window(parent, "Partie locale", new Vector2(0.5f, 0.5f), Vector2.zero, 1100f);
            TMP_Text title = MenuBuilder.Title(window.transform, 40f);

            HorizontalLayoutGroup count = MenuBuilder.Row(window.transform, "Nombre de joueurs", 60f);
            Button fewer = MenuBuilder.Button(count.transform, "Moins", 56f, 64f, 30f);
            Symbol(fewer, "−");
            TMP_Text players = MenuBuilder.Inked(UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(count.transform, "Joueurs", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 28f, FontStyles.Bold, TextAlignmentOptions.Center));
            players.gameObject.AddComponent<LayoutElement>().preferredWidth = 240f;
            Button more = MenuBuilder.Button(count.transform, "Plus", 56f, 64f, 30f);
            Symbol(more, "+");

            SeatRow[] rows = Enumerable.Range(1, Seats).Select(n => BuildSeatRow(window.transform, n)).ToArray();

            // Time of a turn (ARB-80), not timed by default.
            HorizontalLayoutGroup timing = MenuBuilder.Row(window.transform, "Temps de tour", 60f);
            Button turnTime = MenuBuilder.Button(timing.transform, "Temps de tour", 56f, 420f);

            HorizontalLayoutGroup actions = MenuBuilder.Row(window.transform, "Actions", 72f);
            actions.padding = new RectOffset(0, 0, 12, 0);
            Button back = MenuBuilder.Button(actions.transform, "Retour", 64f, 220f);
            Button development = MenuBuilder.Button(actions.transform, "Développement", 64f, 260f);
            Button launch = MenuBuilder.Button(actions.transform, "Lancer", 64f, 340f, 28f);
            launch.GetComponent<Image>().color = UiBuilder.Blood;

            // The development menu covers the local game menu, behind a veil.
            Image veil = MenuBuilder.Veil(parent, "Développement");
            Image devWindow = MenuBuilder.Window(veil.transform, "Fenêtre", new Vector2(0.5f, 0.5f), Vector2.zero, 760f);
            TMP_Text devTitle = MenuBuilder.Title(devWindow.transform, 30f);
            Button posture = MenuBuilder.Button(devWindow.transform, "Posture défensive", 60f);
            Button bounty = MenuBuilder.Button(devWindow.transform, "Prime sur le leader", 60f);
            Button ghosts = MenuBuilder.Button(devWindow.transform, "Éliminés et événement", 60f);
            TMP_InputField seed = MenuBuilder.Field(devWindow.transform, "Graine", 400f, 60f);
            Button devBack = MenuBuilder.Button(devWindow.transform, "Retour", 60f);
            DevMenu devMenu = veil.gameObject.AddComponent<DevMenu>();
            devMenu.Assign(devTitle, posture, bounty, ghosts, seed, devBack);
            veil.gameObject.SetActive(false);

            LocalGameMenu menu = window.gameObject.AddComponent<LocalGameMenu>();
            menu.Assign(title, fewer, more, players, rows, back, launch, development, devMenu);
            menu.AssignTurnTime(turnTime);
            window.gameObject.SetActive(false);
            return menu;
        }

        // A seat: its number, who plays it, the bot's level, and a name.
        private static SeatRow BuildSeatRow(Transform parent, int seat)
        {
            HorizontalLayoutGroup row = MenuBuilder.Row(parent, "Siège " + seat, 64f);
            TMP_Text label = MenuBuilder.Inked(UiBuilder.Label(UiBuilder.Part<TextMeshProUGUI>(row.transform, "Numéro", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero), 24f, FontStyles.Bold, TextAlignmentOptions.Left));
            label.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;
            Button kind = MenuBuilder.Button(row.transform, "Humain ou bot", 56f, 180f);
            Button level = MenuBuilder.Button(row.transform, "Niveau", 56f, 180f);

            // Faded out rather than hidden for a person, so the name fields stay aligned.
            level.gameObject.AddComponent<CanvasGroup>();
            TMP_InputField name = MenuBuilder.Field(row.transform, "Nom", 440f, 56f);
            SeatRow seatRow = row.gameObject.AddComponent<SeatRow>();
            seatRow.Assign(label, kind, level, name);
            return seatRow;
        }

        // An old poster on the wall behind the home menu: faded paper with a band of blood, nothing to read.
        private static void OldPoster(Transform parent, string name, Vector2 position, Vector2 size, float strength)
        {
            Image poster = UiBuilder.Paper(UiBuilder.Fixed<Image>(parent, name, new Vector2(0.5f, 0.5f), position, size));
            poster.color = new Color(poster.color.r * strength, poster.color.g * strength, poster.color.b * strength, 1f);
            Image band = UiBuilder.Part<Image>(poster.transform, "Bandeau", new Vector2(0f, 0.72f), new Vector2(1f, 0.9f), new Vector2(18f, 0f), new Vector2(-18f, 0f));
            band.color = new Color(UiBuilder.Blood.r * strength * 2f, UiBuilder.Blood.g * strength * 2f, UiBuilder.Blood.b * strength * 2f, 1f);
            band.raycastTarget = false;
        }

        // A symbol drawn on a button (not a text of the interface: it never needs translating).
        private static void Symbol(Button button, string symbol)
        {
            button.GetComponentInChildren<TMP_Text>().text = symbol;
        }
    }
}
