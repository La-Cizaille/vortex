using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Test mode (until the gestures of INTERFACE.md 3.3 to 3.7 exist): the moves the engine allows, one button each,
    /// and the answers of a pending decision. Buttons are clones of a template child, so their look is edited in the
    /// scene. Labels are plain text.
    /// </summary>
    public sealed class CommandPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text title = null!;
        [SerializeField] private RectTransform buttons = null!;
        [SerializeField] private Button template = null!;

        private readonly List<Button> _shown = new List<Button>();
        private readonly List<string> _labels = new List<string>();
        private readonly List<Action> _actions = new List<Action>();

        /// <summary>Labels of the buttons shown (tests).</summary>
        public IReadOnlyList<string> Options => _labels;

        /// <summary>Title shown (tests).</summary>
        public string Title => title.text;

        /// <summary>Shows a title and one button per choice.</summary>
        public void Show(string heading, IReadOnlyList<(string Label, Action OnChoose)> choices)
        {
            if (choices is null)
            {
                throw new ArgumentNullException(nameof(choices));
            }

            Clear();
            gameObject.SetActive(true);
            title.richText = false;
            title.text = heading;
            foreach ((string label, Action onChoose) in choices)
            {
                Button button = Instantiate(template, buttons, false);
                button.gameObject.SetActive(true);
                TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
                text.richText = false;
                text.text = label;
                button.onClick.AddListener(() => onChoose());
                _shown.Add(button);
                _labels.Add(label);
                _actions.Add(onChoose);
            }
        }

        /// <summary>Shows only a title (nothing to choose now).</summary>
        public void ShowMessage(string heading) => Show(heading, Array.Empty<(string, Action)>());

        /// <summary>Hides the panel.</summary>
        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        /// <summary>Chooses a shown option, as a click would (tests).</summary>
        public void Choose(int index) => _actions[index]();

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(TMP_Text heading, RectTransform buttonRoot, Button buttonTemplate)
        {
            title = heading;
            buttons = buttonRoot;
            template = buttonTemplate;
        }

        private void Clear()
        {
            foreach (Button button in _shown)
            {
                // Hidden at once: Destroy only takes effect at the end of the frame.
                button.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(button.gameObject);
                }
                else
                {
                    DestroyImmediate(button.gameObject);
                }
            }

            _shown.Clear();
            _labels.Clear();
            _actions.Clear();
        }
    }
}
