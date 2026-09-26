using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        [Tooltip("Place du paquet de chaque marché, en tête de sa rangée (ANIMATIONS.md §2).")]
        [SerializeField] private RectTransform? attackDeckPlace;
        [SerializeField] private RectTransform? defenseDeckPlace;
        [SerializeField] private TMP_Text toggleLabel = null!;

        private readonly List<CardHolder> _attack = new List<CardHolder>();
        private readonly List<CardHolder> _defense = new List<CardHolder>();
        private TableContext? _context;
        private Func<CardSlot, int, bool>? _canBuy;
        private Func<CardSlot, int, Vector2, bool>? _buy;
        private Action<CardSlot, int, RectTransform>? _refused;
        private Action? _released;
        private Action<CardSlot, bool>? _holding;
        private Action<int>? _tap;
        private DeckDisplay? _attackDeck;
        private DeckDisplay? _defenseDeck;
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
            foreach (CardHolder holder in _attack.Concat(_defense))
            {
                if (holder.Anchor != null)
                {
                    holder.Anchor.Concealed = false;
                }
            }

            _attackDeck = NewDeck(_attackDeck, attackDeckPlace);
            _defenseDeck = NewDeck(_defenseDeck, defenseDeckPlace);
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

        /// <summary>Sets what a touch of a market card does, given its uid (a decision's answer, ARB-82).</summary>
        public void SetCardTap(Action<int> tap)
        {
            _tap = tap;
            foreach (CardHolder holder in _attack.Concat(_defense))
            {
                Tap(holder);
            }
        }

        /// <summary>Marks each market card with the colour <paramref name="mark"/> gives its uid, or gives it back its own.</summary>
        public void MarkCards(Func<int, Color?> mark)
        {
            foreach (CardHolder holder in _attack.Concat(_defense))
            {
                if (_context != null && holder.Card != null && holder.Shown != null)
                {
                    holder.Card.Mark(mark(holder.Shown.Uid), _context.Theme);
                }
            }
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
            _attackDeck?.Show(table.AttackMarket.DeckCount);
            _defenseDeck?.Show(table.DefenseMarket.DeckCount);
            attackDeck.text = string.Format(CultureInfo.InvariantCulture, _context.Texts.Get(TextKeys.MarketDeck), table.AttackMarket.DeckCount);
            defenseDeck.text = string.Format(CultureInfo.InvariantCulture, _context.Texts.Get(TextKeys.MarketDeck), table.DefenseMarket.DeckCount);
        }

        /// <summary>The deck of a market (tests), or null without its place.</summary>
        public DeckDisplay? DeckOf(CardSlot slot) => slot == CardSlot.Attack ? _attackDeck : _defenseDeck;

        /// <summary>The place of a market's deck, or null.</summary>
        public RectTransform? DeckPlace(CardSlot slot) => slot == CardSlot.Attack ? attackDeckPlace : defenseDeckPlace;

        /// <summary>The place of the card at <paramref name="index"/> in a market, or null when the row has no such place yet.</summary>
        public RectTransform? PlaceOf(CardSlot slot, int index)
        {
            RectTransform row = slot == CardSlot.Attack ? attackRow : defenseRow;
            List<CardHolder> holders = slot == CardSlot.Attack ? _attack : _defense;
            return index >= 0 && index < holders.Count ? (RectTransform)row.GetChild(row.childCount - holders.Count + index) : null;
        }

        /// <summary>The cards a market shows now, with their places (a recycle sends them back to the deck).</summary>
        public IReadOnlyList<(CardView Card, RectTransform Place)> Shown(CardSlot slot)
        {
            List<CardHolder> holders = slot == CardSlot.Attack ? _attack : _defense;
            var shown = new List<(CardView, RectTransform)>();
            for (int i = 0; i < holders.Count; i++)
            {
                RectTransform? place = PlaceOf(slot, i);
                if (holders[i].Shown != null && place != null)
                {
                    shown.Add((holders[i].Shown!, place));
                }
            }

            return shown;
        }

        /// <summary>Hides the card at a place while a copy of it flies there, or shows it again.</summary>
        public void Conceal(CardSlot slot, int index, bool concealed)
        {
            List<CardHolder> holders = slot == CardSlot.Attack ? _attack : _defense;
            if (index >= 0 && index < holders.Count && holders[index].Anchor != null)
            {
                holders[index].Anchor!.Concealed = concealed;
            }
        }

        /// <summary>Whether the card at a place is hidden, waiting for its copy on the way (tests).</summary>
        public bool IsConcealed(CardSlot slot, int index)
        {
            List<CardHolder> holders = slot == CardSlot.Attack ? _attack : _defense;
            return index >= 0 && index < holders.Count && holders[index].Anchor != null && holders[index].Anchor!.Concealed;
        }

        /// <summary>Where a place of the market is on the layer of the 3D cards, and how tall a card is there.</summary>
        public (Vector3 Position, float Height) OnCardLayer(RectTransform place)
        {
            Camera view = _context!.View;
            Rect rect = CardAnchor.ScreenRectOf(place);
            Vector3 position = view.ViewportToWorldPoint(new Vector3(rect.center.x / view.pixelWidth, rect.center.y / view.pixelHeight, _context.CardDepth));
            return (position, CardAnchor.WorldHeightAt(view, _context.CardDepth, rect.height));
        }

        /// <summary>Wires the places of the decks (editor setup).</summary>
        public void AssignDecks(RectTransform attack, RectTransform defense)
        {
            attackDeckPlace = attack;
            defenseDeckPlace = defense;
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

        // A deck for a game: the card model, seen from its back, over the deck's place.
        private DeckDisplay? NewDeck(DeckDisplay? previous, RectTransform? place)
        {
            if (previous != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(previous.gameObject);
                }
                else
                {
                    DestroyImmediate(previous.gameObject);
                }
            }

            if (place == null || _context is null)
            {
                return null;
            }

            CardDisplay card = Instantiate(_context.CardPrefab, _context.CardRoot, false);
            card.name = "Paquet";
            DeckDisplay deck = card.gameObject.AddComponent<DeckDisplay>();
            deck.Follow(place, _context.View, _context.CardDepth);
            return deck;
        }

        private void Tap(CardHolder holder)
        {
            Action<int>? tap = _tap;
            holder.OnTap = tap is null ? null : () =>
            {
                if (holder.Shown != null)
                {
                    tap(holder.Shown.Uid);
                }
            };
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

                Tap(holder);
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
