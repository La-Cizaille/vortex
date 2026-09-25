using System;
using UnityEngine;
using Vortex.Core.Projection;
using Object = UnityEngine.Object;

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
        private CardView? _shown;
        private int _uid = -1;

        /// <summary>Creates a holder over a place of the interface; outside <paramref name="clip"/>, its card is hidden.</summary>
        public CardHolder(RectTransform slot, RectTransform? clip = null)
        {
            _slot = slot;
            _clip = clip;
        }

        /// <summary>
        /// Whether the card shown can be dragged now (null: never). Set it, with <see cref="OnDrop"/>, before the first
        /// card is shown.
        /// </summary>
        public Func<CardView, bool>? CanDrag { get; set; }

        /// <summary>What a drop of the card at a screen position does; returns false when refused (the card goes back).</summary>
        public Func<CardView, Vector2, bool>? OnDrop { get; set; }

        /// <summary>Explains a drag that is not allowed now, next to the place (null: nothing to say).</summary>
        public Action<CardView, RectTransform>? OnRefused { get; set; }

        /// <summary>Takes the explanation back when the pointer is released.</summary>
        public Action? OnReleased { get; set; }

        /// <summary>Learns when the card shown is taken (true) and put down (false).</summary>
        public Action<CardView, bool>? OnHolding { get; set; }

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
            _shown = card;
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

            _shown = null;
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

                if (OnDrop != null)
                {
                    _card.gameObject.AddComponent<CardDrag>().Bind(
                        () => _shown != null && CanDrag != null && CanDrag(_shown),
                        screen => _shown != null && OnDrop(_shown, screen),
                        context.View,
                        context.CardDepth - 1f,
                        () =>
                        {
                            if (_shown != null)
                            {
                                OnRefused?.Invoke(_shown, _slot);
                            }
                        },
                        () => OnReleased?.Invoke(),
                        held =>
                        {
                            if (_shown != null)
                            {
                                OnHolding?.Invoke(_shown, held);
                            }
                        });
                }
            }

            _card.gameObject.SetActive(true);
            return _card;
        }
    }
}
