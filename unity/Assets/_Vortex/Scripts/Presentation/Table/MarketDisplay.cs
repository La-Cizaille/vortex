using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Core.Projection;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The two black markets in the middle of the table (INTERFACE.md 3.3): attack on the left, defense on the right,
    /// with the cards left in each deck. A place is added to a row for each visible card, so the market size comes from
    /// the rules, not from the layout.
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

        private readonly List<CardHolder> _attack = new List<CardHolder>();
        private readonly List<CardHolder> _defense = new List<CardHolder>();
        private TableContext? _context;

        /// <summary>Cards shown in the attack market (tests).</summary>
        public int AttackCardCount => Count(_attack);

        /// <summary>Cards shown in the defense market (tests).</summary>
        public int DefenseCardCount => Count(_defense);

        /// <summary>Prepares the display for a game.</summary>
        public void Bind(TableContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            attackLabel.text = context.Texts.Get(TextKeys.SlotAttack);
            defenseLabel.text = context.Texts.Get(TextKeys.SlotDefense);
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

            ShowRow(_attack, attackRow, table.AttackMarket, redrawCards);
            ShowRow(_defense, defenseRow, table.DefenseMarket, redrawCards);
            attackDeck.text = string.Format(CultureInfo.InvariantCulture, _context.Texts.Get(TextKeys.MarketDeck), table.AttackMarket.DeckCount);
            defenseDeck.text = string.Format(CultureInfo.InvariantCulture, _context.Texts.Get(TextKeys.MarketDeck), table.DefenseMarket.DeckCount);
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

        private void ShowRow(List<CardHolder> row, RectTransform parent, MarketView market, bool redraw)
        {
            while (row.Count < market.Visible.Count)
            {
                row.Add(new CardHolder(NewPlace(parent)));
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
