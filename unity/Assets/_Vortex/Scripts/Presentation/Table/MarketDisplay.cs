using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Core.Content;
using Vortex.Core.Projection;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The two black markets in the middle of the table (INTERFACE.md 3.3): attack on the left, defense on the right,
    /// with the cards left in each deck. A place is added to a row for each visible card, so the market size comes from
    /// the rules, not from the layout. Outside the person's market phase, the market folds into a strip under the round
    /// banner, and a button opens it again (ARB-81); its 3D cards follow their places, so they shrink with it.
    /// </summary>
    public sealed class MarketDisplay : MonoBehaviour
    {
        [SerializeField] private RectTransform attackRow = null!;
        [SerializeField] private RectTransform defenseRow = null!;
        [SerializeField] private TMP_Text attackLabel = null!;
        [SerializeField] private TMP_Text defenseLabel = null!;
        [SerializeField] private TMP_Text attackDeck = null!;
        [SerializeField] private TMP_Text defenseDeck = null!;
        [Tooltip("Hauteur d'une carte du marché, en unités d'interface (référence 1920 x 1080).")]
        [SerializeField, Min(10f)] private float cardHeight = 133f;
        [Tooltip("Position du marché ouvert, au milieu de la table.")]
        [SerializeField] private Vector2 openPosition = new Vector2(0f, -20f);
        [Tooltip("Position du marché replié, sous le bandeau de manche.")]
        [SerializeField] private Vector2 foldedPosition = new Vector2(0f, 368f);
        [Tooltip("Échelle du marché replié.")]
        [SerializeField, Range(0.2f, 1f)] private float foldedScale = 0.4f;
        [Tooltip("Durée du repli et de l'ouverture, en secondes.")]
        [SerializeField, Min(0f)] private float foldSeconds = 0.25f;
        [SerializeField] private Button toggle = null!;
        [SerializeField] private TMP_Text toggleLabel = null!;

        private readonly List<CardHolder> _attack = new List<CardHolder>();
        private readonly List<CardHolder> _defense = new List<CardHolder>();
        private TableContext? _context;
        private Func<CardSlot, int, bool>? _canBuy;
        private Func<CardSlot, int, Vector2, bool>? _buy;
        private Action<CardSlot, int, RectTransform>? _refused;
        private Action? _released;
        private Action<CardSlot, bool>? _holding;
        private bool? _marketTime;
        private float _unfolded;

        /// <summary>Cards shown in the attack market (tests).</summary>
        public int AttackCardCount => Count(_attack);

        /// <summary>Cards shown in the defense market (tests).</summary>
        public int DefenseCardCount => Count(_defense);

        /// <summary>Whether the market is open, or opening; false while it is folded, or folding (ARB-81).</summary>
        public bool Open { get; private set; }

        /// <summary>Prepares the display for a game; the market starts folded.</summary>
        public void Bind(TableContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            attackLabel.text = context.Texts.Get(TextKeys.SlotAttack);
            defenseLabel.text = context.Texts.Get(TextKeys.SlotDefense);
            toggle.onClick.RemoveAllListeners();
            toggle.onClick.AddListener(Toggle);
            _marketTime = null;
            SetOpen(false, instant: true);
        }

        /// <summary>
        /// Follows the game: the market opens when the person's market phase starts, and folds when it ends. In between,
        /// the person's own choice (<see cref="Toggle"/>) stands.
        /// </summary>
        public void Follow(bool marketTime)
        {
            if (_marketTime == marketTime)
            {
                return;
            }

            _marketTime = marketTime;
            SetOpen(marketTime);
        }

        /// <summary>Opens the folded market, or folds the open one (the button).</summary>
        public void Toggle() => SetOpen(!Open);

        /// <summary>Moves the market towards its open or folded place (called every frame by the director).</summary>
        public void Tick(float deltaTime)
        {
            float target = Open ? 1f : 0f;
            if (_unfolded == target)
            {
                return;
            }

            _unfolded = foldSeconds <= 0f ? target : Mathf.MoveTowards(_unfolded, target, deltaTime / foldSeconds);
            Place();
        }

        /// <summary>
        /// Lets the person buy a card by dragging it (INTERFACE.md 3.3): whether a card can be dragged now, what a drop does,
        /// and why a card cannot be taken now (<paramref name="refused"/>, taken back by <paramref name="released"/>);
        /// <paramref name="holding"/> learns when a card of a market is taken and put down. Set it right after
        /// <see cref="Bind"/>, before the first show.
        /// </summary>
        public void SetPurchase(Func<CardSlot, int, bool> canBuy, Func<CardSlot, int, Vector2, bool> buy, Action<CardSlot, int, RectTransform>? refused = null, Action? released = null, Action<CardSlot, bool>? holding = null)
        {
            _canBuy = canBuy;
            _buy = buy;
            _refused = refused;
            _released = released;
            _holding = holding;
        }

        /// <summary>Shows both markets as the table model has them.</summary>
        public void Show(TableModel table, bool redrawCards = false)
        {
            if (_context is null)
            {
                throw new InvalidOperationException("Bind the market display before showing a market.");
            }

            if (table is null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            ShowRow(_attack, attackRow, CardSlot.Attack, table.AttackMarket, redrawCards);
            ShowRow(_defense, defenseRow, CardSlot.Defense, table.DefenseMarket, redrawCards);
            attackDeck.text = string.Format(CultureInfo.InvariantCulture, _context.Texts.Get(TextKeys.MarketDeck), table.AttackMarket.DeckCount);
            defenseDeck.text = string.Format(CultureInfo.InvariantCulture, _context.Texts.Get(TextKeys.MarketDeck), table.DefenseMarket.DeckCount);
        }

        /// <summary>Wires the button that opens and folds the market (editor setup).</summary>
        public void AssignToggle(Button button, TMP_Text label, Vector2 open, Vector2 folded)
        {
            toggle = button;
            toggleLabel = label;
            openPosition = open;
            foldedPosition = folded;
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(RectTransform attack, RectTransform defense, TMP_Text attackTitle, TMP_Text defenseTitle, TMP_Text attackDeckLabel, TMP_Text defenseDeckLabel, float height)
        {
            cardHeight = height;
            attackRow = attack;
            defenseRow = defense;
            attackLabel = attackTitle;
            defenseLabel = defenseTitle;
            attackDeck = attackDeckLabel;
            defenseDeck = defenseDeckLabel;
        }

        private void SetOpen(bool open, bool instant = false)
        {
            Open = open;
            toggleLabel.text = _context!.Texts.Get(open ? TextKeys.ButtonMarketFold : TextKeys.ButtonMarketOpen);
            if (instant)
            {
                _unfolded = open ? 1f : 0f;
                Place();
            }
        }

        // Between the folded strip (0) and the open market (1), smoothed at both ends.
        private void Place()
        {
            float t = Mathf.SmoothStep(0f, 1f, _unfolded);
            var shape = (RectTransform)transform;
            shape.anchoredPosition = Vector2.Lerp(foldedPosition, openPosition, t);
            shape.localScale = Vector3.one * Mathf.Lerp(foldedScale, 1f, t);
        }

        private static int Count(List<CardHolder> row)
        {
            int count = 0;
            foreach (CardHolder holder in row)
            {
                if (holder.Card != null)
                {
                    count++;
                }
            }

            return count;
        }

        private void ShowRow(List<CardHolder> row, RectTransform parent, CardSlot slot, MarketView market, bool redraw)
        {
            while (row.Count < market.Visible.Count)
            {
                int index = row.Count;
                var holder = new CardHolder(NewPlace(parent));
                if (_buy != null && _canBuy != null)
                {
                    holder.CanDrag = _ => _canBuy(slot, index);
                    holder.OnDrop = (_, screen) => _buy(slot, index, screen);
                    if (_refused != null)
                    {
                        Action<CardSlot, int, RectTransform> refused = _refused;
                        holder.OnRefused = (_, place) => refused(slot, index, place);
                        holder.OnReleased = _released;
                    }

                    if (_holding != null)
                    {
                        Action<CardSlot, bool> holding = _holding;
                        holder.OnHolding = (_, held) => holding(slot, held);
                    }
                }

                row.Add(holder);
            }

            for (int i = 0; i < row.Count; i++)
            {
                row[i].Show(i < market.Visible.Count ? market.Visible[i] : null, _context!, redraw);
            }
        }

        // One card place of a row, with the card's proportions; the row's layout group lines them up, the 3D card follows.
        private RectTransform NewPlace(RectTransform parent)
        {
            var place = new GameObject("Place", typeof(RectTransform), typeof(LayoutElement));
            var shape = (RectTransform)place.transform;
            shape.SetParent(parent, false);
            Vector2 proportions = _context!.CardPrefab.Size;
            var card = new Vector2(cardHeight * proportions.x / proportions.y, cardHeight);
            shape.sizeDelta = card;
            var layout = place.GetComponent<LayoutElement>();
            layout.preferredWidth = card.x;
            layout.preferredHeight = card.y;
            return shape;
        }
    }
}
