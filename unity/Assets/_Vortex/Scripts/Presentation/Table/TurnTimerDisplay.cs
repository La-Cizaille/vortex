using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Theme;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Shows the time left to the person who has to act (INTERFACE.md 3.9, ARB-80): a bar that empties above the end turn
    /// button and the seconds left. In the last seconds it turns to the warning colour and ticks each second (the theme's
    /// sound, or a generated one while there is none). Hidden when turns are not timed or nobody has to act.
    /// </summary>
    public sealed class TurnTimerDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject root = null!;
        [SerializeField] private Image fill = null!;
        [SerializeField] private TMP_Text time = null!;
        [SerializeField] private AudioSource? sound;
        [Tooltip("Dernières secondes signalées (couleur d'alerte et tic chaque seconde).")]
        [SerializeField, Min(0f)] private float warningSeconds = 10f;

        private ThemeSettings? _theme;
        private AudioClip? _tick;
        private int _lastTick = -1;

        /// <summary>True while the timer shows.</summary>
        public bool Shown => root.activeSelf;

        /// <summary>Time shown, e.g. <c>1:05</c> (tests).</summary>
        public string Text => Shown ? time.text : string.Empty;

        /// <summary>True during the last seconds.</summary>
        public bool Warning { get; private set; }

        /// <summary>Ticks played so far (tests).</summary>
        public int Ticks { get; private set; }

        /// <summary>Prepares the display for a game.</summary>
        public void Bind(ThemeSettings theme)
        {
            _theme = theme != null ? theme : throw new ArgumentNullException(nameof(theme));
            _tick = theme.TimerTick != null ? theme.TimerTick : _tick != null ? _tick : PlaceholderSounds.Tick();
            _lastTick = -1;
            Ticks = 0;
            Warning = false;
            root.SetActive(false);
        }

        /// <summary>Shows the count the clock follows, or hides the display when there is none.</summary>
        public void Show(TurnClock clock)
        {
            if (clock is null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            bool visible = clock.Enabled && clock.Counting;
            root.SetActive(visible);
            if (!visible || _theme is null)
            {
                _lastTick = -1;
                Warning = false;
                return;
            }

            float left = clock.Remaining;
            int seconds = Mathf.CeilToInt(left);
            fill.fillAmount = clock.Limit > 0f ? Mathf.Clamp01(left / clock.Limit) : 0f;
            time.richText = false;
            time.text = string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", seconds / 60, seconds % 60);
            Warning = left <= warningSeconds;
            Color colour = Warning ? _theme.Loss : _theme.Highlight;
            fill.color = colour;
            time.color = Warning ? _theme.Loss : _theme.Text;

            // One tick for each of the last seconds, the moment it starts.
            if (Warning && seconds > 0 && seconds != _lastTick)
            {
                _lastTick = seconds;
                Ticks++;
                if (sound != null && _tick != null)
                {
                    sound.PlayOneShot(_tick);
                }
            }
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(GameObject timerRoot, Image bar, TMP_Text label, AudioSource? audioSource)
        {
            root = timerRoot;
            fill = bar;
            time = label;
            sound = audioSource;
        }
    }
}
