using System;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The enlarged card shown while a card of the table is pointed at (INTERFACE.md 1 and 3): a copy of the card, as a
    /// 3D object nearer to the camera than everything else (ADR-0017), beside the card, on the side with more room. The
    /// copy never answers the pointer, so it cannot flicker.
    /// </summary>
    public sealed class CardZoom : MonoBehaviour
    {
        [Tooltip("Hauteur de la carte agrandie, en part de la hauteur de l'écran.")]
        [SerializeField, Range(0.2f, 0.9f)] private float screenHeight = 0.5f;
        [Tooltip("Distance de la carte agrandie à la caméra : plus près que les autres cartes, pour passer devant.")]
        [SerializeField, Min(0.5f)] private float depth = 3f;
        [Tooltip("Écart entre la carte pointée, la carte agrandie et les bords de l'écran, en part de la hauteur de l'écran.")]
        [SerializeField, Range(0f, 0.1f)] private float margin = 0.015f;

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
                _card = Instantiate(_context.CardPrefab, _context.CardRoot, false);
                _card.name = "Carte agrandie";
                _card.SetPointable(false);
            }

            Source = source;
            _card.gameObject.SetActive(true);
            _card.Show(source.Face, _context.Theme, _context.Art);
            _card.ShowTorments(source.Torments);
            Place(source);
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

        // Beside the source, on the side of the screen with more room, kept inside the screen.
        private void Place(CardDisplay source)
        {
            Camera view = _context!.View;
            CardAnchor? anchor = source.GetComponent<CardAnchor>();
            Rect pointed = anchor != null ? anchor.ScreenRect : new Rect(view.WorldToScreenPoint(source.transform.position), Vector2.zero);
            float height = screenHeight * view.pixelHeight;
            float width = height * _card!.Size.x / _card.Size.y;
            float gap = margin * view.pixelHeight;
            float x = pointed.center.x < view.pixelWidth / 2f ? pointed.xMax + gap + (width / 2f) : pointed.xMin - gap - (width / 2f);
            x = Mathf.Clamp(x, gap + (width / 2f), view.pixelWidth - gap - (width / 2f));
            float y = Mathf.Clamp(pointed.center.y, gap + (height / 2f), view.pixelHeight - gap - (height / 2f));

            _card.transform.SetPositionAndRotation(
                view.ViewportToWorldPoint(new Vector3(x / view.pixelWidth, y / view.pixelHeight, depth)),
                view.transform.rotation);
            _card.transform.localScale = Vector3.one * (CardAnchor.WorldHeightAt(view, depth, height) / _card.Size.y);
        }
    }
}
