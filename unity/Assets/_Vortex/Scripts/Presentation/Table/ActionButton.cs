using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vortex.Core.Commands;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// One crew action of the half-circle above the player's ship (INTERFACE.md 3.4): its pictogram, or a short name while
    /// the icon is missing; a help on hover; a tap for the actions without a target, a drag towards an opponent for the
    /// others. What happens is decided by <see cref="PlayerControls"/>, from the engine's legal commands.
    /// </summary>
    public sealed class ActionButton : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private CrewAction action;
        [SerializeField] private Image icon = null!;
        [SerializeField] private TMP_Text shortName = null!;
        [SerializeField] private CanvasGroup group = null!;
        [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.3f;

        private PlayerControls? _controls;

        /// <summary>The action of the button.</summary>
        public CrewAction Action => action;

        /// <summary>True when the engine allows the action now.</summary>
        public bool Available { get; private set; }

        /// <summary>True for the actions aimed at an opponent (dragged), false for the others (tapped).</summary>
        public bool NeedsTarget => NeedsTargetFor(action);

        /// <summary>Whether an action is aimed at an opponent.</summary>
        public static bool NeedsTargetFor(CrewAction crewAction) => crewAction == CrewAction.Attack || crewAction == CrewAction.Sabotage;

        /// <summary>Connects the button to the controls and shows its pictogram (or its short name without one).</summary>
        public void Bind(PlayerControls controls, Sprite? pictogram, string abbreviation)
        {
            _controls = controls;
            icon.sprite = pictogram;
            icon.enabled = pictogram != null;
            shortName.richText = false;
            shortName.text = pictogram != null ? string.Empty : abbreviation;
            SetAvailable(false);
        }

        /// <summary>Lights or dims the button.</summary>
        public void SetAvailable(bool available)
        {
            Available = available;
            group.alpha = available ? 1f : unavailableAlpha;
        }

        /// <inheritdoc/>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_controls != null && Available && !NeedsTarget)
            {
                _controls.UseAction(action);
            }
        }

        /// <inheritdoc/>
        public void OnBeginDrag(PointerEventData eventData)
        {
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
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_controls != null)
            {
                _controls.ShowActionHelp(action, (RectTransform)transform);
            }
        }

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData)
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
