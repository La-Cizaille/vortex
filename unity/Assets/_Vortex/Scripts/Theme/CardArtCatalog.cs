using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// Illustration of each card, event and technology, by content id. Filled automatically from the files dropped in
    /// <c>Assets/_Vortex/Art/Cards/</c> (see CONTRIBUTING.md); an id without art gets a generated placeholder, so a
    /// missing picture never blocks the game.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Habillage/Illustrations des cartes", fileName = "CardArtCatalog")]
    public sealed class CardArtCatalog : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        // Generated on demand, never saved; released with the catalog.
        private readonly Dictionary<string, Sprite> _placeholders = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        /// <summary>The illustrations filled in, sorted by id.</summary>
        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>The art of an id, or null when it has none.</summary>
        public Sprite? Find(string id)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Sprite != null && string.Equals(entry.Id, id, StringComparison.Ordinal))
                {
                    return entry.Sprite;
                }
            }

            return null;
        }

        /// <summary>The illustration to show for an id: its art, or its placeholder (created once, then reused).</summary>
        public CardArt ArtFor(string id)
        {
            Sprite? sprite = Find(id);
            if (sprite != null)
            {
                return new CardArt(sprite, false);
            }

            if (!_placeholders.TryGetValue(id, out Sprite? placeholder) || placeholder == null)
            {
                placeholder = PlaceholderArt.Create(id);
                _placeholders[id] = placeholder;
            }

            return new CardArt(placeholder, true);
        }

        /// <summary>
        /// Replaces the illustrations (art import rules and tests), sorted by id so that the asset diffs cleanly.
        /// Returns false when nothing changed.
        /// </summary>
        public bool Replace(IEnumerable<(string Id, Sprite Sprite)> art)
        {
            List<Entry> next = art
                .OrderBy(a => a.Id, StringComparer.Ordinal)
                .Select(a => new Entry { Id = a.Id, Sprite = a.Sprite })
                .ToList();
            bool same = next.Count == entries.Count
                && next.Zip(entries, (a, b) => a.Id == b.Id && a.Sprite == b.Sprite).All(equal => equal);
            if (same)
            {
                return false;
            }

            entries = next;
            return true;
        }

        private void OnDisable()
        {
            foreach (Sprite placeholder in _placeholders.Values)
            {
                PlaceholderArt.Release(placeholder);
            }

            _placeholders.Clear();
        }

        /// <summary>One illustration.</summary>
        [Serializable]
        public sealed class Entry
        {
            /// <summary>Content id, e.g. <c>A_005</c>, <c>EVT_TROU_NOIR</c> or <c>TECH_BLUE</c>.</summary>
            public string Id = string.Empty;

            /// <summary>The illustration.</summary>
            public Sprite? Sprite;
        }
    }
}
