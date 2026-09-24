using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Speed and skip buttons of the event playback (INTERFACE.md 3.8). The speed button cycles through the speeds;
    /// skip shows every queued event at once.
    /// </summary>
    public sealed class PlaybackControls : MonoBehaviour
    {
        [SerializeField] private Button speed = null!;
        [SerializeField] private TMP_Text speedLabel = null!;
        [SerializeField] private Button skip = null!;
        [SerializeField] private TMP_Text skipLabel = null!;
        [SerializeField] private float[] speeds = { 1f, 2f, 4f };

        private EventPlayer? _player;
        private TableContext? _context;

        /// <summary>Connects the buttons to a game's event player.</summary>
        public void Bind(TableContext context, EventPlayer player)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            skipLabel.text = context.Texts.Get(TextKeys.PlaybackSkip);
            speed.onClick.RemoveListener(NextSpeed);
            speed.onClick.AddListener(NextSpeed);
            skip.onClick.RemoveListener(Skip);
            skip.onClick.AddListener(Skip);
            ShowSpeed();
        }

        /// <summary>Moves to the next speed of the cycle.</summary>
        public void NextSpeed()
        {
            if (_player is null || speeds.Length == 0)
            {
                return;
            }

            int current = Array.FindIndex(speeds, s => Mathf.Approximately(s, _player.Speed));
            _player.Speed = speeds[(current + 1) % speeds.Length];
            ShowSpeed();
        }

        /// <summary>Plays every queued event at once.</summary>
        public void Skip() => _player?.SkipAll();

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(Button speedButton, TMP_Text speedText, Button skipButton, TMP_Text skipText)
        {
            speed = speedButton;
            speedLabel = speedText;
            skip = skipButton;
            skipLabel = skipText;
        }

        private void ShowSpeed()
        {
            if (_player != null && _context != null)
            {
                speedLabel.text = string.Format(CultureInfo.InvariantCulture, _context.Texts.Get(TextKeys.PlaybackSpeed), _player.Speed);
            }
        }
    }
}
