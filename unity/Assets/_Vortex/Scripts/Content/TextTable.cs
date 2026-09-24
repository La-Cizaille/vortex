using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Content
{
    /// <summary>
    /// Texts of the interface, by key (ADR-0015): French only for now, but no interface text is written in the code,
    /// so the game can be translated one day without touching the views. Keys are listed in <see cref="TextKeys"/>.
    /// Card names and texts come from the content files, not from this table.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Textes de l'interface", fileName = "TextTable")]
    public sealed class TextTable : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, string>? _index;

        /// <summary>The texts, in the order of the asset.</summary>
        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>The text of a key; "#key" when the table has none, so that a gap shows on screen instead of failing.</summary>
        public string Get(string key) => Index().TryGetValue(key, out string? text) ? text : "#" + key;

        /// <summary>True when the table has a text for the key.</summary>
        public bool Contains(string key) => Index().ContainsKey(key);

        /// <summary>
        /// Adds the keys the table lacks, with their default text (editor setup). An existing text is never changed:
        /// once written, the texts belong to the designer. Returns false when nothing was added.
        /// </summary>
        public bool AddMissing(IEnumerable<KeyValuePair<string, string>> defaults)
        {
            if (defaults is null)
            {
                throw new ArgumentNullException(nameof(defaults));
            }

            bool added = false;
            foreach (KeyValuePair<string, string> text in defaults)
            {
                if (!Contains(text.Key))
                {
                    entries.Add(new Entry { Key = text.Key, Text = text.Value });
                    _index = null;
                    added = true;
                }
            }

            return added;
        }

        private Dictionary<string, string> Index()
        {
            if (_index is null)
            {
                _index = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (Entry entry in entries)
                {
                    _index[entry.Key] = entry.Text;
                }
            }

            return _index;
        }

        private void OnValidate() => _index = null;

        /// <summary>One text.</summary>
        [Serializable]
        public sealed class Entry
        {
            /// <summary>Key used by the code, e.g. <c>card.usage.durable</c>.</summary>
            public string Key = string.Empty;

            /// <summary>Displayed text.</summary>
            [TextArea] public string Text = string.Empty;
        }
    }
}
