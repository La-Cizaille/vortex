using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Effects.Bricks
{
    /// <summary>Base of activation bricks (RULES A5.3): they run once, from a card, an event or a technology.</summary>
    internal abstract class ActivationBrick : Effect
    {
        public override bool IsActivation => true;

        /// <summary>Adds a "this turn / next attack" status to the activating player.</summary>
        protected static void AddTurnStatus(Game game, EffectSource self, string kind, params KeyValuePair<string, int>[] vars)
        {
            game.AddStatus(self.Holder, kind, self.Holder, StatusExpiry.EndOfTurn, self.Holder, vars: vars);
        }

        protected static KeyValuePair<string, int> Var(string name, int value) => new KeyValuePair<string, int>(name, value);

        /// <summary>Alive opponents of the holder that have at least one modifier.</summary>
        protected static List<int> OpponentsWithModifiers(Game game, EffectSource self)
        {
            return game.Opponents(self.Holder).Where(p => game.Player(p).Modifiers().Any()).ToList();
        }
    }

    // ---------------------------------------------------------------- Next attack / this turn

    /// <summary>A chosen opponent's shield is disabled until the end of the holder's turn.</summary>
    internal sealed class DisableOpponentShieldThisTurn : ActivationBrick
    {
        public override string Name => nameof(DisableOpponentShieldThisTurn);

        public override bool CanActivate(Game game, EffectSource self) => game.Opponents(self.Holder).Any();

        public override void Activate(Game game, EffectSource self)
        {
            int target = game.AskPlayer(self.Holder, "disable.shield", self.Id, game.Opponents(self.Holder));
            game.AddStatus(target, BrickStatuses.ShieldDisabled, self.Holder, StatusExpiry.EndOfTurn, self.Holder);
        }
    }

    /// <summary>Converts X shield points (chosen, X ≤ own shield) into +X attack value for the next attack this turn.</summary>
    internal sealed class ConvertShieldToNextAttack : ActivationBrick
    {
        public override string Name => nameof(ConvertShieldToNextAttack);

        public override bool CanActivate(Game game, EffectSource self) => game.ShieldOf(self.Holder) > 0;

        public override void Activate(Game game, EffectSource self)
        {
            int shield = game.ShieldOf(self.Holder);
            int x = game.AskNumber(self.Holder, "convert.shield", self.Id, 1, shield);
            if (game.TrySetShield(self.Holder, self.Holder, shield - x, self))
            {
                AddTurnStatus(game, self, BrickStatuses.NextAttackBonus, Var(BrickStatuses.VarAmount, x));
            }
        }
    }

    /// <summary>×factor to the damage of the next attack this turn.</summary>
    internal sealed class NextAttackDamageMultiplier : ActivationBrick
    {
        private readonly int _factor;

        public NextAttackDamageMultiplier(int factor) => _factor = factor;

        public override string Name => nameof(NextAttackDamageMultiplier);

        public override void Activate(Game game, EffectSource self)
        {
            AddTurnStatus(game, self, BrickStatuses.NextAttackDamageMultiplier, Var(BrickStatuses.VarFactor, _factor));
        }
    }

    /// <summary>+amount per neutral face-up market card (counted at the attack) to the next attack this turn.</summary>
    internal sealed class NextAttackBonusPerNeutral : ActivationBrick
    {
        private readonly int _amount;

        public NextAttackBonusPerNeutral(int amount) => _amount = amount;

        public override string Name => nameof(NextAttackBonusPerNeutral);

        public override void Activate(Game game, EffectSource self)
        {
            AddTurnStatus(game, self, BrickStatuses.NextAttackBonusPerNeutral, Var(BrickStatuses.VarAmount, _amount));
        }
    }

    /// <summary>The next attack this turn is overcharged without consuming a token.</summary>
    internal sealed class NextAttackOvercharged : ActivationBrick
    {
        public override string Name => nameof(NextAttackOvercharged);

        public override void Activate(Game game, EffectSource self)
        {
            AddTurnStatus(game, self, BrickStatuses.NextAttackOvercharged);
        }
    }

    /// <summary>Discards the holder's modifiers, then the next attack this turn is overcharged with <c>dice</c> dice keeping <c>keep</c>.</summary>
    internal sealed class AllInAttack : ActivationBrick
    {
        private readonly int _dice;
        private readonly int _keep;

        public AllInAttack(int dice, int keep)
        {
            _dice = dice;
            _keep = keep;
        }

        public override string Name => nameof(AllInAttack);

        public override void Activate(Game game, EffectSource self)
        {
            game.DiscardAllModifiers(self.Holder, self);
            AddTurnStatus(game, self, BrickStatuses.NextAttackAllIn, Var(BrickStatuses.VarDice, _dice), Var(BrickStatuses.VarKeep, _keep));
        }
    }

    /// <summary>After the next attack this turn, the holder may exchange one of the target's modifiers (see the status).</summary>
    internal sealed class SwapOnNextAttack : ActivationBrick
    {
        public override string Name => nameof(SwapOnNextAttack);

        public override void Activate(Game game, EffectSource self)
        {
            AddTurnStatus(game, self, BrickStatuses.SwapOnNextAttack);
        }
    }

    // ---------------------------------------------------------------- Modifiers of other players

    /// <summary>Discards both modifiers of a chosen opponent.</summary>
    internal sealed class DiscardOpponentModifiers : ActivationBrick
    {
        public override string Name => nameof(DiscardOpponentModifiers);

        public override bool CanActivate(Game game, EffectSource self) => OpponentsWithModifiers(game, self).Count > 0;

        public override void Activate(Game game, EffectSource self)
        {
            int target = game.AskPlayer(self.Holder, "discard.opponent", self.Id, OpponentsWithModifiers(game, self));
            game.DiscardAllModifiers(target, self);
        }
    }

    /// <summary>Discards the holder's modifiers, then takes both modifiers of a chosen opponent (tokens follow).</summary>
    internal sealed class StealOpponentModifiers : ActivationBrick
    {
        public override string Name => nameof(StealOpponentModifiers);

        public override bool CanActivate(Game game, EffectSource self) => OpponentsWithModifiers(game, self).Count > 0;

        public override void Activate(Game game, EffectSource self)
        {
            int target = game.AskPlayer(self.Holder, "steal.opponent", self.Id, OpponentsWithModifiers(game, self));
            game.DiscardAllModifiers(self.Holder, self);
            foreach (CardSlot slot in new[] { CardSlot.Attack, CardSlot.Defense })
            {
                if (game.Player(target).Slot(slot) != null)
                {
                    game.MoveModifier(target, slot, self.Holder, self);
                }
            }
        }
    }

    /// <summary>
    /// Chooses one modifier of an opponent, then steals it (into the holder's matching slot) or destroys it. Both answers
    /// name the card; "steal" also names the holder, where the card goes (a client offers them as places to drop it).
    /// </summary>
    internal sealed class StealOrDestroyOpponentModifier : ActivationBrick
    {
        public override string Name => nameof(StealOrDestroyOpponentModifier);

        public override bool CanActivate(Game game, EffectSource self) => OpponentsWithModifiers(game, self).Count > 0;

        public override void Activate(Game game, EffectSource self)
        {
            int target = game.AskPlayer(self.Holder, "steal.opponent", self.Id, OpponentsWithModifiers(game, self));
            CardInstance card = game.AskCard(self.Holder, DecisionKind.ChooseModifier, "steal.card", self.Id, game.Player(target).Modifiers().ToList())!;
            CardSlot slot = game.Definition(card).Slot;
            var options = new List<DecisionOption>
            {
                new DecisionOption { Key = "steal", CardUid = card.Uid, Player = self.Holder },
                new DecisionOption { Key = "destroy", CardUid = card.Uid },
            };
            if (game.Ask(self.Holder, DecisionKind.ChooseOption, "steal.or.destroy", self.Id, options) == "steal")
            {
                game.MoveModifier(target, slot, self.Holder, self);
            }
            else
            {
                game.DiscardModifier(target, slot, self);
            }
        }
    }

    // ---------------------------------------------------------------- Markets

    /// <summary>Recycles the given market, then the holder takes one card from it.</summary>
    internal sealed class RefreshMarketAndPick : ActivationBrick
    {
        private readonly CardSlot _market;

        public RefreshMarketAndPick(CardSlot market) => _market = market;

        public override string Name => nameof(RefreshMarketAndPick);

        public override void Activate(Game game, EffectSource self)
        {
            game.RecycleMarket(_market);
            List<CardInstance> visible = game.State.Market(_market).Visible.ToList();
            CardInstance? card = game.AskCard(self.Holder, DecisionKind.ChooseMarketCard, "market.pick", self.Id, visible);
            if (card != null)
            {
                game.TakeFromMarket(self.Holder, _market, game.State.Market(_market).Visible.IndexOf(card), self);
            }
        }
    }

    /// <summary>Recycles both markets.</summary>
    internal sealed class RefreshAllMarkets : ActivationBrick
    {
        public override string Name => nameof(RefreshAllMarkets);

        public override void Activate(Game game, EffectSource self)
        {
            game.RecycleMarket(CardSlot.Attack);
            game.RecycleMarket(CardSlot.Defense);
        }
    }

    // ---------------------------------------------------------------- Torments

    /// <summary>
    /// A chosen opponent and their two alive neighbours (never the holder) receive <c>count</c> token(s) on each of
    /// their modifiers.
    /// </summary>
    internal sealed class TormentOpponentAndNeighbours : ActivationBrick
    {
        private readonly int _count;

        public TormentOpponentAndNeighbours(int count) => _count = count;

        public override string Name => nameof(TormentOpponentAndNeighbours);

        public override bool CanActivate(Game game, EffectSource self) => game.Opponents(self.Holder).Any();

        public override void Activate(Game game, EffectSource self)
        {
            int target = game.AskPlayer(self.Holder, "torment.target", self.Id, game.Opponents(self.Holder));
            var victims = new[] { target, game.Neighbour(target, 1), game.Neighbour(target, -1) }
                .Where(p => p >= 0 && p != self.Holder)
                .Distinct()
                .ToList();
            foreach (int p in game.AliveInTurnOrder().Where(victims.Contains).ToList())
            {
                foreach (CardInstance card in game.Player(p).Modifiers().ToList())
                {
                    for (int i = 0; i < _count; i++)
                    {
                        game.PlaceTorment(self.Holder, card, self);
                    }
                }
            }
        }
    }

    /// <summary>Every token on equipped modifiers inflicts its loss again (RULES A7).</summary>
    internal sealed class ReactivateTorments : ActivationBrick
    {
        public override string Name => nameof(ReactivateTorments);

        public override void Activate(Game game, EffectSource self) => game.ReactivateTorments(self);
    }

    /// <summary>Removes every token (players and markets); the holder heals <c>amount</c> per token removed.</summary>
    internal sealed class ClearAllTormentsHealPer : ActivationBrick
    {
        private readonly int _amount;

        public ClearAllTormentsHealPer(int amount) => _amount = amount;

        public override string Name => nameof(ClearAllTormentsHealPer);

        public override void Activate(Game game, EffectSource self)
        {
            int removed = 0;
            foreach (int p in game.AliveInTurnOrder().ToList())
            {
                foreach (CardInstance card in game.Player(p).Modifiers().ToList())
                {
                    removed += game.RemoveTorments(card);
                }
            }

            foreach (CardInstance card in game.VisibleMarketCards())
            {
                removed += game.RemoveTorments(card);
            }

            game.Heal(self.Holder, removed * _amount, self);
        }
    }

    /// <summary>Each alive player receives <c>count</c> token(s) on each equipped modifier.</summary>
    internal sealed class TormentAllEquipped : ActivationBrick
    {
        private readonly int _count;

        public TormentAllEquipped(int count) => _count = count;

        public override string Name => nameof(TormentAllEquipped);

        public override void Activate(Game game, EffectSource self)
        {
            foreach (int p in game.AliveInTurnOrder().ToList())
            {
                foreach (CardInstance card in game.Player(p).Modifiers().ToList())
                {
                    for (int i = 0; i < _count; i++)
                    {
                        game.PlaceTorment(self.Holder, card, self);
                    }
                }
            }
        }
    }

    /// <summary>Removes every token from the players' equipped modifiers.</summary>
    internal sealed class ClearPlayerTorments : ActivationBrick
    {
        public override string Name => nameof(ClearPlayerTorments);

        public override void Activate(Game game, EffectSource self)
        {
            foreach (int p in game.AliveInTurnOrder().ToList())
            {
                foreach (CardInstance card in game.Player(p).Modifiers().ToList())
                {
                    game.RemoveTorments(card);
                }
            }
        }
    }

    /// <summary>+amount to the value of every torment token for the rest of the game (global, cumulative).</summary>
    internal sealed class TormentValueBonus : ActivationBrick
    {
        private readonly int _amount;

        public TormentValueBonus(int amount) => _amount = amount;

        public override string Name => nameof(TormentValueBonus);

        public override void Activate(Game game, EffectSource self)
        {
            game.AddStatus(-1, BrickStatuses.TormentValueBonus, self.Holder, StatusExpiry.Permanent, -1, vars: new[] { Var(BrickStatuses.VarAmount, _amount) });
        }
    }

    // ---------------------------------------------------------------- Shields

    /// <summary>Converts X HP (chosen; never down to 0, never above the shield bound) into X shield points.</summary>
    internal sealed class ConvertHpToShield : ActivationBrick
    {
        public override string Name => nameof(ConvertHpToShield);

        public override bool CanActivate(Game game, EffectSource self) => MaxConvertible(game, self) > 0;

        public override void Activate(Game game, EffectSource self)
        {
            int x = game.AskNumber(self.Holder, "convert.hp", self.Id, 1, MaxConvertible(game, self));
            game.LoseHp(new HpLossInfo(self.Holder, x, HpLossCause.Self, self.Holder, self, null));
            game.TryAddShield(self.Holder, self.Holder, x, self);
        }

        private static int MaxConvertible(Game game, EffectSource self)
        {
            int room = game.ShieldBoundsOf(self.Holder).Max - game.ShieldOf(self.Holder);
            return System.Math.Min(game.Player(self.Holder).Hp - 1, room);
        }
    }

    /// <summary>Exchanges the shields of two chosen ships (both changes must be permitted).</summary>
    internal sealed class SwapTwoShields : ActivationBrick
    {
        public override string Name => nameof(SwapTwoShields);

        public override void Activate(Game game, EffectSource self)
        {
            List<int> alive = game.AliveInTurnOrder().ToList();
            int first = game.AskPlayer(self.Holder, "swap.shield.first", self.Id, alive);
            int second = game.AskPlayer(self.Holder, "swap.shield.second", self.Id, alive.Where(p => p != first));
            game.TrySwapShields(self.Holder, first, second, self);
        }
    }

    /// <summary>Sets the holder's shield to <c>value</c> (bounded).</summary>
    internal sealed class SetOwnShield : ActivationBrick
    {
        private readonly int _value;

        public SetOwnShield(int value) => _value = value;

        public override string Name => nameof(SetOwnShield);

        public override void Activate(Game game, EffectSource self) => game.TrySetShield(self.Holder, self.Holder, _value, self);
    }

    /// <summary>Sets every alive player's shield to <c>value</c> (source: the holder, or the game for an event).</summary>
    internal sealed class SetAllShields : ActivationBrick
    {
        private readonly int _value;

        public SetAllShields(int value) => _value = value;

        public override string Name => nameof(SetAllShields);

        public override void Activate(Game game, EffectSource self)
        {
            foreach (int p in game.AliveInTurnOrder().ToList())
            {
                game.TrySetShield(self.Holder, p, _value, self);
            }
        }
    }

    /// <summary>The holder's shield is disabled until the start of their next turn.</summary>
    internal sealed class DisableOwnShieldUntilNextTurn : ActivationBrick
    {
        public override string Name => nameof(DisableOwnShieldUntilNextTurn);

        public override void Activate(Game game, EffectSource self)
        {
            game.AddStatus(self.Holder, BrickStatuses.ShieldDisabled, self.Holder, StatusExpiry.StartOfTurn, self.Holder);
        }
    }

    /// <summary>
    /// Every shield moves one seat in the chosen direction among the alive players whose shield the holder may
    /// modify; the others keep theirs and are skipped. Each direction names the seat that would receive the holder's
    /// shield (a client offers that neighbour to choose the direction).
    /// </summary>
    internal sealed class RotateShields : ActivationBrick
    {
        public override string Name => nameof(RotateShields);

        public override bool CanActivate(Game game, EffectSource self) => Participants(game, self).Count >= 2;

        public override void Activate(Game game, EffectSource self)
        {
            List<int> seats = Participants(game, self);
            var options = new List<DecisionOption>
            {
                new DecisionOption { Key = "clockwise", Number = 1, Player = Receiver(seats, self.Holder, clockwise: true) },
                new DecisionOption { Key = "counterclockwise", Number = -1, Player = Receiver(seats, self.Holder, clockwise: false) },
            };
            bool clockwise = game.Ask(self.Holder, DecisionKind.ChooseDirection, "rotate.direction", self.Id, options) == "clockwise";
            List<int> values = seats.Select(game.ShieldOf).ToList();
            int n = seats.Count;
            for (int i = 0; i < n; i++)
            {
                // Clockwise: each shield moves to the next seat, so seat i receives the value of seat i-1.
                int from = clockwise ? (i - 1 + n) % n : (i + 1) % n;
                game.TrySetShield(self.Holder, seats[i], values[from], self);
            }
        }

        private static List<int> Participants(Game game, EffectSource self)
        {
            return game.AliveFrom(0).Where(p => game.CanChangeShield(self.Holder, p)).ToList();
        }

        // The participant after the holder in the direction (seats in increasing order are clockwise), wrapping around.
        private static int Receiver(List<int> seats, int holder, bool clockwise)
        {
            return clockwise
                ? seats.Where(s => s > holder).DefaultIfEmpty(seats[0]).First()
                : seats.Where(s => s < holder).DefaultIfEmpty(seats[seats.Count - 1]).Last();
        }
    }

    // ---------------------------------------------------------------- Health and tokens

    /// <summary>The holder heals <c>amount</c>.</summary>
    internal sealed class Heal : ActivationBrick
    {
        private readonly int _amount;

        public Heal(int amount) => _amount = amount;

        public override string Name => nameof(Heal);

        public override void Activate(Game game, EffectSource self) => game.Heal(self.Holder, _amount, self);
    }

    /// <summary>The holder heals <c>amount</c> per neutral face-up market card.</summary>
    internal sealed class HealPerNeutral : ActivationBrick
    {
        private readonly int _amount;

        public HealPerNeutral(int amount) => _amount = amount;

        public override string Name => nameof(HealPerNeutral);

        public override void Activate(Game game, EffectSource self) => game.Heal(self.Holder, _amount * game.NeutralCardsInMarkets(), self);
    }

    /// <summary>The holder gains <c>amount</c> overcharge token(s), up to the maximum.</summary>
    internal sealed class GainOvercharge : ActivationBrick
    {
        private readonly int _amount;

        public GainOvercharge(int amount) => _amount = amount;

        public override string Name => nameof(GainOvercharge);

        public override void Activate(Game game, EffectSource self) => game.GainOvercharge(self.Holder, _amount);
    }

    /// <summary>The holder cannot lose their overcharge until the start of their next turn.</summary>
    internal sealed class KeepOverchargeUntilNextTurn : ActivationBrick
    {
        public override string Name => nameof(KeepOverchargeUntilNextTurn);

        public override void Activate(Game game, EffectSource self)
        {
            game.AddStatus(self.Holder, BrickStatuses.KeepOvercharge, self.Holder, StatusExpiry.StartOfTurn, self.Holder);
        }
    }

    /// <summary>The holder's next overcharged attack has advantage (no time limit).</summary>
    internal sealed class NextOverchargedAttackAdvantage : ActivationBrick
    {
        public override string Name => nameof(NextOverchargedAttackAdvantage);

        public override void Activate(Game game, EffectSource self)
        {
            game.AddStatus(self.Holder, BrickStatuses.OverchargedAttackAdvantage, self.Holder, StatusExpiry.Permanent, self.Holder);
        }
    }

    // ---------------------------------------------------------------- Rules changes

    /// <summary>Discards every equipped modifier of every alive player.</summary>
    internal sealed class DiscardAllEquippedModifiers : ActivationBrick
    {
        public override string Name => nameof(DiscardAllEquippedModifiers);

        public override void Activate(Game game, EffectSource self)
        {
            foreach (int p in game.AliveInTurnOrder().ToList())
            {
                game.DiscardAllModifiers(p, self);
            }
        }
    }

    /// <summary>Until the start of their next turn, the holder may redirect attacks aimed at them.</summary>
    internal sealed class RedirectAttacksUntilNextTurn : ActivationBrick
    {
        public override string Name => nameof(RedirectAttacksUntilNextTurn);

        public override void Activate(Game game, EffectSource self)
        {
            game.AddStatus(self.Holder, BrickStatuses.RedirectAttacks, self.Holder, StatusExpiry.StartOfTurn, self.Holder);
        }
    }

    /// <summary>+amount crew action(s) this turn (they must still be different).</summary>
    internal sealed class ExtraCrewActionsThisTurn : ActivationBrick
    {
        private readonly int _amount;

        public ExtraCrewActionsThisTurn(int amount) => _amount = amount;

        public override string Name => nameof(ExtraCrewActionsThisTurn);

        public override void Activate(Game game, EffectSource self)
        {
            AddTurnStatus(game, self, BrickStatuses.ExtraCrewActions, Var(BrickStatuses.VarAmount, _amount));
        }
    }
}
