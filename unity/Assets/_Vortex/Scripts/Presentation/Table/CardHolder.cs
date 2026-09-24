using UnityEngine;
using Vortex.Core.Projection;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A place on the table that shows one card or nothing: it creates its card from the prefab when first needed,
    /// scales it to the slot, and redraws it only when the card changes.
    /// </summary>
    public sealed class CardHolder
    {
        private readonly RectTransform _slot;
        private CardDisplay? _card;
        private int _uid = -1;

        /// <summary>Creates a slot on a rectangle of the interface.</summary>
        public CardHolder(RectTransform slot) => _slot = slot;

        /// <summary>The card shown, or null.</summary>
        public CardDisplay? Card => _card != null && _card.gameObject.activeSelf ? _card : null;

        /// <summary>Shows a card (null: empty slot).</summary>
        public void Show(CardView? card, TableContext context, bool redraw = false)
        {
            if (card is null)
            {
                if (_card != null)
                {
                    _card.gameObject.SetActive(false);
                }

                _uid = -1;
                return;
            }

            if (_card == null)
            {
                _card = Object.Instantiate(context.CardPrefab, _slot, false);
                var shape = (RectTransform)_card.transform;
                shape.anchorMin = shape.anchorMax = shape.pivot = new Vector2(0.5f, 0.5f);
                shape.anchoredPosition = Vector2.zero;
                float scale = Mathf.Min(_slot.rect.width / shape.sizeDelta.x, _slot.rect.height / shape.sizeDelta.y);
                shape.localScale = new Vector3(scale, scale, 1f);
            }

            _card.gameObject.SetActive(true);
            if (redraw || card.Uid != _uid)
            {
                _card.Show(context.Face(card.CardId), context.Theme, context.Art);
                _uid = card.Uid;
            }

            _card.ShowTorments(card.Torments);
        }
    }
}
