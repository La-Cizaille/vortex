using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Content;
using Vortex.Core.State;

namespace Vortex.Simulator
{
    /// <summary>A proportion with its standard error (binomial).</summary>
    internal readonly struct Proportion
    {
        public Proportion(double successes, double total)
        {
            Successes = successes;
            Total = total;
        }

        public double Successes { get; }

        public double Total { get; }

        public double Rate => Total == 0 ? 0 : Successes / Total;

        public double StdErr => Total == 0 ? 0 : Math.Sqrt(Rate * (1 - Rate) / Total);
    }

    /// <summary>A mean with its standard error.</summary>
    internal readonly struct Mean
    {
        public Mean(IReadOnlyCollection<double> values)
        {
            Count = values.Count;
            Value = Count == 0 ? 0 : values.Average();
            double m = Value;
            double variance = Count < 2 ? 0 : values.Sum(v => (v - m) * (v - m)) / (Count - 1);
            StdErr = Count == 0 ? 0 : Math.Sqrt(variance / Count);
        }

        public int Count { get; }

        public double Value { get; }

        public double StdErr { get; }
    }

    /// <summary>Every indicator of a batch of games at one table size.</summary>
    internal sealed class ScenarioStats
    {
        private ScenarioStats(int players)
        {
            Players = players;
        }

        public int Players { get; }

        public int Games { get; private set; }

        public int Errors { get; private set; }

        public Proportion Finished { get; private set; }

        public Proportion Draws { get; private set; }

        public Proportion Elections { get; private set; }

        public Proportion DoomReached { get; private set; }

        public Mean Rounds { get; private set; }

        public List<int> RoundsSorted { get; private set; } = new List<int>();

        public Mean Turns { get; private set; }

        /// <summary>Win rate per turn-order position (0 = initiative winner), among decided games.</summary>
        public Proportion[] PositionWins { get; private set; } = Array.Empty<Proportion>();

        /// <summary>Largest gap between a position's win rate and the fair share, in points.</summary>
        public double MaxPositionGap => PositionWins.Length == 0 ? 0 : PositionWins.Max(p => Math.Abs(p.Rate - (1.0 / Players))) * 100;

        public Proportion Harmless { get; private set; }

        public double AttacksPerTurn { get; private set; }

        public double HpPerAttack { get; private set; }

        public Proportion AttacksOnLeader { get; private set; }

        public Proportion AttacksOnWeakest { get; private set; }

        public Mean FirstElimination { get; private set; }

        public Proportion MidGameLeaderWins { get; private set; }

        /// <summary>Share of each crew action (indexed by <see cref="Vortex.Core.Commands.CrewAction"/>) among all crew actions.</summary>
        public Proportion[] CrewActions { get; private set; } = Array.Empty<Proportion>();

        /// <summary>Win rate of a card taker across all cards (reference of card deviations).</summary>
        public double CardBaseline { get; private set; }

        public Dictionary<string, CardStats> Cards { get; } = new Dictionary<string, CardStats>(StringComparer.Ordinal);

        /// <summary>Win rate of players whose most picked non-neutral colour is the key.</summary>
        public Dictionary<TechColor, Proportion> DominantColor { get; } = new Dictionary<TechColor, Proportion>();

        public Dictionary<TechColor, double> CombosPerGame { get; } = new Dictionary<TechColor, double>();

        public Dictionary<string, EventStats> Events { get; } = new Dictionary<string, EventStats>(StringComparer.Ordinal);

        public static ScenarioStats Of(int players, IReadOnlyList<GameRecord> records, GameData data)
        {
            var s = new ScenarioStats(players);
            List<GameRecord> ok = records.Where(r => r.Error == null).ToList();
            List<GameRecord> decided = ok.Where(r => r.Finished && r.Winner >= 0).ToList();
            s.Games = ok.Count;
            s.Errors = records.Count - ok.Count;
            s.Finished = new Proportion(ok.Count(r => r.Finished), ok.Count);
            s.Draws = new Proportion(ok.Count(r => r.Condition == WinCondition.Draw), ok.Count);
            s.Elections = new Proportion(decided.Count(r => r.Condition == WinCondition.GalacticElection), decided.Count);
            s.DoomReached = new Proportion(ok.Count(r => r.DoomReached), ok.Count);
            s.Rounds = new Mean(ok.Select(r => (double)r.Rounds).ToList());
            s.RoundsSorted = ok.Select(r => r.Rounds).OrderBy(x => x).ToList();
            s.Turns = new Mean(ok.Select(r => (double)r.Turns).ToList());
            s.PositionWins = Enumerable.Range(0, players)
                .Select(pos => new Proportion(decided.Count(r => ((r.Winner - r.InitiativeSeat + players) % players) == pos), decided.Count))
                .ToArray();

            long attacks = ok.Sum(r => (long)r.Attacks.Sum());
            s.Harmless = new Proportion(ok.Sum(r => (long)r.HarmlessAttacks.Sum()), attacks);
            s.AttacksPerTurn = Ratio(attacks, ok.Sum(r => (long)r.Turns));
            s.HpPerAttack = Ratio(ok.Sum(r => (long)r.DamageDealt.Sum()), attacks);
            long choice = ok.Sum(r => (long)r.ChoiceAttacks);
            s.AttacksOnLeader = new Proportion(ok.Sum(r => (long)r.AttacksOnLeader), choice);
            s.AttacksOnWeakest = new Proportion(ok.Sum(r => (long)r.AttacksOnWeakest), choice);
            s.FirstElimination = new Mean(ok.Where(r => r.FirstEliminationRound >= 0).Select(r => (double)r.FirstEliminationRound).ToList());
            List<bool> mid = ok.Select(r => r.MidGameLeaderWon).Where(m => m.HasValue).Select(m => m!.Value).ToList();
            s.MidGameLeaderWins = new Proportion(mid.Count(m => m), mid.Count);
            long crew = ok.Sum(r => (long)r.CrewActions.Sum());
            s.CrewActions = Enumerable.Range(0, GameRecord.CrewActionCount).Select(a => new Proportion(ok.Sum(r => (long)r.CrewActions[a]), crew)).ToArray();

            ComputeCards(s, ok, data);
            ComputeColors(s, ok);
            ComputeEvents(s, ok, data);
            return s;
        }

        private static void ComputeCards(ScenarioStats s, List<GameRecord> games, GameData data)
        {
            List<GameRecord> finished = games.Where(g => g.Finished).ToList();
            foreach (CardDefinition card in data.Modifiers)
            {
                s.Cards[card.Id] = new CardStats();
            }

            foreach (GameRecord g in finished)
            {
                foreach ((int seat, string cardId) in g.Picks.Distinct())
                {
                    CardStats c = s.Cards[cardId];
                    c.Takers++;
                    c.Wins += g.Winner == seat ? 1 : 0;
                }

                foreach ((int _, string cardId) in g.Picks)
                {
                    s.Cards[cardId].Picks++;
                }

                foreach ((int _, string cardId) in g.Activations)
                {
                    s.Cards[cardId].Activations++;
                }
            }

            int takers = s.Cards.Values.Sum(c => c.Takers);
            s.CardBaseline = takers == 0 ? 0 : (double)s.Cards.Values.Sum(c => c.Wins) / takers;
            foreach (CardStats c in s.Cards.Values)
            {
                c.PerGame = Ratio(c.Picks, finished.Count);
                c.Deviation = c.Takers == 0 ? 0 : ((double)c.Wins / c.Takers) - s.CardBaseline;
                c.StdErr = c.Takers == 0 ? 0 : Math.Sqrt(s.CardBaseline * (1 - s.CardBaseline) / c.Takers);
            }
        }

        private static void ComputeColors(ScenarioStats s, List<GameRecord> games)
        {
            var colors = new[] { TechColor.Blue, TechColor.Red, TechColor.Green, TechColor.Yellow };
            var counts = colors.ToDictionary(c => c, c => (Takers: 0, Wins: 0));
            foreach (GameRecord g in games.Where(g => g.Finished))
            {
                for (int seat = 0; seat < g.Players; seat++)
                {
                    int best = colors.Max(c => g.ColorPicks[seat, (int)c]);
                    TechColor[] top = colors.Where(c => g.ColorPicks[seat, (int)c] == best).ToArray();
                    if (best == 0 || top.Length != 1)
                    {
                        continue;
                    }

                    var entry = counts[top[0]];
                    counts[top[0]] = (entry.Takers + 1, entry.Wins + (g.Winner == seat ? 1 : 0));
                }
            }

            foreach (TechColor c in colors)
            {
                s.DominantColor[c] = new Proportion(counts[c].Wins, counts[c].Takers);
                s.CombosPerGame[c] = Ratio(games.Sum(g => g.Combos.Count(x => x.Color == c)), games.Count);
            }
        }

        private static void ComputeEvents(ScenarioStats s, List<GameRecord> games, GameData data)
        {
            foreach (EventDefinition e in data.Events)
            {
                var reveals = games.SelectMany(g => g.EventsRevealed).Where(x => x.EventId == e.Id).ToList();
                List<bool> judged = reveals.Where(x => x.LeaderKept.HasValue).Select(x => x.LeaderKept!.Value).ToList();
                s.Events[e.Id] = new EventStats
                {
                    PerGame = Ratio(reveals.Count, games.Count),
                    LeaderKept = new Proportion(judged.Count(k => k), judged.Count),
                };
            }
        }

        private static double Ratio(double a, double b) => b == 0 ? 0 : a / b;
    }

    /// <summary>Per-card indicators.</summary>
    internal sealed class CardStats
    {
        public int Picks { get; set; }

        public int Activations { get; set; }

        /// <summary>(game, seat) pairs that took the card at least once.</summary>
        public int Takers { get; set; }

        public int Wins { get; set; }

        public double PerGame { get; set; }

        /// <summary>Takers' win rate minus the all-cards baseline.</summary>
        public double Deviation { get; set; }

        public double StdErr { get; set; }

        public bool Significant => Takers > 0 && Math.Abs(Deviation) > 2 * StdErr;
    }

    /// <summary>Per-event indicators.</summary>
    internal sealed class EventStats
    {
        public double PerGame { get; set; }

        /// <summary>How often the HP leader before the reveal still led at the end of the round.</summary>
        public Proportion LeaderKept { get; set; }
    }
}
