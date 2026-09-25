using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// What a decision adds to the table (INTERFACE.md 3.6, ARB-82): its question, with no button, and the answers that
    /// are not already on the table. Numbers are die faces to touch; events, or the card that asks when it is not on the
    /// table, are cards shown in the middle. Everything else is answered on the table itself (<see cref="DecisionChoices"/>).
    /// </summary>
    public sealed class DecisionBoard : MonoBehaviour
    {
        [SerializeField] private GameObject questionRoot = null!;
        [SerializeField] private TMP_Text question = null!;
        [SerializeField] private Button[] faces = Array.Empty<Button>();
        [SerializeField] private RectTransform[] middle = Array.Empty<RectTransform>();
        [Tooltip("Écart entre deux cartes montrées au milieu, en unités d'interface.")]
        [SerializeField, Min(0f)] private float middleSpacing = 220f;

        private readonly List<CardHolder> _middle = new List<CardHolder>();
        private TableContext? _context;

        /// <summary>The question shown (tests), empty when none.</summary>
        public string Question => questionRoot.activeSelf ? question.text : string.Empty;

        /// <summary>The die faces offered (tests): their numbers, whether each can be touched.</summary>
        public IReadOnlyList<(int Number, bool Allowed)> Faces
        {
            get
            {
                var shown = new List<(int, bool)>();
                for (int i = 0; i < faces.Length; i++)
                {
                    if (faces[i].gameObject.activeSelf)
                    {
                        shown.Add((i + 1, faces[i].interactable));
                    }
                }

                return shown;
            }
        }

        /// <summary>The cards shown in the middle (tests).</summary>
        public IReadOnlyList<CardDisplay> MiddleCards
        {
            get
            {
                var shown = new List<CardDisplay>();
                foreach (CardHolder holder in _middle)
                {
                    if (holder.Card != null)
                    {
                        shown.Add(holder.Card);
                    }
                }

                return shown;
            }
        }

        /// <summary>How many cards the middle can show.</summary>
        public int MiddleCapacity => middle.Length;

        /// <summary>Prepares the board for a game.</summary>
        public void Bind(TableContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            foreach (CardHolder holder in _middle)
            {
                holder.Release();
            }

            _middle.Clear();
            foreach (RectTransform place in middle)
            {
                _middle.Add(new CardHolder(place));
            }

            for (int i = 0; i < faces.Length; i++)
            {
                faces[i].GetComponentInChildren<TMP_Text>().text = (i + 1).ToString(CultureInfo.InvariantCulture);
            }

            Hide();
        }

        /// <summary>Shows the question of a decision.</summary>
        public void Ask(string text)
        {
            question.richText = false;
            question.text = text;
            questionRoot.SetActive(true);
        }

        /// <summary>Offers die faces 1 to <paramref name="highest"/>; only the <paramref name="allowed"/> ones can be touched.</summary>
        public void OfferNumbers(IReadOnlyCollection<int> allowed, int highest, Action<int> pick)
        {
            if (allowed is null)
            {
                throw new ArgumentNullException(nameof(allowed));
            }

            if (pick is null)
            {
                throw new ArgumentNullException(nameof(pick));
            }

            for (int i = 0; i < faces.Length; i++)
            {
                int number = i + 1;
                Button face = faces[i];
                face.gameObject.SetActive(number <= highest);
                face.interactable = allowed.Contains(number);
                face.onClick.RemoveAllListeners();
                face.onClick.AddListener(() => pick(number));
            }
        }

        /// <summary>
        /// Shows content cards in the middle, side by side, each marked with its colour; touching the card at an index calls
        /// <paramref name="pick"/> with it.
        /// </summary>
        public void OfferCards(IReadOnlyList<(string ContentId, Color Mark)> cards, Action<int> pick)
        {
            if (_context is null)
            {
                throw new InvalidOperationException("Bind the decision board before offering cards.");
            }

            if (cards is null || cards.Count > middle.Length)
            {
                throw new ArgumentException("More cards than places in the middle.", nameof(cards));
            }

            for (int i = 0; i < _middle.Count; i++)
            {
                CardHolder holder = _middle[i];
                if (i >= cards.Count)
                {
                    holder.Clear();
                    holder.OnTap = null;
                    continue;
                }

                middle[i].anchoredPosition = new Vector2((i - (cards.Count - 1) / 2f) * middleSpacing, middle[i].anchoredPosition.y);
                holder.ShowFace(_context.Face(cards[i].ContentId), _context);
                holder.Card!.Mark(cards[i].Mark, _context.Theme);
                int index = i;
                holder.OnTap = () => pick(index);
            }
        }

        /// <summary>Takes the question and the answers away.</summary>
        public void Hide()
        {
            questionRoot.SetActive(false);
            foreach (Button face in faces)
            {
                face.onClick.RemoveAllListeners();
                face.gameObject.SetActive(false);
            }

            foreach (CardHolder holder in _middle)
            {
                holder.Clear();
                holder.OnTap = null;
            }
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(GameObject questionPanel, TMP_Text questionLabel, Button[] dieFaces, RectTransform[] middlePlaces)
        {
            questionRoot = questionPanel;
            question = questionLabel;
            faces = dieFaces;
            middle = middlePlaces;
        }
    }
}
