using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Content;
using Vortex.Simulator;

namespace Vortex.Core.Tests.Bots
{
    /// <summary>Grid files: cartesian products of content patches, ranked on balance targets.</summary>
    [TestFixture]
    public class GridTests
    {
        private const string Targets = "\"targets\": { \"doomReached\": { \"max\": 0.3 } }";
        private const string HpAxis = "{ \"name\": \"PV\", \"values\": { \"25\": { \"config\": { \"startingHp\": 25, \"maxHp\": 25 } }, \"30\": { \"config\": { \"startingHp\": 30, \"maxHp\": 30 } } } }";
        private const string ShieldAxis = "{ \"name\": \"Bouclier\", \"values\": { \"3\": { \"config\": { \"playerCounts\": { \"5\": { \"startShield\": 3 } } } }, \"5\": { \"config\": { \"playerCounts\": { \"5\": { \"startShield\": 5 } } } } } }";

        private string _dir = string.Empty;

        [SetUp]
        public void CreateDirectory()
        {
            _dir = Path.Combine(Path.GetTempPath(), "vortex-grids-" + TestContext.CurrentContext.Test.ID);
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void DeleteDirectory()
        {
            Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void A_grid_is_the_product_of_its_axes_in_odometer_order()
        {
            (GridSpec spec, List<GridCell> cells) = Load("{ \"name\": \"g\", \"axes\": [ " + HpAxis + ", " + ShieldAxis + " ], " + Targets + " }");

            Assert.That(spec.Axes.Select(a => a.Name), Is.EqualTo(new[] { "PV", "Bouclier" }));
            Assert.That(cells.Select(c => string.Join("/", c.Labels)), Is.EqualTo(new[] { "25/3", "25/5", "30/3", "30/5" }));
            Assert.That(cells[1].Content.Config.StartingHp, Is.EqualTo(25));
            Assert.That(cells[1].Content.Config.ForPlayers(5)!.StartShield, Is.EqualTo(5));
            Assert.That(cells[1].Content.Config.ForPlayers(4)!.StartShield, Is.EqualTo(TestPaths.LoadRealConfig().ForPlayers(4)!.StartShield), "Other tables are untouched.");
            Assert.That(cells.Select(c => c.Content.ContentSha256).Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void A_cell_equal_to_the_repository_content_has_the_reference_fingerprint()
        {
            int shield = TestPaths.LoadRealConfig().ForPlayers(5)!.StartShield;
            string axis = "{ \"name\": \"Bouclier\", \"values\": { \"réf\": {}, \"même\": { \"config\": { \"playerCounts\": { \"5\": { \"startShield\": " + shield + " } } } } } }";
            (GridSpec _, List<GridCell> cells) = Load("{ \"name\": \"g\", \"axes\": [ " + axis + " ], " + Targets + " }");
            string reference = Variants.LoadReference(TestPaths.DataDir).ContentSha256;
            Assert.That(cells.Select(c => c.Content.ContentSha256), Has.All.EqualTo(reference));
        }

        [TestCase("{ \"name\": \"g\", \"axes\": [ HP, HP2 ], TARGETS }", "both change 'config.startingHp'")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ ], TARGETS }", "'axes' must be a list")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ HP ], \"targets\": { \"fun\": { \"min\": 1 } } }", "unknown indicator 'fun'")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ HP ], \"targets\": { \"doomReached\": { \"min\": 0.5, \"max\": 0.2 } } }", "'min' <= 'max'")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ HP ], \"targets\": { \"doomReached\": { } } }", "'min' <= 'max'")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ HP ], \"targets\": { \"doomReached\": { \"max\": \"low\" } } }", "bounds must be numbers")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ HP ], \"targets\": { } }", "at least one indicator")]
        [TestCase("{ \"name\": \"a|b\", \"axes\": [ HP ], TARGETS }", "forbidden character U+007C")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ { \"name\": \"x\", \"values\": { \"a|b\": {} } } ], TARGETS }", "forbidden character U+007C")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ { \"name\": \"x\", \"values\": { \"a\": { \"name\": \"n\" } } } ], TARGETS }", "unknown key 'name'")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ { \"name\": \"x\", \"values\": { } } ], TARGETS }", "labelled patches")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ HP, { \"name\": \"PV\", \"values\": { \"a\": {} } } ], TARGETS }", "same name")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ { \"name\": \"x\", \"values\": { \"a\": { \"config\": { \"playerCounts\": { \"9\": { \"startShield\": 3 } } } } } } ], TARGETS }", "unknown player count '9'")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ { \"name\": \"x\", \"values\": { \"a\": { \"config\": { \"playerCounts\": { \"5\": { \"players\": 4 } } } } } } ], TARGETS }", "does not change 'players'")]
        [TestCase("{ \"name\": \"g\", \"axes\": [ HP ], TARGETS, \"extra\": 1 }", "unknown key 'extra'")]
        public void Invalid_grids_are_refused_with_a_message(string template, string expected)
        {
            ArgumentNullException.ThrowIfNull(template);
            string json = template
                .Replace("HP2", HpAxis.Replace("\"PV\"", "\"PV bis\""), StringComparison.Ordinal)
                .Replace("HP", HpAxis, StringComparison.Ordinal)
                .Replace("TARGETS", Targets, StringComparison.Ordinal);
            Assert.That(() => Load(json), Throws.TypeOf<GameDataException>().With.Message.Contains(expected));
        }

        [Test]
        public void Grids_are_bounded_in_size()
        {
            string values = "{ " + string.Join(", ", Enumerable.Range(0, 8).Select(i => "\"" + i + "\": {}")) + " }";
            string axes = string.Join(", ", Enumerable.Range(0, 3).Select(i => "{ \"name\": \"a" + i + "\", \"values\": " + values + " }"));
            Assert.That(() => Load("{ \"name\": \"g\", \"axes\": [ " + axes + " ], " + Targets + " }"), Throws.TypeOf<GameDataException>().With.Message.Contains("512 combinations"));

            string nine = "{ " + string.Join(", ", Enumerable.Range(0, 9).Select(i => "\"" + i + "\": {}")) + " }";
            Assert.That(() => Load("{ \"name\": \"g\", \"axes\": [ { \"name\": \"a\", \"values\": " + nine + " } ], " + Targets + " }"), Throws.TypeOf<GameDataException>());
        }

        [Test]
        public void Target_distance_is_relative_to_the_missed_bound()
        {
            var range = new GridTarget(GridIndicator.DurationMinutes, 20, 25);
            Assert.That(range.Distance(22), Is.Zero);
            Assert.That(range.Distance(20), Is.Zero);
            Assert.That(range.Distance(15), Is.EqualTo(0.25).Within(1e-12));
            Assert.That(range.Distance(30), Is.EqualTo(0.2).Within(1e-12));
            Assert.That(new GridTarget(GridIndicator.DoomReached, null, 0.3).Distance(0.6), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(new GridTarget(GridIndicator.FirstEliminationRound, 6, null).Distance(double.MaxValue), Is.Zero, "No elimination at all meets a minimum.");
        }

        [Test]
        public void Ranking_puts_most_targets_met_first_then_smallest_distance()
        {
            var targets = new[] { new GridTarget(GridIndicator.DoomReached, null, 0.3), new GridTarget(GridIndicator.DurationMinutes, 20, 25) };
            GridResult a = Result("a", targets, doom: 0.9, minutes: 22);   // 1 met, distance 2.0
            GridResult b = Result("b", targets, doom: 0.4, minutes: 30);   // 0 met, distance 0.53
            GridResult c = Result("c", targets, doom: 0.6, minutes: 21);   // 1 met, distance 1.0
            GridResult d = Result("d", targets, doom: 0.6, minutes: 24);   // same as c: keeps grid order

            List<GridResult> ranked = GridResult.Rank(new[] { a, b, c, d });
            Assert.That(ranked.Select(r => r.Cell.Labels[0]), Is.EqualTo(new[] { "c", "d", "a", "b" }));
            Assert.That(c.Met, Is.EqualTo(1));
            Assert.That(c.Score, Is.EqualTo(1.0).Within(1e-12));
        }

        [Test]
        public void The_grid_report_ranks_marks_the_reference_and_bolds_met_targets()
        {
            var targets = new[] { new GridTarget(GridIndicator.DoomReached, null, 0.3) };
            var spec = new GridSpec("Rythme", "Essai.", new[] { new GridAxis("PV", new List<(string, Newtonsoft.Json.Linq.JObject)>()) }, targets);
            GridResult good = Result("25", targets, doom: 0.2, minutes: 22);
            GridResult reference = Result("30", targets, doom: 0.8, minutes: 22);
            var settings = new RunSettings { Command = "grid --games 4", ContentSha256 = reference.Cell.Content.ContentSha256 };

            string report = GridReport.Write(settings, spec, 5, 4, reference.Cell.Content.ContentSha256, GridResult.Rank(new[] { reference, good }));

            Assert.That(report, Does.Contain("# Grille : Rythme"));
            Assert.That(report, Does.Contain("| 1 | 25 | 1/1 | 0,0 | **20,0 %** |"));
            Assert.That(report, Does.Contain("| 2 (réf.) | 30 | 0/1 |"));
            Assert.That(report, Does.Contain("Aucune erreur du moteur."));
        }

        [Test]
        public void Every_grid_shipped_in_the_repository_is_valid()
        {
            string dir = Path.Combine(TestPaths.RepoRoot, "docs", "balance", "grids");
            string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json") : Array.Empty<string>();
            foreach (string file in files)
            {
                Assert.That(() => Grids.Load(TestPaths.DataDir, file), Throws.Nothing, file);
            }
        }

        private static GridResult Result(string label, IReadOnlyList<GridTarget> targets, double doom, double minutes)
        {
            GameData data = TestPaths.LoadRealContent();
            ContentSet content = label == "30" ? Variants.LoadReference(TestPaths.DataDir) : new ContentSet(label, string.Empty, data, TestPaths.LoadRealConfig(), new string('b', 64));
            var values = new Dictionary<GridIndicator, double>
            {
                [GridIndicator.DoomReached] = doom,
                [GridIndicator.DurationMinutes] = minutes,
            };
            return new GridResult(new GridCell(new[] { label }, content), ScenarioStats.Of(5, new List<GameRecord>(), data), values, targets);
        }

        private (GridSpec Spec, List<GridCell> Cells) Load(string json)
        {
            string path = Path.Combine(_dir, "grid.json");
            File.WriteAllText(path, json);
            return Grids.Load(TestPaths.DataDir, path);
        }
    }
}
