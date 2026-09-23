using System.Linq;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Effects;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Engine
{
    /// <summary>RULES A5.4 (crew actions) and A6 (attack), with scripted dice.</summary>
    [TestFixture]
    public class AttackAndCrewTests
    {
        private static (Scenario S, int Me, int Foe) Duel(TestCatalog? catalog = null)
        {
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            return (s, s.Current, 1 - s.Current);
        }

        [TestCase(5, 3, 2)]
        [TestCase(3, 3, 0)]
        [TestCase(2, 6, 0)]
        public void Damage_is_die_minus_shield_never_negative(int die, int shield, int expected)
        {
            (Scenario s, int me, int foe) = Duel();
            s.P(foe).Shield = shield;
            AttackInfo a = s.Game(die).ResolveAttack(me, foe, useOvercharge: false);
            Assert.That(a.Damage, Is.EqualTo(expected));
            Assert.That(s.P(foe).Hp, Is.EqualTo(30 - expected));
        }

        [Test]
        public void Overcharged_attack_adds_a_die_and_consumes_the_token()
        {
            (Scenario s, int me, int foe) = Duel();
            s.P(me).Overcharge = 1;
            AttackInfo a = s.Game(3, 6).ResolveAttack(me, foe, useOvercharge: true);
            Assert.That(a.KeptSum, Is.EqualTo(9));
            Assert.That(a.Damage, Is.EqualTo(5));
            Assert.That(s.P(me).Overcharge, Is.Zero);
        }

        [Test]
        public void Critical_without_modifier_adds_one_damage()
        {
            (Scenario s, int me, int foe) = Duel();
            AttackInfo a = s.Game(8).ResolveAttack(me, foe, false);
            Assert.That(a.Critical, Is.True);
            Assert.That(a.Damage, Is.EqualTo(8 - 4 + 1));
        }

        [Test]
        public void Bonuses_apply_before_the_shield_and_multipliers_after()
        {
            // (die 2 + 4) − shield 4 = 2, then ×2 = 4 (RULES A6 steps 6-8, B4).
            var catalog = new TestCatalog().Card("A_901", new TestAttackBonus(4)).Card("A_902", new TestDamageMultiplier(2));
            (Scenario s, int me, int foe) = Duel(catalog);
            s.Equip(me, "A_901");
            AttackInfo a = s.Game(2).ResolveAttack(me, foe, false);
            Assert.That(a.Value, Is.EqualTo(6));
            Assert.That(a.Damage, Is.EqualTo(2));
            s.Equip(me, "A_902");
            s.P(me).Statuses.Clear();
            AttackInfo b = s.Game(5).ResolveAttack(me, foe, false);
            Assert.That(b.Damage, Is.EqualTo(2), "A_902 replaced A_901: (5 − 4) × 2.");
        }

        [Test]
        public void Ignored_shield_counts_as_zero_without_changing_its_value()
        {
            var catalog = new TestCatalog().Card("A_901", new TestIgnoreShield());
            (Scenario s, int me, int foe) = Duel(catalog);
            s.Equip(me, "A_901");
            AttackInfo a = s.Game(3).ResolveAttack(me, foe, false);
            Assert.That(a.EffectiveShield, Is.Zero);
            Assert.That(a.Damage, Is.EqualTo(3));
            Assert.That(s.P(foe).Shield, Is.EqualTo(4));
        }

        [Test]
        public void Target_loss_caps_and_cancellations_apply_in_the_hp_loss_calculation()
        {
            var catalog = new TestCatalog().Card("D_901", new TestLossCap(1)).Card("D_902", new TestLossCancel());
            (Scenario s, int me, int foe) = Duel(catalog);
            s.Equip(foe, "D_901");
            Assert.That(s.Game(7).ResolveAttack(me, foe, false).HpLost, Is.EqualTo(1), "(7 − 4) = 3, capped to 1.");
            s.Equip(foe, "D_902");
            Assert.That(s.Game(7).ResolveAttack(me, foe, false).HpLost, Is.Zero);
        }

        [Test]
        public void Advantage_keeps_the_best_throw_and_cancels_with_disadvantage()
        {
            var catalog = new TestCatalog().Card("A_901", new TestAdvantage(true)).Card("D_901", new TestAdvantage(false));
            (Scenario s, int me, int foe) = Duel(catalog);
            s.Equip(me, "A_901");
            AttackInfo adv = s.Game(2, 7).ResolveAttack(me, foe, false);
            Assert.That(adv.Kept, Is.EqualTo(new[] { 7 }));
            Assert.That(adv.Rolls, Is.EqualTo(new[] { 2, 7 }));

            s.Equip(foe, "D_901");
            AttackInfo neutral = s.Game(2).ResolveAttack(me, foe, false);
            Assert.That(neutral.Rolls, Is.EqualTo(new[] { 2 }), "One advantage and one disadvantage cancel: a single throw.");
        }

        [Test]
        public void Damage_loses_the_overcharge_but_a_zero_damage_attack_does_not()
        {
            (Scenario s, int me, int foe) = Duel();
            s.P(foe).Overcharge = 1;
            s.Game(1).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Overcharge, Is.EqualTo(1));
            s.Game(7).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Overcharge, Is.Zero);
        }

        [Test]
        public void Reroll_with_overcharge_sums_two_dice_bounded_by_the_maximum()
        {
            (Scenario s, int me, int _) = Duel();
            s.P(me).Overcharge = 1;
            Game g = s.Game(6, 5);
            g.Execute(me, Command.RerollShield(useOvercharge: true));
            Assert.That(s.P(me).Shield, Is.EqualTo(8));
            Assert.That(s.P(me).Overcharge, Is.Zero);
        }

        [Test]
        public void Sabotage_replaces_the_target_shield_and_is_illegal_when_refused()
        {
            var catalog = new TestCatalog().Card("D_901", new TestDenyShieldChange());
            (Scenario s, int me, int foe) = Duel(catalog);
            Game g = s.Game(2);
            g.Execute(me, Command.Sabotage(foe));
            Assert.That(s.P(foe).Shield, Is.EqualTo(2));

            (Scenario s2, int me2, int foe2) = Duel(catalog);
            s2.Equip(foe2, "D_901");
            Assert.That(s2.Submit(me2, Command.Sabotage(foe2)).Error!.Code, Is.EqualTo(CommandErrorCode.ShieldChangeNotAllowed));
        }

        [Test]
        public void One_crew_action_per_turn_unless_an_effect_allows_more_and_they_must_differ()
        {
            (Scenario s, int me, int foe) = Duel();
            s.MustAccept(me, Command.Overcharge());
            Assert.That(s.Submit(me, Command.RerollShield()).Error!.Code, Is.EqualTo(CommandErrorCode.NoCrewActionLeft));

            var catalog = new TestCatalog().Card("A_901", new TestExtraCrewActions(1));
            (Scenario t, int me2, int foe2) = Duel(catalog);
            t.Equip(me2, "A_901");
            t.MustAccept(me2, Command.Overcharge());
            Assert.That(t.Submit(me2, Command.Overcharge()).Error!.Code, Is.EqualTo(CommandErrorCode.CrewActionAlreadyUsed));
            t.MustAccept(me2, Command.Sabotage(foe2));
        }

        [Test]
        public void Overcharge_is_capped_and_attacking_with_it_requires_a_token()
        {
            (Scenario s, int me, int foe) = Duel();
            Assert.That(s.Submit(me, Command.Attack(foe, useOvercharge: true)).Error!.Code, Is.EqualTo(CommandErrorCode.NoOvercharge));
            s.P(me).Overcharge = 1;
            Assert.That(s.Submit(me, Command.Overcharge()).Error!.Code, Is.EqualTo(CommandErrorCode.OverchargeFull));
        }

        [Test]
        public void Attacking_self_or_an_eliminated_player_is_rejected()
        {
            var s = Scenario.Start(3);
            s.ToMain();
            int me = s.Current;
            int other = (me + 1) % 3;
            Assert.That(s.Submit(me, Command.Attack(me)).Error!.Code, Is.EqualTo(CommandErrorCode.InvalidTarget));
            s.P(other).Hp = 0;
            s.P(other).Eliminated = true;
            Assert.That(s.Submit(me, Command.Attack(other)).Error!.Code, Is.EqualTo(CommandErrorCode.InvalidTarget));
            Assert.That(s.Submit(me, Command.Attack(42)).Error!.Code, Is.EqualTo(CommandErrorCode.InvalidTarget));
        }

        [Test]
        public void Attack_through_the_engine_emits_the_expected_facts()
        {
            (Scenario s, int me, int foe) = Duel();
            EngineResult r = s.MustAccept(me, Command.Attack(foe));
            Assert.That(r.Events.Select(e => e.Type), Is.SupersetOf(new[]
            {
                GameEventType.AttackDeclared, GameEventType.DiceRolled, GameEventType.AttackResolved, GameEventType.CrewActionPerformed,
            }));
            Assert.That(s.State.CrewActionsTaken, Is.EqualTo(new[] { CrewAction.Attack }));
        }
    }
}
