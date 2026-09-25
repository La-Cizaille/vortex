using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Lets a card of the table be touched to answer a decision (ARB-82). A drag is not a touch: the event system sends no
    /// click once the pointer has dragged the card.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    public sealed class CardTap : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>What a touch does.</summary>
        public Action? Tapped { get; set; }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke();
    }
}
