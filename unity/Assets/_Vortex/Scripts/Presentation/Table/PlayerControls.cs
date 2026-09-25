using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Projection;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// What the person playing does with the table (INTERFACE.md 3.3 to 3.7, ARB-71): crew actions around the ship, the
    /// market's buttons and cards, their own cards, the combo, the overcharge token, the end of the turn and the
    /// decisions. Everything offered comes from the commands the engine allows now (ADR-0015): a gesture only chooses one
    /// of them, and the session still validates it.
    /// </summary>
    public sealed class PlayerControls : MonoBehaviour
    {
        [SerializeField] private ActionButton[] actions = Array.Empty<ActionButton>();
        [SerializeField] private Button combo = null!;
        [SerializeField] private Image comboBackground = null!;
        [SerializeField] private TMP_Text comboLabel = null!;
        [SerializeField] private Button endTurn = null!;
        [SerializeField] private Image endTurnBackground = null!;
        [SerializeField] private TMP_Text endTurnLabel = null!;
        [SerializeField] private Button endMarket = null!;
        [SerializeField] private TMP_Text endMarketLabel = null!;
        [SerializeField] private Button recycleAttack = null!;
        [SerializeField] private Button recycleDefense = null!;
        [SerializeField] private Button overcharge = null!;
        [SerializeField] private Graphic overchargeGlow = null!;
        [SerializeField] private CommandPanel decision = null!;
        [SerializeField] private HelpBubble help = null!;
        [SerializeField] private RectTransform aimLine = null!;
        [SerializeField] private RectTransform purchaseZone = null!;
        [SerializeField] private RectTransform activationZone = null!;

        private readonly List<Command> _legal = new List<Command>();
        private readonly Dictionary<Command, string?> _previews = new Dictionary<Command, string?>();
        private TableContext? _context;
        private CommandLabels? _labels;
        private PreviewText? _previewText;
        private IControlsHost? _host;
        private GameView? _view;
        private DecisionRequest? _decision;
        private CrewAction? _aiming;
        private RectTransform? _aimFrom;
        private int _aimedAt = -1;
        private int _seat = -1;

        /// <summary>True while the person can act (their turn, or their decision), false while others play.</summary>
        public bool Offered { get; private set; }

        /// <summary>True when the overcharge token is armed (ARB-67): the next attack or shield reroll spends it.</summary>
        public bool OverchargeArmed { get; private set; }

        /// <summary>The action buttons, around the ship.</summary>
        public IReadOnlyList<ActionButton> Actions => actions;

        /// <summary>True when ending the turn is allowed.</summary>
        public bool CanEndTurn => endTurn.interactable;

        /// <summary>True when leaving the market phase is allowed.</summary>
        public bool CanEndMarket => endMarket.gameObject.activeSelf && endMarket.interactable;

        /// <summary>The decision window (tests choose from it).</summary>
        public CommandPanel Decision => decision;

        /// <summary>The help bubble (tests read the preview in it).</summary>
        public HelpBubble Help => help;

        /// <summary>Connects the controls to a game.</summary>
        /// <param name="context">Theme, texts and cards of the game.</param>
        /// <param name="labels">Words for the decision answers.</param>
        /// <param name="icons">Pictograms of the actions.</param>
        /// <param name="rules">Public rules of the game: the defensive posture button shows only when its option is on, the dice faces are named in previews.</param>
        /// <param name="host">The table: seats under the pointer, commands sent, targets lit, previews.</param>
        public void Bind(TableContext context, CommandLabels labels, IconCatalog icons, GameConfig rules, IControlsHost host)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            TextTable texts = context.Texts;
            _previewText = new PreviewText(context, rules.DieFaces);
            bool postureEnabled = rules.DefensivePostureBonus > 0;
            foreach (ActionButton button in actions)
            {
                button.gameObject.SetActive(button.Action != CrewAction.DefensivePosture || postureEnabled);
                button.Bind(this, icons.Find(IconName(button.Action)), texts.Get(TextKeys.ActionShort(button.Action)));
            }

            comboLabel.text = texts.Get(TextKeys.ButtonCombo);
            endTurnLabel.text = texts.Get(TextKeys.ButtonEndTurn);
            endMarketLabel.text = texts.Get(TextKeys.ButtonEndMarket);
            foreach (Button recycle in new[] { recycleAttack, recycleDefense })
            {
                recycle.GetComponentInChildren<TMP_Text>().text = texts.Get(TextKeys.ButtonRecycle);
            }

            Listen(combo, () => SubmitFirst(c => c.Type == CommandType.ActivateTechnology));
            Listen(endTurn, EndTurn);
            Listen(endMarket, EndMarket);
            Listen(recycleAttack, () => SubmitFirst(c => c.Type == CommandType.RecycleMarket && c.Slot == CardSlot.Attack));
            Listen(recycleDefense, () => SubmitFirst(c => c.Type == CommandType.RecycleMarket && c.Slot == CardSlot.Defense));
            Listen(overcharge, ToggleOvercharge);
            OverchargeArmed = false;
            Withdraw();
        }

        /// <summary>Offers what the engine allows the person at <paramref name="seat"/> now.</summary>
        public void Offer(int seat, GameView view, IReadOnlyList<Command> legal, DecisionRequest? pending)
        {
            _seat = seat;
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _decision = pending;
            _legal.Clear();
            _legal.AddRange(legal ?? throw new ArgumentNullException(nameof(legal)));
            _previews.Clear();
            Offered = true;

            bool turn = pending is null;
            foreach (ActionButton button in actions)
            {
                button.SetAvailable(turn && _legal.Any(c => c.Type == TypeOf(button.Action)));
            }

            PlayerView me = view.Players[seat];
            bool comboReady = turn && Has(c => c.Type == CommandType.ActivateTechnology);
            combo.interactable = comboReady;
            comboBackground.color = comboReady && me.AttackSlot != null ? _context!.Theme.Technology(TechOf(me.AttackSlot)) : Dim(_context!.Theme.MutedText);

            bool canEnd = turn && Has(c => c.Type == CommandType.EndTurn);
            endTurn.interactable = canEnd;
            bool nothingElse = canEnd && _legal.All(c => c.Type == CommandType.EndTurn);
            endTurnBackground.color = nothingElse ? _context.Theme.Highlight : new Color(1f, 1f, 1f, 0.12f);
            endTurnLabel.color = nothingElse ? new Color32(40, 30, 10, 255) : _context.Theme.Text;

            endMarket.gameObject.SetActive(turn && Has(c => c.Type == CommandType.EndMarket));
            endMarket.interactable = true;
            recycleAttack.gameObject.SetActive(turn && Has(c => c.Type == CommandType.RecycleMarket && c.Slot == CardSlot.Attack));
            recycleDefense.gameObject.SetActive(turn && Has(c => c.Type == CommandType.RecycleMarket && c.Slot == CardSlot.Defense));

            bool armable = turn && me.Overcharge > 0 && Has(c => c.UseOvercharge);
            overcharge.interactable = armable;
            OverchargeArmed &= armable;
            overchargeGlow.enabled = OverchargeArmed;

            if (pending != null)
            {
                decision.Show(view.Players[seat].Name + " : " + _labels!.Question(pending), _legal.Select(c => (_labels.Describe(c, view, pending), (Action)(() => Send(c)))).ToList());
            }
            else
            {
                decision.Hide();
            }
        }

        /// <summary>Takes everything back while others play or events are shown.</summary>
        public void Withdraw()
        {
            Offered = false;
            _legal.Clear();
            _previews.Clear();
            _decision = null;
            foreach (ActionButton button in actions)
            {
                button.SetAvailable(false);
            }

            combo.interactable = false;
            endTurn.interactable = false;
            endMarket.gameObject.SetActive(false);
            recycleAttack.gameObject.SetActive(false);
            recycleDefense.gameObject.SetActive(false);
            overcharge.interactable = false;
            overchargeGlow.enabled = OverchargeArmed;
            decision.Hide();
            CancelAim();
            HideHelp();
        }

        /// <summary>Uses an action without a target (tap): reroll, overcharge, defensive posture.</summary>
        public bool UseAction(CrewAction action) => Send(Pick(_legal, TypeOf(action), -1, OverchargeArmed));

        /// <summary>Aims an action at the opponent of <paramref name="target"/> (the drop of a drag); false when not allowed.</summary>
        /// <remarks>A drop on no seat (-1) is refused: it never falls back on any target.</remarks>
        public bool UseActionOn(CrewAction action, int target) => target >= 0 && Send(Pick(_legal, TypeOf(action), target, OverchargeArmed));

        /// <summary>Starts aiming an action at an opponent: the seats it may target light up.</summary>
        public void BeginAim(CrewAction action, RectTransform from, Vector2 screen)
        {
            _aiming = action;
            _aimFrom = from;
            _aimedAt = -1;
            HideHelp();
            _host!.ShowTargets(_legal.Where(c => c.Type == TypeOf(action)).Select(c => c.Target).Distinct().ToList());
            Aim(screen);
        }

        /// <summary>
        /// Follows the pointer while aiming: a line from the action to the pointer, and over a seat the action may target,
        /// what it would likely do there (ADR-0018).
        /// </summary>
        public void Aim(Vector2 screen)
        {
            if (_aiming is null || _aimFrom == null)
            {
                return;
            }

            AimAt(_host!.SeatAt(screen));

            Rect from = CardAnchor.ScreenRectOf(_aimFrom);
            var parent = (RectTransform)aimLine.parent;
            Canvas canvas = parent.GetComponentInParent<Canvas>().rootCanvas;
            Camera? interfaceCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, from.center, interfaceCamera, out Vector2 start);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, interfaceCamera, out Vector2 end);
            Vector2 along = end - start;
            aimLine.gameObject.SetActive(true);
            aimLine.localPosition = start;
            aimLine.sizeDelta = new Vector2(along.magnitude, aimLine.sizeDelta.y);
            aimLine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
        }

        /// <summary>Ends the aim: the action goes to the seat under the pointer when the engine allows it.</summary>
        public bool EndAim(Vector2 screen)
        {
            CrewAction? action = _aiming;
            CancelAim();
            return action.HasValue && _host != null && UseActionOn(action.Value, _host.SeatAt(screen));
        }

        /// <summary>
        /// The preview of the aimed action on <paramref name="target"/> (shown in the help bubble next to its panel), or
        /// null when the action may not go there. Computed once per offer and command.
        /// </summary>
        public string? PreviewOn(CrewAction action, int target)
        {
            Command? command = target >= 0 ? Pick(_legal, TypeOf(action), target, OverchargeArmed) : null;
            if (command is null || _host is null || _view is null || _previewText is null)
            {
                return null;
            }

            if (!_previews.TryGetValue(command, out string? text))
            {
                text = _previewText.Describe(_host.Preview(_seat, command), command, _view, _seat);
                _previews[command] = text;
            }

            return text;
        }

        /// <summary>Whether the market card at <paramref name="index"/> can be bought now (dragged).</summary>
        public bool CanBuy(CardSlot slot, int index) => Has(c => c.Type == CommandType.PickMarket && c.Slot == slot && c.MarketIndex == index);

        /// <summary>Buys a market card dropped on the player's side of the table; false when dropped elsewhere or not allowed.</summary>
        public bool Buy(CardSlot slot, int index, Vector2 screen) =>
            Contains(purchaseZone, screen) && SubmitFirst(c => c.Type == CommandType.PickMarket && c.Slot == slot && c.MarketIndex == index);

        /// <summary>Whether the player's card <paramref name="uid"/> can be used now (dragged to the middle).</summary>
        public bool CanUse(int uid) => Has(c => c.Type == CommandType.ActivateCard && c.CardUid == uid);

        /// <summary>Uses a card of the player dropped in the middle of the table; false when dropped elsewhere or not allowed.</summary>
        public bool UseCard(int uid, Vector2 screen) =>
            Contains(activationZone, screen) && SubmitFirst(c => c.Type == CommandType.ActivateCard && c.CardUid == uid);

        /// <summary>Arms or disarms the overcharge token (ARB-67).</summary>
        public void ToggleOvercharge()
        {
            if (!overcharge.interactable)
            {
                return;
            }

            OverchargeArmed = !OverchargeArmed;
            overchargeGlow.enabled = OverchargeArmed;
        }

        /// <summary>Ends the turn (button).</summary>
        public void EndTurn() => SubmitFirst(c => c.Type == CommandType.EndTurn);

        /// <summary>Leaves the market phase (button).</summary>
        public void EndMarket() => SubmitFirst(c => c.Type == CommandType.EndMarket);

        /// <summary>Shows the help of an action.</summary>
        public void ShowActionHelp(CrewAction action, RectTransform about) => help.Show(_context!.Texts.Get(TextKeys.ActionHelp(action)), about);

        /// <summary>Hides the help.</summary>
        public void HideHelp() => help.Hide();

        /// <summary>
        /// The legal command of a type for a target (-1: an action without target), spending the overcharge when armed. An armed token is
        /// never spent silently: when no command spends it, the one that keeps it is chosen, and the other way round.
        /// </summary>
        public static Command? Pick(IEnumerable<Command> legal, CommandType type, int target, bool armed)
        {
            List<Command> matching = legal.Where(c => c.Type == type && (target < 0 || c.Target == target)).ToList();
            return matching.FirstOrDefault(c => c.UseOvercharge == armed) ?? matching.FirstOrDefault();
        }

        /// <summary>Command type of a crew action.</summary>
        public static CommandType TypeOf(CrewAction action) => action switch
        {
            CrewAction.Attack => CommandType.Attack,
            CrewAction.Sabotage => CommandType.Sabotage,
            CrewAction.RerollShield => CommandType.RerollShield,
            CrewAction.Overcharge => CommandType.Overcharge,
            _ => CommandType.DefensivePosture,
        };

        /// <summary>Name of the pictogram of an action in the icon catalog (docs/ASSETS.md).</summary>
        public static string IconName(CrewAction action) => action switch
        {
            CrewAction.Attack => "Action_Attaque",
            CrewAction.Sabotage => "Action_Sabotage",
            CrewAction.RerollShield => "Action_Reparametrage",
            CrewAction.Overcharge => "Action_Surcharge",
            _ => "Action_Posture",
        };

        /// <summary>Wires the parts of the layout (editor setup).</summary>
        public void Assign(ActionButton[] actionButtons, Button comboButton, Image comboImage, TMP_Text comboText, Button endTurnButton, Image endTurnImage, TMP_Text endTurnText, Button endMarketButton, TMP_Text endMarketText, Button recycleAttackButton, Button recycleDefenseButton, Button overchargeButton, Graphic overchargeArmed, CommandPanel decisionWindow, HelpBubble helpBubble, RectTransform line, RectTransform purchase, RectTransform activation)
        {
            actions = actionButtons;
            combo = comboButton;
            comboBackground = comboImage;
            comboLabel = comboText;
            endTurn = endTurnButton;
            endTurnBackground = endTurnImage;
            endTurnLabel = endTurnText;
            endMarket = endMarketButton;
            endMarketLabel = endMarketText;
            recycleAttack = recycleAttackButton;
            recycleDefense = recycleDefenseButton;
            overcharge = overchargeButton;
            overchargeGlow = overchargeArmed;
            decision = decisionWindow;
            help = helpBubble;
            aimLine = line;
            purchaseZone = purchase;
            activationZone = activation;
        }

        private static Color Dim(Color color) => new Color(color.r, color.g, color.b, 0.25f);

        private static void Listen(Button button, Action action)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }

        private static bool Contains(RectTransform zone, Vector2 screen) => CardAnchor.ScreenRectOf(zone).Contains(screen);

        private TechColor TechOf(CardView card) => _context!.Face(card.CardId).Color;

        private bool Has(Func<Command, bool> predicate) => Offered && _legal.Any(predicate);

        private bool SubmitFirst(Func<Command, bool> predicate) => Send(Offered ? _legal.FirstOrDefault(predicate) : null);

        private bool Send(Command? command)
        {
            if (command is null || !Offered || _host is null)
            {
                return false;
            }

            _host.Submit(_seat, command);
            return true;
        }

        // Over a new seat while aiming: its preview next to its panel, or nothing.
        private void AimAt(int target)
        {
            if (target == _aimedAt || _aiming is null)
            {
                return;
            }

            _aimedAt = target;
            string? text = PreviewOn(_aiming.Value, target);
            RectTransform? panel = text is null ? null : _host!.SeatPanel(target);
            if (text is null || panel == null)
            {
                HideHelp();
                return;
            }

            help.Show(text, panel);
        }

        private void CancelAim()
        {
            _aiming = null;
            _aimFrom = null;
            _aimedAt = -1;
            aimLine.gameObject.SetActive(false);
            _host?.ShowTargets(null);
            HideHelp();
        }
    }
}
