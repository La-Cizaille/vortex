using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Lets a card of the table be dragged (INTERFACE.md 3.3 and 3.5): a market card towards the player's ship to buy it,
    /// one of the player's cards towards the middle to use it. The owner decides when a drag is allowed and what a drop
    /// does; a refused drop sends the card back to its place. While dragged, the card follows the pointer, nearer to the
    /// camera than the other cards. A drag that is not allowed now tells why, until the pointer is released.
    /// </summary>
    [RequireComponent(typeof(CardAnchor))]
    public sealed class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        private Func<bool>? _canDrag;
        private Func<Vector2, bool>? _onDrop;
        private Action? _refused;
        private Action? _released;
        private Action<bool>? _holding;
        private Camera? _view;
        private float _depth;

        /// <summary>True while the card is being dragged.</summary>
        public bool Dragging { get; private set; }

        /// <summary>
        /// Sets when a drag is allowed and what a drop does; <paramref name="refused"/> explains a drag that is not allowed,
        /// and <paramref name="released"/> takes the explanation back when the pointer is released; <paramref name="holding"/>
        /// learns when the card is taken (true) and put down (false).
        /// </summary>
        public void Bind(Func<bool> canDrag, Func<Vector2, bool> onDrop, Camera view, float depth, Action? refused = null, Action? released = null, Action<bool>? holding = null)
        {
            _canDrag = canDrag;
            _onDrop = onDrop;
            _refused = refused;
            _released = released;
            _holding = holding;
            _view = view;
            _depth = depth;
        }

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null || _canDrag is null || !_canDrag())
            {
                // Not now: the drag is dropped, so no further drag event reaches the card; the reason shows instead.
                if (eventData != null)
                {
                    eventData.pointerDrag = null;
                }

                _refused?.Invoke();
                return;
            }

            Dragging = true;
            GetComponent<CardAnchor>().Held = true;
            CardHover hover = GetComponent<CardHover>();
            if (hover != null)
            {
                hover.HideZoom();
            }

            Follow(eventData.position);
            _holding?.Invoke(true);
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData eventData)
        {
            if (Dragging && eventData != null)
            {
                Follow(eventData.position);
            }
        }

        /// <inheritdoc/>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (Dragging && eventData != null)
            {
                Drop(eventData.position);
            }
        }

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData) => _released?.Invoke();

        /// <summary>Drops the card at a screen position (also called by tests); returns whether the drop was accepted.</summary>
        public bool Drop(Vector2 screen)
        {
            Dragging = false;
            bool accepted = _onDrop != null && _onDrop(screen);
            CardAnchor anchor = GetComponent<CardAnchor>();
            anchor.Held = false;
            anchor.Place();
            _holding?.Invoke(false);
            return accepted;
        }

        private void Follow(Vector2 screen)
        {
            if (_view != null)
            {
                transform.position = _view.ScreenToWorldPoint(new Vector3(screen.x, screen.y, _depth));
            }
        }
    }
}
