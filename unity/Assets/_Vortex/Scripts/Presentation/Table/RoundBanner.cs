using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Core.State;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Top of the screen (INTERFACE.md 3.8): the round, the event of the round, the rounds left before the doom event,
    /// and the result once the game is over.
    /// </summary>
    public sealed class RoundBanner : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TMP_Text round = null!;
        [SerializeField] private TMP_Text roundEvent = null!;
        [SerializeField] private TMP_Text doom = null!;
        [SerializeField] private TMP_Text outcome = null!;

        private TableContext? _context;
        private HoverIntent? _intent;
        private string? _eventId;

        /// <summary>Result text shown (tests); empty while the game goes on.</summary>
        public string OutcomeText => outcome.text;

        /// <summary>Prepares the banner for a game; pointing at it enlarges the event of the round (INTERFACE.md 3.8).</summary>
        public void Bind(TableContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _intent?.Cancel();
            _intent = new HoverIntent(ShowEvent, HideEvent);
            if (TryGetComponent(out Graphic background))
            {
                background.raycastTarget = true;
            }
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

        /// <summary>Shows the table model's round, event and result.</summary>
        public void Show(TableModel table)
        {
            if (_context is null)
            {
                throw new InvalidOperationException("Bind the banner before showing a round.");
            }

            if (table is null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            TextTable texts = _context.Texts;
            round.text = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.BannerRound), table.Round);
            _eventId = table.ActiveEventId;
            roundEvent.richText = false;
            roundEvent.text = table.ActiveEventId is null ? texts.Get(TextKeys.BannerNoEvent) : _context.Face(table.ActiveEventId).Title;
            doom.text = table.RoundsBeforeDoom > 0
                ? string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.BannerDoomIn), table.RoundsBeforeDoom)
                : texts.Get(TextKeys.BannerDoomPassed);

            outcome.richText = false;
            GameOutcome? result = table.Outcome;
            if (result is null)
            {
                outcome.text = string.Empty;
            }
            else if (result.Winner < 0 || result.Winner >= table.Seats.Count)
            {
                outcome.text = texts.Get(TextKeys.BannerDraw);
            }
            else
            {
                outcome.text = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.BannerWinner), table.Seats[result.Winner].Name, texts.Get(TextKeys.Win(result.Condition)));
            }
        }

        private void Update() => Tick(Time.unscaledDeltaTime);

        private void ShowEvent()
        {
            if (_context?.Zoom != null && _eventId != null)
            {
                _context.Zoom.ShowFace(_context.Face(_eventId), CardAnchor.ScreenRectOf((RectTransform)transform));
            }
        }

        private void HideEvent()
        {
            if (_context?.Zoom != null)
            {
                _context.Zoom.Hide();
            }
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(TMP_Text roundLabel, TMP_Text eventLabel, TMP_Text doomLabel, TMP_Text outcomeLabel)
        {
            round = roundLabel;
            roundEvent = eventLabel;
            doom = doomLabel;
            outcome = outcomeLabel;
        }
    }
}
