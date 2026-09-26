using System;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Engine
{
    /// <summary>RULES A3 (setup) and A4-A5 (rounds, turns, market).</summary>
    [TestFixture]
    public class SetupAndFlowTests
    {
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void New_game_matches_the_setup_rules(int players)
        {
            var s = Scenario.Start(players);
            s.AssertValid();
            Assert.That(s.State.Players, Has.Count.EqualTo(players));
            Assert.That(s.State.Players, Has.All.Matches<PlayerState>(p => p.Hp == 30 && p.Shield == 4 && p.Overcharge == 0 && p.AttackSlot == null));
            Assert.That(s.State.AttackMarket.Visible, Has.Count.EqualTo(5));
            Assert.That(s.State.DefenseMarket.Visible, Has.Count.EqualTo(5));
            Assert.That(s.State.Round, Is.EqualTo(1));
            Assert.That(s.State.Phase, Is.EqualTo(TurnPhase.Market));
            Assert.That(s.State.CurrentPlayer, Is.EqualTo(s.State.InitiativeSeat));
            Assert.That(s.State.EventDeck, Has.None.EqualTo(TestContent.DoomEvent));
        }

        [Test]
        public void Same_seed_gives_the_same_game()
        {
            string first = Scenario.Json(Scenario.Start(4, seed: 77).State);
            string again = Scenario.Json(Scenario.Start(4, seed: 77).State);
            string other = Scenario.Json(Scenario.Start(4, seed: 78).State);
            Assert.That(again, Is.EqualTo(first));
            Assert.That(other, Is.Not.EqualTo(first));
        }

        [Test]
        public void Initiative_highest_roll_wins_and_only_tied_players_reroll()
        {
            // A=5, B=8, C=8 → B and C reroll: B=3, C=6 → C starts (RULES A3.4).
            var state = new GameState { Rng = Dice.Pcg32.Seeded(1, 1) };
            var game = new Game(state, TestContent.Data(), TestContent.Config(), new TestCatalog(), Array.Empty<string>())
            {
                DiceOverride = new ScriptedDice(5, 8, 8, 3, 6),
            };
            game.Setup(new[] { "A", "B", "C" });
            Assert.That(state.InitiativeSeat, Is.EqualTo(2));
            Assert.That(game.Events.Count(e => e.Type == GameEventType.InitiativeRolled), Is.EqualTo(5));
            Assert.That(state.CurrentPlayer, Is.EqualTo(2));
        }

        [TestCase(1)]
        [TestCase(6)]
        public void Unsupported_player_counts_are_rejected(int players)
        {
            GameEngine engine = Scenario.Start().Engine;
            Assert.Throws<ArgumentException>(() => engine.NewGame(1, Enumerable.Range(0, players).Select(i => "P" + i).ToList()));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("ABCDEFGHIJKLMNOPQRSTUVWXYZ")]
        [TestCase("bad\nname")]
        public void Invalid_names_are_rejected(string name)
        {
            GameEngine engine = Scenario.Start().Engine;
            Assert.Throws<ArgumentException>(() => engine.NewGame(1, new[] { "ok", name }));
        }

        [Test]
        public void Picking_equips_the_card_refills_the_market_and_ends_the_market_phase()
        {
            var s = Scenario.Start();
            int p = s.Current;
            CardInstance card = s.State.AttackMarket.Visible[2];
            s.MustAccept(p, Command.PickMarket(CardSlot.Attack, 2));
            Assert.That(s.P(p).AttackSlot!.Uid, Is.EqualTo(card.Uid));
            Assert.That(s.State.AttackMarket.Visible, Has.Count.EqualTo(5));
            Assert.That(s.State.Phase, Is.EqualTo(TurnPhase.Main));
        }

        [Test]
        public void Market_events_say_where_a_card_leaves_and_where_the_new_one_comes()
        {
            var s = Scenario.Start();
            int p = s.Current;
            EngineResult r = s.MustAccept(p, Command.PickMarket(CardSlot.Attack, 2));
            GameEvent taken = r.Events.Single(e => e.Type == GameEventType.MarketCardTaken);
            GameEvent revealed = r.Events.Single(e => e.Type == GameEventType.MarketCardRevealed);
            Assert.That((taken.Amount, revealed.Amount), Is.EqualTo((2, 4)), "Taken from its place; the new card comes at the end.");
            Assert.That(s.State.AttackMarket.Visible[4].Uid, Is.EqualTo(revealed.CardUid));
        }

        [Test]
        public void Picking_replaces_the_equipped_card_which_goes_to_the_discard_with_its_tokens()
        {
            var s = Scenario.Start();
            int p = s.Current;
            CardInstance old = s.Equip(p, "A_901");
            old.Torments = 2;
            s.MustAccept(p, Command.PickMarket(CardSlot.Attack, 0));
            Assert.That(s.State.AttackMarket.Discard, Has.Some.Matches<CardInstance>(c => c.Uid == old.Uid && c.Torments == 0));
        }

        [Test]
        public void Recycling_replaces_the_market_and_is_forbidden_after_a_pick()
        {
            var s = Scenario.Start(config: TestContent.Config());
            int p = s.Current;
            var before = s.State.DefenseMarket.Visible.Select(c => c.Uid).ToList();
            s.MustAccept(p, Command.RecycleMarket(CardSlot.Defense));
            Assert.That(s.State.DefenseMarket.Visible.Select(c => c.Uid), Is.Not.EquivalentTo(before));
            Assert.That(s.State.DefenseMarket.Discard.Select(c => c.Uid), Is.SupersetOf(before));
            Assert.That(s.State.Phase, Is.EqualTo(TurnPhase.Main));
        }

        [Test]
        public void Recycle_after_pick_is_rejected_with_two_picks_allowed()
        {
            var catalog = new TestCatalog().Status("unused", new TestAttackBonus(0));
            var s = Scenario.Start(catalog: catalog);
            s.State.MarketPicksLeft = 2;
            int p = s.Current;
            s.MustAccept(p, Command.PickMarket(CardSlot.Attack, 0));
            Assert.That(s.State.Phase, Is.EqualTo(TurnPhase.Market));
            EngineResult r = s.Submit(p, Command.RecycleMarket(CardSlot.Attack));
            Assert.That(r.Error!.Code, Is.EqualTo(CommandErrorCode.CannotRecycleAfterPick));
        }

        [Test]
        public void Rejected_commands_leave_the_state_untouched()
        {
            var s = Scenario.Start(3);
            string before = Scenario.Json(s.State);
            int other = (s.Current + 1) % 3;
            Assert.That(s.Submit(other, Command.EndMarket()).Error!.Code, Is.EqualTo(CommandErrorCode.NotYourTurn));
            Assert.That(s.Submit(s.Current, Command.EndTurn()).Error!.Code, Is.EqualTo(CommandErrorCode.WrongPhase));
            Assert.That(s.Submit(s.Current, Command.PickMarket(CardSlot.Attack, 9)).Error!.Code, Is.EqualTo(CommandErrorCode.InvalidMarketCard));
            Assert.That(s.Submit(s.Current, Command.PickMarket((CardSlot)7, 0)).Error!.Code, Is.EqualTo(CommandErrorCode.InvalidMarketCard));
            Assert.That(s.Submit(s.Current, new Command { Type = (CommandType)99 }).Error!.Code, Is.EqualTo(CommandErrorCode.MalformedCommand));
            Assert.That(s.Submit(s.Current, Command.Answer("0.0", "yes")).Error!.Code, Is.EqualTo(CommandErrorCode.NoDecisionPending));
            Assert.That(Scenario.Json(s.State), Is.EqualTo(before));
        }

        [Test]
        public void Turns_go_clockwise_and_a_new_round_starts_with_the_initiative_seat()
        {
            var s = Scenario.Start(3);
            int first = s.State.InitiativeSeat;
            for (int i = 1; i <= 3; i++)
            {
                s.ToMain();
                s.MustAccept(s.Current, Command.EndTurn());
                int expected = (first + i) % 3;
                Assert.That(s.Current, Is.EqualTo(expected));
            }

            Assert.That(s.State.Round, Is.EqualTo(2));
        }

        [Test]
        public void Eliminated_players_are_skipped_and_rounds_start_at_the_next_alive_seat()
        {
            var s = Scenario.Start(4);
            int first = s.State.InitiativeSeat;
            void Eliminate(int seat)
            {
                s.P(seat).Hp = 0;
                s.P(seat).Eliminated = true;
            }

            // The second seat is skipped.
            Eliminate((first + 1) % 4);
            s.ToMain();
            s.MustAccept(s.Current, Command.EndTurn());
            Assert.That(s.Current, Is.EqualTo((first + 2) % 4));

            // The initiative seat dies before the new round: the round starts at the next alive seat.
            s.ToMain();
            s.MustAccept(s.Current, Command.EndTurn());
            Assert.That(s.Current, Is.EqualTo((first + 3) % 4));
            Eliminate(first);
            s.ToMain();
            s.MustAccept(s.Current, Command.EndTurn());
            Assert.That(s.State.Round, Is.EqualTo(2));
            Assert.That(s.Current, Is.EqualTo((first + 2) % 4));
        }

        [Test]
        public void An_event_is_revealed_each_round_and_the_doom_event_at_the_doom_round()
        {
            var s = Scenario.Start(2, config: TestContent.Config(doomRound: 3));
            Assert.That(s.State.ActiveEventId, Is.EqualTo(TestContent.CalmEvent));
            s.TurnOf(s.State.InitiativeSeat);
            for (int i = 0; i < 4; i++)
            {
                s.ToMain();
                s.MustAccept(s.Current, Command.EndTurn());
            }

            Assert.That(s.State.Round, Is.EqualTo(3));
            Assert.That(s.State.ActiveEventId, Is.EqualTo(TestContent.DoomEvent));
            s.ToMain();
            s.MustAccept(s.Current, Command.EndTurn());
            s.ToMain();
            s.MustAccept(s.Current, Command.EndTurn());
            Assert.That(s.State.EventDiscard, Has.None.EqualTo(TestContent.DoomEvent), "The doom event never enters the piles.");
        }

        [Test]
        public void Events_can_be_disabled()
        {
            var s = Scenario.Start(2, config: TestContent.Config(eventFrequency: 0));
            Assert.That(s.State.ActiveEventId, Is.Null);
        }

        [Test]
        public void Legal_commands_are_exactly_the_accepted_ones()
        {
            var s = Scenario.Start(3);
            foreach (Command c in s.Engine.LegalCommands(s.State, s.Current))
            {
                Assert.That(s.Engine.Submit(s.State, s.Current, c).Accepted, Is.True, c.ToString());
            }

            Assert.That(s.Engine.LegalCommands(s.State, (s.Current + 1) % 3), Is.Empty);
        }
    }
}
