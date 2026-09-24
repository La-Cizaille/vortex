using System;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The enlarged card shown while a card of the table is pointed at (INTERFACE.md 1 and 3): the small cards of the
    /// panels and the market become readable. It appears beside the card, on the side with more room, and never takes
    /// pointer events itself.
    /// </summary>
    public sealed class CardZoom : MonoBehaviour
    {
        [SerializeField] private RectTransform area = null!;
        [Tooltip("Taille de la carte agrandie (1 = taille du prefab, 250 x 350).")]
        [SerializeField, Min(0.5f)] private float scale = 1.5f;
        [Tooltip("Écart entre la carte pointée, la carte agrandie et les bords de l'écran.")]
        [SerializeField, Min(0f)] private float margin = 16f;

        private TableContext? _context;
        private CardDisplay? _card;

        /// <summary>The card pointed at, or null.</summary>
        public CardDisplay? Source { get; private set; }

        /// <summary>The enlarged card while it is shown, or null.</summary>
        public CardDisplay? Shown => _card != null && _card.gameObject.activeSelf ? _card : null;

        /// <summary>Prepares the zoom for a game.</summary>
        public void Bind(TableContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Hide();
        }

        /// <summary>Shows a card of the table enlarged.</summary>
        public void Show(CardDisplay source)
        {
            if (_context is null || source == null)
            {
                return;
            }

            if (_card == null)
            {
                _card = Instantiate(_context.CardPrefab, area, false);
                _card.name = "Carte agrandie";
                var shape = (RectTransform)_card.transform;
                shape.anchorMin = shape.anchorMax = shape.pivot = new Vector2(0.5f, 0.5f);
                shape.localScale = new Vector3(scale, scale, 1f);
            }

            Source = source;
            _card.Show(source.Face, _context.Theme, _context.Art);
            _card.ShowTorments(source.Torments);
            _card.transform.localPosition = PlaceBeside(source);
            _card.gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>Hides the zoom if it shows <paramref name="source"/>.</summary>
        public void Hide(CardDisplay source)
        {
            if (Source == source)
            {
                Hide();
            }
        }

        /// <summary>Hides the zoom.</summary>
        public void Hide()
        {
            Source = null;
            if (_card != null)
            {
                _card.gameObject.SetActive(false);
            }
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(RectTransform zoomArea) => area = zoomArea;

        // Beside the source, on the side of the screen with more room, kept inside the screen.
        private Vector3 PlaceBeside(CardDisplay source)
        {
            Bounds pointed = RectTransformUtility.CalculateRelativeRectTransformBounds(area, source.transform);
            Vector2 size = ((RectTransform)_card!.transform).sizeDelta * scale;
            Rect screen = area.rect;
            float x = pointed.center.x < screen.center.x
                ? pointed.max.x + margin + (size.x / 2f)
                : pointed.min.x - margin - (size.x / 2f);
            x = Mathf.Clamp(x, screen.xMin + margin + (size.x / 2f), screen.xMax - margin - (size.x / 2f));
            float y = Mathf.Clamp(pointed.center.y, screen.yMin + margin + (size.y / 2f), screen.yMax - margin - (size.y / 2f));
            return new Vector3(x, y, 0f);
        }
    }
}
