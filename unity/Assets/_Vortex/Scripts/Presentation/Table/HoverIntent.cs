using System;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// When to show what "hovering" shows (a help, an enlarged card, a grown panel), mouse and finger alike (INTERFACE.md
    /// 1): the mouse shows it as soon as it is over the element; a finger, which cannot hover, shows it after a long press,
    /// and hides it when it lifts, leaves the element or starts a drag. A tap never shows it.
    /// </summary>
    /// <remarks>
    /// The owner forwards its pointer events and advances the intent with the frame time. After a long press, the click
    /// that follows the release must be ignored: a long press only shows information (<see cref="WasHeld"/>).
    /// </remarks>
    public sealed class HoverIntent
    {
        /// <summary>How long a finger must stay down before the long press shows, in seconds.</summary>
        public const float HoldSeconds = 0.45f;

        private readonly Action _show;
        private readonly Action _hide;
        private bool _holding;
        private float _held;

        /// <summary>Creates the intent of an element: what to show and what to hide.</summary>
        public HoverIntent(Action show, Action hide)
        {
            _show = show ?? throw new ArgumentNullException(nameof(show));
            _hide = hide ?? throw new ArgumentNullException(nameof(hide));
        }

        /// <summary>True while it is shown.</summary>
        public bool Shown { get; private set; }

        /// <summary>True when the last press was a long press: the click that follows it must do nothing.</summary>
        public bool WasHeld { get; private set; }

        /// <summary>True when the event comes from a finger on a touch screen.</summary>
        public static bool IsTouch(PointerEventData? eventData) =>
            eventData is ExtendedPointerEventData extended && extended.pointerType == UIPointerType.Touch;

        /// <summary>The pointer came over the element: the mouse shows at once.</summary>
        public void Enter(PointerEventData? eventData)
        {
            if (!IsTouch(eventData))
            {
                Show();
            }
        }

        /// <summary>The pointer left the element.</summary>
        public void Exit(PointerEventData? eventData) => Cancel();

        /// <summary>A finger came down: the long press starts counting.</summary>
        public void Down(PointerEventData? eventData)
        {
            if (IsTouch(eventData))
            {
                _holding = true;
                _held = 0f;
                WasHeld = false;
            }
        }

        /// <summary>The finger lifted.</summary>
        public void Up(PointerEventData? eventData)
        {
            if (IsTouch(eventData))
            {
                Cancel();
            }
        }

        /// <summary>Advances a long press by <paramref name="deltaTime"/> seconds.</summary>
        public void Tick(float deltaTime)
        {
            if (!_holding || Shown)
            {
                return;
            }

            _held += deltaTime;
            if (_held >= HoldSeconds)
            {
                WasHeld = true;
                Show();
            }
        }

        /// <summary>Hides what shows and forgets a press in progress (the pointer left, a drag started).</summary>
        public void Cancel()
        {
            _holding = false;
            _held = 0f;
            if (Shown)
            {
                Shown = false;
                _hide();
            }
        }

        private void Show()
        {
            if (!Shown)
            {
                Shown = true;
                _show();
            }
        }
    }
}
