using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Engine
{
    /// <summary>ADR-0009 decisions, RULES A8 technologies, A9 eliminations and victory, B5 statuses, B7 order.</summary>
    [TestFixture]
    public class DecisionAndLifecycleTests
    {
        // Finds a seed whose next attack die is a critical, so the target must choose a modifier to lose.
        private static (Scenario S, int Me, int Foe) CriticalSetup()
        {
            for (ulong seed = 1; seed < 500; seed++)
            {
                var s = Scenario.Start(2, seed: seed);
                s.ToMain();
                int me = s.Current;
                int foe = 1 - me;
                s.Equip(foe, "A_901");
                s.Equip(foe, "D_901");
                if (s.State.Rng.Clone().Roll(8) == 8)
                {
                    return (s, me, foe);
                }
            }

            Assert.Fail("No seed gives a critical first roll.");
            return default;
        }

        [Test]
        public void A_critical_hit_suspends_until_the_target_chooses()
        {
            (Scenario s, int me, int foe) = CriticalSetup();
            EngineResult r = s.MustAccept(me, Command.Attack(foe));
            Assert.That(r.Decision, Is.Not.Null);
            Assert.That(r.Decision!.Player, Is.EqualTo(foe));
            Assert.That(r.Decision.Kind, Is.EqualTo(DecisionKind.ChooseModifier));
            Assert.That(r.Decision.Options, Has.Count.EqualTo(2));
            Assert.That(r.Events.Select(e => e.Type), Has.Member(GameEventType.CriticalHit), "Facts up to the decision are delivered.");
            Assert.That(s.P(foe).Hp, Is.EqualTo(30), "Nothing is applied until the decision is answered.");

            DecisionOption keepAttack = r.Decision.Options.Single(o => o.CardUid == s.P(foe).DefenseSlot!.Uid);
            EngineResult done = s.MustAccept(foe, Command.Answer(r.Decision.Id, keepAttack.Key));
            Assert.That(done.Decision, Is.Null);
            Assert.That(s.P(foe).DefenseSlot, Is.Null);
            Assert.That(s.P(foe).AttackSlot, Is.Not.Null);
            Assert.That(s.P(foe).Hp, Is.EqualTo(30 - 4));
            Assert.That(done.Events.Select(e => e.Type), Has.No.Member(GameEventType.AttackDeclared), "The delivered prefix is not repeated.");
            Assert.That(done.Events.Select(e => e.Type), Has.Member(GameEventType.HpLost));
        }

        [Test]
        public void Wrong_answers_are_rejected_without_changing_the_state()
        {
            (Scenario s, int me, int foe) = CriticalSetup();
            DecisionRequest d = s.MustAccept(me, Command.Attack(foe)).Decision!;
            string before = Scenario.Json(s.State);
            Assert.That(s.Submit(me, Command.Answer(d.Id, d.Options[0].Key)).Error!.Code, Is.EqualTo(CommandErrorCode.NotYourDecision));
            Assert.That(s.Submit(foe, Command.Answer("9.9", d.Options[0].Key)).Error!.Code, Is.EqualTo(CommandErrorCode.WrongDecision));
            Assert.That(s.Submit(foe, Command.Answer(d.Id, "c999999")).Error!.Code, Is.EqualTo(CommandErrorCode.InvalidOption));
            Assert.That(s.Submit(foe, Command.Answer(d.Id, null!)).Error!.Code, Is.EqualTo(CommandErrorCode.InvalidOption));
            Assert.That(s.Submit(me, Command.EndTurn()).Error!.Code, Is.EqualTo(CommandErrorCode.DecisionPending));
            Assert.That(Scenario.Json(s.State), Is.EqualTo(before));
        }

        [Test]
        public void A_suspended_game_survives_serialization()
        {
            (Scenario s, int me, int foe) = CriticalSetup();
            DecisionRequest d = s.MustAccept(me, Command.Attack(foe)).Decision!;
            s.State = JsonConvert.DeserializeObject<GameState>(JsonConvert.SerializeObject(s.State))!;
            s.AssertValid();
            EngineResult r = s.MustAccept(foe, Command.Answer(d.Id, d.Options[1].Key));
            Assert.That(r.Decision, Is.Null);
        }

        [Test]
        public void Reaching_zero_hp_eliminates_and_the_last_ship_wins()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            int foe = 1 - me;
            s.P(foe).Hp = 1;
            s.Equip(foe, "A_901");
            s.Game(7).ResolveAttack(me, foe, false);
            Assert.That(s.P(foe).Eliminated, Is.True);
            Assert.That(s.P(foe).AttackSlot, Is.Null, "An eliminated ship's cards are discarded.");
            Assert.That(s.State.Outcome!.Winner, Is.EqualTo(me));
            Assert.That(s.State.Outcome.Condition, Is.EqualTo(WinCondition.Domination));
            Assert.That(s.State.Phase, Is.EqualTo(TurnPhase.GameOver));
            Assert.That(s.Engine.LegalCommands(s.State, me), Is.Empty);
        }

        [Test]
        public void Simultaneous_elimination_of_everyone_is_a_draw()
        {
            var catalog = new TestCatalog().Event(TestContent.CalmEvent, new TestEventDamage(5));
            var s = Scenario.Start(3, catalog);
            foreach (PlayerState p in s.State.Players)
            {
                p.Hp = 3;
            }

            s.Game().BeginRound();
            Assert.That(s.State.Outcome!.Condition, Is.EqualTo(WinCondition.Draw));
            Assert.That(s.State.Outcome.Winner, Is.EqualTo(-1));
        }

        [Test]
        public void A_technology_needs_two_modifiers_of_the_same_non_neutral_colour()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            s.Equip(me, "A_901");
            s.Equip(me, "D_902");
            Assert.That(s.Submit(me, Command.ActivateTechnology()).Error!.Code, Is.EqualTo(CommandErrorCode.NoTechnologyCombo), "Neutral never forms a combo.");
            s.Equip(me, "A_907");
            s.Equip(me, "D_909");
            Assert.That(s.Submit(me, Command.ActivateTechnology()).Error!.Code, Is.EqualTo(CommandErrorCode.NoTechnologyCombo), "Blue and red differ.");
            s.Equip(me, "D_908");
            s.MustAccept(me, Command.ActivateTechnology());
            Assert.That(s.P(me).Technologies, Is.EqualTo(new[] { TechColor.Blue }));
            Assert.That(s.P(me).AttackSlot, Is.Null);
            Assert.That(s.P(me).DefenseSlot, Is.Null);
        }

        [Test]
        public void Obtaining_every_technology_wins_the_galactic_election()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            s.P(me).Technologies.AddRange(new[] { TechColor.Red, TechColor.Green, TechColor.Yellow });
            s.Equip(me, "A_907");
            s.Equip(me, "D_907");
            s.MustAccept(me, Command.ActivateTechnology());
            Assert.That(s.State.Outcome!.Winner, Is.EqualTo(me));
            Assert.That(s.State.Outcome.Condition, Is.EqualTo(WinCondition.GalacticElection));
        }

        [Test]
        public void An_activated_card_runs_its_effect_and_is_discarded()
        {
            var catalog = new TestCatalog().Card("A_901", new TestActivateOvercharge());
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            int me = s.Current;
            CardInstance card = s.Equip(me, "A_901");
            Assert.That(s.Engine.LegalCommands(s.State, me), Has.Some.Matches<Command>(c => c.Type == CommandType.ActivateCard && c.CardUid == card.Uid));
            s.MustAccept(me, Command.ActivateCard(card.Uid));
            Assert.That(s.P(me).Overcharge, Is.EqualTo(1));
            Assert.That(s.P(me).AttackSlot, Is.Null);
            Assert.That(s.Submit(me, Command.ActivateCard(card.Uid)).Error!.Code, Is.EqualTo(CommandErrorCode.InvalidCard));
        }

        [Test]
        public void A_card_without_activation_cannot_be_activated()
        {
            var s = Scenario.Start(2);
            s.ToMain();
            int me = s.Current;
            CardInstance card = s.Equip(me, "A_902");
            Assert.That(s.Submit(me, Command.ActivateCard(card.Uid)).Error!.Code, Is.EqualTo(CommandErrorCode.CardNotActivatable));
        }

        [Test]
        public void An_imposed_action_restricts_the_next_turn_and_must_be_done_when_possible()
        {
            var catalog = new TestCatalog().Card("D_901", new TestImposeOnHit(CrewAction.Overcharge));
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            int me = s.Current;
            int foe = 1 - me;
            s.Equip(foe, "D_901");
            s.Game(5).Execute(me, Command.Attack(foe)); // scripted die: no critical
            Assert.That(s.P(me).Statuses.Single().IsActive, Is.False, "Dormant until the attacker's next turn.");
            s.MustAccept(me, Command.EndTurn());
            s.TurnOf(me);
            Assert.That(s.Submit(me, Command.Attack(foe)).Error!.Code, Is.EqualTo(CommandErrorCode.ForcedActionRequired));
            Assert.That(s.Submit(me, Command.EndTurn()).Error!.Code, Is.EqualTo(CommandErrorCode.ForcedActionRequired));
            s.MustAccept(me, Command.Overcharge());
            Assert.That(s.P(me).Statuses, Is.Empty);
            s.MustAccept(me, Command.EndTurn());
        }

        [Test]
        public void An_impossible_imposed_action_means_no_crew_action_but_the_turn_can_end()
        {
            var catalog = new TestCatalog().Card("D_901", new TestImposeOnHit(CrewAction.Overcharge));
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            int me = s.Current;
            int foe = 1 - me;
            s.Equip(foe, "D_901");
            s.Game(5).Execute(me, Command.Attack(foe)); // scripted die: no critical
            s.MustAccept(me, Command.EndTurn());
            s.TurnOf(me);
            s.P(me).Overcharge = 1; // overcharge is full: the imposed action is impossible
            Assert.That(s.Submit(me, Command.RerollShield()).Error!.Code, Is.EqualTo(CommandErrorCode.ForcedActionRequired));
            s.MustAccept(me, Command.EndTurn());
            Assert.That(s.P(me).Statuses, Is.Empty, "The status expired at the end of that turn.");
        }

        [Test]
        public void Reactions_follow_the_resolution_order_of_B7()
        {
            var log = new List<string>();
            var catalog = new TestCatalog()
                .Event(TestContent.CalmEvent, new TestRecorder("event", log))
                .Card("A_901", new TestRecorder("atk", log))
                .Card("D_901", new TestRecorder("def", log));
            var s = Scenario.Start(3, catalog);
            s.ToMain();
            int p0 = s.Current;
            int p1 = (p0 + 1) % 3;
            int p2 = (p0 + 2) % 3;
            s.Equip(p2, "D_901");
            s.Equip(p2, "A_901");
            s.Equip(p1, "D_901");
            log.Clear();
            s.MustAccept(p0, Command.EndTurn());
            // Turn of p1: global first, then p1 (current), then p2; ATK before DEF for each player.
            Assert.That(log, Is.EqualTo(new[] { "event@-1", "def@" + p1, "atk@" + p2, "def@" + p2 }));
        }

        [Test]
        public void A_reaction_loop_is_stopped_by_the_depth_limit()
        {
            var catalog = new TestCatalog().Card("D_901", new TestLoop());
            var s = Scenario.Start(2, catalog);
            s.ToMain();
            int me = s.Current;
            s.Equip(1 - me, "D_901");
            Assert.Throws<EngineException>(() => s.Game(7).ResolveAttack(me, 1 - me, false));
        }
    }
}
