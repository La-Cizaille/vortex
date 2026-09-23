using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Decisions;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Effects.Bricks
{
    /// <summary>Condition on an attack, shared by several bricks.</summary>
    internal enum AttackCondition
    {
        /// <summary>Every attack.</summary>
        Always = 0,
        /// <summary>Overcharged attacks only.</summary>
        Overcharged = 1,
        /// <summary>When at least one of the target's modifiers carries a torment token.</summary>
        TargetHasTorment = 2,
    }

    /// <summary>Scope rule of every brick: a card's effect covers its holder; a global effect (event) covers everyone.</summary>
    internal static class Scope
    {
        public static bool Covers(EffectSource self, int player) => self.IsGlobal || self.Holder == player;

        public static bool Holds(Game game, AttackCondition condition, AttackInfo attack)
        {
            switch (condition)
            {
                case AttackCondition.Overcharged: return attack.Overcharged;
                case AttackCondition.TargetHasTorment: return game.TormentsOn(attack.Target) > 0;
                default: return true;
            }
        }
    }

    /// <summary>+amount to the attack value, before the shield (RULES A6 step 6).</summary>
    internal sealed class AttackValueBonus : Effect
    {
        private readonly int _amount;
        private readonly AttackCondition _when;

        public AttackValueBonus(int amount, AttackCondition when)
        {
            _amount = amount;
            _when = when;
        }

        public override string Name => nameof(AttackValueBonus);

        public override void ModifyAttackValue(Game game, EffectSource self, AttackInfo attack, ValueModifiers value)
        {
            if (Scope.Covers(self, attack.Attacker) && Scope.Holds(game, _when, attack))
            {
                value.Add(_amount);
            }
        }
    }

    /// <summary>Attack value +even when the sum of kept dice is even, +odd otherwise.</summary>
    internal sealed class ParityAttackBonus : Effect
    {
        private readonly int _even;
        private readonly int _odd;

        public ParityAttackBonus(int even, int odd)
        {
            _even = even;
            _odd = odd;
        }

        public override string Name => nameof(ParityAttackBonus);

        public override void ModifyAttackValue(Game game, EffectSource self, AttackInfo attack, ValueModifiers value)
        {
            if (Scope.Covers(self, attack.Attacker))
            {
                value.Add(attack.KeptSum % 2 == 0 ? _even : _odd);
            }
        }
    }

    /// <summary>The target's effective shield counts as 0 (ignored, not modified).</summary>
    internal sealed class IgnoreTargetShield : Effect
    {
        private readonly AttackCondition _when;

        public IgnoreTargetShield(AttackCondition when) => _when = when;

        public override string Name => nameof(IgnoreTargetShield);

        public override void ModifyEffectiveShield(Game game, EffectSource self, AttackInfo attack, ValueModifiers shield)
        {
            if (Scope.Covers(self, attack.Attacker) && Scope.Holds(game, _when, attack))
            {
                shield.Replace(0);
            }
        }
    }

    /// <summary>Advantage on the covered player's attacks.</summary>
    internal sealed class AttackAdvantage : Effect
    {
        public override string Name => nameof(AttackAdvantage);

        public override void ModifyAttackDice(Game game, EffectSource self, AttackInfo attack, DicePool pool)
        {
            if (Scope.Covers(self, attack.Attacker))
            {
                pool.Advantage++;
            }
        }
    }

    /// <summary>Disadvantage on attacks suffered by the covered player.</summary>
    internal sealed class IncomingAttackDisadvantage : Effect
    {
        public override string Name => nameof(IncomingAttackDisadvantage);

        public override void ModifyAttackDice(Game game, EffectSource self, AttackInfo attack, DicePool pool)
        {
            if (Scope.Covers(self, attack.Target))
            {
                pool.Disadvantage++;
            }
        }
    }

    /// <summary>The attacker heals when their attack makes the target lose HP.</summary>
    internal sealed class HealOnHit : Effect
    {
        private readonly int _amount;

        public HealOnHit(int amount) => _amount = amount;

        public override string Name => nameof(HealOnHit);

        public override void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
            if (Scope.Covers(self, attack.Attacker) && attack.HpLost > 0)
            {
                game.Heal(attack.Attacker, _amount, self);
            }
        }
    }

    /// <summary>Before the roll, moves up to <c>amount</c> shield points from the target to the attacker.</summary>
    internal sealed class StealShieldBeforeAttack : Effect
    {
        private readonly int _amount;

        public StealShieldBeforeAttack(int amount) => _amount = amount;

        public override string Name => nameof(StealShieldBeforeAttack);

        public override void OnBeforeRoll(Game game, EffectSource self, AttackInfo attack)
        {
            if (!Scope.Covers(self, attack.Attacker))
            {
                return;
            }

            int stolen = System.Math.Min(_amount, game.ShieldOf(attack.Target));
            if (stolen > 0 && game.TrySetShield(attack.Attacker, attack.Target, game.ShieldOf(attack.Target) - stolen, self))
            {
                game.TryAddShield(attack.Attacker, attack.Attacker, stolen, self);
            }
        }
    }

    /// <summary>Before the roll the attacker announces a face; ×factor damage if a kept die shows it.</summary>
    internal sealed class DieBetMultiplier : Effect
    {
        private readonly int _factor;

        public DieBetMultiplier(int factor) => _factor = factor;

        public override string Name => nameof(DieBetMultiplier);

        public override void OnBeforeRoll(Game game, EffectSource self, AttackInfo attack)
        {
            if (Scope.Covers(self, attack.Attacker))
            {
                attack.Bet = game.AskNumber(attack.Attacker, "bet.face", self.Id, 1, game.Config.DieFaces);
                game.Emit(new GameEvent { Type = GameEventType.EffectTriggered, Player = attack.Attacker, Id = self.Id, Text = "bet", Value = attack.Bet.Value });
            }
        }

        public override void ModifyDamage(Game game, EffectSource self, AttackInfo attack, ValueModifiers damage)
        {
            if (Scope.Covers(self, attack.Attacker) && attack.Bet.HasValue && attack.Kept.Contains(attack.Bet.Value))
            {
                damage.Multiply(_factor);
            }
        }
    }

    /// <summary>On a hit, the attacker may reroll (1 die) the shield of any player whose shield they may modify.</summary>
    internal sealed class RerollShieldOnHit : Effect
    {
        public override string Name => nameof(RerollShieldOnHit);

        public override void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
            if (!Scope.Covers(self, attack.Attacker) || attack.HpLost <= 0)
            {
                return;
            }

            List<int> candidates = game.AliveInTurnOrder().Where(p => game.CanChangeShield(attack.Attacker, p)).ToList();
            int chosen = game.AskPlayer(attack.Attacker, "reroll.shield", self.Id, candidates, optional: true);
            if (chosen >= 0)
            {
                game.TrySetShield(attack.Attacker, chosen, game.RollDie(attack.Attacker, self.Id), self);
            }
        }
    }

    /// <summary>On a hit, the attacker may discard one of the target's modifiers.</summary>
    internal sealed class DiscardTargetModifierOnHit : Effect
    {
        public override string Name => nameof(DiscardTargetModifierOnHit);

        public override void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
            if (!Scope.Covers(self, attack.Attacker) || attack.HpLost <= 0 || game.Player(attack.Target).Eliminated)
            {
                return;
            }

            List<CardInstance> mods = game.Player(attack.Target).Modifiers().ToList();
            CardInstance? card = game.AskCard(attack.Attacker, DecisionKind.ChooseModifier, "discard.targetModifier", self.Id, mods, optional: true);
            if (card != null)
            {
                game.DiscardModifier(attack.Target, game.Definition(card).Slot, self);
            }
        }
    }

    /// <summary>After each attack, the attacker places <c>count</c> torment tokens on the target's modifiers, one at a time.</summary>
    internal sealed class TormentTargetOnAttack : Effect
    {
        private readonly int _count;

        public TormentTargetOnAttack(int count) => _count = count;

        public override string Name => nameof(TormentTargetOnAttack);

        public override void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
            if (!Scope.Covers(self, attack.Attacker))
            {
                return;
            }

            for (int i = 0; i < _count && !game.IsOver && !game.Player(attack.Target).Eliminated; i++)
            {
                List<CardInstance> mods = game.Player(attack.Target).Modifiers().ToList();
                CardInstance? card = game.AskCard(attack.Attacker, DecisionKind.ChooseModifier, "torment.place", self.Id, mods);
                if (card == null)
                {
                    return; // no modifier: no token (RULES A7)
                }

                game.PlaceTorment(attack.Attacker, card, self);
            }
        }
    }

    /// <summary>On a hit, the attacker places one token on each of <c>count</c> different face-up market cards.</summary>
    internal sealed class TormentMarketOnHit : Effect
    {
        private readonly int _count;

        public TormentMarketOnHit(int count) => _count = count;

        public override string Name => nameof(TormentMarketOnHit);

        public override void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
            if (!Scope.Covers(self, attack.Attacker) || attack.HpLost <= 0)
            {
                return;
            }

            var chosen = new HashSet<int>();
            for (int i = 0; i < _count; i++)
            {
                List<CardInstance> candidates = game.VisibleMarketCards().Where(c => !chosen.Contains(c.Uid)).ToList();
                CardInstance? card = game.AskCard(attack.Attacker, DecisionKind.ChooseMarketCard, "torment.market", self.Id, candidates);
                if (card == null)
                {
                    return;
                }

                chosen.Add(card.Uid);
                game.PlaceTorment(attack.Attacker, card, self);
            }
        }
    }

    /// <summary>
    /// When the holder's attack eliminates a ship, every other player loses their modifiers, then their shield is
    /// set to 0 (subject to the permission "modifier un bouclier"); the card is then discarded.
    /// </summary>
    internal sealed class ScorchedEarthOnKill : Effect
    {
        public override string Name => nameof(ScorchedEarthOnKill);

        public override void OnPlayerEliminated(Game game, EffectSource self, EliminationInfo elimination)
        {
            if (self.IsGlobal || elimination.Responsible != self.Holder || elimination.LastCause != HpLossCause.Attack)
            {
                return;
            }

            List<int> others = game.AliveInTurnOrder().Where(p => p != self.Holder).ToList();
            foreach (int p in others)
            {
                game.DiscardAllModifiers(p, self);
            }

            foreach (int p in others)
            {
                game.TrySetShield(self.Holder, p, 0, self);
            }

            game.DiscardSource(self);
        }
    }
}
