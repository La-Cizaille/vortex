using UnityEngine;
using UnityEngine.EventSystems;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Shows its card enlarged while the pointer is on it (INTERFACE.md 1): hovering with a mouse, a long press on a touch
    /// screen (<see cref="HoverIntent"/>). Added by the table to each card it shows.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    public sealed class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private CardZoom? _zoom;
        private CardDisplay? _card;
        private HoverIntent? _intent;

        /// <summary>Connects the card to the zoom of the table.</summary>
        public void Bind(CardZoom zoom)
        {
            _zoom = zoom;
            _card = GetComponent<CardDisplay>();
            _intent = new HoverIntent(ShowZoom, HideCopy);
        }

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData) => _intent?.Enter(eventData);

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData) => _intent?.Exit(eventData);

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData) => _intent?.Down(eventData);

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData) => _intent?.Up(eventData);

        /// <summary>Advances a long press (the frame loop, or tests).</summary>
        public void Tick(float deltaTime) => _intent?.Tick(deltaTime);

        /// <summary>Hides the enlarged copy if it shows this card (when a drag starts, for instance).</summary>
        public void HideZoom()
        {
            if (_intent != null)
            {
                _intent.Cancel();
            }

            HideCopy();
        }

        private void Update() => Tick(Time.unscaledDeltaTime);

        private void OnDisable() => HideZoom();

        private void ShowZoom()
        {
            if (_zoom != null && _card != null)
            {
                _zoom.Show(_card);
            }
        }

        private void HideCopy()
        {
            if (_zoom != null && _card != null)
            {
                _zoom.Hide(_card);
            }
        }
    }
}
