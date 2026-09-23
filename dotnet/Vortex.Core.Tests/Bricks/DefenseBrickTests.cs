using System.Linq;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Effects;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Bricks
{
    [TestFixture]
    public class DefenseBrickTests : BrickTestBase
    {
        [Test]
        public void DenyShieldChangeByOpponents_blocks_opponents_but_not_self_or_the_game()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("DenyShieldChangeByOpponents")));
            s.Equip(foe, "D_901");
            Assert.That(s.Submit(me, Command.Sabotage(foe)).Error!.Code, Is.EqualTo(CommandErrorCode.ShieldChangeNotAllowed));
            Game g = s.Game(2);
            Assert.That(g.TrySetShield(foe, foe, 7, null), Is.True, "The holder may change their own shield.");
            Assert.That(g.TrySetShield(-1, foe, 1, null), Is.True, "The game (events) is not an opponent.");
        }

        [Test]
        public void CapIncomingAttackLoss_caps_and_can_be_discarded_by_an_overcharged_attack()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("CapIncomingAttackLoss", ("max", 1), ("unlessOvercharged", true), ("discardWhenOvercharged", true))));
            s.Equip(foe, "D_901");
            Assert.That(s.Game(8 - 1).ResolveAttack(me, foe, false).HpLost, Is.EqualTo(1));
            s.P(me).Overcharge = 1;
            Assert.That(s.Game(6, 5).ResolveAttack(me, foe, true).HpLost, Is.EqualTo(7));
            Assert.That(s.P(foe).DefenseSlot, Is.Null);
        }

        [Test]
        public void CapIncomingAttackLoss_can_depend_on_remaining_hp()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("CapIncomingAttackLoss", ("max", 1), ("whenHpAtMost", 10))));
            s.Equip(foe, "D_901");
            Assert.That(s.Game(7).ResolveAttack(me, foe, false).HpLost, Is.EqualTo(3));
            s.P(foe).Hp = 10;
            Assert.That(s.Game(7).ResolveAttack(me, foe, false).HpLost, Is.EqualTo(1));
        }

        [Test]
        public void PreventNextAttackLoss_is_used_by_the_next_attack_even_a_harmless_one()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("PreventNextAttackLoss")));
            s.Equip(foe, "D_901");
            s.Game(1).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).DefenseSlot, Is.Null);
            s.Equip(foe, "D_901");
            Assert.That(s.Game(7).ResolveAttack(me, foe, false).HpLost, Is.Zero);
            Assert.That(s.P(foe).DefenseSlot, Is.Null);
        }

        [Test]
        public void PreventFatalAttackLoss_only_triggers_on_a_fatal_loss()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("PreventFatalAttackLoss")));
            s.Equip(foe, "D_901");
            s.Game(7).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).DefenseSlot, Is.Not.Null);
            s.P(foe).Hp = 2;
            Assert.That(s.Game(7).ResolveAttack(me, foe, false).HpLost, Is.Zero);
            Assert.That(s.P(foe).Hp, Is.EqualTo(2));
            Assert.That(s.P(foe).DefenseSlot, Is.Null);
        }

        [Test]
        public void BlockAttackerNextTurn_prevents_the_same_attacker_on_its_next_turn()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("BlockAttackerNextTurn")));
            s.Equip(foe, "D_901");
            s.Game(1).Execute(me, Command.Attack(foe));
            s.MustAccept(me, Command.EndTurn());
            s.TurnOf(me);
            Assert.That(s.Submit(me, Command.Attack(foe)).Error!.Code, Is.EqualTo(CommandErrorCode.TargetNotAllowed));
            s.MustAccept(me, Command.EndTurn());
            s.TurnOf(me);
            Assert.That(s.Engine.LegalCommands(s.State, me), Has.Some.Matches<Command>(c => c.Type == CommandType.Attack), "Only for one turn.");
        }

        [Test]
        public void CapEnemyShield_bounds_the_chosen_enemy_to_the_holders_shield_minus_offset()
        {
            (Scenario s, int me, int foe) = Setup(3, ("D_901", B("CapEnemyShield", ("offset", 2))));
            int third = (me + 2) % 3;
            MarketState market = s.State.DefenseMarket;
            CardInstance card = market.Deck.Concat(market.Discard).First(c => c.CardId == "D_901");
            market.Deck.Remove(card);
            market.Discard.Remove(card);
            s.P(foe).Shield = 6;

            // Equipping the card asks its holder to choose the enemy.
            Game g = s.GameWith(Answers(P(foe)));
            g.Equip(me, card, null);
            Assert.That(card.Vars["enemy"], Is.EqualTo(foe));
            Assert.That(g.ShieldOf(foe), Is.EqualTo(2), "Bounded to 4 − 2.");
            Assert.That(g.ShieldOf(third), Is.EqualTo(4));
        }

        [Test]
        public void DictateAttackerAction_imposes_the_chosen_action()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("DictateAttackerAction")));
            s.Equip(foe, "D_901");
            s.GameWith(Answers("overcharge"), 7).ResolveAttack(me, foe, false);
            StatusState forced = s.P(me).Statuses.Single(st => st.Kind == StatusKinds.ForcedCrewAction);
            Assert.That((CrewAction)forced.Var(StatusKinds.VarAction), Is.EqualTo(CrewAction.Overcharge));
            Assert.That(forced.IsActive, Is.False, "Applies from the attacker's next turn.");
        }

        [Test]
        public void HealWhenOthersDamaged_counts_attack_losses_of_others_only()
        {
            (Scenario s, int me, int foe) = Setup(3, ("D_901", B("HealWhenOthersDamaged", ("amount", 1))));
            int third = (me + 2) % 3;
            s.Equip(third, "D_901");
            s.P(third).Hp = 20;
            s.Game(7).ResolveAttack(me, foe, false);
            Assert.That(s.P(third).Hp, Is.EqualTo(21));
            s.Game(7).ResolveAttack(me, third, false);
            Assert.That(s.P(third).Hp, Is.EqualTo(18), "Not for the holder's own losses: 21 − 3.");
        }

        [Test]
        public void ReflectAttackLoss_sends_the_loss_back_without_chaining()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("ReflectAttackLoss")), ("D_902", B("ReflectAttackLoss")));
            s.Equip(foe, "D_901");
            s.Equip(me, "D_902");
            s.Game(7).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Hp, Is.EqualTo(27));
            Assert.That(s.P(me).Hp, Is.EqualTo(27), "Reflect losses trigger no reaction (RULES B7): no ping-pong.");
        }

        [Test]
        public void HealOnOwnTormentLoss_offsets_torment_losses()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("HealOnOwnTormentLoss")));
            CardInstance card = s.Equip(foe, "D_901");
            s.P(foe).Hp = 20;
            s.Game().PlaceTorment(me, card, null);
            Assert.That(s.P(foe).Hp, Is.EqualTo(20));
        }

        [Test]
        public void TormentAttackerOnLoss_marks_the_attackers_modifier()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("TormentAttackerOnLoss", ("count", 1))));
            s.Equip(foe, "D_901");
            CardInstance mine = s.Equip(me, "A_902");
            s.Game(7).ResolveAttack(me, foe, false);
            Assert.That(mine.Torments, Is.EqualTo(1));
            Assert.That(s.P(me).Hp, Is.EqualTo(29));
        }

        [TestCase(2, 0)]
        [TestCase(3, 6)]
        public void GambleOnHpLoss_even_cancels_odd_adds(int gambleDie, int expectedLoss)
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("GambleOnHpLoss", ("penalty", 3))));
            s.Equip(foe, "D_901");
            Assert.That(s.Game(7, gambleDie).ResolveAttack(me, foe, false).HpLost, Is.EqualTo(expectedLoss));
        }

        [Test]
        public void GambleOnHpLoss_ignores_torment_losses()
        {
            (Scenario s, int me, int foe) = Setup(2, ("D_901", B("GambleOnHpLoss", ("penalty", 3))));
            CardInstance card = s.Equip(foe, "D_901");
            s.Game().PlaceTorment(me, card, null);
            Assert.That(s.P(foe).Hp, Is.EqualTo(29), "No die rolled (a scripted die would be missing).");
        }

        [Test]
        public void Turn_start_bricks_copy_balance_and_grow()
        {
            (Scenario s, int me, int foe) = Setup(3,
                ("D_901", B("CopyHighestShieldOnTurnStart")),
                ("D_902", B("HpBalanceOnTurnStart", ("threshold", 10), ("amount", 3))),
                ("D_903", B("ShieldChangeOnTurnStart", ("amount", 2))));
            int third = (me + 2) % 3;
            s.Equip(foe, "D_901");
            s.P(third).Shield = 7;
            s.Game().Raise((e, src) => e.OnTurnStart(s.Game(), src, foe));
            Assert.That(s.P(foe).Shield, Is.EqualTo(7));

            s.Equip(foe, "D_902");
            s.P(foe).Hp = 20;
            s.Game().Raise((e, src) => e.OnTurnStart(s.Game(), src, foe));
            Assert.That(s.P(foe).Hp, Is.EqualTo(17));
            s.P(foe).Hp = 5;
            s.Game().Raise((e, src) => e.OnTurnStart(s.Game(), src, foe));
            Assert.That(s.P(foe).Hp, Is.EqualTo(8));

            s.Equip(foe, "D_903");
            s.P(foe).Shield = 7;
            s.Game().Raise((e, src) => e.OnTurnStart(s.Game(), src, foe));
            Assert.That(s.P(foe).Shield, Is.EqualTo(8), "Bounded by the maximum.");
        }

        [Test]
        public void PreRollDie_is_used_as_the_first_die_of_the_next_crew_roll()
        {
            var catalog = new TestCatalog().Card("D_901", B("PreRollDie"));
            var s = Scenario.Start(2, catalog);
            int me = s.Current;
            s.Equip(me, "D_901");
            Game g = s.Game(6);
            g.EndMarketPhase();
            Assert.That(s.P(me).Statuses.Single(st => st.Kind == StatusKinds.PreRolledDie).Var(StatusKinds.VarValue), Is.EqualTo(6));
            AttackInfo a = s.Game(1).ResolveAttack(me, 1 - me, false);
            Assert.That(a.Kept, Is.EqualTo(new[] { 6 }), "The scripted 1 is not used: the pre-rolled 6 is.");
            Assert.That(s.P(me).Statuses, Is.Empty);
        }

        [Test]
        public void Event_DisableShields_and_ExtraMarketPicks_apply_to_everyone()
        {
            var catalog = new TestCatalog().Event(TestContent.CalmEvent, B("DisableShields"), B("ExtraMarketPicks", ("amount", 1)));
            var s = Scenario.Start(2, catalog);
            Assert.That(s.State.MarketPicksLeft, Is.EqualTo(2));
            s.ToMain();
            Assert.That(s.Game(3).ResolveAttack(s.Current, 1 - s.Current, false).EffectiveShield, Is.Zero);
            Assert.That(s.P(1 - s.Current).Shield, Is.EqualTo(4), "Disabled, not modified.");
        }
    }
}
