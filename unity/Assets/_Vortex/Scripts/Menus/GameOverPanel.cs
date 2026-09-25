using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;

namespace Vortex.Client.Menus
{
    /// <summary>The end of a game (INTERFACE.md 5): the winner and how, with "Rejouer" (same seats) and "Menu".</summary>
    public sealed class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject window = null!;
        [SerializeField] private TMP_Text outcome = null!;
        [SerializeField] private Button replay = null!;
        [SerializeField] private Button menu = null!;

        /// <summary>True while the panel shows.</summary>
        public bool Shown => window.activeSelf;

        /// <summary>The outcome written, or empty when hidden (tests).</summary>
        public string Text => Shown ? outcome.text : string.Empty;

        /// <summary>Connects the panel to a game.</summary>
        public void Bind(TextTable texts, Action replayed, Action left)
        {
            if (texts == null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            MenuButtons.Label(replay, texts.Get(TextKeys.GameOverReplay));
            MenuButtons.Label(menu, texts.Get(TextKeys.GameOverMenu));
            MenuButtons.Wire(replay, replayed ?? throw new ArgumentNullException(nameof(replayed)));
            MenuButtons.Wire(menu, left ?? throw new ArgumentNullException(nameof(left)));
            Hide();
        }

        /// <summary>Shows the outcome (a text built from the public view: names shown without rich text).</summary>
        public void Show(string result)
        {
            outcome.richText = false;
            outcome.text = result;
            window.SetActive(true);
        }

        /// <summary>Hides the panel.</summary>
        public void Hide() => window.SetActive(false);

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(GameObject windowRoot, TMP_Text outcomeLabel, Button replayButton, Button menuButton)
        {
            window = windowRoot;
            outcome = outcomeLabel;
            replay = replayButton;
            menu = menuButton;
        }
    }
}
