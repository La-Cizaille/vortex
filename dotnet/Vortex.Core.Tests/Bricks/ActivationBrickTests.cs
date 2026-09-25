using System.Linq;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Effects;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Bricks
{
    [TestFixture]
    public class ActivationBrickTests : BrickTestBase
    {
        // Equips an activation brick on A_901 for the current player and activates it with the given answers.
        private static (Scenario S, int Me, int Foe) Activate(int players, Effect brick, string[] answers, params int[] dice)
        {
            (Scenario s, int me, int foe) = Setup(players, ("A_901", brick));
            CardInstance card = s.Equip(me, "A_901");
            s.GameWith(answers, dice).Execute(me, Command.ActivateCard(card.Uid));
            Assert.That(s.P(me).Modifiers().Any(c => c.Uid == card.Uid), Is.False, "An activated card is discarded.");
            return (s, me, foe);
        }

        [Test]
        public void DisableOpponentShieldThisTurn_until_the_end_of_the_holders_turn()
        {
            (Scenario s, int me, int foe) = Activate(2, B("DisableOpponentShieldThisTurn"), Answers());
            Assert.That(s.Game(3).ResolveAttack(me, foe, false).EffectiveShield, Is.Zero);
            s.MustAccept(me, Command.EndTurn());
            Assert.That(s.P(foe).Statuses, Is.Empty);
        }

        [Test]
        public void ConvertShieldToNextAttack_moves_points_into_the_next_attack()
        {
            (Scenario s, int me, int foe) = Activate(2, B("ConvertShieldToNextAttack"), Answers(N(3)));
            Assert.That(s.P(me).Shield, Is.EqualTo(1));
            Assert.That(s.Game(2).ResolveAttack(me, foe, false).Value, Is.EqualTo(5));
            Assert.That(s.Game(2).ResolveAttack(me, foe, false).Value, Is.EqualTo(2), "Used up by the first attack.");
        }

        [Test]
        public void NextAttackDamageMultiplier_is_used_once()
        {
            (Scenario s, int me, int foe) = Activate(2, B("NextAttackDamageMultiplier", ("factor", 2)), Answers());
            Assert.That(s.Game(7).ResolveAttack(me, foe, false).Damage, Is.EqualTo(6));
            Assert.That(s.Game(7).ResolveAttack(me, foe, false).Damage, Is.EqualTo(3));
        }

        [Test]
        public void NextAttackBonusPerNeutral_counts_neutral_market_cards_at_the_attack()
        {
            (Scenario s, int me, int foe) = Activate(2, B("NextAttackBonusPerNeutral", ("amount", 1)), Answers());
            int neutral = s.Game().NeutralCardsInMarkets();
            Assert.That(s.Game(2).ResolveAttack(me, foe, false).Value, Is.EqualTo(2 + neutral));
        }

        [Test]
        public void NextAttackOvercharged_adds_a_die_without_a_token()
        {
            (Scenario s, int me, int foe) = Activate(2, B("NextAttackOvercharged"), Answers());
            AttackInfo a = s.Game(3, 4).ResolveAttack(me, foe, false);
            Assert.That(a.Overcharged, Is.True);
            Assert.That(a.KeptSum, Is.EqualTo(7));
        }

        [Test]
        public void AllInAttack_discards_own_modifiers_and_keeps_the_best_dice()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("AllInAttack", ("dice", 3), ("keep", 2))));
            CardInstance card = s.Equip(me, "A_901");
            s.Equip(me, "D_902");
            s.Game().Execute(me, Command.ActivateCard(card.Uid));
            Assert.That(s.P(me).Modifiers(), Is.Empty);
            AttackInfo a = s.Game(2, 6, 5).ResolveAttack(me, foe, false);
            Assert.That(a.Kept, Is.EqualTo(new[] { 6, 5 }));
            Assert.That(a.Overcharged, Is.True);
        }

        [Test]
        public void SwapOnNextAttack_exchanges_a_target_modifier_with_a_market_card()
        {
            (Scenario s, int me, int foe) = Activate(2, B("SwapOnNextAttack"), Answers());
            CardInstance theirs = s.Equip(foe, "A_902");
            CardInstance market = s.State.AttackMarket.Visible[0];
            s.GameWith(Answers(C(theirs.Uid), C(market.Uid)), 1).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).AttackSlot!.Uid, Is.EqualTo(market.Uid));
            Assert.That(s.State.AttackMarket.Visible, Has.Some.Matches<CardInstance>(c => c.Uid == theirs.Uid));
        }

        [Test]
        public void DiscardOpponentModifiers_and_StealOpponentModifiers()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("DiscardOpponentModifiers")), ("A_903", B("StealOpponentModifiers")));
            s.Equip(foe, "A_902");
            s.Equip(foe, "D_902").Torments = 1;
            s.Game().Execute(me, Command.ActivateCard(s.Equip(me, "A_901").Uid));
            Assert.That(s.P(foe).Modifiers(), Is.Empty);

            CardInstance atk = s.Equip(foe, "A_904");
            CardInstance def = s.Equip(foe, "D_904");
            def.Torments = 2;
            s.Game().Execute(me, Command.ActivateCard(s.Equip(me, "A_903").Uid));
            Assert.That(s.P(me).AttackSlot!.Uid, Is.EqualTo(atk.Uid));
            Assert.That(s.P(me).DefenseSlot!.Torments, Is.EqualTo(2), "Tokens follow the stolen card.");
            Assert.That(s.P(foe).Modifiers(), Is.Empty);
        }

        [TestCase("steal")]
        [TestCase("destroy")]
        public void StealOrDestroyOpponentModifier(string choice)
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("StealOrDestroyOpponentModifier")));
            CardInstance card = s.Equip(me, "A_901");
            CardInstance theirs = s.Equip(foe, "D_903");
            s.GameWith(Answers(choice)).Execute(me, Command.ActivateCard(card.Uid));
            Assert.That(s.P(foe).DefenseSlot, Is.Null);
            Assert.That(s.P(me).DefenseSlot?.Uid, choice == "steal" ? Is.EqualTo(theirs.Uid) : Is.Null);
        }

        [Test]
        public void StealOrDestroyOpponentModifier_names_the_card_and_where_a_stolen_card_goes()
        {
            (Scenario s, int me, int foe) = Setup(2, ("A_901", B("StealOrDestroyOpponentModifier")));
            CardInstance theirs = s.Equip(foe, "D_903");
            EngineResult r = s.MustAccept(me, Command.ActivateCard(s.Equip(me, "A_901").Uid));
            Assert.That(r.Decision!.Prompt, Is.EqualTo("steal.or.destroy"), "The only opponent and card are chosen without asking.");
            Assert.That(r.Decision.Options.Select(o => (o.Key, o.CardUid, o.Player)), Is.EqualTo(new[] { ("steal", theirs.Uid, me), ("destroy", theirs.Uid, -1) }));
        }

        [Test]
        public void RefreshMarketAndPick_recycles_then_equips_the_chosen_card()
        {
            (Scenario s, int me, int _) = Setup(2, ("A_901", B("RefreshMarketAndPick", ("market", "Defense"))));
            CardInstance card = s.Equip(me, "A_901");
            var before = s.State.DefenseMarket.Visible.Select(c => c.Uid).ToList();

            EngineResult r = s.MustAccept(me, Command.ActivateCard(card.Uid));
            Assert.That(r.Decision!.Kind, Is.EqualTo(Decisions.DecisionKind.ChooseMarketCard));
            Assert.That(r.Decision.Options.Select(o => o.CardUid), Has.None.AnyOf(before.ToArray()), "Offered cards come from the recycled market.");

            int chosen = r.Decision.Options[1].CardUid;
            s.MustAccept(me, Command.Answer(r.Decision.Id, r.Decision.Options[1].Key));
            Assert.That(s.P(me).DefenseSlot!.Uid, Is.EqualTo(chosen));
            Assert.That(s.State.DefenseMarket.Discard.Select(c => c.Uid), Is.SupersetOf(before));
        }

        [Test]
        public void RefreshAllMarkets_replaces_both_markets()
        {
            (Scenario s, int _, int _) = Activate(2, B("RefreshAllMarkets"), Answers());
            Assert.That(s.State.AttackMarket.Discard.Count + s.State.DefenseMarket.Discard.Count, Is.GreaterThanOrEqualTo(10));
        }

        [Test]
        public void TormentOpponentAndNeighbours_never_hits_the_holder()
        {
            (Scenario s, int me, int _) = Setup(4, ("A_901", B("TormentOpponentAndNeighbours", ("count", 1))));
            int opposite = (me + 2) % 4;
            CardInstance card = s.Equip(me, "A_901");
            s.Equip(me, "D_901");
            CardInstance[] others = Enumerable.Range(1, 3).Select(i => s.Equip((me + i) % 4, "D_90" + (i + 1))).ToArray();
            s.GameWith(Answers(P((me + 1) % 4))).Execute(me, Command.ActivateCard(card.Uid));
            Assert.That(s.P(me).DefenseSlot!.Torments, Is.Zero, "The holder is a neighbour of the target but is spared.");
            Assert.That(others[0].Torments, Is.EqualTo(1));
            Assert.That(s.P(opposite).DefenseSlot!.Torments, Is.EqualTo(1), "The target's other neighbour.");
            Assert.That(others[2].Torments, Is.Zero);
        }

        [Test]
        public void Torment_bricks_reactivate_clear_mark_all_and_raise_the_value()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            int foe = 1 - me;
            CardInstance mine = s.Equip(me, "D_901");
            CardInstance theirs = s.Equip(foe, "D_902");
            var source = new EffectSource(EffectOrigin.Event, "test", -1);

            B("TormentAllEquipped", ("count", 1)).Activate(s.Game(), source);
            Assert.That((mine.Torments, theirs.Torments), Is.EqualTo((1, 1)));
            Assert.That((s.P(me).Hp, s.P(foe).Hp), Is.EqualTo((29, 29)));

            B("TormentValueBonus", ("amount", 1)).Activate(s.Game(), source);
            Assert.That(s.Game().TormentValue(), Is.EqualTo(2));
            B("ReactivateTorments").Activate(s.Game(), source);
            Assert.That((s.P(me).Hp, s.P(foe).Hp), Is.EqualTo((27, 27)));

            B("ClearPlayerTorments").Activate(s.Game(), source);
            Assert.That(mine.Torments + theirs.Torments, Is.Zero);

            s.State.AttackMarket.Visible[0].Torments = 2;
            mine.Torments = 1;
            s.P(me).Hp = 10;
            B("ClearAllTormentsHealPer", ("amount", 3)).Activate(s.Game(), new EffectSource(EffectOrigin.Card, "test", me));
            Assert.That(s.P(me).Hp, Is.EqualTo(10 + 9), "3 tokens removed × 3 HP.");
            Assert.That(s.State.AttackMarket.Visible[0].Torments, Is.Zero);
        }

        [Test]
        public void ConvertHpToShield_never_goes_to_zero_hp_nor_above_the_bound()
        {
            (Scenario s, int me, int _) = Activate(2, B("ConvertHpToShield"), Answers(N(3)));
            Assert.That(s.P(me).Hp, Is.EqualTo(27));
            Assert.That(s.P(me).Shield, Is.EqualTo(7));
            // Only 1 point of room is left: the single option is chosen without asking.
            s.Game().Execute(me, Command.ActivateCard(s.Equip(me, "A_901").Uid));
            Assert.That(s.P(me).Hp, Is.EqualTo(26));
            Assert.That(s.P(me).Shield, Is.EqualTo(8));
        }

        [Test]
        public void SwapTwoShields_needs_both_permissions()
        {
            (Scenario s, int me, int foe) = Setup(3, ("A_901", B("SwapTwoShields")), ("D_901", B("DenyShieldChangeByOpponents")));
            int third = (me + 2) % 3;
            s.P(foe).Shield = 7;
            s.GameWith(Answers(P(foe), P(third))).Execute(me, Command.ActivateCard(s.Equip(me, "A_901").Uid));
            Assert.That((s.P(foe).Shield, s.P(third).Shield), Is.EqualTo((4, 7)));
            s.Equip(third, "D_901");
            s.GameWith(Answers(P(foe), P(third))).Execute(me, Command.ActivateCard(s.Equip(me, "A_901").Uid));
            Assert.That((s.P(foe).Shield, s.P(third).Shield), Is.EqualTo((4, 7)), "Refused for one: nothing happens.");
        }

        [Test]
        public void Shield_setters_own_all_and_disable()
        {
            (Scenario s, int me, int foe) = Activate(2, B("SetOwnShield", ("value", 8)), Answers());
            Assert.That(s.P(me).Shield, Is.EqualTo(8));
            s.Equip(foe, "D_901");
            s.Catalog.Card("D_901", B("DenyShieldChangeByOpponents"));
            B("SetAllShields", ("value", 0)).Activate(s.Game(), new EffectSource(EffectOrigin.Event, "test", -1));
            Assert.That((s.P(me).Shield, s.P(foe).Shield), Is.EqualTo((0, 0)), "An event is not an opponent.");

            s.P(me).Shield = 5;
            B("DisableOwnShieldUntilNextTurn").Activate(s.Game(), new EffectSource(EffectOrigin.Card, "test", me));
            Assert.That(s.Game(3).ResolveAttack(foe, me, false).EffectiveShield, Is.Zero);
        }

        [Test]
        public void RotateShields_moves_every_shield_one_seat_clockwise()
        {
            (Scenario s, int me, int _) = Setup(3, ("A_901", B("RotateShields")));
            s.P(0).Shield = 1;
            s.P(1).Shield = 2;
            s.P(2).Shield = 3;
            s.GameWith(Answers("clockwise")).Execute(me, Command.ActivateCard(s.Equip(me, "A_901").Uid));

            // Each shield moves to the next seat clockwise: seat 0 receives seat 2's, seat 1 receives seat 0's...
            Assert.That(new[] { s.P(0).Shield, s.P(1).Shield, s.P(2).Shield }, Is.EqualTo(new[] { 3, 1, 2 }));
        }

        [Test]
        public void RotateShields_names_the_neighbour_receiving_the_holders_shield_in_each_direction()
        {
            (Scenario s, int me, int _) = Setup(4, ("A_901", B("RotateShields")));
            EngineResult r = s.MustAccept(me, Command.ActivateCard(s.Equip(me, "A_901").Uid));
            Assert.That(r.Decision!.Options.Select(o => (o.Key, o.Player)), Is.EqualTo(new[] { ("clockwise", (me + 1) % 4), ("counterclockwise", (me + 3) % 4) }));
        }

        [Test]
        public void RotateShields_skips_players_whose_shield_cannot_be_modified()
        {
            (Scenario s, int me, int _) = Setup(3, ("A_901", B("RotateShields")), ("D_901", B("DenyShieldChangeByOpponents")));
            int protectedSeat = (me + 1) % 3;
            s.Equip(protectedSeat, "D_901");
            s.P(0).Shield = 1;
            s.P(1).Shield = 2;
            s.P(2).Shield = 3;
            int kept = s.P(protectedSeat).Shield;
            int[] others = new[] { 0, 1, 2 }.Where(seat => seat != protectedSeat).ToArray();
            int[] before = others.Select(seat => s.P(seat).Shield).ToArray();

            s.GameWith(Answers("counterclockwise")).Execute(me, Command.ActivateCard(s.Equip(me, "A_901").Uid));

            Assert.That(s.P(protectedSeat).Shield, Is.EqualTo(kept), "A protected shield stays; its seat is skipped.");
            Assert.That(others.Select(seat => s.P(seat).Shield), Is.EqualTo(before.Reverse()), "The two others exchange.");
        }

        [Test]
        public void Health_and_overcharge_bricks()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            var mine = new EffectSource(EffectOrigin.Card, "test", me);
            s.P(me).Hp = 10;
            B("Heal", ("amount", 15)).Activate(s.Game(), mine);
            Assert.That(s.P(me).Hp, Is.EqualTo(25));
            B("HealPerNeutral", ("amount", 1)).Activate(s.Game(), mine);
            Assert.That(s.P(me).Hp, Is.EqualTo(System.Math.Min(30, 25 + s.Game().NeutralCardsInMarkets())));
            B("GainOvercharge", ("amount", 1)).Activate(s.Game(), mine);
            Assert.That(s.P(me).Overcharge, Is.EqualTo(1));
            B("KeepOverchargeUntilNextTurn").Activate(s.Game(), mine);
            s.Game(7).ResolveAttack(1 - me, me, false);
            Assert.That(s.P(me).Overcharge, Is.EqualTo(1), "Damage does not remove the token while protected.");
        }

        [Test]
        public void NextOverchargedAttackAdvantage_waits_for_an_overcharged_attack()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            B("NextOverchargedAttackAdvantage").Activate(s.Game(), new EffectSource(EffectOrigin.Card, "test", me));
            Assert.That(s.Game(3).ResolveAttack(me, 1 - me, false).Rolls, Has.Count.EqualTo(1), "Not overcharged: no advantage, not used.");
            s.P(me).Overcharge = 1;
            AttackInfo a = s.Game(1, 1, 6, 6).ResolveAttack(me, 1 - me, true);
            Assert.That(a.Rolls, Has.Count.EqualTo(4));
            Assert.That(a.KeptSum, Is.EqualTo(12));
            Assert.That(s.P(me).Statuses, Is.Empty);
        }

        [Test]
        public void DiscardAllEquippedModifiers_empties_every_slot()
        {
            var s = Scenario.Start(2);
            s.Equip(0, "A_901");
            s.Equip(1, "D_901");
            B("DiscardAllEquippedModifiers").Activate(s.Game(), new EffectSource(EffectOrigin.Event, "test", -1));
            Assert.That(s.State.Players.SelectMany(p => p.Modifiers()), Is.Empty);
        }

        [Test]
        public void RedirectAttacksUntilNextTurn_sends_the_attack_elsewhere_once()
        {
            var s = Scenario.Start(3);
            s.ToMain();
            int me = s.Current;
            int foe = (me + 1) % 3;
            int third = (me + 2) % 3;
            B("RedirectAttacksUntilNextTurn").Activate(s.Game(), new EffectSource(EffectOrigin.Technology, "TECH_BLUE", foe));
            AttackInfo a = s.GameWith(Answers(P(third)), 7).ResolveAttack(me, foe, false);
            Assert.That(a.Target, Is.EqualTo(third));
            Assert.That(s.P(third).Hp, Is.EqualTo(27));
            Assert.That(s.P(foe).Hp, Is.EqualTo(30));
        }

        [Test]
        public void ExtraCrewActionsThisTurn_allows_a_second_different_action()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            B("ExtraCrewActionsThisTurn", ("amount", 1)).Activate(s.Game(), new EffectSource(EffectOrigin.Technology, "TECH_YELLOW", me));
            s.MustAccept(me, Command.Overcharge());
            s.MustAccept(me, Command.RerollShield());
            Assert.That(s.Submit(me, Command.Sabotage(1 - me)).Error!.Code, Is.EqualTo(CommandErrorCode.NoCrewActionLeft));
        }

        [Test]
        public void SwapOnNextAttack_never_uses_the_attackers_own_card()
        {
            (Scenario s, int me, int foe) = Activate(2, B("SwapOnNextAttack"), Answers());
            CardInstance theirs = s.Equip(foe, "A_902");
            CardInstance mine = s.Equip(me, "A_903");
            Assert.Throws<EngineException>(() => s.GameWith(Answers(C(theirs.Uid), C(mine.Uid)), 1).ResolveAttack(me, foe, false), "The attacker's card is not an option.");
        }
    }
}
