using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Dice;
using Vortex.Core.Events;
using Vortex.Core.Rules;

namespace Vortex.Core.Tests.Content
{
    /// <summary>
    /// Plays many random games with the real content (every card, event and technology) and checks the state
    /// invariants after each command. This is where unexpected interactions between bricks would surface.
    /// </summary>
    [TestFixture]
    public class RealContentPlayTests
    {
        private static readonly Lazy<GameEngine> Engine = new Lazy<GameEngine>(() =>
        {
            GameData data = TestPaths.LoadRealContent();
            GameConfig config = TestPaths.LoadRealConfig();
            return new GameEngine(data, config);
        });

        [TestCase(2, 40)]
        [TestCase(3, 40)]
        [TestCase(5, 30)]
        public void Random_games_with_real_content_keep_every_invariant(int players, int games)
        {
            GameEngine engine = Engine.Value;
            var seenCards = new HashSet<string>(StringComparer.Ordinal);
            var seenEvents = new HashSet<string>(StringComparer.Ordinal);
            int finished = 0;
            for (int g = 0; g < games; g++)
            {
                EngineResult r = engine.NewGame((ulong)(7000 + (players * 1000) + g), Enumerable.Range(0, players).Select(i => "P" + i).ToList());
                var state = r.State;
                Pcg32 choice = Pcg32.Seeded((ulong)g, (ulong)players);
                for (int i = 0; i < 3000 && state.Outcome == null; i++)
                {
                    int actor = state.Pending?.Decision.Player ?? state.CurrentPlayer;
                    IReadOnlyList<Command> legal = engine.LegalCommands(state, actor);
                    Assert.That(legal, Is.Not.Empty, "Someone can always act in a running game.");

                    // Favour playing over ending the turn so that cards and attacks are exercised.
                    List<Command> preferred = legal.Where(c => c.Type != CommandType.EndTurn && c.Type != CommandType.EndMarket).ToList();
                    Command command = preferred.Count > 0 && choice.NextInt(4) > 0 ? preferred[choice.NextInt(preferred.Count)] : legal[choice.NextInt(legal.Count)];

                    EngineResult result = engine.Submit(state, actor, command);
                    Assert.That(result.Accepted, Is.True, () => "Legal command rejected: " + command + " → " + result.Error);
                    state = result.State;
                    foreach (GameEvent e in result.Events)
                    {
                        if (e.Type == GameEventType.CardActivated || e.Type == GameEventType.CardEquipped)
                        {
                            seenCards.Add(e.Id!);
                        }
                        else if (e.Type == GameEventType.EventRevealed)
                        {
                            seenEvents.Add(e.Id!);
                        }
                    }

                    IReadOnlyList<string> errors = engine.ValidateState(state);
                    Assert.That(errors, Is.Empty, () => "After " + command + ": " + string.Join("; ", errors));
                }

                finished += state.Outcome != null ? 1 : 0;
            }

            Assert.That(finished, Is.GreaterThan(games / 2), "Most random games reach an end.");
            Assert.That(seenCards.Count, Is.GreaterThan(40), "Random play exercises most cards.");
            Assert.That(seenEvents.Count, Is.GreaterThanOrEqualTo(5));
        }
    }
}
