using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Core.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>What every display of the table shares: theme, art, interface texts, the card prefab and the card faces.</summary>
    public sealed class TableContext
    {
        private readonly Dictionary<string, CardFace> _faces;

        /// <summary>Creates the context of a game.</summary>
        public TableContext(ThemeSettings theme, CardArtCatalog art, TextTable texts, GameData data, CardDisplay cardPrefab, CardZoom? zoom = null)
        {
            Zoom = zoom;
            Theme = theme != null ? theme : throw new ArgumentNullException(nameof(theme));
            Art = art != null ? art : throw new ArgumentNullException(nameof(art));
            Texts = texts != null ? texts : throw new ArgumentNullException(nameof(texts));
            CardPrefab = cardPrefab != null ? cardPrefab : throw new ArgumentNullException(nameof(cardPrefab));
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _faces = data.Modifiers.Select(c => CardFace.Of(c, texts))
                .Concat(data.Events.Select(e => CardFace.Of(e, texts)))
                .Concat(data.Technologies.Select(t => CardFace.Of(t, texts)))
                .ToDictionary(f => f.Id, StringComparer.Ordinal);
        }

        /// <summary>Colours and fonts.</summary>
        public ThemeSettings Theme { get; }

        /// <summary>Illustrations.</summary>
        public CardArtCatalog Art { get; }

        /// <summary>Interface texts.</summary>
        public TextTable Texts { get; }

        /// <summary>Enlarged card shown when a table card is pointed at, or null (no zoom).</summary>
        public CardZoom? Zoom { get; }

        /// <summary>Layout of a card.</summary>
        public CardDisplay CardPrefab { get; }

        /// <summary>The face of a content id (modifier, event or technology).</summary>
        public CardFace Face(string id) =>
            _faces.TryGetValue(id, out CardFace face) ? face : new CardFace(id, id, string.Empty, string.Empty, TechColor.Neutral);
    }
}
