using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Bots;
using Vortex.Core.Rules;
using Vortex.Simulator;

namespace Vortex.Core.Tests.Bots
{
    [TestFixture]
    public class SimulatorTests
    {
        [Test]
        public void A_small_simulation_reports_every_section_without_errors()
        {
            var engine = new GameEngine(TestPaths.LoadRealContent(), TestPaths.LoadRealConfig());
            var scenarios = new List<ScenarioResult>();
            foreach (bool gap in new[] { false, true })
            {
                var records = Enumerable.Range(0, 4)
                    .Select(i => GameRunner.Play(engine, i, (ulong)(50 + i), new IBot[] { new HeuristicBot((ulong)i), gap ? new RandomBot((ulong)i) : new HeuristicBot((ulong)i + 9) }, doomRound: 10))
                    .ToList();
                scenarios.Add(new ScenarioResult(2, gap, records));
            }

            Assert.That(scenarios.SelectMany(s => s.Records).Select(r => r.Error), Has.All.Null);
            string report = Report.Write(new RunSettings { Games = 4, Seed = 1, Samples = 2, ContentSha256 = new string('a', 64) }, TestPaths.LoadRealContent(), scenarios);
            foreach (string section in new[] { "## 1.", "## 2.", "## 3.", "## 4.", "## 5.", "## 6.", "## 7." })
            {
                Assert.That(report, Does.Contain(section));
            }

            Assert.That(report, Does.Contain("`A_005`"));
            Assert.That(report, Does.Contain("Aucune : toutes les parties"));
        }

        [Test]
        public void Records_capture_the_game()
        {
            var engine = new GameEngine(TestPaths.LoadRealContent(), TestPaths.LoadRealConfig());
            GameRecord r = GameRunner.Play(engine, 0, 77, new IBot[] { new HeuristicBot(1), new HeuristicBot(2), new HeuristicBot(3) }, doomRound: 10);
            Assert.That(r.Error, Is.Null);
            Assert.That(r.Finished, Is.True);
            Assert.That(r.Winner, Is.InRange(-1, 2));
            Assert.That(r.Turns, Is.GreaterThan(0));
            Assert.That(r.Attacks.Sum(), Is.GreaterThan(0));
            Assert.That(r.EventsRevealed, Is.Not.Empty);
        }
    }
}
