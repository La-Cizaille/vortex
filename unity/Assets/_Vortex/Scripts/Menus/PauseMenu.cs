using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;

namespace Vortex.Client.Menus
{
    /// <summary>
    /// The pause menu of a game (INTERFACE.md 5): resume, restart, options, quit. While it is open the game waits: no event
    /// plays and no bot moves. A veil covers the table under the menu and its options, so nothing else can be touched.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private Button open = null!;
        [SerializeField] private GameObject veil = null!;
        [SerializeField] private GameObject window = null!;
        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private Button resume = null!;
        [SerializeField] private Button restart = null!;
        [SerializeField] private Button options = null!;
        [SerializeField] private Button quit = null!;
        [SerializeField] private OptionsMenu optionsMenu = null!;

        private Action<bool>? _paused;
        private Action<UserOptions>? _optionsChanged;

        /// <summary>True while the menu (or its options) is open.</summary>
        public bool IsOpen => veil.activeSelf;

        /// <summary>The options opened from the pause menu.</summary>
        public OptionsMenu Options => optionsMenu;

        /// <summary>Connects the menu to a game.</summary>
        /// <param name="texts">Interface texts.</param>
        /// <param name="defaultSpeed">Animation speed when no option is kept yet.</param>
        /// <param name="paused">Pauses (true) or resumes (false) the game.</param>
        /// <param name="restarted">Starts the same game again.</param>
        /// <param name="optionsChanged">Applies the options once changed.</param>
        /// <param name="left">Leaves the game for the home menu.</param>
        public void Bind(TextTable texts, float defaultSpeed, Action<bool> paused, Action restarted, Action<UserOptions> optionsChanged, Action left)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            _paused = paused ?? throw new ArgumentNullException(nameof(paused));
            _optionsChanged = optionsChanged ?? throw new ArgumentNullException(nameof(optionsChanged));
            if (restarted is null || left is null)
            {
                throw new ArgumentNullException(restarted is null ? nameof(restarted) : nameof(left));
            }

            title.richText = false;
            title.text = texts.Get(TextKeys.PauseTitle);
            MenuButtons.Label(open, texts.Get(TextKeys.PauseButton));
            MenuButtons.Label(resume, texts.Get(TextKeys.PauseResume));
            MenuButtons.Label(restart, texts.Get(TextKeys.PauseRestart));
            MenuButtons.Label(options, texts.Get(TextKeys.PauseOptions));
            MenuButtons.Label(quit, texts.Get(TextKeys.PauseQuit));
            MenuButtons.Wire(open, Open);
            MenuButtons.Wire(resume, Close);
            MenuButtons.Wire(restart, () =>
            {
                Hide();
                restarted();
            });
            MenuButtons.Wire(options, OpenOptions);
            MenuButtons.Wire(quit, left);
            optionsMenu.Bind(texts, defaultSpeed);
            Hide();
        }

        /// <summary>Opens the menu: the game waits.</summary>
        public void Open()
        {
            veil.SetActive(true);
            window.SetActive(true);
            _paused?.Invoke(true);
        }

        /// <summary>Closes the menu: the game goes on.</summary>
        public void Close()
        {
            Hide();
            _paused?.Invoke(false);
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(Button openButton, GameObject veilRoot, GameObject windowRoot, TMP_Text titleLabel, Button resumeButton, Button restartButton, Button optionsButton, Button quitButton, OptionsMenu optionsPanel)
        {
            open = openButton;
            veil = veilRoot;
            window = windowRoot;
            title = titleLabel;
            resume = resumeButton;
            restart = restartButton;
            options = optionsButton;
            quit = quitButton;
            optionsMenu = optionsPanel;
        }

        private void OpenOptions()
        {
            window.SetActive(false);
            optionsMenu.Open(changed =>
            {
                _optionsChanged?.Invoke(changed);
                window.SetActive(true);
            });
        }

        private void Hide()
        {
            window.SetActive(false);
            optionsMenu.gameObject.SetActive(false);
            veil.SetActive(false);
        }
    }
}
