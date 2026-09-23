using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Decisions;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Effects.Bricks
{
    /// <summary>Refuses "modifier un bouclier" on the holder's shield when the source is an opponent (RULES B2.2).</summary>
    internal sealed class DenyShieldChangeByOpponents : Effect
    {
        public override string Name => nameof(DenyShieldChangeByOpponents);

        public override bool CanChangeShield(Game game, EffectSource self, int sourcePlayer, int target)
        {
            return !(Scope.Covers(self, target) && sourcePlayer >= 0 && sourcePlayer != target);
        }
    }

    /// <summary>
    /// Caps the covered player's attack losses at <c>max</c>, optionally only at or below <c>whenHpAtMost</c> HP and
    /// never against overcharged attacks when <c>unlessOvercharged</c>. With <c>discardWhenOvercharged</c>, suffering
    /// an overcharged attack discards the card.
    /// </summary>
    internal sealed class CapIncomingAttackLoss : Effect
    {
        private readonly int _max;
        private readonly bool _unlessOvercharged;
        private readonly int _whenHpAtMost;
        private readonly bool _discardWhenOvercharged;

        public CapIncomingAttackLoss(int max, bool unlessOvercharged, int whenHpAtMost, bool discardWhenOvercharged)
        {
            _max = max;
            _unlessOvercharged = unlessOvercharged;
            _whenHpAtMost = whenHpAtMost;
            _discardWhenOvercharged = discardWhenOvercharged;
        }

        public override string Name => nameof(CapIncomingAttackLoss);

        public override void ModifyHpLoss(Game game, EffectSource self, HpLossInfo loss, ValueModifiers amount)
        {
            if (!Scope.Covers(self, loss.Player) || loss.Cause != HpLossCause.Attack || loss.Attack == null)
            {
                return;
            }

            if (loss.Attack.Overcharged && (_unlessOvercharged || _discardWhenOvercharged))
            {
                if (_discardWhenOvercharged)
                {
                    amount.Defer(() => game.DiscardSource(self));
                }

                return;
            }

            if (_whenHpAtMost > 0 && game.Player(loss.Player).Hp > _whenHpAtMost)
            {
                return;
            }

            amount.Cap(_max);
        }
    }

    /// <summary>The next attack suffered causes no HP loss; the card is then discarded.</summary>
    internal sealed class PreventNextAttackLoss : Effect
    {
        public override string Name => nameof(PreventNextAttackLoss);

        public override void ModifyHpLoss(Game game, EffectSource self, HpLossInfo loss, ValueModifiers amount)
        {
            if (Scope.Covers(self, loss.Player) && loss.Cause == HpLossCause.Attack)
            {
                amount.Cancel(onApplied: () => game.DiscardSource(self));
            }
        }
    }

    /// <summary>An attack loss that would eliminate the covered player is cancelled; the card is then discarded.</summary>
    internal sealed class PreventFatalAttackLoss : Effect
    {
        public override string Name => nameof(PreventFatalAttackLoss);

        public override void ModifyHpLoss(Game game, EffectSource self, HpLossInfo loss, ValueModifiers amount)
        {
            if (Scope.Covers(self, loss.Player) && loss.Cause == HpLossCause.Attack)
            {
                int hp = game.Player(loss.Player).Hp;
                amount.Cancel(value => value >= hp, () => game.DiscardSource(self));
            }
        }
    }

    /// <summary>After an attack on the holder, that attacker cannot attack the holder during their next turn.</summary>
    internal sealed class BlockAttackerNextTurn : Effect
    {
        public override string Name => nameof(BlockAttackerNextTurn);

        public override void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
            if (self.IsGlobal || attack.Target != self.Holder || game.Player(attack.Attacker).Eliminated)
            {
                return;
            }

            game.AddStatus(attack.Attacker, BrickStatuses.CannotTargetPlayer, self.Holder, StatusExpiry.EndOfTurn, attack.Attacker, dormantUntilExpiryPlayersTurn: true, vars: new[]
            {
                new KeyValuePair<string, int>(BrickStatuses.VarPlayer, self.Holder),
            });
        }
    }

    /// <summary>
    /// When equipped, the holder chooses an enemy; that enemy's shield is bounded to (holder's shield − offset),
    /// minimum 0, recalculated continuously (calculation "bornes du bouclier").
    /// </summary>
    internal sealed class CapEnemyShield : Effect
    {
        private const string VarEnemy = "enemy";
        private readonly int _offset;

        public CapEnemyShield(int offset) => _offset = offset;

        public override string Name => nameof(CapEnemyShield);

        public override void OnEquipped(Game game, EffectSource self, SlotChangeInfo change)
        {
            if (change.Card.Uid != self.CardUid)
            {
                return;
            }

            int enemy = game.AskPlayer(self.Holder, "cap.enemy", self.Id, game.Opponents(self.Holder));
            if (enemy >= 0)
            {
                change.Card.Vars[VarEnemy] = enemy;
            }
        }

        public override void ModifyShieldBounds(Game game, EffectSource self, int player, ShieldBounds bounds)
        {
            CardInstance? card = game.CardOf(self);
            if (card != null && card.Vars.TryGetValue(VarEnemy, out int enemy) && enemy == player)
            {
                // The holder's stored value is used (not its bounded value) to avoid mutual recursion between two such cards.
                bounds.LowerMax(System.Math.Max(0, game.Player(self.Holder).Shield - _offset));
            }
        }
    }

    /// <summary>When an attack makes the holder lose HP, the holder imposes the attacker's crew action (and targets) for their next turn.</summary>
    internal sealed class DictateAttackerAction : Effect
    {
        public override string Name => nameof(DictateAttackerAction);

        public override void OnHpLost(Game game, EffectSource self, HpLossInfo loss)
        {
            int attacker = loss.SourcePlayer;
            if (self.IsGlobal || loss.Player != self.Holder || loss.Cause != HpLossCause.Attack || loss.Lost <= 0 || attacker < 0 || game.Player(attacker).Eliminated)
            {
                return;
            }

            var options = new List<DecisionOption>();
            foreach (int target in game.Opponents(attacker))
            {
                options.Add(new DecisionOption { Key = "attack:" + target, Player = target, Number = (int)CrewAction.Attack });
                options.Add(new DecisionOption { Key = "sabotage:" + target, Player = target, Number = (int)CrewAction.Sabotage });
            }

            options.Add(new DecisionOption { Key = "reroll", Number = (int)CrewAction.RerollShield });
            options.Add(new DecisionOption { Key = "overcharge", Number = (int)CrewAction.Overcharge });
            string key = game.Ask(self.Holder, DecisionKind.ChooseCrewAction, "dictate.action", self.Id, options);
            DecisionOption chosen = options.First(o => o.Key == key);
            game.ImposeCrewAction(attacker, (CrewAction)chosen.Number, chosen.Player, self.Holder);
        }
    }

    /// <summary>The holder heals each time an attack makes another player lose HP (cause Attack only).</summary>
    internal sealed class HealWhenOthersDamaged : Effect
    {
        private readonly int _amount;

        public HealWhenOthersDamaged(int amount) => _amount = amount;

        public override string Name => nameof(HealWhenOthersDamaged);

        public override void OnHpLost(Game game, EffectSource self, HpLossInfo loss)
        {
            if (!self.IsGlobal && loss.Player != self.Holder && loss.Cause == HpLossCause.Attack && loss.Lost > 0 && loss.SourcePlayer >= 0)
            {
                game.Heal(self.Holder, _amount, self);
            }
        }
    }

    /// <summary>When an attack makes the holder lose HP, the attacker loses as many (cause Reflect, which triggers no reaction).</summary>
    internal sealed class ReflectAttackLoss : Effect
    {
        public override string Name => nameof(ReflectAttackLoss);

        public override void OnHpLost(Game game, EffectSource self, HpLossInfo loss)
        {
            if (!self.IsGlobal && loss.Player == self.Holder && loss.Cause == HpLossCause.Attack && loss.Lost > 0 && loss.SourcePlayer >= 0)
            {
                game.LoseHp(new HpLossInfo(loss.SourcePlayer, loss.Lost, HpLossCause.Reflect, self.Holder, self, null));
            }
        }
    }

    /// <summary>The holder heals the HP they lose to torment tokens.</summary>
    internal sealed class HealOnOwnTormentLoss : Effect
    {
        public override string Name => nameof(HealOnOwnTormentLoss);

        public override void OnHpLost(Game game, EffectSource self, HpLossInfo loss)
        {
            if (!self.IsGlobal && loss.Player == self.Holder && loss.Cause == HpLossCause.Torment && loss.Lost > 0)
            {
                game.Heal(self.Holder, loss.Lost, self);
            }
        }
    }

    /// <summary>When an attack makes the holder lose HP, the holder places <c>count</c> torment tokens on the attacker's modifiers.</summary>
    internal sealed class TormentAttackerOnLoss : Effect
    {
        private readonly int _count;

        public TormentAttackerOnLoss(int count) => _count = count;

        public override string Name => nameof(TormentAttackerOnLoss);

        public override void OnHpLost(Game game, EffectSource self, HpLossInfo loss)
        {
            int attacker = loss.SourcePlayer;
            if (self.IsGlobal || loss.Player != self.Holder || loss.Cause != HpLossCause.Attack || loss.Lost <= 0 || attacker < 0)
            {
                return;
            }

            for (int i = 0; i < _count && !game.Player(attacker).Eliminated; i++)
            {
                CardInstance? card = game.AskCard(self.Holder, DecisionKind.ChooseModifier, "torment.place", self.Id, game.Player(attacker).Modifiers().ToList());
                if (card == null)
                {
                    return;
                }

                game.PlaceTorment(self.Holder, card, self);
            }
        }
    }

    /// <summary>
    /// Every time an HP loss other than torment, reflect or self-inflicted (Self) hits the covered player, roll a die: even → no loss,
    /// odd → +penalty.
    /// </summary>
    internal sealed class GambleOnHpLoss : Effect
    {
        private readonly int _penalty;

        public GambleOnHpLoss(int penalty) => _penalty = penalty;

        public override string Name => nameof(GambleOnHpLoss);

        public override void ModifyHpLoss(Game game, EffectSource self, HpLossInfo loss, ValueModifiers amount)
        {
            if (!Scope.Covers(self, loss.Player) || loss.Amount <= 0 || loss.Cause == HpLossCause.Torment || loss.Cause == HpLossCause.Reflect || loss.Cause == HpLossCause.Self)
            {
                return;
            }

            if (game.RollDie(loss.Player, self.Id) % 2 == 0)
            {
                amount.Cancel();
            }
            else
            {
                amount.Add(_penalty);
            }
        }
    }

    // ---------------------------------------------------------------- Turn-start bricks

    /// <summary>At the start of the holder's turn, their shield copies the highest shield of the other players.</summary>
    internal sealed class CopyHighestShieldOnTurnStart : Effect
    {
        public override string Name => nameof(CopyHighestShieldOnTurnStart);

        public override void OnTurnStart(Game game, EffectSource self, int player)
        {
            if (self.IsGlobal || player != self.Holder)
            {
                return;
            }

            List<int> others = game.Opponents(self.Holder).ToList();
            if (others.Count > 0)
            {
                game.TrySetShield(self.Holder, self.Holder, others.Max(game.ShieldOf), self);
            }
        }
    }

    /// <summary>At the start of the holder's turn: lose <c>amount</c> HP (cause Self) at or above <c>threshold</c>, else heal <c>amount</c>.</summary>
    internal sealed class HpBalanceOnTurnStart : Effect
    {
        private readonly int _threshold;
        private readonly int _amount;

        public HpBalanceOnTurnStart(int threshold, int amount)
        {
            _threshold = threshold;
            _amount = amount;
        }

        public override string Name => nameof(HpBalanceOnTurnStart);

        public override void OnTurnStart(Game game, EffectSource self, int player)
        {
            if (self.IsGlobal || player != self.Holder)
            {
                return;
            }

            if (game.Player(player).Hp >= _threshold)
            {
                game.LoseHp(new HpLossInfo(player, _amount, HpLossCause.Self, player, self, null));
            }
            else
            {
                game.Heal(player, _amount, self);
            }
        }
    }

    /// <summary>At the start of the holder's turn, their shield changes by <c>amount</c> (bounded).</summary>
    internal sealed class ShieldChangeOnTurnStart : Effect
    {
        private readonly int _amount;

        public ShieldChangeOnTurnStart(int amount) => _amount = amount;

        public override string Name => nameof(ShieldChangeOnTurnStart);

        public override void OnTurnStart(Game game, EffectSource self, int player)
        {
            if (!self.IsGlobal && player == self.Holder)
            {
                game.TryAddShield(self.Holder, self.Holder, _amount, self);
            }
        }
    }

    /// <summary>When the holder's market phase ends, a die is rolled and kept as the first die of their next crew action roll.</summary>
    internal sealed class PreRollDie : Effect
    {
        public override string Name => nameof(PreRollDie);

        public override void OnMarketEnd(Game game, EffectSource self, int player)
        {
            if (self.IsGlobal || player != self.Holder)
            {
                return;
            }

            int value = game.RollDie(player, self.Id);
            game.AddStatus(player, StatusKinds.PreRolledDie, player, StatusExpiry.EndOfTurn, player, vars: new[]
            {
                new KeyValuePair<string, int>(StatusKinds.VarValue, value),
            });
        }
    }

    // ---------------------------------------------------------------- Global passive bricks (events)

    /// <summary>Every covered player's shield counts as 0 in attacks (disabled, value unchanged).</summary>
    internal sealed class DisableShields : Effect
    {
        public override string Name => nameof(DisableShields);

        public override void ModifyEffectiveShield(Game game, EffectSource self, AttackInfo attack, ValueModifiers shield)
        {
            if (Scope.Covers(self, attack.Target))
            {
                shield.Replace(0);
            }
        }
    }

    /// <summary>+amount market picks for the covered players.</summary>
    internal sealed class ExtraMarketPicks : Effect
    {
        private readonly int _amount;

        public ExtraMarketPicks(int amount) => _amount = amount;

        public override string Name => nameof(ExtraMarketPicks);

        public override void ModifyMarketPicks(Game game, EffectSource self, int player, ValueModifiers picks)
        {
            if (Scope.Covers(self, player))
            {
                picks.Add(_amount);
            }
        }
    }
}
