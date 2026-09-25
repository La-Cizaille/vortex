using TMPro;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The "Tour de X" banner shown when the view turns to another person on the same device (INTERFACE.md 4). It shows
    /// for a moment, then fades; the game director advances it with the frame time, so it pauses with the game.
    /// </summary>
    public sealed class TurnAnnouncement : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group = null!;
        [SerializeField] private TMP_Text text = null!;
        [Tooltip("Durée d'affichage, en secondes.")]
        [SerializeField, Min(0.1f)] private float duration = 1.8f;
        [Tooltip("Durée du fondu final, en secondes.")]
        [SerializeField, Min(0.01f)] private float fade = 0.4f;

        private float _left;

        /// <summary>Text shown, or empty when hidden (tests).</summary>
        public string Text => group.gameObject.activeSelf ? text.text : string.Empty;

        /// <summary>Shows a message (a name from the public view: rich text off).</summary>
        public void Show(string message)
        {
            text.richText = false;
            text.text = message;
            _left = duration;
            group.alpha = 1f;
            group.gameObject.SetActive(true);
        }

        /// <summary>Advances the banner by <paramref name="deltaTime"/> seconds.</summary>
        public void Tick(float deltaTime)
        {
            if (!group.gameObject.activeSelf)
            {
                return;
            }

            _left -= deltaTime;
            group.alpha = Mathf.Clamp01(_left / fade);
            if (_left <= 0f)
            {
                Hide();
            }
        }

        /// <summary>Hides the banner.</summary>
        public void Hide()
        {
            _left = 0f;
            group.gameObject.SetActive(false);
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(CanvasGroup canvasGroup, TMP_Text label)
        {
            group = canvasGroup;
            text = label;
        }
    }
}
