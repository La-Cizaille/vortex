using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vortex.Core.Bots;
using Vortex.Core.Content;
using Vortex.Core.Rules;

namespace Vortex.Simulator
{
    /// <summary>
    /// Balance simulator (docs/balance/README.md). Two commands:
    /// <list type="bullet">
    /// <item><c>run</c> (default): full report on the repository content, or on one variant.</item>
    /// <item><c>compare</c>: the reference against one or more variants, with the same seeds, and confidence intervals.</item>
    /// </list>
    /// Exit codes: 0 ok, 1 engine errors during the games, 2 usage or content error.
    /// </summary>
    internal static class Program
    {
        private const int MaxGames = 100_000;
        private const int MaxVariants = 8;

        private const string Usage =
            "Usage: Vortex.Simulator [run|compare] [--players 2,3,4,5] [--games 200] [--seed 1] [--bot normal]\n" +
            "                        [--skill normal,random] [--variant <file.json>]... [--data <dir>] [--out <file.md>]\n" +
            "  Bot levels: random, naive, normal, strong. 'compare' needs at least one --variant.";

        // Indexed by BotLevel: the names shown in reports and accepted on the command line.
        private static readonly string[] BotNames = { "random", "naive", "normal", "strong" };

        private static int Main(string[] args)
        {
            Options options;
            try
            {
                options = Options.Parse(args);
            }
            catch (ArgumentException ex)
            {
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(Usage);
                return 2;
            }

            var sets = new List<ContentSet>();
            try
            {
                if (options.Compare || options.Variants.Count == 0)
                {
                    sets.Add(Variants.LoadReference(options.DataDir));
                }

                sets.AddRange(options.Variants.Select(v => Variants.LoadVariant(options.DataDir, v)));
                foreach (ContentSet set in sets)
                {
                    int unsupported = options.Players.FirstOrDefault(p => set.Config.ForPlayers(p) == null);
                    if (unsupported != 0)
                    {
                        throw new GameDataException(set.Name + ": the configuration has no preset for " + unsupported + " players.");
                    }
                }
            }
            catch (Exception ex) when (ex is GameDataException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Cannot load content: " + ex.Message);
                return 2;
            }

            var settings = new RunSettings
            {
                Command = options.Describe(),
                ContentSha256 = sets[0].ContentSha256,
                MainBot = BotNames[(int)options.MainBot],
                SkillHero = BotNames[(int)options.SkillHero],
                SkillOthers = BotNames[(int)options.SkillOthers],
            };

            string report;
            bool errors;
            if (options.Compare)
            {
                var entries = new List<CompareEntry>();
                foreach (ContentSet set in sets)
                {
                    var engine = new GameEngine(set.Data, set.Config);
                    var batches = options.Players.Select(p => Batch(engine, set, p, skillGap: false, options)).ToList();
                    var byPlayers = batches.Select(b => ScenarioStats.Of(b.Players, b.Records, set.Data)).ToList();
                    var all = ScenarioStats.Of(0, batches.SelectMany(b => b.Records).ToList(), set.Data);
                    entries.Add(new CompareEntry(set, byPlayers, all));
                    Console.Error.WriteLine(set.Name + ": done.");
                }

                report = CompareReport.Write(settings, entries);
                errors = entries.Any(e => e.ByPlayers.Any(s => s.Errors > 0));
            }
            else
            {
                ContentSet set = sets[0];
                var engine = new GameEngine(set.Data, set.Config);
                var scenarios = new List<ScenarioResult>();
                foreach (int players in options.Players)
                {
                    scenarios.Add(Batch(engine, set, players, skillGap: false, options));
                    scenarios.Add(Batch(engine, set, players, skillGap: true, options));
                    Console.Error.WriteLine($"{players} players: done.");
                }

                report = Report.Write(settings, set.Data, scenarios);
                errors = scenarios.Any(s => s.Records.Any(r => r.Error != null));
            }

            if (options.Output != null)
            {
                File.WriteAllText(options.Output, report);
                Console.Error.WriteLine("Report written to " + options.Output);
            }
            else
            {
                Console.Out.Write(report);
            }

            return errors ? 1 : 0;
        }

        private static ScenarioResult Batch(GameEngine engine, ContentSet set, int players, bool skillGap, Options options)
        {
            int doomRound = set.Config.ForPlayers(players)!.DoomRound;
            var records = new GameRecord[options.Games];
            Parallel.For(0, options.Games, index =>
            {
                // Every game has its own seeds: results do not depend on thread scheduling, and every content set
                // plays the very same deals and dice streams (paired comparison).
                ulong gameSeed = (options.Seed * 1_000_003UL) + ((ulong)players * 10_007UL) + (skillGap ? 500_009UL : 0UL) + (ulong)index;
                int heroSeat = index % players;
                var bots = new List<IBot>();
                for (int seat = 0; seat < players; seat++)
                {
                    BotLevel level = !skillGap ? options.MainBot : (seat == heroSeat ? options.SkillHero : options.SkillOthers);
                    bots.Add(BotFactory.Create(level, (gameSeed * 31UL) + (ulong)seat));
                }

                records[index] = GameRunner.Play(engine, index, gameSeed, bots, doomRound);
            });

            return new ScenarioResult(players, skillGap, records.ToList());
        }

        internal sealed class Options
        {
            private static readonly string DefaultDataDir = Path.Combine("core", "Runtime", "Data");

            public bool Compare { get; private set; }

            public List<int> Players { get; private set; } = new List<int> { 2, 3, 4, 5 };

            public int Games { get; private set; } = 200;

            public ulong Seed { get; private set; } = 1;

            public BotLevel MainBot { get; private set; } = BotLevel.Normal;

            public BotLevel SkillHero { get; private set; } = BotLevel.Normal;

            public BotLevel SkillOthers { get; private set; } = BotLevel.Random;

            public List<string> Variants { get; } = new List<string>();

            public string DataDir { get; private set; } = DefaultDataDir;

            public string? Output { get; private set; }

            public static Options Parse(string[] args)
            {
                var o = new Options();
                int start = 0;
                if (args.Length > 0 && (args[0] == "run" || args[0] == "compare"))
                {
                    o.Compare = args[0] == "compare";
                    start = 1;
                }

                for (int i = start; i < args.Length; i++)
                {
                    string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException("Missing value for " + args[i] + ".");
                    switch (args[i])
                    {
                        case "--players":
                            o.Players = Value().Split(',').Select(p => ParseInt(p, 2, 8, "--players")).Distinct().OrderBy(p => p).ToList();
                            break;
                        case "--games":
                            o.Games = ParseInt(Value(), 1, MaxGames, "--games");
                            break;
                        case "--seed":
                            o.Seed = ulong.TryParse(Value(), NumberStyles.None, CultureInfo.InvariantCulture, out ulong seed) ? seed : throw new ArgumentException("--seed must be a non-negative integer.");
                            break;
                        case "--bot":
                            o.MainBot = ParseBot(Value(), "--bot");
                            break;
                        case "--skill":
                            string[] pair = Value().Split(',');
                            if (pair.Length != 2)
                            {
                                throw new ArgumentException("--skill expects two levels: hero,others (e.g. strong,random).");
                            }

                            o.SkillHero = ParseBot(pair[0], "--skill");
                            o.SkillOthers = ParseBot(pair[1], "--skill");
                            break;
                        case "--variant":
                            o.Variants.Add(Value());
                            break;
                        case "--data":
                            o.DataDir = Value();
                            break;
                        case "--out":
                            o.Output = Value();
                            break;
                        default:
                            throw new ArgumentException("Unknown argument: " + args[i]);
                    }
                }

                if (o.Compare && o.Variants.Count == 0)
                {
                    throw new ArgumentException("compare needs at least one --variant.");
                }

                if (!o.Compare && o.Variants.Count > 1)
                {
                    throw new ArgumentException("run accepts at most one --variant (use compare for several).");
                }

                if (o.Variants.Count > MaxVariants)
                {
                    throw new ArgumentException("At most " + MaxVariants + " variants per comparison.");
                }

                return o;
            }

            /// <summary>The command with every setting made explicit, echoed in the report so it can be replayed.</summary>
            public string Describe()
            {
                var parts = new List<string>
                {
                    Compare ? "compare" : "run",
                    "--players " + string.Join(",", Players),
                    "--games " + Games.ToString(CultureInfo.InvariantCulture),
                    "--seed " + Seed.ToString(CultureInfo.InvariantCulture),
                    "--bot " + BotNames[(int)MainBot],
                };
                if (!Compare)
                {
                    parts.Add("--skill " + BotNames[(int)SkillHero] + "," + BotNames[(int)SkillOthers]);
                }

                parts.AddRange(Variants.Select(v => "--variant " + v.Replace('\\', '/')));
                if (DataDir != DefaultDataDir)
                {
                    parts.Add("--data " + DataDir.Replace('\\', '/'));
                }

                return string.Join(" ", parts);
            }

            private static BotLevel ParseBot(string text, string name)
            {
                int index = Array.IndexOf(BotNames, text);
                if (index < 0)
                {
                    throw new ArgumentException(name + ": unknown bot level '" + text + "' (random, naive, normal, strong).");
                }

                return (BotLevel)index;
            }

            private static int ParseInt(string text, int min, int max, string name)
            {
                if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < min || value > max)
                {
                    throw new ArgumentException(name + " must be an integer in " + min + ".." + max + ".");
                }

                return value;
            }
        }
    }
}
