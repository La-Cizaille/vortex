using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vortex.Core.Commands;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// One crew action of the half-circle above the player's ship (INTERFACE.md 3.4): its pictogram, or a short name while
    /// the icon is missing; a help on hover (a long press on a touch screen); a tap for the actions without a target, a
    /// drag towards an opponent for the others. What happens is decided by <see cref="PlayerControls"/>, from the engine's
    /// legal commands.
    /// </summary>
    public sealed class ActionButton : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private CrewAction action;
        [SerializeField] private Image icon = null!;
        [SerializeField] private TMP_Text shortName = null!;
        [SerializeField] private CanvasGroup group = null!;
        [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.3f;

        private PlayerControls? _controls;
        private HoverIntent? _intent;

        /// <summary>The action of the button.</summary>
        public CrewAction Action => action;

        /// <summary>True when the engine allows the action now.</summary>
        public bool Available { get; private set; }

        /// <summary>
        /// False for an action the rules of the game leave out (the defensive posture when its option is off): it never
        /// shows and takes no place on the arc.
        /// </summary>
        public bool InRules { get; set; } = true;

        /// <summary>True for the actions aimed at an opponent (dragged), false for the others (tapped).</summary>
        public bool NeedsTarget => NeedsTargetFor(action);

        /// <summary>Whether an action is aimed at an opponent.</summary>
        public static bool NeedsTargetFor(CrewAction crewAction) => crewAction == CrewAction.Attack || crewAction == CrewAction.Sabotage;

        /// <summary>Connects the button to the controls and shows its pictogram (or its short name without one).</summary>
        public void Bind(PlayerControls controls, Sprite? pictogram, string abbreviation)
        {
            _controls = controls;
            _intent = new HoverIntent(ShowHelp, HideHelp);
            icon.sprite = pictogram;
            icon.enabled = pictogram != null;
            shortName.richText = false;
            shortName.text = pictogram != null ? string.Empty : abbreviation;
            SetAvailable(false);
        }

        /// <summary>Shows the button while the action is possible, and hides it otherwise (ARB-87); its place on the arc stays.</summary>
        public void SetAvailable(bool available)
        {
            Available = available;
            group.alpha = available ? 1f : unavailableAlpha;
            gameObject.SetActive(available && InRules);
        }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData)
        {
            // A long press only shows the help: lifting the finger afterwards does not use the action.
            if (_controls != null && Available && !NeedsTarget && !(_intent?.WasHeld ?? false))
            {
                _controls.UseAction(action);
            }
        }

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData eventData)
        {
            _intent?.Cancel();
            if (_controls == null || !Available || !NeedsTarget || eventData == null)
            {
                if (eventData != null)
                {
                    eventData.pointerDrag = null;
                }

                return;
            }

            _controls.BeginAim(action, (RectTransform)transform, eventData.position);
        }

        /// <inheritdoc/>
        public void OnDrag(PointerEventData eventData)
        {
            if (_controls != null && eventData != null)
            {
                _controls.Aim(eventData.position);
            }
        }

        /// <inheritdoc/>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_controls != null && eventData != null)
            {
                _controls.EndAim(eventData.position);
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

        private void Update() => Tick(Time.unscaledDeltaTime);

        private void ShowHelp()
        {
            if (_controls != null)
            {
                _controls.ShowActionHelp(action, (RectTransform)transform);
            }
        }

        private void HideHelp()
        {
            if (_controls != null)
            {
                _controls.HideHelp();
            }
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(CrewAction crewAction, Image pictogram, TMP_Text abbreviation, CanvasGroup canvasGroup)
        {
            action = crewAction;
            icon = pictogram;
            shortName = abbreviation;
            group = canvasGroup;
        }
    }
}
