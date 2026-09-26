using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Content;
using Vortex.Core.Effects;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Core.Rules
{
    /// <summary>Attack pipeline (RULES A6) and dice of crew actions.</summary>
    internal sealed partial class Game
    {
        /// <summary>Resolves an attack, step by step as numbered in RULES A6.</summary>
        public AttackInfo ResolveAttack(int attacker, int target, bool useOvercharge)
        {
            var attack = new AttackInfo(attacker, target);
            Attacks.Add(attack);

            // 1. Declaration (legality was validated with the permission "cibler").
            if (useOvercharge)
            {
                ConsumeOvercharge(attacker);
                attack.Overcharged = true;
            }

            foreach (KeyValuePair<Effect, EffectSource> pair in ActiveEffects())
            {
                pair.Key.ConfigureAttack(this, pair.Value, attack);
            }

            Emit(new GameEvent { Type = GameEventType.AttackDeclared, Player = attacker, Other = target, Value = attack.Overcharged ? 1 : 0 });

            // 2. Redirection (reaction "attaque déclarée").
            Raise((e, s) => e.OnAttackDeclared(this, s, attack));

            // 3. Before the roll.
            Raise((e, s) => e.OnBeforeRoll(this, s, attack));

            // 4-5. Roll and critical.
            var pool = new DicePool { Count = attack.Overcharged ? 2 : 1 };
            pool.Keep = pool.Count;
            foreach (KeyValuePair<Effect, EffectSource> pair in ActiveEffects())
            {
                pair.Key.ModifyAttackDice(this, pair.Value, attack, pool);
            }

            RollAttackDice(attack, pool);
            attack.Critical = attack.Kept.Contains(Config.DieFaces);

            // 6. Attack value, with the bounty on the sole HP leader when the rule option is on.
            int bounty = Config.LeaderBounty > 0 && attack.Target == SoleHpLeader() ? Config.LeaderBounty : 0;
            if (bounty > 0)
            {
                Emit(new GameEvent { Type = GameEventType.LeaderBountyApplied, Player = attacker, Other = attack.Target, Amount = bounty });
            }

            attack.Value = Math.Max(0, Calculate(attack.KeptSum, (e, s, m) => e.ModifyAttackValue(this, s, attack, m), m => m.Add(bounty), attack.Bonuses));

            // 7. Effective shield.
            attack.EffectiveShield = Math.Max(0, Calculate(ShieldOf(attack.Target), (e, s, m) => e.ModifyEffectiveShield(this, s, attack, m)));

            // 8. Damage.
            int raw = Math.Max(0, attack.Value - attack.EffectiveShield);
            attack.Damage = Math.Max(0, Calculate(raw, (e, s, m) => e.ModifyDamage(this, s, attack, m)));
            int afterEffects = attack.Damage;

            // 9. Critical effect: the target chooses the modifier destroyed, or takes extra damage.
            if (attack.Critical)
            {
                Emit(new GameEvent { Type = GameEventType.CriticalHit, Player = attacker, Other = attack.Target });
                List<CardInstance> mods = State.Players[attack.Target].Modifiers().ToList();
                if (mods.Count > 0)
                {
                    CardInstance chosen = AskCard(attack.Target, Decisions.DecisionKind.ChooseModifier, "critical.discard", null, mods)!;
                    DiscardModifier(attack.Target, Definition(chosen).Slot, null);
                }
                else
                {
                    attack.Damage += Config.CriticalBonusWithoutModifier;
                }
            }

            // What stopped the attack, for the client to tell a parry by the shield from damage cancelled by an effect.
            Emit(new GameEvent
            {
                Type = GameEventType.AttackResolved,
                Player = attacker,
                Other = attack.Target,
                Value = attack.Value,
                Amount = attack.Damage,
                Values = new List<int> { attack.EffectiveShield, raw, afterEffects },
            });

            // 10. Application ("perte de PV", cause Attack).
            attack.HpLost = LoseHp(new HpLossInfo(attack.Target, attack.Damage, HpLossCause.Attack, attacker, null, attack));

            // 11. After the attack.
            Raise((e, s) => e.OnAttackResolved(this, s, attack));
            ConsumeAttackStatuses(attack);

            // 12. Eliminations and victory.
            CheckEliminations();
            return attack;
        }

        /// <summary>
        /// Effective shield of <paramref name="target"/> against a plain attack by <paramref name="attacker"/>
        /// (RULES A6 step 7), computed without resolving any attack. Callers work on a copy of the state.
        /// </summary>
        public int PreviewEffectiveShield(int target, int attacker)
        {
            var attack = new AttackInfo(attacker, target);
            return Math.Max(0, Calculate(ShieldOf(target), (e, s, m) => e.ModifyEffectiveShield(this, s, attack, m)));
        }

        /// <summary>Redirects an attack once (RULES A6 step 2). Returns false if not allowed.</summary>
        public bool Redirect(AttackInfo attack, int newTarget)
        {
            if (attack.Redirected || newTarget == attack.Attacker || newTarget == attack.Target || State.Players[newTarget].Eliminated)
            {
                return false;
            }

            int previous = attack.Target;
            attack.OriginalTarget = previous;
            attack.Target = newTarget;
            Emit(new GameEvent { Type = GameEventType.AttackRedirected, Player = newTarget, Other = previous });
            return true;
        }

        // Throws once, or twice with net advantage/disadvantage, keeping the best/worst total (RULES B4).
        private void RollAttackDice(AttackInfo attack, DicePool pool)
        {
            int count = Math.Max(1, pool.Count);
            int keep = Math.Max(1, Math.Min(pool.Keep, count));
            int net = pool.Advantage - pool.Disadvantage;

            List<int> first = ThrowDice(attack.Attacker, count, usePreRolled: true);
            List<int> kept = KeepHighest(first, keep);
            if (net != 0)
            {
                List<int> second = ThrowDice(attack.Attacker, count, usePreRolled: false);
                List<int> keptSecond = KeepHighest(second, keep);
                bool secondIsBetter = keptSecond.Sum() > kept.Sum();
                if ((net > 0 && secondIsBetter) || (net < 0 && keptSecond.Sum() < kept.Sum()))
                {
                    kept = keptSecond;
                }

                attack.Rolls.AddRange(first);
                attack.Rolls.AddRange(second);
            }
            else
            {
                attack.Rolls.AddRange(first);
            }

            attack.Kept.AddRange(kept);
            attack.NetAdvantage = Math.Sign(net);
            Emit(new GameEvent { Type = GameEventType.DiceRolled, Player = attack.Attacker, Values = new List<int>(attack.Rolls), Amount = attack.KeptSum, Value = net });
        }

        /// <summary>
        /// Rolls dice for a crew action. The first die may be a die rolled in advance by an effect
        /// (status <see cref="StatusKinds.PreRolledDie"/>), which is then used up.
        /// </summary>
        public List<int> ThrowDice(int player, int count, bool usePreRolled)
        {
            var dice = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                StatusState? preRolled = i == 0 && usePreRolled ? State.Players[player].Statuses.FirstOrDefault(s => s.IsActive && s.Kind == StatusKinds.PreRolledDie) : null;
                if (preRolled != null)
                {
                    dice.Add(preRolled.Var(StatusKinds.VarValue, 1));
                    RemoveStatus(player, preRolled);
                }
                else
                {
                    dice.Add(NextDie());
                }
            }

            return dice;
        }

        private static List<int> KeepHighest(List<int> dice, int keep)
        {
            return dice.OrderByDescending(d => d).Take(keep).ToList();
        }

        // "Next attack" statuses of the attacker are used up by this attack (RULES B5).
        private void ConsumeAttackStatuses(AttackInfo attack)
        {
            PlayerState attacker = State.Players[attack.Attacker];
            foreach (StatusState status in attacker.Statuses.Where(s => s.IsActive).ToList())
            {
                var source = new EffectSource(EffectOrigin.Status, status.Kind, attack.Attacker, statusUid: status.Uid);
                if (Catalog.ForStatus(status.Kind).IsConsumedBy(this, source, attack))
                {
                    RemoveStatus(attack.Attacker, status);
                }
            }
        }
    }
}
