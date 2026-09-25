using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A short help shown next to what the pointer is on (INTERFACE.md 3.4: the effect of an action, the combo, the
    /// overcharge token, the preview of an aimed attack). It lives in the foreground layer, above the table and its
    /// cards, and never takes pointer events. It goes above the element, or below it when there is no room above,
    /// and always stays inside the screen.
    /// </summary>
    public sealed class HelpBubble : MonoBehaviour
    {
        [SerializeField] private RectTransform bubble = null!;
        [SerializeField] private TMP_Text text = null!;
        [Tooltip("Écart entre l'élément survolé et la bulle, en unités d'interface.")]
        [SerializeField] private float gap = 12f;

        /// <summary>Text shown, or empty when hidden (tests).</summary>
        public string Text => bubble.gameObject.activeSelf ? text.text : string.Empty;

        /// <summary>The bubble itself (tests check where it is).</summary>
        public RectTransform Bubble => bubble;

        /// <summary>Shows a help next to an element of the interface.</summary>
        public void Show(string help, RectTransform about)
        {
            text.richText = false;
            text.text = help;
            bubble.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);

            Rect place = CardAnchor.ScreenRectOf(about);
            var parent = (RectTransform)bubble.parent;
            Canvas canvas = parent.GetComponentInParent<Canvas>().rootCanvas;
            Camera? interfaceCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(place.center.x, place.yMax), interfaceCamera, out Vector2 top)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, new Vector2(place.center.x, place.yMin), interfaceCamera, out Vector2 bottom))
            {
                return;
            }

            Rect area = parent.rect;
            Vector2 size = bubble.rect.size;
            bool above = top.y + gap + size.y <= area.yMax;
            bubble.pivot = new Vector2(0.5f, above ? 0f : 1f);
            Vector2 at = above ? top + new Vector2(0f, gap) : bottom - new Vector2(0f, gap);
            float half = Mathf.Min(size.x / 2f, area.width / 2f);
            at.x = Mathf.Clamp(at.x, area.xMin + half, area.xMax - half);
            bubble.localPosition = at;
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
