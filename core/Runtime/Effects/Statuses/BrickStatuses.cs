using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Effects.Statuses
{
    /// <summary>
    /// Generic statuses created by activation bricks (RULES B5). A status acts for its holder
    /// (<see cref="EffectSource.Holder"/>, -1 for global statuses); its parameters are in its variables.
    /// "Next attack" statuses are used up by the holder's next attack and expire at the end of the turn.
    /// </summary>
    internal static class BrickStatuses
    {
        public const string ShieldDisabled = "ShieldDisabled";
        public const string NextAttackBonus = "NextAttackBonus";
        public const string NextAttackBonusPerNeutral = "NextAttackBonusPerNeutral";
        public const string NextAttackDamageMultiplier = "NextAttackDamageMultiplier";
        public const string NextAttackOvercharged = "NextAttackOvercharged";
        public const string NextAttackAllIn = "NextAttackAllIn";
        public const string SwapOnNextAttack = "SwapOnNextAttack";
        public const string KeepOvercharge = "KeepOvercharge";
        public const string CannotTargetPlayer = "CannotTargetPlayer";
        public const string RedirectAttacks = "RedirectAttacks";
        public const string ExtraCrewActions = "ExtraCrewActions";
        public const string OverchargedAttackAdvantage = "OverchargedAttackAdvantage";
        public const string TormentValueBonus = "TormentValueBonus";

        public const string VarAmount = "amount";
        public const string VarFactor = "factor";
        public const string VarDice = "dice";
        public const string VarKeep = "keep";
        public const string VarPlayer = "player";

        /// <summary>Every brick status behaviour, by kind.</summary>
        public static Dictionary<string, Effect> All()
        {
            return new Dictionary<string, Effect>(System.StringComparer.Ordinal)
            {
                [ShieldDisabled] = new ShieldDisabledStatus(),
                [NextAttackBonus] = new NextAttackBonusStatus(perNeutralCard: false),
                [NextAttackBonusPerNeutral] = new NextAttackBonusStatus(perNeutralCard: true),
                [NextAttackDamageMultiplier] = new NextAttackDamageMultiplierStatus(),
                [NextAttackOvercharged] = new NextAttackOverchargedStatus(),
                [NextAttackAllIn] = new NextAttackAllInStatus(),
                [SwapOnNextAttack] = new SwapOnNextAttackStatus(),
                [KeepOvercharge] = new KeepOverchargeStatus(),
                [CannotTargetPlayer] = new CannotTargetPlayerStatus(),
                [RedirectAttacks] = new RedirectAttacksStatus(),
                [ExtraCrewActions] = new ExtraCrewActionsStatus(),
                [OverchargedAttackAdvantage] = new OverchargedAttackAdvantageStatus(),
                [TormentValueBonus] = new TormentValueBonusStatus(),
            };
        }

        /// <summary>Variables of the status carried by <paramref name="self"/>.</summary>
        public static StatusState Of(Game game, EffectSource self)
        {
            return game.FindStatus(self.StatusUid) ?? throw new EngineException("Status " + self + " not found.");
        }
    }

    /// <summary>The holder's shield counts as 0 in attacks (disabled, value unchanged).</summary>
    internal sealed class ShieldDisabledStatus : Effect
    {
        public override string Name => BrickStatuses.ShieldDisabled;

        public override void ModifyEffectiveShield(Game game, EffectSource self, AttackInfo attack, ValueModifiers shield)
        {
            if (attack.Target == self.Holder)
            {
                shield.Replace(0);
            }
        }
    }

    /// <summary>Base of statuses used up by the holder's next attack.</summary>
    internal abstract class NextAttackStatus : Effect
    {
        public override bool IsConsumedBy(Game game, EffectSource self, AttackInfo attack) => attack.Attacker == self.Holder;

        protected static bool IsMine(EffectSource self, AttackInfo attack) => attack.Attacker == self.Holder;
    }

    /// <summary>+amount (or +amount per neutral card visible in the markets, counted at the attack) to the next attack.</summary>
    internal sealed class NextAttackBonusStatus : NextAttackStatus
    {
        private readonly bool _perNeutralCard;

        public NextAttackBonusStatus(bool perNeutralCard) => _perNeutralCard = perNeutralCard;

        public override string Name => _perNeutralCard ? BrickStatuses.NextAttackBonusPerNeutral : BrickStatuses.NextAttackBonus;

        public override void ModifyAttackValue(Game game, EffectSource self, AttackInfo attack, ValueModifiers value)
        {
            if (IsMine(self, attack))
            {
                int amount = BrickStatuses.Of(game, self).Var(BrickStatuses.VarAmount);
                value.Add(_perNeutralCard ? amount * game.NeutralCardsInMarkets() : amount);
            }
        }
    }

    /// <summary>×factor to the damage of the next attack.</summary>
    internal sealed class NextAttackDamageMultiplierStatus : NextAttackStatus
    {
        public override string Name => BrickStatuses.NextAttackDamageMultiplier;

        public override void ModifyDamage(Game game, EffectSource self, AttackInfo attack, ValueModifiers damage)
        {
            if (IsMine(self, attack))
            {
                damage.Multiply(BrickStatuses.Of(game, self).Var(BrickStatuses.VarFactor, 1));
            }
        }
    }

    /// <summary>The next attack is overcharged without consuming a token.</summary>
    internal sealed class NextAttackOverchargedStatus : NextAttackStatus
    {
        public override string Name => BrickStatuses.NextAttackOvercharged;

        public override void ConfigureAttack(Game game, EffectSource self, AttackInfo attack)
        {
            if (IsMine(self, attack))
            {
                attack.Overcharged = true;
            }
        }
    }

    /// <summary>The next attack is overcharged and rolls <c>dice</c> dice keeping the best <c>keep</c>.</summary>
    internal sealed class NextAttackAllInStatus : NextAttackStatus
    {
        public override string Name => BrickStatuses.NextAttackAllIn;

        public override void ConfigureAttack(Game game, EffectSource self, AttackInfo attack)
        {
            if (IsMine(self, attack))
            {
                attack.Overcharged = true;
            }
        }

        public override void ModifyAttackDice(Game game, EffectSource self, AttackInfo attack, DicePool pool)
        {
            if (IsMine(self, attack))
            {
                StatusState s = BrickStatuses.Of(game, self);
                pool.Count = s.Var(BrickStatuses.VarDice, pool.Count);
                pool.Keep = s.Var(BrickStatuses.VarKeep, pool.Keep);
            }
        }
    }

    /// <summary>
    /// After the next attack, the holder may exchange one of the target's modifiers with the same-slot card of
    /// another player (neither the target nor the holder) or of the matching market.
    /// </summary>
    internal sealed class SwapOnNextAttackStatus : NextAttackStatus
    {
        public override string Name => BrickStatuses.SwapOnNextAttack;

        public override void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
            if (!IsMine(self, attack) || game.Player(attack.Target).Eliminated)
            {
                return;
            }

            List<CardInstance> targetCards = game.Player(attack.Target).Modifiers().ToList();
            CardInstance? card = game.AskCard(self.Holder, Decisions.DecisionKind.ChooseModifier, "swap.targetCard", self.Id, targetCards, optional: true);
            if (card == null)
            {
                return;
            }

            Content.CardSlot slot = game.Definition(card).Slot;
            var partners = new List<CardInstance>();
            foreach (int seat in game.AliveInTurnOrder())
            {
                CardInstance? other = seat == attack.Target || seat == self.Holder ? null : game.Player(seat).Slot(slot);
                if (other != null)
                {
                    partners.Add(other);
                }
            }

            partners.AddRange(game.State.Market(slot).Visible);
            CardInstance? partner = game.AskCard(self.Holder, Decisions.DecisionKind.ChooseModifier, "swap.partner", self.Id, partners);
            if (partner == null)
            {
                return;
            }

            int partnerOwner = game.OwnerOf(partner);
            if (partnerOwner >= 0)
            {
                game.SwapModifiers(attack.Target, partnerOwner, slot, self);
            }
            else
            {
                game.SwapWithMarket(attack.Target, slot, partner, self);
            }
        }
    }

    /// <summary>The holder cannot lose their overcharge.</summary>
    internal sealed class KeepOverchargeStatus : Effect
    {
        public override string Name => BrickStatuses.KeepOvercharge;

        public override bool CanLoseOvercharge(Game game, EffectSource self, int player) => player != self.Holder;
    }

    /// <summary>The holder cannot attack the player in variable <c>player</c>.</summary>
    internal sealed class CannotTargetPlayerStatus : Effect
    {
        public override string Name => BrickStatuses.CannotTargetPlayer;

        public override bool CanTarget(Game game, EffectSource self, int actor, int target, CrewAction action)
        {
            return !(action == CrewAction.Attack && actor == self.Holder && target == BrickStatuses.Of(game, self).Var(BrickStatuses.VarPlayer, -1));
        }
    }

    /// <summary>The holder may redirect an attack aimed at them to another player (not the attacker).</summary>
    internal sealed class RedirectAttacksStatus : Effect
    {
        public override string Name => BrickStatuses.RedirectAttacks;

        public override void OnAttackDeclared(Game game, EffectSource self, AttackInfo attack)
        {
            if (attack.Target != self.Holder || attack.Redirected)
            {
                return;
            }

            IEnumerable<int> candidates = game.AliveInTurnOrder()
                .Where(p => p != attack.Attacker && p != self.Holder && game.CanTarget(attack.Attacker, p, CrewAction.Attack))
                .ToList();
            int newTarget = game.AskPlayer(self.Holder, "redirect.attack", self.Id, candidates, optional: true);
            if (newTarget >= 0)
            {
                game.Redirect(attack, newTarget);
            }
        }
    }

    /// <summary>+amount crew actions for the holder.</summary>
    internal sealed class ExtraCrewActionsStatus : Effect
    {
        public override string Name => BrickStatuses.ExtraCrewActions;

        public override void ModifyCrewActions(Game game, EffectSource self, int player, ValueModifiers actions)
        {
            if (player == self.Holder)
            {
                actions.Add(BrickStatuses.Of(game, self).Var(BrickStatuses.VarAmount));
            }
        }
    }

    /// <summary>The holder's next overcharged attack has advantage.</summary>
    internal sealed class OverchargedAttackAdvantageStatus : Effect
    {
        public override string Name => BrickStatuses.OverchargedAttackAdvantage;

        public override void ModifyAttackDice(Game game, EffectSource self, AttackInfo attack, DicePool pool)
        {
            if (attack.Attacker == self.Holder && attack.Overcharged)
            {
                pool.Advantage++;
            }
        }

        public override bool IsConsumedBy(Game game, EffectSource self, AttackInfo attack) => attack.Attacker == self.Holder && attack.Overcharged;
    }

    /// <summary>+amount to the value of every torment token (global).</summary>
    internal sealed class TormentValueBonusStatus : Effect
    {
        public override string Name => BrickStatuses.TormentValueBonus;

        public override void ModifyTormentValue(Game game, EffectSource self, ValueModifiers value)
        {
            value.Add(BrickStatuses.Of(game, self).Var(BrickStatuses.VarAmount));
        }
    }
}
