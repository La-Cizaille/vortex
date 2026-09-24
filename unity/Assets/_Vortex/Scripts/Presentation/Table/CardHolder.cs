using UnityEngine;
using Vortex.Core.Projection;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A place of the interface that shows one card or nothing. The card is a 3D object (ADR-0017), created from the
    /// prefab when first needed, that follows the place; it is redrawn only when the card changes.
    /// </summary>
    public sealed class CardHolder
    {
        private readonly RectTransform _slot;
        private readonly RectTransform? _clip;
        private CardDisplay? _card;
        private int _uid = -1;

        /// <summary>Creates a holder over a place of the interface; outside <paramref name="clip"/>, its card is hidden.</summary>
        public CardHolder(RectTransform slot, RectTransform? clip = null)
        {
            _slot = slot;
            _clip = clip;
        }

        /// <summary>The card shown, or null.</summary>
        public CardDisplay? Card => _card != null && _card.gameObject.activeSelf ? _card : null;

        /// <summary>Shows a card of the table (null: empty place).</summary>
        public void Show(CardView? card, TableContext context, bool redraw = false)
        {
            if (card is null)
            {
                Clear();
                return;
            }

            CardDisplay shown = Ensure(context);
            if (redraw || card.Uid != _uid)
            {
                shown.Show(context.Face(card.CardId), context.Theme, context.Art);
                _uid = card.Uid;
            }

            shown.ShowTorments(card.Torments);
        }

        /// <summary>Shows a card face that is not on the table (gallery).</summary>
        public void ShowFace(CardFace face, TableContext context)
        {
            CardDisplay shown = Ensure(context);
            shown.Show(face, context.Theme, context.Art);
            shown.ShowTorments(0);
            _uid = -1;
        }

        /// <summary>Empties the place.</summary>
        public void Clear()
        {
            if (_card != null)
            {
                _card.gameObject.SetActive(false);
            }

            _uid = -1;
        }

        /// <summary>Destroys the card (when the place itself goes away).</summary>
        public void Release()
        {
            if (_card != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(_card.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(_card.gameObject);
                }
            }

            _card = null;
            _uid = -1;
        }

        private CardDisplay Ensure(TableContext context)
        {
            if (_card == null)
            {
                _card = Object.Instantiate(context.CardPrefab, context.CardRoot, false);
                _card.name = "Carte";
                _card.gameObject.AddComponent<CardAnchor>().Follow(_slot, context.View, context.CardDepth, _clip);
                if (context.Zoom != null)
                {
                    _card.gameObject.AddComponent<CardHover>().Bind(context.Zoom);
                }
            }

            _card.gameObject.SetActive(true);
            return _card;
        }
    }
}
