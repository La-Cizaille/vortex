using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The game log (INTERFACE.md 3.8): a folding panel, closed by default, with the latest lines. Lines hold player
    /// names as typed, so they are shown as plain text.
    /// </summary>
    public sealed class GameLogDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject panel = null!;
        [SerializeField] private TMP_Text lines = null!;
        [SerializeField] private Button toggle = null!;
        [SerializeField] private TMP_Text toggleLabel = null!;
        [SerializeField, Min(1)] private int maxLines = 14;

        private readonly Queue<string> _lines = new Queue<string>();

        /// <summary>Lines kept, oldest first (tests).</summary>
        public IReadOnlyCollection<string> Lines => _lines;

        /// <summary>Prepares the log for a game; it starts closed and empty.</summary>
        public void Bind(TableContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            toggleLabel.text = context.Texts.Get(TextKeys.LogButton);
            toggle.onClick.RemoveListener(Toggle);
            toggle.onClick.AddListener(Toggle);
            lines.richText = false;
            panel.SetActive(false);
            _lines.Clear();
            lines.text = string.Empty;
        }

        /// <summary>Adds a line, dropping the oldest beyond the limit.</summary>
        public void Add(string line)
        {
            _lines.Enqueue(line ?? throw new ArgumentNullException(nameof(line)));
            while (_lines.Count > maxLines)
            {
                _lines.Dequeue();
            }

            lines.text = string.Join("\n", _lines);
        }

        /// <summary>Opens or closes the panel.</summary>
        public void Toggle() => panel.SetActive(!panel.activeSelf);

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(GameObject logPanel, TMP_Text logLines, Button toggleButton, TMP_Text toggleText)
        {
            panel = logPanel;
            lines = logLines;
            toggle = toggleButton;
            toggleLabel = toggleText;
        }
    }
}
