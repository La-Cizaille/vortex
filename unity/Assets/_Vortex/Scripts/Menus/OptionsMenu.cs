using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;

namespace Vortex.Client.Menus
{
    /// <summary>
    /// The options (INTERFACE.md 5): the default animation speed, the narrator's comments (ARB-95), and under Windows the
    /// full screen and the resolution.
    /// Each change is kept at once on this device. Opened from the home menu and from the pause menu.
    /// </summary>
    public sealed class OptionsMenu : MonoBehaviour
    {
        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private Button speed = null!;
        [SerializeField] private Button commentary = null!;
        [SerializeField] private Button fullScreen = null!;
        [SerializeField] private Button resolution = null!;
        [SerializeField] private Button back = null!;

        private TextTable? _texts;
        private Action<UserOptions>? _closed;
        private float _defaultSpeed = 1f;

        /// <summary>The options shown, or null before <see cref="Open"/>.</summary>
        public UserOptions? Options { get; private set; }

        /// <summary>Connects the menu to the interface texts; <paramref name="defaultSpeed"/> applies when no option is kept yet.</summary>
        public void Bind(TextTable texts, float defaultSpeed)
        {
            _texts = texts != null ? texts : throw new ArgumentNullException(nameof(texts));
            _defaultSpeed = defaultSpeed;
            title.richText = false;
            title.text = texts.Get(TextKeys.OptionsTitle);
            MenuButtons.Label(back, texts.Get(TextKeys.MenuBack));
            MenuButtons.Wire(speed, NextSpeed);
            MenuButtons.Wire(commentary, ToggleCommentary);
            MenuButtons.Wire(fullScreen, ToggleFullScreen);
            MenuButtons.Wire(resolution, NextResolution);
            MenuButtons.Wire(back, Close);
            fullScreen.gameObject.SetActive(UserOptions.ScreenOptionsAvailable);
            resolution.gameObject.SetActive(UserOptions.ScreenOptionsAvailable);
            gameObject.SetActive(false);
        }

        /// <summary>Shows the options kept on this device; <paramref name="closed"/> receives them when the menu closes.</summary>
        public void Open(Action<UserOptions>? closed)
        {
            Options = UserOptions.Load(_defaultSpeed);
            _closed = closed;
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Closes the menu.</summary>
        public void Close()
        {
            gameObject.SetActive(false);
            if (Options != null)
            {
                _closed?.Invoke(Options);
            }
        }

        /// <summary>The next animation speed.</summary>
        public void NextSpeed()
        {
            if (Options is null)
            {
                return;
            }

            Options.Speed = UserOptions.NextSpeed(Options.Speed);
            Options.Save();
            Refresh();
        }

        /// <summary>The narrator's comments on or off.</summary>
        public void ToggleCommentary()
        {
            if (Options is null)
            {
                return;
            }

            Options.Commentary = !Options.Commentary;
            Options.Save();
            Refresh();
        }

        /// <summary>Full screen on or off (Windows).</summary>
        public void ToggleFullScreen()
        {
            if (Options is null)
            {
                return;
            }

            Options.FullScreen = !Options.FullScreen;
            Options.Save();
            Options.ApplyScreen();
            Refresh();
        }

        /// <summary>The next resolution the screen supports (Windows).</summary>
        public void NextResolution()
        {
            if (Options is null)
            {
                return;
            }

            Vector2Int[] sizes = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Distinct().OrderBy(s => s.x).ThenBy(s => s.y).ToArray();
            if (sizes.Length == 0)
            {
                return;
            }

            int current = Array.IndexOf(sizes, CurrentSize());
            Vector2Int next = sizes[(current + 1) % sizes.Length];
            Options.Width = next.x;
            Options.Height = next.y;
            Options.Save();
            Options.ApplyScreen();
            Refresh();
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(TMP_Text titleLabel, Button speedButton, Button commentaryButton, Button fullScreenButton, Button resolutionButton, Button backButton)
        {
            title = titleLabel;
            speed = speedButton;
            commentary = commentaryButton;
            fullScreen = fullScreenButton;
            resolution = resolutionButton;
            back = backButton;
        }

        private Vector2Int CurrentSize() => Options != null && Options.Width > 0
            ? new Vector2Int(Options.Width, Options.Height)
            : new Vector2Int(Screen.width, Screen.height);

        private void Refresh()
        {
            if (Options is null || _texts is null)
            {
                return;
            }

            MenuButtons.Label(speed, string.Format(CultureInfo.InvariantCulture, _texts.Get(TextKeys.OptionsSpeed), Options.Speed));
            MenuButtons.Label(commentary, string.Format(CultureInfo.InvariantCulture, _texts.Get(TextKeys.OptionsCommentary), _texts.Get(Options.Commentary ? TextKeys.Yes : TextKeys.No)));
            MenuButtons.Label(fullScreen, string.Format(CultureInfo.InvariantCulture, _texts.Get(TextKeys.OptionsFullScreen), _texts.Get(Options.FullScreen ? TextKeys.Yes : TextKeys.No)));
            Vector2Int size = CurrentSize();
            MenuButtons.Label(resolution, string.Format(CultureInfo.InvariantCulture, _texts.Get(TextKeys.OptionsResolution), size.x, size.y));
        }
    }
}
