using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Keeps an interface element at the screen position of a scene object (ADR-0015), for instance an opponent's panel
    /// under their ship. The offset is in interface units (reference resolution 1920 x 1080).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScreenAnchor : MonoBehaviour
    {
        [SerializeField] private Transform? target;
        [SerializeField] private Camera? view;
        [SerializeField] private Vector2 offset;

        /// <summary>Follows a scene object as seen by a camera.</summary>
        public void Follow(Transform sceneObject, Camera sceneCamera, Vector2 referenceOffset)
        {
            target = sceneObject;
            view = sceneCamera;
            offset = referenceOffset;
            Place();
        }

        /// <summary>Moves the element to its target's screen position now.</summary>
        public void Place()
        {
            if (target == null || view == null || transform.parent is not RectTransform parent)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            Camera? interfaceCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screen = view.WorldToScreenPoint(target.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, interfaceCamera, out Vector2 local))
            {
                transform.localPosition = local + offset;
            }
        }

        private void LateUpdate() => Place();
    }
}
