using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>The illustration a card shows: its art, or a generated placeholder while it has none.</summary>
    public readonly struct CardArt
    {
        /// <summary>Creates a result.</summary>
        public CardArt(Sprite sprite, bool isPlaceholder)
        {
            Sprite = sprite;
            IsPlaceholder = isPlaceholder;
        }

        /// <summary>The sprite to show.</summary>
        public Sprite Sprite { get; }

        /// <summary>True for a generated placeholder, which views tint with the card's technology colour.</summary>
        public bool IsPlaceholder { get; }
    }
}
