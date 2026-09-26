using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The deck of a black market (ANIMATIONS.md §2): the card model seen from its back, tilted so that its edge shows,
    /// as thick as the cards left in it, over its place in the market. Not a pile of real cards: one block, which
    /// disappears when the deck is empty. Revealed cards leave from it, recycled ones go back into it.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    public sealed class DeckDisplay : MonoBehaviour
    {
        [Tooltip("Inclinaison du paquet vers la caméra, en degrés, pour que sa tranche se voie.")]
        [SerializeField] private float tilt = 28f;
        [Tooltip("Épaisseur du paquet plein, en épaisseurs de carte.")]
        [SerializeField, Min(1f)] private float fullThickness = 8f;

        private RectTransform? _place;
        private Camera? _view;
        private float _depth;
        private int _full = 1;

        /// <summary>Cards left in the deck, as last shown.</summary>
        public int Count { get; private set; }

        /// <summary>Follows <paramref name="place"/> at <paramref name="depth"/> units in front of <paramref name="view"/>.</summary>
        public void Follow(RectTransform place, Camera view, float depth)
        {
            _place = place;
            _view = view;
            _depth = depth;
            GetComponent<CardDisplay>().SetPointable(false);
            Place();
        }

        /// <summary>Shows <paramref name="count"/> cards left; the thickest deck seen in the game is the full one.</summary>
        public void Show(int count)
        {
            Count = Mathf.Max(0, count);
            _full = Mathf.Max(_full, Count);
            Place();
        }

        /// <summary>Moves the deck over its place now.</summary>
        public void Place()
        {
            if (_place == null || _view == null)
            {
                return;
            }

            CardDisplay card = GetComponent<CardDisplay>();
            Rect rect = CardAnchor.ScreenRectOf(_place);
            bool shown = Count > 0 && _place.gameObject.activeInHierarchy && rect.height > 0f;
            card.SetVisible(shown);
            if (!shown)
            {
                return;
            }

            transform.position = _view.ViewportToWorldPoint(new Vector3(rect.center.x / _view.pixelWidth, rect.center.y / _view.pixelHeight, _depth));

            // Its back to the camera, tilted so the edge shows; thicker the more cards are left.
            transform.rotation = _view.transform.rotation * Quaternion.Euler(tilt, 180f, 0f);
            float size = CardAnchor.WorldHeightAt(_view, _depth, rect.height) / card.Size.y;
            float thickness = Mathf.Lerp(1f, fullThickness, (float)Count / _full);
            transform.localScale = new Vector3(size, size, size * thickness);
        }

        private void LateUpdate() => Place();
    }
}
