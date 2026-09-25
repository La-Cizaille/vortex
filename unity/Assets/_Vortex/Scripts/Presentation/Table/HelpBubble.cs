using TMPro;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A short help shown next to what the pointer is on (INTERFACE.md 3.4: the effect of an action, the combo, the
    /// overcharge token). It lives in the foreground layer, above the table and its cards, and never takes pointer events.
    /// </summary>
    public sealed class HelpBubble : MonoBehaviour
    {
        [SerializeField] private RectTransform bubble = null!;
        [SerializeField] private TMP_Text text = null!;
        [Tooltip("Écart entre l'élément survolé et la bulle, en unités d'interface.")]
        [SerializeField] private float gap = 12f;

        /// <summary>Text shown, or empty when hidden (tests).</summary>
        public string Text => bubble.gameObject.activeSelf ? text.text : string.Empty;

        /// <summary>Shows a help above an element of the interface.</summary>
        public void Show(string help, RectTransform about)
        {
            text.richText = false;
            text.text = help;
            bubble.gameObject.SetActive(true);
            Rect place = CardAnchor.ScreenRectOf(about);
            var parent = (RectTransform)bubble.parent;
            Canvas canvas = parent.GetComponentInParent<Canvas>().rootCanvas;
            Camera? interfaceCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(place.center.x, place.yMax), interfaceCamera, out Vector2 local))
            {
                bubble.localPosition = local + new Vector2(0f, gap);
            }
        }

        /// <summary>Hides the help.</summary>
        public void Hide() => bubble.gameObject.SetActive(false);

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(RectTransform bubbleRoot, TMP_Text label)
        {
            bubble = bubbleRoot;
            text = label;
        }
    }
}
