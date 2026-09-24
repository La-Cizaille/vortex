using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Effects;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Core.Rules
{
    /// <summary>Validation and execution of player commands (RULES A5).</summary>
    internal sealed partial class Game
    {
        /// <summary>Checks a command against the current state without changing anything visible to the game.</summary>
        public CommandError? Validate(int player, Command command)
        {
            if (IsOver)
            {
                return Error(CommandErrorCode.GameOver, "The game is over.");
            }

            if (player < 0 || player >= State.Players.Count)
            {
                return Error(CommandErrorCode.NotYourTurn, "Unknown player.");
            }

            if (command.Type == CommandType.AnswerDecision)
            {
                return Error(CommandErrorCode.NoDecisionPending, "No decision is pending.");
            }

            if (player != State.CurrentPlayer)
            {
                return Error(CommandErrorCode.NotYourTurn, "It is not this player's turn.");
            }

            switch (command.Type)
            {
                case CommandType.PickMarket:
                    if (State.Phase != TurnPhase.Market)
                    {
                        return WrongPhase();
                    }

                    if (State.MarketPicksLeft <= 0)
                    {
                        return Error(CommandErrorCode.NoPicksLeft, "No market pick left.");
                    }

                    if (!Enum.IsDefined(typeof(CardSlot), command.Slot) || command.MarketIndex < 0 || command.MarketIndex >= State.Market(command.Slot).Visible.Count)
                    {
                        return Error(CommandErrorCode.InvalidMarketCard, "No such market card.");
                    }

                    return null;

                case CommandType.RecycleMarket:
                    if (State.Phase != TurnPhase.Market)
                    {
                        return WrongPhase();
                    }

                    if (!Enum.IsDefined(typeof(CardSlot), command.Slot))
                    {
                        return Error(CommandErrorCode.MalformedCommand, "Unknown market.");
                    }

                    return State.PickedThisTurn ? Error(CommandErrorCode.CannotRecycleAfterPick, "A market cannot be recycled after taking a card.") : null;

                case CommandType.EndMarket:
                    return State.Phase == TurnPhase.Market ? null : WrongPhase();

                case CommandType.ActivateCard:
                    return State.Phase == TurnPhase.Main ? ValidateActivation(player, command.CardUid) : WrongPhase();

                case CommandType.ActivateTechnology:
                    if (State.Phase != TurnPhase.Main)
                    {
                        return WrongPhase();
                    }

                    return TechnologyComboOf(player) == null ? Error(CommandErrorCode.NoTechnologyCombo, "Equipped modifiers are not of the same non-neutral colour.") : null;

                case CommandType.Attack:
                case CommandType.RerollShield:
                case CommandType.Sabotage:
                case CommandType.Overcharge:
                case CommandType.DefensivePosture:
                    return State.Phase == TurnPhase.Main ? ValidateCrewAction(player, command, ignoreForcedAction: false) : WrongPhase();

                case CommandType.EndTurn:
                    if (State.Phase != TurnPhase.Main)
                    {
                        return WrongPhase();
                    }

                    StatusState? forced = ForcedActionOf(player);
                    if (forced != null && ValidateCrewAction(player, ForcedCommand(forced), ignoreForcedAction: true) == null)
                    {
                        return Error(CommandErrorCode.ForcedActionRequired, "The imposed crew action must be performed before ending the turn.");
                    }

                    return null;

                default:
                    return Error(CommandErrorCode.MalformedCommand, "Unknown command type.");
            }
        }

        /// <summary>Executes a validated command.</summary>
        public void Execute(int player, Command command)
        {
            switch (command.Type)
            {
                case CommandType.PickMarket:
                    TakeFromMarket(player, command.Slot, command.MarketIndex, null);
                    State.PickedThisTurn = true;
                    State.MarketPicksLeft--;
                    CheckEliminations();
                    if (!IsOver && State.MarketPicksLeft <= 0 && State.Phase == TurnPhase.Market)
                    {
                        EndMarketPhase();
                    }

                    break;

                case CommandType.RecycleMarket:
                    RecycleMarket(command.Slot);
                    EndMarketPhase();
                    break;

                case CommandType.EndMarket:
                    EndMarketPhase();
                    break;

                case CommandType.ActivateCard:
                    ActivateCard(player, command.CardUid);
                    break;

                case CommandType.ActivateTechnology:
                    ActivateTechnology(player);
                    break;

                case CommandType.Attack:
                case CommandType.RerollShield:
                case CommandType.Sabotage:
                case CommandType.Overcharge:
                case CommandType.DefensivePosture:
                    PerformCrewAction(player, command);
                    break;

                case CommandType.EndTurn:
                    EndTurn();
                    break;

                default:
                    throw new EngineException("Execute called with an unvalidated command.");
            }

            // A player eliminated during their own turn (e.g. a reflected loss) ends it at once.
            if (!IsOver && State.Players[State.CurrentPlayer].Eliminated)
            {
                EndTurn();
            }
        }

        // ---------------------------------------------------------------- Activation (RULES A5.3, A8)

        /// <summary>Activation effects of a card.</summary>
        public IEnumerable<Effect> ActivationEffects(string cardId) => Catalog.ForCard(cardId).Where(e => e.IsActivation);

        private CommandError? ValidateActivation(int player, int cardUid)
        {
            CardInstance? card = State.Players[player].Modifiers().FirstOrDefault(c => c.Uid == cardUid);
            if (card == null)
            {
                return Error(CommandErrorCode.InvalidCard, "The card is not equipped by this player.");
            }

            List<Effect> effects = ActivationEffects(card.CardId).ToList();
            var source = new EffectSource(EffectOrigin.Card, card.CardId, player, cardUid: card.Uid);
            if (effects.Count == 0 || effects.Any(e => !e.CanActivate(this, source)))
            {
                return Error(CommandErrorCode.CardNotActivatable, "The card cannot be activated now.");
            }

            return null;
        }

        /// <summary>Runs a card's activation, then discards it if it is still in its slot (RULES A5.3).</summary>
        private void ActivateCard(int player, int cardUid)
        {
            CardInstance card = State.Players[player].Modifiers().First(c => c.Uid == cardUid);
            var source = new EffectSource(EffectOrigin.Card, card.CardId, player, cardUid: card.Uid);
            Emit(new GameEvent { Type = GameEventType.CardActivated, Player = player, CardUid = card.Uid, Id = card.CardId });
            foreach (Effect effect in ActivationEffects(card.CardId).ToList())
            {
                if (IsOver)
                {
                    return;
                }

                effect.Activate(this, source);
            }

            PlayerState p = State.Players[player];
            if (!p.Eliminated && p.Modifiers().Any(c => c.Uid == cardUid))
            {
                DiscardModifier(player, Definition(card).Slot, source);
            }

            CheckEliminations();
        }

        /// <summary>Colour of the player's technology combo, or null (RULES A8).</summary>
        public TechColor? TechnologyComboOf(int player)
        {
            PlayerState p = State.Players[player];
            if (p.AttackSlot == null || p.DefenseSlot == null)
            {
                return null;
            }

            TechColor a = Definition(p.AttackSlot).Color;
            return a != TechColor.Neutral && a == Definition(p.DefenseSlot).Color ? a : (TechColor?)null;
        }

        private void ActivateTechnology(int player)
        {
            TechColor color = TechnologyComboOf(player)!.Value;
            TechnologyDefinition tech = Data.Technologies.First(t => t.Color == color);
            var source = new EffectSource(EffectOrigin.Technology, tech.Id, player);

            // 1. Both modifiers are discarded without their single-use effect.
            DiscardAllModifiers(player, source);
            Emit(new GameEvent { Type = GameEventType.TechnologyActivated, Player = player, Value = (int)color, Id = tech.Id });

            // 2. Technology effect.
            foreach (Effect effect in Catalog.ForTechnology(color))
            {
                if (IsOver)
                {
                    return;
                }

                if (effect.IsActivation)
                {
                    effect.Activate(this, source);
                }
            }

            // 3. Marked as obtained; the galactic election is won with the configured number of technologies (RULES A9).
            PlayerState p = State.Players[player];
            if (!p.Technologies.Contains(color))
            {
                p.Technologies.Add(color);
            }

            CheckEliminations();
            if (!IsOver && !p.Eliminated && p.Technologies.Count >= Config.TechnologiesToWin)
            {
                EndGame(player, WinCondition.GalacticElection);
            }
        }

        // ---------------------------------------------------------------- Crew actions (RULES A5.4)

        /// <summary>Number of crew actions allowed this turn (calculation "actions d'équipage").</summary>
        public int CrewActionsAllowed(int player)
        {
            return Math.Max(0, Calculate(1, (e, s, m) => e.ModifyCrewActions(this, s, player, m)));
        }

        /// <summary>False for a crew action that exists only when a rule option enables it, and it is disabled.</summary>
        public bool IsCrewActionEnabled(CrewAction action)
        {
            return action != CrewAction.DefensivePosture || Config.DefensivePostureBonus > 0;
        }

        /// <summary>Imposed crew action of a player (active status), or null.</summary>
        public StatusState? ForcedActionOf(int player)
        {
            return State.Players[player].Statuses.FirstOrDefault(s => s.IsActive && s.Kind == StatusKinds.ForcedCrewAction);
        }

        private static Command ForcedCommand(StatusState forced)
        {
            var action = (CrewAction)forced.Var(StatusKinds.VarAction);
            int target = forced.Var(StatusKinds.VarTarget, -1);
            switch (action)
            {
                case CrewAction.Attack: return Command.Attack(target);
                case CrewAction.RerollShield: return Command.RerollShield();
                case CrewAction.Sabotage: return Command.Sabotage(target);
                case CrewAction.DefensivePosture: return Command.DefensivePosture();
                default: return Command.Overcharge();
            }
        }

        private static CrewAction ActionOf(CommandType type)
        {
            switch (type)
            {
                case CommandType.Attack: return CrewAction.Attack;
                case CommandType.RerollShield: return CrewAction.RerollShield;
                case CommandType.Sabotage: return CrewAction.Sabotage;
                case CommandType.DefensivePosture: return CrewAction.DefensivePosture;
                default: return CrewAction.Overcharge;
            }
        }

        private CommandError? ValidateCrewAction(int player, Command command, bool ignoreForcedAction)
        {
            CrewAction action = ActionOf(command.Type);
            if (!IsCrewActionEnabled(action))
            {
                return Error(CommandErrorCode.ActionNotAvailable, "This crew action is not enabled by the rules.");
            }

            if (State.CrewActionsTaken.Count >= CrewActionsAllowed(player))
            {
                return Error(CommandErrorCode.NoCrewActionLeft, "No crew action left this turn.");
            }

            if (State.CrewActionsTaken.Contains(action))
            {
                return Error(CommandErrorCode.CrewActionAlreadyUsed, "Crew actions in the same turn must be different.");
            }

            if (!ignoreForcedAction && ForcedActionOf(player) is StatusState forced)
            {
                Command required = ForcedCommand(forced);
                bool matches = required.Type == command.Type && (required.Target < 0 || required.Target == command.Target);
                if (!matches)
                {
                    return Error(CommandErrorCode.ForcedActionRequired, "An imposed crew action applies this turn.");
                }
            }

            PlayerState p = State.Players[player];
            switch (command.Type)
            {
                case CommandType.Attack:
                    CommandError? targetError = ValidateOpponentTarget(player, command.Target, CrewAction.Attack);
                    if (targetError != null)
                    {
                        return targetError;
                    }

                    return command.UseOvercharge && p.Overcharge <= 0 ? Error(CommandErrorCode.NoOvercharge, "No overcharge token.") : null;

                case CommandType.RerollShield:
                    return command.UseOvercharge && p.Overcharge <= 0 ? Error(CommandErrorCode.NoOvercharge, "No overcharge token.") : null;

                case CommandType.Sabotage:
                    CommandError? sabotageError = ValidateOpponentTarget(player, command.Target, CrewAction.Sabotage);
                    if (sabotageError != null)
                    {
                        return sabotageError;
                    }

                    return CanChangeShield(player, command.Target) ? null : Error(CommandErrorCode.ShieldChangeNotAllowed, "This shield cannot be modified by this player.");

                case CommandType.DefensivePosture:
                    return null;

                default:
                    return p.Overcharge >= Config.MaxOvercharge ? Error(CommandErrorCode.OverchargeFull, "Overcharge tokens are at the maximum.") : null;
            }
        }

        private CommandError? ValidateOpponentTarget(int player, int target, CrewAction action)
        {
            if (target < 0 || target >= State.Players.Count || target == player || State.Players[target].Eliminated)
            {
                return Error(CommandErrorCode.InvalidTarget, "The target must be an alive opponent.");
            }

            return CanTarget(player, target, action) ? null : Error(CommandErrorCode.TargetNotAllowed, "This target cannot be chosen.");
        }

        private void PerformCrewAction(int player, Command command)
        {
            CrewAction action = ActionOf(command.Type);
            switch (command.Type)
            {
                case CommandType.Attack:
                    ResolveAttack(player, command.Target, command.UseOvercharge);
                    break;

                case CommandType.RerollShield:
                    if (command.UseOvercharge)
                    {
                        ConsumeOvercharge(player);
                    }

                    List<int> dice = ThrowDice(player, command.UseOvercharge ? 2 : 1, usePreRolled: true);
                    Emit(new GameEvent { Type = GameEventType.DiceRolled, Player = player, Values = dice, Amount = dice.Sum() });
                    TrySetShield(player, player, dice.Sum(), null);
                    break;

                case CommandType.Sabotage:
                    List<int> die = ThrowDice(player, 1, usePreRolled: true);
                    Emit(new GameEvent { Type = GameEventType.DiceRolled, Player = player, Values = die, Amount = die[0] });
                    TrySetShield(player, command.Target, die[0], null);
                    break;

                case CommandType.DefensivePosture:
                    // Until the start of the player's next turn (RULES A5.4); a new posture replaces the previous one.
                    RemoveStatuses(s => s.Kind == StatusKinds.DefensivePosture && StatusesOf(player).Contains(s));
                    AddStatus(player, StatusKinds.DefensivePosture, player, StatusExpiry.StartOfTurn, player, vars: new[]
                    {
                        new KeyValuePair<string, int>(StatusKinds.VarAmount, Config.DefensivePostureBonus),
                    });
                    break;

                default:
                    GainOvercharge(player, 1);
                    break;
            }

            State.CrewActionsTaken.Add(action);
            Emit(new GameEvent { Type = GameEventType.CrewActionPerformed, Player = player, Value = (int)action });
            if (ForcedActionOf(player) is StatusState forced && (CrewAction)forced.Var(StatusKinds.VarAction) == action)
            {
                RemoveStatus(player, forced);
            }

            CheckEliminations();
        }

        private static CommandError WrongPhase() => new CommandError(CommandErrorCode.WrongPhase, "Not allowed in the current phase.");

        private static CommandError Error(CommandErrorCode code, string message) => new CommandError(code, message);
    }
}
