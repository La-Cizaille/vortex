using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Bots;
using Vortex.Core.Content;
using Vortex.Core.Rules;
using Vortex.Simulator;

namespace Vortex.Core.Tests.Bots
{
    [TestFixture]
    public class SimulatorTests
    {
        private static readonly RunSettings Settings = new RunSettings { Command = "run --games 4", ContentSha256 = new string('a', 64) };

        [Test]
        public void A_small_simulation_reports_every_section_without_errors()
        {
            GameData data = TestPaths.LoadRealContent();
            var engine = new GameEngine(data, TestPaths.LoadRealConfig());
            var scenarios = new List<ScenarioResult>
            {
                new ScenarioResult(2, false, Play(engine, BotLevel.Normal, BotLevel.Normal)),
                new ScenarioResult(2, true, Play(engine, BotLevel.Normal, BotLevel.Random)),
            };

            Assert.That(scenarios.SelectMany(s => s.Records).Select(r => r.Error), Has.All.Null);
            string report = Report.Write(Settings, data, scenarios);
            foreach (string section in new[] { "## 1.", "## 2.", "## 3.", "## 4.", "## 5.", "## 6.", "## 7.", "## 8." })
            {
                Assert.That(report, Does.Contain(section));
            }

            Assert.That(report, Does.Contain("`A_005`"));
            Assert.That(report, Does.Contain("`run --games 4`"));
            Assert.That(report, Does.Contain("`aaaaaaaaaaaa…`"));
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
            Assert.That(r.HpByRound, Has.Count.EqualTo(r.Rounds));
            Assert.That(r.Picks, Has.Count.EqualTo(Enumerable.Range(0, 3).Sum(s => Enumerable.Range(0, 5).Sum(c => r.ColorPicks[s, c]))));
        }

        [Test]
        public void Statistics_count_positions_from_the_initiative_winner()
        {
            GameData data = TestPaths.LoadRealContent();
            var engine = new GameEngine(data, TestPaths.LoadRealConfig());
            List<GameRecord> records = Play(engine, BotLevel.Naive, BotLevel.Naive);
            ScenarioStats s = ScenarioStats.Of(2, records, data);
            List<GameRecord> decided = records.Where(r => r.Finished && r.Winner >= 0).ToList();

            Assert.That(s.Games, Is.EqualTo(records.Count));
            Assert.That(s.PositionWins.Sum(p => p.Successes), Is.EqualTo(decided.Count));
            Assert.That(s.PositionWins[0].Successes, Is.EqualTo(decided.Count(r => r.Winner == r.InitiativeSeat)));
            Assert.That(s.MaxPositionGap, Is.EqualTo(Math.Abs(s.PositionWins[0].Rate - 0.5) * 100).Within(1e-9));
        }

        [Test]
        public void Proportions_and_means_carry_their_standard_error()
        {
            var p = new Proportion(25, 100);
            Assert.That(p.Rate, Is.EqualTo(0.25));
            Assert.That(p.StdErr, Is.EqualTo(Math.Sqrt(0.25 * 0.75 / 100)).Within(1e-12));
            Assert.That(new Proportion(0, 0).StdErr, Is.Zero);

            var m = new Mean(new double[] { 2, 4, 6 });
            Assert.That(m.Value, Is.EqualTo(4));
            Assert.That(m.StdErr, Is.EqualTo(Math.Sqrt(4.0 / 3)).Within(1e-12));
            Assert.That(new Mean(Array.Empty<double>()).StdErr, Is.Zero);
        }

        [Test]
        public void The_comparison_report_shows_each_variant_with_a_margin()
        {
            ContentSet reference = Variants.LoadReference(TestPaths.DataDir);
            var entries = new List<CompareEntry> { Entry(reference), Entry(reference) };
            string report = CompareReport.Write(Settings, entries);

            Assert.That(report, Does.Contain("## 2 joueurs"));
            Assert.That(report, Does.Contain("| Victoires en position 1 |"));
            Assert.That(report, Does.Contain("±"));
            Assert.That(report, Does.Not.Contain(" *)"), "Identical content and seeds cannot differ.");
            Assert.That(report, Does.Contain("Aucune carte ne change de façon significative."));
            Assert.That(report, Does.Contain("Aucune erreur du moteur."));
            Assert.That(report, Does.Contain("| Attaques sur le meneur | — | — |"), "Not applicable in a duel: no difference shown.");
        }

        [TestCase(new string[0], false, "run --players 5 --games 200 --seed 1 --bot normal --skill normal,random")]
        [TestCase(new[] { "run", "--players", "5,3,3", "--bot", "strong", "--skill", "strong,naive" }, false, "run --players 3,5 --games 200 --seed 1 --bot strong --skill strong,naive")]
        [TestCase(new[] { "compare", "--variant", "a.json", "--variant", "b.json", "--games", "50" }, true, "compare --players 5 --games 50 --seed 1 --bot normal --variant a.json --variant b.json")]
        public void Options_are_parsed_and_echoed_explicitly(string[] args, bool compare, string described)
        {
            Program.Options o = Program.Options.Parse(args);
            Assert.That(o.Compare, Is.EqualTo(compare));
            Assert.That(o.Describe(), Is.EqualTo(described));
        }

        [TestCase("compare")]
        [TestCase("run", "--variant", "a.json", "--variant", "b.json")]
        [TestCase("--bot", "godlike")]
        [TestCase("--bot", "2")]
        [TestCase("--skill", "normal")]
        [TestCase("--games", "0")]
        [TestCase("--games", "-5")]
        [TestCase("--players", "1")]
        [TestCase("--seed", "x")]
        [TestCase("--games")]
        [TestCase("--unknown")]
        public void Bad_options_are_refused(params string[] args)
        {
            Assert.That(() => Program.Options.Parse(args), Throws.TypeOf<ArgumentException>());
        }

        private static List<GameRecord> Play(GameEngine engine, BotLevel first, BotLevel second)
        {
            return Enumerable.Range(0, 4)
                .Select(i => GameRunner.Play(engine, i, (ulong)(50 + i), new[] { BotFactory.Create(first, (ulong)i), BotFactory.Create(second, (ulong)i + 9) }, doomRound: 10))
                .ToList();
        }

        private static CompareEntry Entry(ContentSet set)
        {
            var engine = new GameEngine(set.Data, set.Config);
            List<GameRecord> records = Play(engine, BotLevel.Naive, BotLevel.Naive);
            return new CompareEntry(set, new[] { ScenarioStats.Of(2, records, set.Data) }, ScenarioStats.Of(0, records, set.Data));
        }
    }
}
