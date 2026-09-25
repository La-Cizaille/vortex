using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public sealed class SeatDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        [Tooltip("Agrandissement de la fiche au survol (1 : aucun). Réservé aux adversaires.")]
        [SerializeField, Min(1f)] private float hoverScale = 1f;
        [Tooltip("Transparence d'une fiche qui ne peut pas être visée pendant qu'on vise.")]
        [SerializeField, Range(0f, 1f)] private float notTargetAlpha = 0.4f;

        private TableContext? _context;
        private CardHolder? _attack;
        private CardHolder? _defense;
        private bool _eliminated;
        private bool? _target;

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
            Release();
            _attack = new CardHolder(attackSlot);
            _defense = new CardHolder(defenseSlot);
            TMP_Text leaderLabel = leaderMarker.GetComponentInChildren<TMP_Text>(true);
            if (leaderLabel != null)
            {
                leaderLabel.text = context.Texts.Get(TextKeys.SeatLeader);
            }
        }

        /// <summary>Destroys the 3D cards shown (before the display is bound again or goes away): none is left behind.</summary>
        public void Release()
        {
            _attack?.Release();
            _defense?.Release();
            _attack = null;
            _defense = null;
        }

        /// <summary>The overcharge token (the player's own is a button that arms it).</summary>
        public Image OverchargeToken => overcharge;

        /// <summary>
        /// Lets the person use one of their cards by dragging it to the middle (INTERFACE.md 3.5). Set it right after
        /// <see cref="Bind"/>, before the first show.
        /// </summary>
        public void SetCardUse(Func<int, bool> canUse, Func<int, Vector2, bool> use)
        {
            foreach (CardHolder? holder in new[] { _attack, _defense })
            {
                if (holder != null)
                {
                    holder.CanDrag = card => canUse(card.Uid);
                    holder.OnDrop = (card, screen) => use(card.Uid, screen);
                }
            }
        }

        /// <summary>While an action is aimed: true lights the seat as a target, false dims it, null is back to normal.</summary>
        public void ShowTargeting(bool? target)
        {
            _target = target;
            ApplyAlpha();
            if (target == true && _context != null)
            {
                turnHighlight.color = _context.Theme.Overcharge;
                turnHighlight.enabled = true;
            }
        }

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverScale > 1f)
            {
                transform.localScale = Vector3.one * hoverScale;
                transform.SetAsLastSibling();
            }
        }

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData) => transform.localScale = Vector3.one;

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

            if (_target != true)
            {
                turnHighlight.color = theme.Highlight;
                turnHighlight.enabled = table.Outcome is null && table.CurrentPlayer == seat.Seat && !seat.Eliminated;
            }

            leaderMarker.SetActive(table.LeaderBounty > 0 && table.SoleHpLeader == seat.Seat);
            _eliminated = seat.Eliminated;
            ApplyAlpha();
        }

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(TMP_Text nameLabel, TMP_Text hpLabel, TMP_Text shieldLabel, TMP_Text statusLabel, Image overchargeToken, Image[] technologyRounds, RectTransform attack, RectTransform defense, Graphic highlight, GameObject leader, CanvasGroup canvasGroup, float growOnHover = 1f)
        {
            hoverScale = growOnHover;
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

        private void ApplyAlpha() => group.alpha = _eliminated ? eliminatedAlpha : (_target == false ? notTargetAlpha : 1f);
    }
}
