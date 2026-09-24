using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Simulator
{
    /// <summary>Everything measured on one simulated game.</summary>
    internal sealed class GameRecord
    {
        public GameRecord(int index, ulong seed, IReadOnlyList<string> bots)
        {
            Index = index;
            Seed = seed;
            Bots = bots;
            int n = bots.Count;
            DamageDealt = new int[n];
            Attacks = new int[n];
            HarmlessAttacks = new int[n];
            Technologies = new int[n];
            EliminatedRound = Enumerable.Repeat(-1, n).ToArray();
            ColorPicks = new int[n, 5];
        }

        public int Index { get; }

        public ulong Seed { get; }

        /// <summary>Bot name per seat.</summary>
        public IReadOnlyList<string> Bots { get; }

        public int Players => Bots.Count;

        public bool Finished { get; set; }

        public int Winner { get; set; } = -1;

        public WinCondition? Condition { get; set; }

        public int Rounds { get; set; }

        public int Commands { get; set; }

        public int Turns { get; set; }

        public int InitiativeSeat { get; set; }

        public bool DoomReached { get; set; }

        /// <summary>Exception message if the engine failed (must never happen).</summary>
        public string? Error { get; set; }

        public int[] DamageDealt { get; }

        public int[] Attacks { get; }

        public int[] HarmlessAttacks { get; }

        public int[] Technologies { get; }

        public int[] EliminatedRound { get; }

        /// <summary>Market picks per seat and per colour (second index = <see cref="TechColor"/>).</summary>
        public int[,] ColorPicks { get; }

        /// <summary>Crew actions performed, indexed by <see cref="CrewAction"/>.</summary>
        public int[] CrewActions { get; } = new int[CrewActionCount];

        /// <summary>Number of crew action kinds (size of <see cref="CrewActions"/>).</summary>
        public const int CrewActionCount = 5;

        /// <summary>Attacks made while at least two opponents were alive (so "leader" and "weakest" mean something).</summary>
        public int ChoiceAttacks { get; set; }

        /// <summary>Among <see cref="ChoiceAttacks"/>: the target was the opponent with the most HP.</summary>
        public int AttacksOnLeader { get; set; }

        /// <summary>Among <see cref="ChoiceAttacks"/>: the target was the opponent with the fewest HP.</summary>
        public int AttacksOnWeakest { get; set; }

        /// <summary>HP of every seat at the start of each round (0 once eliminated).</summary>
        public List<int[]> HpByRound { get; } = new List<int[]>();

        /// <summary>(seat, card id) for each card taken from a market.</summary>
        public List<(int Seat, string CardId)> Picks { get; } = new List<(int, string)>();

        /// <summary>(seat, card id) for each card activated.</summary>
        public List<(int Seat, string CardId)> Activations { get; } = new List<(int, string)>();

        /// <summary>(seat, colour) for each technology combo activated.</summary>
        public List<(int Seat, TechColor Color)> Combos { get; } = new List<(int, TechColor)>();

        /// <summary>Events revealed, with whether the HP leader before the reveal still led at the end of that round (null: no clear leader).</summary>
        public List<(string EventId, bool? LeaderKept)> EventsRevealed { get; } = new List<(string, bool?)>();

        /// <summary>First round in which someone was eliminated, or -1.</summary>
        public int FirstEliminationRound => EliminatedRound.Where(r => r >= 0).DefaultIfEmpty(-1).Min();

        /// <summary>True when the sole HP leader at mid-game won; null when undecided (no winner, tie, very short game).</summary>
        public bool? MidGameLeaderWon
        {
            get
            {
                if (Winner < 0 || HpByRound.Count < 2)
                {
                    return null;
                }

                int[] hp = HpByRound[HpByRound.Count / 2];
                int max = hp.Max();
                int[] leaders = Enumerable.Range(0, hp.Length).Where(s => hp[s] == max).ToArray();
                return leaders.Length == 1 ? leaders[0] == Winner : (bool?)null;
            }
        }
    }

    /// <summary>Plays one game between bots and records its statistics.</summary>
    internal static class GameRunner
    {
        public const int MaxCommands = 20_000;

        public static GameRecord Play(GameEngine engine, int index, ulong seed, IReadOnlyList<IBot> bots, int doomRound)
        {
            var record = new GameRecord(index, seed, bots.Select(b => b.Name).ToList());
            var colors = engine.Data.Modifiers.ToDictionary(c => c.Id, c => c.Color, StringComparer.Ordinal);
            var watch = new EventWatch();
            try
            {
                EngineResult r = engine.NewGame(seed, bots.Select((_, i) => "Bot" + i).ToList());
                GameState state = r.State;
                Observe(record, r.Events, state, state, colors, watch);
                for (int i = 0; i < MaxCommands && state.Outcome == null; i++)
                {
                    int actor = state.Pending?.Decision.Player ?? state.CurrentPlayer;
                    IReadOnlyList<Command> legal = engine.LegalCommands(state, actor);
                    Command command = bots[actor].Choose(engine, state, actor, legal);
                    EngineResult result = engine.Submit(state, actor, command);
                    if (!result.Accepted)
                    {
                        throw new InvalidOperationException("Bot " + bots[actor].Name + " chose an illegal command: " + result.Error);
                    }

                    GameState before = state;
                    state = result.State;
                    record.Commands++;
                    Observe(record, result.Events, before, state, colors, watch);
                }

                watch.Close(record, state);
                record.Rounds = state.Round;
                record.DoomReached = state.Round >= doomRound;
                record.Finished = state.Outcome != null;
                if (state.Outcome != null)
                {
                    record.Winner = state.Outcome.Winner;
                    record.Condition = state.Outcome.Condition;
                }

                for (int seat = 0; seat < state.Players.Count; seat++)
                {
                    record.Technologies[seat] = state.Players[seat].Technologies.Count;
                }
            }
            catch (Exception ex) when (ex is EngineException || ex is InvalidOperationException || ex is ArgumentException)
            {
                record.Error = ex.GetType().Name + ": " + ex.Message;
            }

            return record;
        }

        /// <summary>Seat with strictly the most HP among alive players, or -1 on a tie.</summary>
        internal static int Leader(GameState state)
        {
            List<PlayerState> alive = state.Players.Where(p => !p.Eliminated).ToList();
            if (alive.Count == 0)
            {
                return -1;
            }

            int max = alive.Max(p => p.Hp);
            List<PlayerState> top = alive.Where(p => p.Hp == max).ToList();
            return top.Count == 1 ? top[0].Seat : -1;
        }

        // "before" is the state before the command that produced these events, "after" the state after it.
        private static void Observe(GameRecord record, IReadOnlyList<GameEvent> events, GameState before, GameState after, Dictionary<string, TechColor> colors, EventWatch watch)
        {
            foreach (GameEvent e in events)
            {
                switch (e.Type)
                {
                    case GameEventType.InitiativeWon:
                        record.InitiativeSeat = e.Player;
                        break;
                    case GameEventType.RoundStarted:
                        // The previous round is over: judge its event on the state as it ended.
                        watch.Close(record, before);
                        break;
                    case GameEventType.EventRevealed:
                        watch.Open(e.Id!, Leader(before));
                        break;
                    case GameEventType.TurnStarted:
                        record.Turns++;
                        break;
                    case GameEventType.CrewActionPerformed when e.Value >= 0 && e.Value < GameRecord.CrewActionCount:
                        record.CrewActions[e.Value]++;
                        break;
                    case GameEventType.MarketCardTaken:
                        record.Picks.Add((e.Player, e.Id!));
                        record.ColorPicks[e.Player, (int)colors[e.Id!]]++;
                        break;
                    case GameEventType.CardActivated:
                        record.Activations.Add((e.Player, e.Id!));
                        break;
                    case GameEventType.TechnologyActivated:
                        record.Combos.Add((e.Player, (TechColor)e.Value));
                        break;
                    case GameEventType.AttackResolved:
                        record.Attacks[e.Player]++;
                        if (e.Amount == 0)
                        {
                            record.HarmlessAttacks[e.Player]++;
                        }

                        ClassifyTarget(record, before, e.Player, e.Other);
                        break;
                    case GameEventType.HpLost when e.Cause == HpLossCause.Attack && e.Other >= 0:
                        record.DamageDealt[e.Other] += e.Amount;
                        break;
                    case GameEventType.PlayerEliminated:
                        record.EliminatedRound[e.Player] = after.Round;
                        break;
                    default:
                        break;
                }
            }

            // One HP snapshot per round, taken after the command that started it.
            while (record.HpByRound.Count < after.Round)
            {
                record.HpByRound.Add(after.Players.Select(p => p.Eliminated ? 0 : p.Hp).ToArray());
            }
        }

        // Was the target the healthiest or the weakest opponent, as seen before the attack?
        private static void ClassifyTarget(GameRecord record, GameState before, int attacker, int target)
        {
            List<PlayerState> opponents = before.Players.Where(p => p.Seat != attacker && !p.Eliminated).ToList();
            if (opponents.Count < 2 || target < 0)
            {
                return;
            }

            record.ChoiceAttacks++;
            int hp = before.Players[target].Hp;
            if (hp == opponents.Max(p => p.Hp))
            {
                record.AttacksOnLeader++;
            }

            if (hp == opponents.Min(p => p.Hp))
            {
                record.AttacksOnWeakest++;
            }
        }

        /// <summary>Follows the event of the current round to see whether the HP leader kept the lead.</summary>
        private sealed class EventWatch
        {
            private string? _eventId;
            private int _leader = -1;

            public void Open(string eventId, int leader)
            {
                _eventId = eventId;
                _leader = leader;
            }

            public void Close(GameRecord record, GameState endOfRound)
            {
                if (_eventId == null)
                {
                    return;
                }

                record.EventsRevealed.Add((_eventId, _leader < 0 ? (bool?)null : Leader(endOfRound) == _leader));
                _eventId = null;
            }
        }
    }
}
