using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Dice;
using Vortex.Core.Projection;
using Vortex.Core.Rules;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Engine
{
    /// <summary>Properties that must hold for any sequence of legal commands.</summary>
    [TestFixture]
    public class RobustnessTests
    {
        // Plays random legal commands (answers included); returns the number of games that ended.
        private static int PlayRandomGames(int games, int players, int maxCommands, System.Action<Scenario>? afterEach = null)
        {
            int finished = 0;
            for (int g = 0; g < games; g++)
            {
                var s = Scenario.Start(players, config: TestContent.Config(doomRound: 8, startingHp: 12), seed: (ulong)(1000 + g));
                Pcg32 choice = Pcg32.Seeded((ulong)g, 99);
                for (int i = 0; i < maxCommands && s.State.Outcome == null; i++)
                {
                    int actor = s.State.Pending?.Decision.Player ?? s.State.CurrentPlayer;
                    IReadOnlyList<Command> legal = s.Engine.LegalCommands(s.State, actor);
                    Assert.That(legal, Is.Not.Empty, "A running game always offers a legal command to someone.");
                    s.MustAccept(actor, legal[choice.NextInt(legal.Count)]);
                    afterEach?.Invoke(s);
                }

                finished += s.State.Outcome != null ? 1 : 0;
            }

            return finished;
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void Random_legal_play_never_breaks_invariants(int players)
        {
            int finished = PlayRandomGames(games: 30, players, maxCommands: 600);
            Assert.That(finished, Is.GreaterThan(0), "Some random games reach an end.");
        }

        [Test]
        public void Clone_copies_every_field()
        {
            PlayRandomGames(games: 3, players: 4, maxCommands: 200, afterEach: s =>
            {
                string original = JsonConvert.SerializeObject(s.State);
                Assert.That(JsonConvert.SerializeObject(s.State.Clone()), Is.EqualTo(original));
            });
        }

        [Test]
        public void Replaying_the_same_commands_gives_the_same_game()
        {
            var first = Scenario.Start(3, seed: 5);
            var second = Scenario.Start(3, seed: 5);
            Pcg32 choice = Pcg32.Seeded(1, 1);
            for (int i = 0; i < 300 && first.State.Outcome == null; i++)
            {
                int actor = first.State.Pending?.Decision.Player ?? first.State.CurrentPlayer;
                IReadOnlyList<Command> legal = first.Engine.LegalCommands(first.State, actor);
                Command c = legal[choice.NextInt(legal.Count)];
                first.MustAccept(actor, c);
                second.MustAccept(actor, c);
            }

            Assert.That(JsonConvert.SerializeObject(second.State), Is.EqualTo(JsonConvert.SerializeObject(first.State)));
        }

        [Test]
        public void The_public_view_never_exposes_hidden_information()
        {
            var s = Scenario.Start(3);
            GameView view = GameView.Of(s.State);
            string json = JsonConvert.SerializeObject(view);

            Assert.That(json, Does.Not.Contain("\"Rng\""));
            Assert.That(json, Does.Not.Contain("\"Increment\""));
            Assert.That(json, Does.Not.Contain("\"EventDeck\":["), "Only the event pile size is public.");

            var hiddenUids = s.State.AttackMarket.Deck.Concat(s.State.DefenseMarket.Deck).Select(c => c.Uid).ToHashSet();
            var visibleUids = view.AttackMarket.Visible.Concat(view.DefenseMarket.Visible).Select(c => c.Uid);
            Assert.That(visibleUids, Has.None.Matches<int>(hiddenUids.Contains));
            Assert.That(view.AttackMarket.DeckCount, Is.EqualTo(s.State.AttackMarket.Deck.Count));
        }

        [Test]
        public void A_tampered_state_is_detected()
        {
            var s = Scenario.Start(3);
            s.State.Players[0].Hp = 999;
            s.State.AttackMarket.Deck.RemoveAt(0);
            s.State.Players[1].Statuses.Add(new StatusState { Uid = 1, Kind = "NoSuchStatus" });
            IReadOnlyList<string> errors = s.Engine.ValidateState(s.State);
            Assert.That(errors, Has.Some.Contains("HP out of range"));
            Assert.That(errors, Has.Some.Contains("Card count not conserved"));
            Assert.That(errors, Has.Some.Contains("unknown status kind"));
        }
    }
}
