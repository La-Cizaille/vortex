using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Shows one seat (INTERFACE.md 3.1 and 3.2): name, HP, shield, overcharge, technologies obtained, its two modifiers
    /// with their Torment tokens, its temporary effects, whose turn it is, the HP leader marker (leader bounty option)
    /// and the eliminated state. The same component fills the opponent panel and the player's own panel; only their
    /// layout differs.
    /// </summary>
    public sealed class SeatDisplay : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerName = null!;
        [SerializeField] private TMP_Text hp = null!;
        [SerializeField] private TMP_Text shield = null!;
        [SerializeField] private TMP_Text statuses = null!;
        [SerializeField] private Image overcharge = null!;
        [SerializeField] private Image[] technologies = Array.Empty<Image>();
        [SerializeField] private RectTransform attackSlot = null!;
        [SerializeField] private RectTransform defenseSlot = null!;
        [SerializeField] private Graphic turnHighlight = null!;
        [SerializeField] private GameObject leaderMarker = null!;
        [SerializeField] private CanvasGroup group = null!;
        [SerializeField, Range(0f, 1f)] private float eliminatedAlpha = 0.35f;

        private TableContext? _context;
        private CardHolder? _attack;
        private CardHolder? _defense;

        /// <summary>HP text shown (tests).</summary>
        public string HpText => hp.text;

        /// <summary>The attack modifier shown, or null.</summary>
        public CardDisplay? AttackCard => _attack?.Card;

        /// <summary>The defense modifier shown, or null.</summary>
        public CardDisplay? DefenseCard => _defense?.Card;

        /// <summary>Prepares the display for a game.</summary>
        public void Bind(TableContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _attack = new CardHolder(attackSlot);
            _defense = new CardHolder(defenseSlot);
            TMP_Text leaderLabel = leaderMarker.GetComponentInChildren<TMP_Text>(true);
            if (leaderLabel != null)
            {
                leaderLabel.text = context.Texts.Get(TextKeys.SeatLeader);
            }
        }

        /// <summary>Shows a seat as the table model has it now.</summary>
        public void Show(SeatModel seat, TableModel table, bool redrawCards = false)
        {
            if (_context is null || _attack is null || _defense is null)
            {
                throw new InvalidOperationException("Bind the seat display before showing a seat.");
            }

            if (seat is null)
            {
                throw new ArgumentNullException(nameof(seat));
            }

            if (table is null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            TextTable texts = _context.Texts;
            var theme = _context.Theme;
            playerName.richText = false;
            playerName.text = seat.Name;
            hp.text = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.SeatHp), seat.Hp);
            shield.text = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.SeatShield), seat.Shield);
            overcharge.color = seat.Overcharge > 0 ? theme.Overcharge : Dim(theme.MutedText);

            for (int i = 0; i < technologies.Length; i++)
            {
                technologies[i].gameObject.SetActive(i < table.TechnologiesToWin);
                technologies[i].color = i < seat.Technologies.Count ? theme.Technology(seat.Technologies[i]) : Dim(theme.MutedText);
            }

            statuses.richText = false;
            statuses.text = seat.Eliminated
                ? texts.Get(TextKeys.SeatEliminated)
                : string.Join(" · ", seat.Statuses.Where(s => s.Active).Select(s => texts.Get(TextKeys.Status(s.Kind))).Distinct());

            _attack.Show(seat.AttackCard, _context, redrawCards);
            _defense.Show(seat.DefenseCard, _context, redrawCards);

            turnHighlight.color = theme.Highlight;
            turnHighlight.enabled = table.Outcome is null && table.CurrentPlayer == seat.Seat && !seat.Eliminated;
            leaderMarker.SetActive(table.LeaderBounty > 0 && table.SoleHpLeader == seat.Seat);
            group.alpha = seat.Eliminated ? eliminatedAlpha : 1f;
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(TMP_Text nameLabel, TMP_Text hpLabel, TMP_Text shieldLabel, TMP_Text statusLabel, Image overchargeToken, Image[] technologyRounds, RectTransform attack, RectTransform defense, Graphic highlight, GameObject leader, CanvasGroup canvasGroup)
        {
            playerName = nameLabel;
            hp = hpLabel;
            shield = shieldLabel;
            statuses = statusLabel;
            overcharge = overchargeToken;
            technologies = technologyRounds;
            attackSlot = attack;
            defenseSlot = defense;
            turnHighlight = highlight;
            leaderMarker = leader;
            group = canvasGroup;
        }

        private static Color Dim(Color color) => new Color(color.r, color.g, color.b, 0.25f);
    }
}
