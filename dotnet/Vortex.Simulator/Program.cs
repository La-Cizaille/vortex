using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Vortex.Core.Bots;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Rules;

namespace Vortex.Simulator
{
    /// <summary>
    /// Usage: Vortex.Simulator [--players 2,3,4,5] [--games 200] [--seed 1] [--samples 2]
    ///                         [--data core/Runtime/Data] [--out report.md]
    /// Plays, for each table size, one batch of heuristic bots and one "skill gap" batch (one heuristic bot
    /// against random bots), then writes the markdown report. Exit codes: 0 ok, 1 engine errors, 2 usage error.
    /// </summary>
    internal static class Program
    {
        private const int MaxGames = 100_000;

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
                Console.Error.WriteLine("Usage: Vortex.Simulator [--players 2,3,4,5] [--games 200] [--seed 1] [--samples 2] [--data <dir>] [--out <file.md>]");
                return 2;
            }

            GameData data;
            GameConfig config;
            string contentHash;
            try
            {
                string Read(string name) => File.ReadAllText(Path.Combine(options.DataDir, name));
                string cards = Read(CardsFile.FileName);
                data = GameDataLoader.Load(cards, Read(EventsFile.FileName), Read(TechnologiesFile.FileName));
                config = GameDataLoader.LoadConfig(Read(GameConfig.FileName), data);
                contentHash = Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(cards)));
            }
            catch (Exception ex) when (ex is GameDataException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Cannot load content: " + ex.Message);
                return 2;
            }

            var engine = new GameEngine(data, config);
            var scenarios = new List<ScenarioResult>();
            foreach (int players in options.Players)
            {
                int doomRound = config.ForPlayers(players)?.DoomRound ?? int.MaxValue;
                scenarios.Add(Run(engine, players, skillGap: false, options, doomRound));
                scenarios.Add(Run(engine, players, skillGap: true, options, doomRound));
                Console.Error.WriteLine($"{players} players: done.");
            }

            var settings = new RunSettings { Games = options.Games, Seed = options.Seed, Samples = options.Samples, ContentSha256 = contentHash };
            string report = Report.Write(settings, data, scenarios);
            if (options.Output != null)
            {
                File.WriteAllText(options.Output, report);
                Console.Error.WriteLine("Report written to " + options.Output);
            }
            else
            {
                Console.Out.Write(report);
            }

            return scenarios.Any(s => s.Records.Any(r => r.Error != null)) ? 1 : 0;
        }

        private static ScenarioResult Run(GameEngine engine, int players, bool skillGap, Options options, int doomRound)
        {
            var records = new GameRecord[options.Games];
            Parallel.For(0, options.Games, index =>
            {
                // Every game has its own seeds: results do not depend on thread scheduling.
                ulong gameSeed = (options.Seed * 1_000_003UL) + ((ulong)players * 10_007UL) + (skillGap ? 500_009UL : 0UL) + (ulong)index;
                var bots = new List<IBot>();
                int heuristicSeat = index % players;
                for (int seat = 0; seat < players; seat++)
                {
                    ulong botSeed = (gameSeed * 31UL) + (ulong)seat;
                    bool heuristic = !skillGap || seat == heuristicSeat;
                    bots.Add(heuristic ? new HeuristicBot(botSeed, options.Samples) : new RandomBot(botSeed));
                }

                records[index] = GameRunner.Play(engine, index, gameSeed, bots, doomRound);
            });

            return new ScenarioResult(players, skillGap, records.ToList());
        }

        private sealed class Options
        {
            public List<int> Players { get; private set; } = new List<int> { 2, 3, 4, 5 };

            public int Games { get; private set; } = 200;

            public ulong Seed { get; private set; } = 1;

            public int Samples { get; private set; } = 2;

            public string DataDir { get; private set; } = Path.Combine("core", "Runtime", "Data");

            public string? Output { get; private set; }

            public static Options Parse(string[] args)
            {
                var o = new Options();
                for (int i = 0; i < args.Length; i++)
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
                        case "--samples":
                            o.Samples = ParseInt(Value(), 1, 16, "--samples");
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

                return o;
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
