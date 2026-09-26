using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Keeps a 3D card over a place of the interface (ADR-0017). Layouts stay in the interface, where the designer edits
    /// them; the card follows its place at a fixed distance from the camera, facing it, and takes the place's height.
    /// Outside an optional clipping area (a scroll view), the card is hidden.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    public sealed class CardAnchor : MonoBehaviour
    {
        // Scratch buffer for GetWorldCorners (main thread only), so that placing a card allocates nothing.
        private static readonly Vector3[] Corners = new Vector3[4];

        private RectTransform? _slot;
        private RectTransform? _clip;
        private Camera? _view;
        private CardDisplay? _card;
        private float _depth;

        /// <summary>While true, the card stays where it is put (it is being dragged); it goes back to its place after.</summary>
        public bool Held { get; set; }

        /// <summary>While true, the card is hidden: a copy of it is still on its way to this place (a card revealed).</summary>
        public bool Concealed { get; set; }

        /// <summary>The place followed, in screen pixels of the camera, as last placed.</summary>
        public Rect ScreenRect { get; private set; }

        /// <summary>Follows <paramref name="slot"/> at <paramref name="depth"/> units in front of <paramref name="view"/>.</summary>
        public void Follow(RectTransform slot, Camera view, float depth, RectTransform? clip = null)
        {
            _slot = slot;
            _view = view;
            _depth = depth;
            _clip = clip;
            _card = GetComponent<CardDisplay>();
            Place();
        }

        /// <summary>Moves the card over its place now.</summary>
        public void Place()
        {
            if (_slot == null || _view == null || _card == null || Held)
            {
                return;
            }

            Rect place = ScreenRectOf(_slot);
            ScreenRect = place;
            bool shown = !Concealed && _slot.gameObject.activeInHierarchy && place.height > 0f && (_clip == null || ScreenRectOf(_clip).Overlaps(place));
            _card.SetVisible(shown);
            if (!shown)
            {
                return;
            }

            Transform card = transform;
            card.SetPositionAndRotation(
                _view.ViewportToWorldPoint(new Vector3(place.center.x / _view.pixelWidth, place.center.y / _view.pixelHeight, _depth)),
                _view.transform.rotation);
            float height = WorldHeightAt(_view, _depth, place.height);
            card.localScale = Vector3.one * (height / _card.Size.y);
        }

        /// <summary>World height, at a distance from the camera, of <paramref name="pixels"/> screen pixels.</summary>
        public static float WorldHeightAt(Camera view, float depth, float pixels) =>
            2f * depth * Mathf.Tan(view.fieldOfView * 0.5f * Mathf.Deg2Rad) * (pixels / view.pixelHeight);

        /// <summary>The screen rectangle of an interface element, in the pixels of the camera that draws it.</summary>
        public static Rect ScreenRectOf(RectTransform element)
        {
            Canvas? canvas = element.GetComponentInParent<Canvas>();
            Camera? interfaceCamera = canvas == null || canvas.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.rootCanvas.worldCamera;
            element.GetWorldCorners(Corners);
            Vector2 min = RectTransformUtility.WorldToScreenPoint(interfaceCamera, Corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(interfaceCamera, Corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private void LateUpdate() => Place();
    }
}
