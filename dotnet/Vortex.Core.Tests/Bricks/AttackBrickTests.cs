using System.Linq;
using NUnit.Framework;
using Vortex.Core.Effects;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Bricks
{
    [TestFixture]
    public class AttackBrickTests : BrickTestBase
    {
        [Test]
        public void AttackValueBonus_applies_to_the_holders_attacks_only()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("AttackValueBonus", ("amount", 4))));
            s.Equip(me, "A_901");
            Assert.That(s.Game(3).ResolveAttack(me, foe, false).Value, Is.EqualTo(7));
            Assert.That(s.Game(3).ResolveAttack(foe, me, false).Value, Is.EqualTo(3));
        }

        [Test]
        public void AttackValueBonus_conditions_overcharged_and_target_torment()
        {
            (Scenario s, int me, int foe) = Setup(2,
                ("A_901", B("AttackValueBonus", ("amount", 4), ("when", "Overcharged"))),
                ("D_901", B("AttackValueBonus", ("amount", 2), ("when", "TargetHasTorment"))));
            s.Equip(me, "A_901");
            s.Equip(me, "D_901");
            Assert.That(s.Game(3).ResolveAttack(me, foe, false).Value, Is.EqualTo(3));
            s.P(me).Overcharge = 1;
            Assert.That(s.Game(3, 1).ResolveAttack(me, foe, true).Value, Is.EqualTo(8));
            s.Equip(foe, "A_902").Torments = 1;
            Assert.That(s.Game(3).ResolveAttack(me, foe, false).Value, Is.EqualTo(5));
        }

        [Test]
        public void AttackValueBonus_carried_by_an_event_applies_to_every_attack()
        {
            var catalog = new TestCatalog().Event(TestContent.CalmEvent, B("AttackValueBonus", ("amount", 4)));
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            Assert.That(s.State.ActiveEventId, Is.EqualTo(TestContent.CalmEvent));
            Assert.That(s.Game(1).ResolveAttack(s.Current, 1 - s.Current, false).Value, Is.EqualTo(5));
            Assert.That(s.Game(1).ResolveAttack(1 - s.Current, s.Current, false).Value, Is.EqualTo(5));
        }

        [TestCase(4, 7)]
        [TestCase(5, 4)]
        public void ParityAttackBonus_depends_on_the_kept_dice(int die, int expectedValue)
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("ParityAttackBonus", ("even", 3), ("odd", -1))));
            s.Equip(me, "A_901");
            Assert.That(s.Game(die).ResolveAttack(me, foe, false).Value, Is.EqualTo(expectedValue));
        }

        [Test]
        public void IgnoreTargetShield_when_overcharged_only()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("IgnoreTargetShield", ("when", "Overcharged"))));
            s.Equip(me, "A_901");
            Assert.That(s.Game(3).ResolveAttack(me, foe, false).EffectiveShield, Is.EqualTo(4));
            s.P(me).Overcharge = 1;
            Assert.That(s.Game(3, 1).ResolveAttack(me, foe, true).EffectiveShield, Is.Zero);
        }

        [Test]
        public void AttackAdvantage_and_IncomingAttackDisadvantage()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("AttackAdvantage")), ("D_901", B("IncomingAttackDisadvantage")));
            s.Equip(me, "A_901");
            Assert.That(s.Game(2, 6).ResolveAttack(me, foe, false).Kept, Is.EqualTo(new[] { 6 }));
            s.Equip(me, "D_901");
            Assert.That(s.Game(6, 2).ResolveAttack(foe, me, false).Kept, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void HealOnHit_heals_only_when_the_target_loses_hp()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("HealOnHit", ("amount", 4))));
            s.Equip(me, "A_901");
            s.P(me).Hp = 20;
            s.Game(1).ResolveAttack(me, foe, false);
            Assert.That(s.P(me).Hp, Is.EqualTo(20));
            s.Game(7).ResolveAttack(me, foe, false);
            Assert.That(s.P(me).Hp, Is.EqualTo(24));
        }

        [Test]
        public void StealShieldBeforeAttack_moves_points_unless_refused()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("StealShieldBeforeAttack", ("amount", 2))), ("D_901", B("DenyShieldChangeByOpponents")));
            s.Equip(me, "A_901");
            AttackInfo a = s.Game(5).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Shield, Is.EqualTo(2));
            Assert.That(s.P(me).Shield, Is.EqualTo(6));
            Assert.That(a.Damage, Is.EqualTo(3), "The steal happens before the roll: 5 − 2.");

            s.Equip(foe, "D_901");
            s.Game(5).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Shield, Is.EqualTo(2), "Refused by the permission 'modifier un bouclier'.");
        }

        [Test]
        public void DieBetMultiplier_doubles_damage_when_a_kept_die_shows_the_bet()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("DieBetMultiplier", ("factor", 2))));
            s.Equip(me, "A_901");
            Assert.That(s.GameWith(Answers(N(6)), 6).ResolveAttack(me, foe, false).Damage, Is.EqualTo(4));
            Assert.That(s.GameWith(Answers(N(5)), 6).ResolveAttack(me, foe, false).Damage, Is.EqualTo(2));
        }

        [Test]
        public void RerollShieldOnHit_rerolls_the_chosen_shield_after_a_hit()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("RerollShieldOnHit")));
            s.Equip(me, "A_901");
            s.GameWith(Answers(P(foe)), 7, 1).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Shield, Is.EqualTo(1));
            s.GameWith(Answers("none"), 7).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Shield, Is.EqualTo(1), "Optional: 'none' keeps the shield.");
        }

        [Test]
        public void DiscardTargetModifierOnHit_is_optional_and_needs_a_hit()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("DiscardTargetModifierOnHit")));
            s.Equip(me, "A_901");
            CardInstance target = s.Equip(foe, "A_902");
            s.Game(1).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).AttackSlot, Is.Not.Null, "No hit, no discard.");
            s.GameWith(Answers(C(target.Uid)), 7).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).AttackSlot, Is.Null);
        }

        [Test]
        public void TormentTargetOnAttack_places_tokens_even_without_damage_but_needs_a_modifier()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("TormentTargetOnAttack", ("count", 2))));
            s.Equip(me, "A_901");
            s.Game(1).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Hp, Is.EqualTo(30), "No modifier: no token (RULES A7).");
            CardInstance card = s.Equip(foe, "D_902");
            s.Game(1).ResolveAttack(me, foe, false);
            Assert.That(card.Torments, Is.EqualTo(2));
            Assert.That(s.P(foe).Hp, Is.EqualTo(28), "Each token costs one HP when placed.");
        }

        [Test]
        public void TormentMarketOnHit_marks_different_market_cards()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("TormentMarketOnHit", ("count", 2))));
            s.Equip(me, "A_901");
            CardInstance m0 = s.State.AttackMarket.Visible[0];
            CardInstance m1 = s.State.DefenseMarket.Visible[0];
            s.GameWith(Answers(C(m0.Uid), C(m1.Uid)), 7).ResolveAttack(me, foe, false);
            Assert.That(m0.Torments + m1.Torments, Is.EqualTo(2));
            Assert.That(s.P(foe).Hp, Is.EqualTo(27), "Market tokens cost nobody HP.");
        }

        [Test]
        public void ScorchedEarthOnKill_strips_every_other_player_and_discards_itself()
        {
            (Scenario s, int me, int foe) = Setup(3, ("A_901", B("ScorchedEarthOnKill")));
            int third = (me + 2) % 3;
            s.Equip(me, "A_901");
            s.Equip(third, "A_902");
            s.P(foe).Hp = 1;
            s.Game(7).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Eliminated, Is.True);
            Assert.That(s.P(third).AttackSlot, Is.Null);
            Assert.That(s.P(third).Shield, Is.Zero);
            Assert.That(s.P(me).AttackSlot, Is.Null, "The card is discarded after triggering.");
            Assert.That(s.P(me).Shield, Is.EqualTo(4), "The holder keeps their shield.");
        }
    }
}
