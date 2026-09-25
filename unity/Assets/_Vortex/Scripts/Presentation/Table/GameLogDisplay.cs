using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The game log (INTERFACE.md 3.8): a folding panel, closed by default, that can be scrolled back through the game
    /// (playtest 2). It follows the latest line, unless the player has scrolled up to read. Lines hold player names as
    /// typed, so they are shown as plain text.
    /// </summary>
    public sealed class GameLogDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject panel = null!;
        [SerializeField] private TMP_Text lines = null!;
        [SerializeField] private Button toggle = null!;
        [SerializeField] private TMP_Text toggleLabel = null!;
        [SerializeField] private ScrollRect? scroll;
        [Tooltip("Nombre de lignes gardées : au-delà, les plus anciennes disparaissent.")]
        [SerializeField, Min(1)] private int maxLines = 300;

        private readonly Queue<string> _lines = new Queue<string>();
        private bool _dirty;

        /// <summary>Lines kept, oldest first (tests).</summary>
        public IReadOnlyCollection<string> Lines => _lines;

        /// <summary>Text shown in the panel (tests).</summary>
        public string Text => lines.text;

        /// <summary>True while the log shows its latest line (it then follows the new ones).</summary>
        public bool AtEnd => scroll == null || scroll.verticalNormalizedPosition <= 0.001f || !scroll.content || scroll.content.rect.height <= scroll.viewport.rect.height;

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
            _dirty = false;
        }

        /// <summary>Adds a line, dropping the oldest beyond the limit. The text is rebuilt once per frame at most.</summary>
        public void Add(string line)
        {
            _lines.Enqueue(line ?? throw new ArgumentNullException(nameof(line)));
            while (_lines.Count > maxLines)
            {
                _lines.Dequeue();
            }

            _dirty = true;
        }

        /// <summary>Opens or closes the panel; it opens on the latest line.</summary>
        public void Toggle()
        {
            panel.SetActive(!panel.activeSelf);
            if (panel.activeSelf)
            {
                Refresh(followEnd: true);
            }
        }

        /// <summary>Writes the lines added since the last refresh (the frame loop, or tests).</summary>
        public void Refresh() => Refresh(followEnd: AtEnd);

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(GameObject logPanel, TMP_Text logLines, Button toggleButton, TMP_Text toggleText, ScrollRect? scrollView = null)
        {
            panel = logPanel;
            lines = logLines;
            toggle = toggleButton;
            toggleLabel = toggleText;
            scroll = scrollView;
        }

        private void LateUpdate()
        {
            if (_dirty && panel.activeSelf)
            {
                Refresh();
            }
        }

        private void Refresh(bool followEnd)
        {
            if (_dirty || lines.text.Length == 0)
            {
                lines.text = string.Join("\n", _lines);
                _dirty = false;
            }

            if (scroll != null && followEnd)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                scroll.verticalNormalizedPosition = 0f;
            }
        }
    }
}
