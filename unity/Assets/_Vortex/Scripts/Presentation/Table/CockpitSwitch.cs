using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vortex.Client.Presentation
{
    /// <summary>The cockpit's overcharge switch (ARB-90): a touch arms or disarms the token (ARB-67).</summary>
    public sealed class CockpitSwitch : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>What a touch does.</summary>
        public Action? Pressed { get; set; }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData) => Pressed?.Invoke();
    }
}
