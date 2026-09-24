using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
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

        /// <summary>(seat, card id) for each card taken from a market.</summary>
        public List<(int Seat, string CardId)> Picks { get; } = new List<(int, string)>();

        /// <summary>(seat, card id) for each card activated.</summary>
        public List<(int Seat, string CardId)> Activations { get; } = new List<(int, string)>();

        /// <summary>Events revealed, in order.</summary>
        public List<string> EventsRevealed { get; } = new List<string>();
    }

    /// <summary>Plays one game between bots and records its statistics.</summary>
    internal static class GameRunner
    {
        public const int MaxCommands = 20_000;

        public static GameRecord Play(GameEngine engine, int index, ulong seed, IReadOnlyList<IBot> bots, int doomRound)
        {
            var record = new GameRecord(index, seed, bots.Select(b => b.Name).ToList());
            try
            {
                EngineResult r = engine.NewGame(seed, bots.Select((_, i) => "Bot" + i).ToList());
                GameState state = r.State;
                Observe(record, r.Events, state);
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

                    state = result.State;
                    record.Commands++;
                    Observe(record, result.Events, state);
                }

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

        private static void Observe(GameRecord record, IReadOnlyList<GameEvent> events, GameState state)
        {
            foreach (GameEvent e in events)
            {
                switch (e.Type)
                {
                    case GameEventType.InitiativeWon:
                        record.InitiativeSeat = e.Player;
                        break;
                    case GameEventType.TurnStarted:
                        record.Turns++;
                        break;
                    case GameEventType.MarketCardTaken:
                        record.Picks.Add((e.Player, e.Id!));
                        break;
                    case GameEventType.CardActivated:
                        record.Activations.Add((e.Player, e.Id!));
                        break;
                    case GameEventType.EventRevealed:
                        record.EventsRevealed.Add(e.Id!);
                        break;
                    case GameEventType.AttackResolved:
                        record.Attacks[e.Player]++;
                        if (e.Amount == 0)
                        {
                            record.HarmlessAttacks[e.Player]++;
                        }

                        break;
                    case GameEventType.HpLost when e.Cause == HpLossCause.Attack && e.Other >= 0:
                        record.DamageDealt[e.Other] += e.Amount;
                        break;
                    case GameEventType.PlayerEliminated:
                        record.EliminatedRound[e.Player] = state.Round;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
