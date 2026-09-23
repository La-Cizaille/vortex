using System.Collections.Generic;
using Vortex.Core.Commands;
using Vortex.Core.Effects;
using Vortex.Core.Events;
using Vortex.Core.Rules;

namespace Vortex.Core.Tests.Support
{
    /// <summary>+N to the holder's attack value.</summary>
    internal sealed class TestAttackBonus : Effect
    {
        private readonly int _amount;

        public TestAttackBonus(int amount) => _amount = amount;

        public override string Name => "TestAttackBonus";

        public override void ModifyAttackValue(Game game, EffectSource self, AttackInfo attack, ValueModifiers value)
        {
            if (attack.Attacker == self.Holder)
            {
                value.Add(_amount);
            }
        }
    }

    /// <summary>×K to the holder's damage.</summary>
    internal sealed class TestDamageMultiplier : Effect
    {
        private readonly int _factor;

        public TestDamageMultiplier(int factor) => _factor = factor;

        public override string Name => "TestDamageMultiplier";

        public override void ModifyDamage(Game game, EffectSource self, AttackInfo attack, ValueModifiers damage)
        {
            if (attack.Attacker == self.Holder)
            {
                damage.Multiply(_factor);
            }
        }
    }

    /// <summary>The holder's attack ignores the target's shield (replace by 0).</summary>
    internal sealed class TestIgnoreShield : Effect
    {
        public override string Name => "TestIgnoreShield";

        public override void ModifyEffectiveShield(Game game, EffectSource self, AttackInfo attack, ValueModifiers shield)
        {
            if (attack.Attacker == self.Holder)
            {
                shield.Replace(0);
            }
        }
    }

    /// <summary>Caps the holder's attack losses.</summary>
    internal sealed class TestLossCap : Effect
    {
        private readonly int _max;

        public TestLossCap(int max) => _max = max;

        public override string Name => "TestLossCap";

        public override void ModifyHpLoss(Game game, EffectSource self, HpLossInfo loss, ValueModifiers amount)
        {
            if (loss.Player == self.Holder && loss.Cause == HpLossCause.Attack)
            {
                amount.Cap(_max);
            }
        }
    }

    /// <summary>Cancels the holder's attack losses.</summary>
    internal sealed class TestLossCancel : Effect
    {
        public override string Name => "TestLossCancel";

        public override void ModifyHpLoss(Game game, EffectSource self, HpLossInfo loss, ValueModifiers amount)
        {
            if (loss.Player == self.Holder && loss.Cause == HpLossCause.Attack)
            {
                amount.Cancel();
            }
        }
    }

    /// <summary>Opponents cannot modify the holder's shield.</summary>
    internal sealed class TestDenyShieldChange : Effect
    {
        public override string Name => "TestDenyShieldChange";

        public override bool CanChangeShield(Game game, EffectSource self, int sourcePlayer, int target)
        {
            return !(target == self.Holder && sourcePlayer >= 0 && sourcePlayer != self.Holder);
        }
    }

    /// <summary>Advantage (+1) or disadvantage (-1) on attacks made (+) or suffered (-) by the holder.</summary>
    internal sealed class TestAdvantage : Effect
    {
        private readonly bool _advantage;

        public TestAdvantage(bool advantage) => _advantage = advantage;

        public override string Name => "TestAdvantage";

        public override void ModifyAttackDice(Game game, EffectSource self, AttackInfo attack, DicePool pool)
        {
            if (_advantage && attack.Attacker == self.Holder)
            {
                pool.Advantage++;
            }
            else if (!_advantage && attack.Target == self.Holder)
            {
                pool.Disadvantage++;
            }
        }
    }

    /// <summary>Activation: gain an overcharge token.</summary>
    internal sealed class TestActivateOvercharge : Effect
    {
        public override string Name => "TestActivateOvercharge";

        public override bool IsActivation => true;

        public override void Activate(Game game, EffectSource self) => game.GainOvercharge(self.Holder, 1);
    }

    /// <summary>+N crew actions for the holder.</summary>
    internal sealed class TestExtraCrewActions : Effect
    {
        private readonly int _amount;

        public TestExtraCrewActions(int amount) => _amount = amount;

        public override string Name => "TestExtraCrewActions";

        public override void ModifyCrewActions(Game game, EffectSource self, int player, ValueModifiers actions)
        {
            if (player == self.Holder)
            {
                actions.Add(_amount);
            }
        }
    }

    /// <summary>Records the order in which reactions reach effects (RULES B7).</summary>
    internal sealed class TestRecorder : Effect
    {
        private readonly string _label;
        private readonly List<string> _log;

        public TestRecorder(string label, List<string> log)
        {
            _label = label;
            _log = log;
        }

        public override string Name => "TestRecorder";

        public override void OnTurnStart(Game game, EffectSource self, int player) => _log.Add(_label + "@" + self.Holder);
    }

    /// <summary>When the holder loses HP, they lose 1 more HP (cause Event): an infinite chain.</summary>
    internal sealed class TestLoop : Effect
    {
        public override string Name => "TestLoop";

        public override void OnHpLost(Game game, EffectSource self, HpLossInfo loss)
        {
            if (loss.Player == self.Holder)
            {
                game.LoseHp(new HpLossInfo(self.Holder, 1, HpLossCause.Event, -1, null, null));
            }
        }
    }

    /// <summary>When the holder is hit, the attacker must gain overcharge on their next turn.</summary>
    internal sealed class TestImposeOnHit : Effect
    {
        private readonly CrewAction _action;
        private readonly int _target;

        public TestImposeOnHit(CrewAction action, int target = -1)
        {
            _action = action;
            _target = target;
        }

        public override string Name => "TestImposeOnHit";

        public override void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
            if (attack.Target == self.Holder)
            {
                game.ImposeCrewAction(attack.Attacker, _action, _target, self.Holder);
            }
        }
    }

    /// <summary>Event activation: every player loses N HP (cause Event).</summary>
    internal sealed class TestEventDamage : Effect
    {
        private readonly int _amount;

        public TestEventDamage(int amount) => _amount = amount;

        public override string Name => "TestEventDamage";

        public override bool IsActivation => true;

        public override void Activate(Game game, EffectSource self)
        {
            foreach (int seat in new List<int>(game.AliveInTurnOrder()))
            {
                game.LoseHp(new HpLossInfo(seat, _amount, HpLossCause.Event, -1, self, null));
            }
        }
    }
}
