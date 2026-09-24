using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Dice;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Tests.Bots
{
    /// <summary>Bots play legally, deterministically, without seeing hidden information, and skill matters (ADR-0010).</summary>
    [TestFixture]
    public class BotTests
    {
        private static readonly Lazy<GameEngine> Engine = new Lazy<GameEngine>(() => new GameEngine(TestPaths.LoadRealContent(), TestPaths.LoadRealConfig()));

        private static (int Winner, int Commands) Play(IBot[] bots, ulong seed)
        {
            GameEngine engine = Engine.Value;
            GameState state = engine.NewGame(seed, bots.Select((_, i) => "B" + i).ToList()).State;
            int commands = 0;
            while (state.Outcome == null && commands < 5000)
            {
                int actor = state.Pending?.Decision.Player ?? state.CurrentPlayer;
                IReadOnlyList<Command> legal = engine.LegalCommands(state, actor);
                Command chosen = bots[actor].Choose(engine, state, actor, legal);
                Assert.That(legal.Any(c => Same(c, chosen)), Is.True, "Bots only choose legal commands.");
                EngineResult r = engine.Submit(state, actor, chosen);
                Assert.That(r.Accepted, Is.True, () => r.Error!.ToString());
                state = r.State;
                commands++;
            }

            Assert.That(state.Outcome, Is.Not.Null, "Bot games end.");
            return (state.Outcome!.Winner, commands);
        }

        private static bool Same(Command a, Command b) => a.ToString() == b.ToString() && a.DecisionId == b.DecisionId;

        [Test]
        public void Heuristic_bots_play_complete_legal_games_deterministically()
        {
            var first = Play(new IBot[] { new HeuristicBot(1), new HeuristicBot(2), new HeuristicBot(3) }, 42);
            var again = Play(new IBot[] { new HeuristicBot(1), new HeuristicBot(2), new HeuristicBot(3) }, 42);
            Assert.That(again, Is.EqualTo(first), "Same seeds, same game.");
        }

        [Test]
        public void Random_bots_play_complete_legal_games()
        {
            for (ulong seed = 1; seed <= 5; seed++)
            {
                Play(new IBot[] { new RandomBot(seed), new RandomBot(seed + 100) }, seed);
            }
        }

        [Test]
        public void Skill_matters_heuristic_beats_random_in_duels()
        {
            int wins = 0;
            const int games = 20;
            for (int g = 0; g < games; g++)
            {
                int heuristicSeat = g % 2;
                var bots = new IBot[2];
                bots[heuristicSeat] = new HeuristicBot((ulong)g);
                bots[1 - heuristicSeat] = new RandomBot((ulong)g);
                wins += Play(bots, (ulong)(900 + g)).Winner == heuristicSeat ? 1 : 0;
            }

            Assert.That(wins, Is.GreaterThanOrEqualTo(15), "A sensible player must beat random play clearly.");
        }

        [Test]
        public void The_heuristic_bot_cannot_see_hidden_information()
        {
            // Two states identical in everything a player can see, but with a different generator and deck order:
            // the bot must make the same choice (it re-guesses hidden information itself).
            GameEngine engine = Engine.Value;
            GameState visible = engine.NewGame(7, new[] { "A", "B", "C" }).State;
            GameState other = visible.Clone();
            other.Rng = Pcg32.Seeded(123456, 789);
            other.AttackMarket.Deck.Reverse();
            other.DefenseMarket.Deck.Reverse();
            other.EventDeck.Reverse();

            int seat = visible.CurrentPlayer;
            Command a = new HeuristicBot(5).Choose(engine, visible, seat, engine.LegalCommands(visible, seat));
            Command b = new HeuristicBot(5).Choose(engine, other, seat, engine.LegalCommands(other, seat));
            Assert.That(b.ToString(), Is.EqualTo(a.ToString()));
        }

        [Test]
        public void Evaluation_prefers_winning_and_damaging_opponents()
        {
            var bot = new HeuristicBot(1);
            GameState state = Engine.Value.NewGame(3, new[] { "A", "B" }).State;
            double baseline = bot.Evaluate(state, 0);
            GameState hurt = state.Clone();
            hurt.Players[1].Hp -= 5;
            Assert.That(bot.Evaluate(hurt, 0), Is.GreaterThan(baseline));
            GameState won = state.Clone();
            won.Outcome = new GameOutcome { Winner = 0, Condition = WinCondition.Domination };
            Assert.That(bot.Evaluate(won, 0), Is.EqualTo(10_000));
            Assert.That(bot.Evaluate(won, 1), Is.EqualTo(-10_000));
        }

        [Test]
        public void Bots_reject_empty_choices()
        {
            GameState state = Engine.Value.NewGame(3, new[] { "A", "B" }).State;
            Assert.Throws<ArgumentException>(() => new RandomBot(1).Choose(Engine.Value, state, 0, Array.Empty<Command>()));
            Assert.Throws<ArgumentException>(() => new HeuristicBot(1).Choose(Engine.Value, state, 0, Array.Empty<Command>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeuristicBot(1, samples: 0));
        }
    }
}
