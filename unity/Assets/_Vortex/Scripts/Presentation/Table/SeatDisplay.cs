using System;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Core.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Shows one seat (INTERFACE.md 3.1 and 3.2): name, HP, shield, overcharge, technologies obtained, its two modifiers
    /// with their Torment tokens, its temporary effects, whose turn it is, the HP leader marker (leader bounty option)
    /// and the eliminated state. The same component fills the opponent panel and the player's own panel; only their
    /// layout differs.
    /// </summary>
    public sealed class SeatDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
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
        private HoverIntent? _intent;
        private bool _panelPointed;
        private bool _attackPointed;
        private bool _defensePointed;
        private bool _grown;
        private bool _shrinkPending;

        /// <summary>HP text shown (tests).</summary>
        public string HpText => hp.text;

        /// <summary>Status line shown (tests): effects in play, or the wreck's marker.</summary>
        public string StatusText => statuses.text;

        /// <summary>The attack modifier shown, or null.</summary>
        public CardDisplay? AttackCard => _attack?.Card;

        /// <summary>The defense modifier shown, or null.</summary>
        public CardDisplay? DefenseCard => _defense?.Card;

        /// <summary>Prepares the display for a game.</summary>
        public void Bind(TableContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Release();
            _attack = new CardHolder(attackSlot) { OnPointed = pointed => CardPointed(ref _attackPointed, pointed) };
            _defense = new CardHolder(defenseSlot) { OnPointed = pointed => CardPointed(ref _defensePointed, pointed) };
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
        /// Lets the person use one of their cards by dragging it to the middle (INTERFACE.md 3.5); a card that cannot be used
        /// now says why (<paramref name="refused"/>, taken back by <paramref name="released"/>). Set it right after
        /// <see cref="Bind"/>, before the first show.
        /// </summary>
        public void SetCardUse(Func<int, bool> canUse, Func<int, Vector2, bool> use, Action<int, RectTransform>? refused = null, Action? released = null)
        {
            foreach (CardHolder? holder in new[] { _attack, _defense })
            {
                if (holder != null)
                {
                    holder.CanDrag = card => canUse(card.Uid);
                    holder.OnDrop = (card, screen) => use(card.Uid, screen);
                    holder.OnRefused = refused is null ? null : (card, place) => refused(card.Uid, place);
                    holder.OnReleased = released;
                }
            }
        }

        /// <summary>
        /// While a market card of <paramref name="slot"/> is dragged, marks the card of that slot, which the purchase would
        /// replace (INTERFACE.md 3.3); false gives it back its colour.
        /// </summary>
        public void MarkLoss(CardSlot slot, bool marked)
        {
            CardDisplay? card = slot == CardSlot.Attack ? AttackCard : DefenseCard;
            if (_context != null && card != null && card.gameObject.activeSelf)
            {
                card.Mark(marked ? _context.Theme.Loss : (Color?)null, _context.Theme);
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
        public void OnPointerEnter(PointerEventData eventData) => Intent.Enter(eventData);

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData) => Intent.Exit(eventData);

        /// <inheritdoc/>
        public void OnPointerDown(PointerEventData eventData) => Intent.Down(eventData);

        /// <inheritdoc/>
        public void OnPointerUp(PointerEventData eventData) => Intent.Up(eventData);

        /// <summary>Advances a long press, and closes the panel once nothing of it is pointed at (the frame loop, or tests).</summary>
        public void Tick(float deltaTime)
        {
            Intent.Tick(deltaTime);
            if (_shrinkPending)
            {
                _shrinkPending = false;
                if (!Pointed)
                {
                    SetGrown(false);
                }
            }
        }

        /// <summary>True while the panel is enlarged.</summary>
        public bool Grown => _grown;

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
                ? texts.Get(table.GhostsChooseEvent ? TextKeys.SeatGhost : TextKeys.SeatEliminated)
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

        // Hovering (a long press on a touch screen) grows the panel so its cards can be read (INTERFACE.md 3.1).
        private HoverIntent Intent => _intent ??= new HoverIntent(Grow, Shrink);

        // Its cards are 3D objects outside the panel: moving from the panel onto a card leaves the panel. The panel stays
        // open while it or one of its cards is pointed at, and only closes a frame later, once no event reopened it:
        // otherwise it would shrink, move the card away from the pointer, and grow again, over and over (playtest 2).
        private bool Pointed => _panelPointed || _attackPointed || _defensePointed;

        private void Update() => Tick(Time.unscaledDeltaTime);

        private void Grow()
        {
            _panelPointed = true;
            Changed();
        }

        private void Shrink()
        {
            _panelPointed = false;
            Changed();
        }

        private void CardPointed(ref bool slot, bool pointed)
        {
            slot = pointed;
            Changed();
        }

        private void Changed()
        {
            if (Pointed)
            {
                _shrinkPending = false;
                SetGrown(true);
            }
            else if (_grown)
            {
                _shrinkPending = true;
            }
        }

        private void SetGrown(bool grown)
        {
            _grown = grown && hoverScale > 1f;
            transform.localScale = Vector3.one * (_grown ? hoverScale : 1f);
            if (_grown)
            {
                transform.SetAsLastSibling();
            }
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
