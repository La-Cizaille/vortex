using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A help shown in the help bubble while the pointer is on an element, or after a long press on a touch screen
    /// (INTERFACE.md 3.2: the combo button, the overcharge token). The text is asked for when it shows, so it follows the
    /// game.
    /// </summary>
    public sealed class HoverHelp : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private HoverIntent? _intent;
        private Func<string?>? _text;
        private HelpBubble? _bubble;

        /// <summary>True when the last press was a long press: the click that follows it must do nothing.</summary>
        public bool WasHeld => _intent?.WasHeld ?? false;

        /// <summary>Shows <paramref name="text"/> (nothing when it gives null) in <paramref name="bubble"/>, next to this element.</summary>
        public void Bind(Func<string?> text, HelpBubble bubble)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _bubble = bubble != null ? bubble : throw new ArgumentNullException(nameof(bubble));
            _intent?.Cancel();
            _intent = new HoverIntent(Show, Hide);
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

        private void Update() => Tick(Time.unscaledDeltaTime);

        private void OnDisable() => _intent?.Cancel();

        private void Show()
        {
            string? help = _text?.Invoke();
            if (help != null && _bubble != null)
            {
                _bubble.Show(help, (RectTransform)transform);
            }
        }

        private void Hide()
        {
            if (_bubble != null)
            {
                _bubble.Hide();
            }
        }
    }
}
