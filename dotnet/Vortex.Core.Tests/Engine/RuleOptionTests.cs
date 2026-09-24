using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.State;
using Vortex.Core.Tests.Support;

namespace Vortex.Core.Tests.Engine
{
    /// <summary>Configurable base-rule options used by the balance work (RULES A4.4, A9).</summary>
    [TestFixture]
    public class RuleOptionTests
    {
        [TestCase(RoundStartRotation.None, 0, 0, 0)]
        [TestCase(RoundStartRotation.Clockwise, 0, 1, 2)]
        [TestCase(RoundStartRotation.CounterClockwise, 0, 2, 1)]
        public void The_first_player_of_each_round_follows_the_rotation(RoundStartRotation rotation, int round1, int round2, int round3)
        {
            var s = Scenario.Start(3, config: TestContent.Config(rotation: rotation));
            int initiative = s.State.InitiativeSeat;
            var starts = new List<int> { s.Current };
            while (starts.Count < 3)
            {
                int round = s.State.Round;
                s.ToMain();
                s.MustAccept(s.Current, Command.EndTurn());
                if (s.State.Round != round)
                {
                    starts.Add(s.Current);
                }
            }

            Assert.That(starts, Is.EqualTo(new[] { round1, round2, round3 }.Select(o => (initiative + o) % 3)));
        }

        [Test]
        public void Counter_clockwise_rotation_gives_the_last_player_two_turns_in_a_row()
        {
            var s = Scenario.Start(3, config: TestContent.Config(rotation: RoundStartRotation.CounterClockwise));
            int last = (s.State.InitiativeSeat + 2) % 3;
            s.TurnOf(last);
            s.MustAccept(last, Command.EndTurn());
            Assert.That(s.State.Round, Is.EqualTo(2));
            Assert.That(s.Current, Is.EqualTo(last));
        }

        [Test]
        public void Rotation_skips_an_eliminated_starting_seat()
        {
            var s = Scenario.Start(3, config: TestContent.Config(rotation: RoundStartRotation.Clockwise));
            int next = (s.State.InitiativeSeat + 1) % 3;
            s.P(next).Hp = 0;
            s.P(next).Eliminated = true;
            s.ToMain();
            s.MustAccept(s.Current, Command.EndTurn());
            s.ToMain();
            s.MustAccept(s.Current, Command.EndTurn());
            Assert.That(s.State.Round, Is.EqualTo(2));
            Assert.That(s.Current, Is.EqualTo((next + 1) % 3), "The round would start with the eliminated seat: the next alive one starts.");
        }

        [Test]
        public void The_galactic_election_needs_the_configured_number_of_technologies()
        {
            var s = Scenario.Start(2, config: TestContent.Config(technologiesToWin: 3));
            s.ToMain();
            int me = s.Current;
            s.P(me).Technologies.AddRange(new[] { TechColor.Red, TechColor.Green });
            s.Equip(me, "A_907");
            s.Equip(me, "D_907");
            s.MustAccept(me, Command.ActivateTechnology());
            Assert.That(s.State.Outcome!.Condition, Is.EqualTo(WinCondition.GalacticElection));
        }

        [Test]
        public void Configuration_rejects_impossible_technology_counts()
        {
            GameConfig config = TestContent.Config(technologiesToWin: 5);
            Assert.That(config.Validate(), Has.Some.Contains("technologiesToWin"));
        }
    }
}
