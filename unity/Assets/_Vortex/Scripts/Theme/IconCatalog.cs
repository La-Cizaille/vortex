using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// Icons by name (docs/ASSETS.md §3: <c>Action_Attaque</c>, <c>Tech_BLUE</c>…). Filled automatically from the images
    /// dropped in <c>Assets/_Vortex/Art/Icons/</c>; a name without an image returns null, and the view shows a short text
    /// instead, so a missing icon never blocks the game.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Habillage/Icônes", fileName = "IconCatalog")]
    public sealed class IconCatalog : ScriptableObject
    {
        [SerializeField] private List<Entry> entries = new List<Entry>();

        /// <summary>The icons filled in, sorted by name.</summary>
        public IReadOnlyList<Entry> Entries => entries;

        /// <summary>The icon of a name, or null when it has none.</summary>
        public Sprite? Find(string name)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Sprite != null && string.Equals(entry.Name, name, StringComparison.Ordinal))
                {
                    return entry.Sprite;
                }
            }

            return null;
        }

        /// <summary>Replaces the icons (import rules and tests), sorted by name. Returns false when nothing changed.</summary>
        public bool Replace(IEnumerable<(string Name, Sprite Sprite)> icons)
        {
            List<Entry> next = icons
                .OrderBy(i => i.Name, StringComparer.Ordinal)
                .Select(i => new Entry { Name = i.Name, Sprite = i.Sprite })
                .ToList();
            bool same = next.Count == entries.Count
                && next.Zip(entries, (a, b) => a.Name == b.Name && a.Sprite == b.Sprite).All(equal => equal);
            if (same)
            {
                return false;
            }

            entries = next;
            return true;
        }

        /// <summary>One icon.</summary>
        [Serializable]
        public sealed class Entry
        {
            /// <summary>File name without extension, e.g. <c>Action_Attaque</c>.</summary>
            public string Name = string.Empty;

            /// <summary>The icon.</summary>
            public Sprite? Sprite;
        }
    }
}
