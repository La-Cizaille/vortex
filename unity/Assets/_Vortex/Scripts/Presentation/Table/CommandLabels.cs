using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Client.Content;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Projection;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Words for the commands and decision answers offered by the engine, used by the test mode's command panel. The
    /// texts come from the interface texts (<c>command.*</c>, <c>decision.*</c>, <c>option.*</c>); names come from the
    /// public view and the content, so nothing hidden is ever described.
    /// </summary>
    public sealed class CommandLabels
    {
        private readonly TableContext _context;

        /// <summary>Creates the labels of a game.</summary>
        public CommandLabels(TableContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

        /// <summary>The question of a decision.</summary>
        public string Question(DecisionRequest decision)
        {
            if (decision is null)
            {
                throw new ArgumentNullException(nameof(decision));
            }

            string question = _context.Texts.Get(TextKeys.Decision(decision.Prompt));
            return decision.SourceId is null ? question : question + " (" + _context.Face(decision.SourceId).Title + ")";
        }

        /// <summary>What a command does, in a few words.</summary>
        public string Describe(Command command, GameView view, DecisionRequest? decision)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (view is null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            switch (command.Type)
            {
                case CommandType.PickMarket:
                    MarketView market = view.Market(command.Slot);
                    string card = command.MarketIndex >= 0 && command.MarketIndex < market.Visible.Count ? CardName(market.Visible[command.MarketIndex].CardId) : "?";
                    return Format(TextKeys.CommandPick, card, Slot(command.Slot));
                case CommandType.RecycleMarket:
                    return Format(TextKeys.CommandRecycle, Slot(command.Slot));
                case CommandType.EndMarket:
                    return Text(TextKeys.CommandEndMarket);
                case CommandType.ActivateCard:
                    return Format(TextKeys.CommandActivate, CardName(view, command.CardUid));
                case CommandType.ActivateTechnology:
                    return Text(TextKeys.CommandTechnology);
                case CommandType.Attack:
                    return Format(command.UseOvercharge ? TextKeys.CommandAttackOvercharged : TextKeys.CommandAttack, PlayerName(view, command.Target));
                case CommandType.RerollShield:
                    return Text(command.UseOvercharge ? TextKeys.CommandRerollOvercharged : TextKeys.CommandReroll);
                case CommandType.Sabotage:
                    return Format(TextKeys.CommandSabotage, PlayerName(view, command.Target));
                case CommandType.Overcharge:
                    return Text(TextKeys.CommandOvercharge);
                case CommandType.DefensivePosture:
                    return Text(TextKeys.CommandPosture);
                case CommandType.EndTurn:
                    return Text(TextKeys.CommandEndTurn);
                case CommandType.AnswerDecision:
                    DecisionOption? option = decision?.Options.FirstOrDefault(o => o.Key == command.Option);
                    return option is null ? command.Option ?? "?" : Option(option, view);
                default:
                    return command.Type.ToString();
            }
        }

        private string Option(DecisionOption option, GameView view)
        {
            if (option.Player >= 0)
            {
                return PlayerName(view, option.Player);
            }

            if (option.CardUid >= 0)
            {
                return CardName(view, option.CardUid);
            }

            if (option.ContentId != null)
            {
                return CardName(option.ContentId);
            }

            string key = TextKeys.Option(option.Key);
            return _context.Texts.Contains(key) ? Text(key) : option.Number.ToString(CultureInfo.InvariantCulture);
        }

        // A card of the public view by uid: its name and where it is (a player's slot or a market).
        private string CardName(GameView view, int uid)
        {
            foreach (PlayerView player in view.Players)
            {
                foreach (CardView? card in new[] { player.AttackSlot, player.DefenseSlot })
                {
                    if (card != null && card.Uid == uid)
                    {
                        return CardName(card.CardId) + " (" + player.Name + ")";
                    }
                }
            }

            foreach (CardView card in view.AttackMarket.Visible.Concat(view.DefenseMarket.Visible))
            {
                if (card.Uid == uid)
                {
                    return CardName(card.CardId) + " (" + Text(TextKeys.OptionMarket) + ")";
                }
            }

            return "#" + uid.ToString(CultureInfo.InvariantCulture);
        }

        private string CardName(string id) => _context.Face(id).Title;

        private static string PlayerName(GameView view, int seat) => seat >= 0 && seat < view.Players.Count ? view.Players[seat].Name : "?";

        private string Slot(CardSlot slot) => Text(slot == CardSlot.Attack ? TextKeys.SlotAttack : TextKeys.SlotDefense);

        private string Text(string key) => _context.Texts.Get(key);

        private string Format(string key, params object[] values)
        {
            try
            {
                return string.Format(CultureInfo.InvariantCulture, Text(key), values);
            }
            catch (FormatException)
            {
                return "#" + key;
            }
        }
    }
}
