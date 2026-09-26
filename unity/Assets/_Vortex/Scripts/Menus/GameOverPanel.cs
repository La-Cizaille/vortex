using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;

namespace Vortex.Client.Menus
{
    /// <summary>
    /// The end of a game (INTERFACE.md 5): a poster (docs/DIRECTION_ARTISTIQUE.md 6.5) with the verdict in a stamp ("RECHERCHÉ"
    /// for the last ship standing, "ÉLU" for the election), the winner and how, the epitaphs of the others, and "Rejouer"
    /// (same seats) and "Menu".
    /// </summary>
    public sealed class GameOverPanel : MonoBehaviour
    {
        [SerializeField] private GameObject window = null!;
        [SerializeField] private TMP_Text outcome = null!;
        [SerializeField] private Button replay = null!;
        [SerializeField] private Button menu = null!;
        [SerializeField] private TMP_Text? verdict;
        [SerializeField] private TMP_Text? epitaphs;

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

        /// <summary>The verdict and the epitaphs shown, or empty when hidden (tests).</summary>
        public (string Verdict, string Epitaphs) Poster => Shown
            ? (verdict != null ? verdict.text : string.Empty, epitaphs != null ? epitaphs.text : string.Empty)
            : (string.Empty, string.Empty);

        /// <summary>
        /// Shows the outcome, with the <paramref name="stamp"/> verdict and the <paramref name="epitaph"/> lines of the
        /// fallen (texts built from the public view and the interface texts: names shown without rich text).
        /// </summary>
        public void Show(string result, string stamp = "", string epitaph = "")
        {
            outcome.richText = false;
            outcome.text = result;
            if (verdict != null)
            {
                verdict.richText = false;
                verdict.text = stamp;
                verdict.transform.parent.gameObject.SetActive(stamp.Length > 0);
            }

            if (epitaphs != null)
            {
                epitaphs.richText = false;
                epitaphs.text = epitaph;
                epitaphs.gameObject.SetActive(epitaph.Length > 0);
            }

            window.SetActive(true);
        }

        /// <summary>Hides the panel.</summary>
        public void Hide() => window.SetActive(false);

        /// <summary>Wires the verdict stamp and the epitaphs (editor setup).</summary>
        public void AssignPoster(TMP_Text verdictLabel, TMP_Text epitaphLabel)
        {
            verdict = verdictLabel;
            epitaphs = epitaphLabel;
        }

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
