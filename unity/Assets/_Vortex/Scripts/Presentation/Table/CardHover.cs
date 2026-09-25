using UnityEngine;
using UnityEngine.EventSystems;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Shows its card enlarged while the pointer is on it (INTERFACE.md 1): hovering with a mouse, pressing on a touch
    /// screen. Added by the table to each card it shows.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    public sealed class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CardZoom? _zoom;
        private CardDisplay? _card;

        /// <summary>Connects the card to the zoom of the table.</summary>
        public void Bind(CardZoom zoom)
        {
            _zoom = zoom;
            _card = GetComponent<CardDisplay>();
        }

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_zoom != null && _card != null)
            {
                _zoom.Show(_card);
            }
        }

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData) => HideZoom();

        private void OnDisable() => HideZoom();

        /// <summary>Hides the enlarged copy if it shows this card (when a drag starts, for instance).</summary>
        public void HideZoom()
        {
            if (_zoom != null && _card != null)
            {
                _zoom.Hide(_card);
            }
        }
    }
}
