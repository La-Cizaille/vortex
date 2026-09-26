using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Session;
using Vortex.Client.Theme;
using Vortex.Core.Rules;

namespace Vortex.Client.Menus
{
    /// <summary>
    /// The home menu (INTERFACE.md 5, ARB-65): local game, online game and social (not yet available, greyed out),
    /// options, and quit under Windows. It leads to the local game menu and the options.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text? tagline;
        [SerializeField] private GameContent content = null!;
        [SerializeField] private ThemeSettings theme = null!;
        [SerializeField] private TextTable texts = null!;
        [SerializeField] private Camera view = null!;

        [Header("Accueil")]
        [SerializeField] private GameObject home = null!;
        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private Button localGame = null!;
        [SerializeField] private Button findGame = null!;
        [SerializeField] private Button options = null!;
        [SerializeField] private Button social = null!;
        [SerializeField] private Button quit = null!;

        [Header("Sous-menus")]
        [SerializeField] private LocalGameMenu local = null!;
        [SerializeField] private OptionsMenu optionsMenu = null!;

        /// <summary>The local game menu.</summary>
        public LocalGameMenu LocalGame => local;

        /// <summary>The options.</summary>
        public OptionsMenu Options => optionsMenu;

        /// <summary>True while the home screen shows.</summary>
        public bool AtHome => home.activeSelf;

        /// <summary>Prepares every menu and shows the home screen; <paramref name="start"/> starts a chosen game.</summary>
        public void Setup(Action<MatchSetup> start)
        {
            GameEngine engine = content.CreateEngine();
            view.backgroundColor = theme.Background;
            title.richText = false;
            title.text = texts.Get(TextKeys.MenuTitle);
            if (tagline != null)
            {
                tagline.text = texts.Get(TextKeys.MenuTagline);
            }
            MenuButtons.Label(localGame, texts.Get(TextKeys.MenuLocalGame));
            MenuButtons.Label(findGame, texts.Get(TextKeys.MenuFindGame));
            MenuButtons.Label(options, texts.Get(TextKeys.MenuOptions));
            MenuButtons.Label(social, texts.Get(TextKeys.MenuSocial));
            MenuButtons.Label(quit, texts.Get(TextKeys.MenuQuit));
            MenuButtons.Wire(localGame, ShowLocalGame);
            MenuButtons.Wire(options, ShowOptions);
            MenuButtons.Wire(quit, Application.Quit);

            // Coming later (phase 2): shown, but greyed out.
            findGame.interactable = false;
            social.interactable = false;

            // Quitting is up to the system on a phone; under Windows, the menu offers it.
            quit.gameObject.SetActive(!Application.isMobilePlatform);

            local.Bind(texts, engine.SupportedPlayerCounts, RuleOptions.Of(engine.Config), ShowHome, start);
            optionsMenu.Bind(texts, theme.PlaybackSpeed);
            UserOptions.Load(theme.PlaybackSpeed).ApplyScreen();
            ShowHome();
        }

        /// <summary>The home screen.</summary>
        public void ShowHome()
        {
            home.SetActive(true);
            local.gameObject.SetActive(false);
            optionsMenu.gameObject.SetActive(false);
        }

        /// <summary>The local game menu.</summary>
        public void ShowLocalGame()
        {
            home.SetActive(false);
            local.gameObject.SetActive(true);
        }

        /// <summary>The options.</summary>
        public void ShowOptions()
        {
            home.SetActive(false);
            optionsMenu.Open(_ => ShowHome());
        }

        /// <summary>Wires the content and the look (editor setup).</summary>
        public void Assign(GameContent gameContent, ThemeSettings themeSettings, TextTable textTable, Camera sceneCamera)
        {
            content = gameContent;
            theme = themeSettings;
            texts = textTable;
            view = sceneCamera;
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void AssignLayout(GameObject homeRoot, TMP_Text titleLabel, Button localButton, Button findButton, Button optionsButton, Button socialButton, Button quitButton, LocalGameMenu localMenu, OptionsMenu optionsPanel)
        {
            home = homeRoot;
            title = titleLabel;
            localGame = localButton;
            findGame = findButton;
            options = optionsButton;
            social = socialButton;
            quit = quitButton;
            local = localMenu;
            optionsMenu = optionsPanel;
        }

        private void Start() => Setup(MatchLauncher.Launch);

        /// <summary>Wires the tagline under the title (editor setup).</summary>
        public void AssignTagline(TMPro.TMP_Text line) => tagline = line;
    }
}
