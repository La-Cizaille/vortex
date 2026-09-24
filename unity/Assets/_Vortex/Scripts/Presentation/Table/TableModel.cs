using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Core.Projection;
using Vortex.Core.State;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// What the table shows (ADR-0014). The engine resolves a command at once; while its events play one by one, this
    /// model follows them, so that hit points, shields, overcharge, eliminations, technologies, the round and the
    /// current player change on screen when their event plays. Cards, markets and statuses change when the playback
    /// is over and the model is rebuilt from the final public view. While a command waits for a decision, the public
    /// view is still the state before that command (ADR-0009), so the model is rebuilt only once no decision is pending.
    /// </summary>
    public sealed class TableModel
    {
        private readonly List<SeatModel> _seats;

        private TableModel(GameView view, GameConfig rules)
        {
            _seats = view.Players.Select(SeatModel.From).ToList();
            Round = view.Round;
            CurrentPlayer = view.CurrentPlayer;
            ActiveEventId = view.ActiveEventId;
            Outcome = view.Outcome;
            AttackMarket = view.AttackMarket;
            DefenseMarket = view.DefenseMarket;
            DoomRound = rules.ForPlayers(_seats.Count)?.DoomRound ?? 0;
            TechnologiesToWin = rules.TechnologiesToWin;
            LeaderBounty = rules.LeaderBounty;
        }

        /// <summary>The seats, in table order.</summary>
        public IReadOnlyList<SeatModel> Seats => _seats;

        /// <summary>Current round.</summary>
        public int Round { get; private set; }

        /// <summary>Seat whose turn it is.</summary>
        public int CurrentPlayer { get; private set; }

        /// <summary>Event of the round, or null.</summary>
        public string? ActiveEventId { get; private set; }

        /// <summary>Result of the game, or null while it goes on.</summary>
        public GameOutcome? Outcome { get; private set; }

        /// <summary>Attack black market.</summary>
        public MarketView AttackMarket { get; }

        /// <summary>Defense black market.</summary>
        public MarketView DefenseMarket { get; }

        /// <summary>Round of the doom event at this table size (0 if none).</summary>
        public int DoomRound { get; }

        /// <summary>Different technologies needed for the Galactic Election.</summary>
        public int TechnologiesToWin { get; }

        /// <summary>Bonus against the sole HP leader (rule option; 0 when disabled).</summary>
        public int LeaderBounty { get; }

        /// <summary>Rounds left before the doom event (0 once it has come or when there is none).</summary>
        public int RoundsBeforeDoom => DoomRound > Round ? DoomRound - Round : 0;

        /// <summary>The only living seat with the most HP, or -1 (tie, or nobody).</summary>
        public int SoleHpLeader
        {
            get
            {
                int leader = -1;
                int best = int.MinValue;
                foreach (SeatModel seat in _seats.Where(s => !s.Eliminated))
                {
                    if (seat.Hp > best)
                    {
                        best = seat.Hp;
                        leader = seat.Seat;
                    }
                    else if (seat.Hp == best)
                    {
                        leader = -1;
                    }
                }

                return leader;
            }
        }

        /// <summary>Builds the model from the public view and the rules of the game.</summary>
        public static TableModel From(GameView view, GameConfig rules)
        {
            if (view is null)
            {
                throw new ArgumentNullException(nameof(view));
            }

            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return new TableModel(view, rules);
        }

        /// <summary>Follows one event. Returns false when the event changes nothing this model shows before the resync.</summary>
        public bool Apply(GameEvent gameEvent)
        {
            if (gameEvent is null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            SeatModel? seat = gameEvent.Player >= 0 && gameEvent.Player < _seats.Count ? _seats[gameEvent.Player] : null;
            switch (gameEvent.Type)
            {
                case GameEventType.RoundStarted:
                    Round = gameEvent.Value;
                    return true;
                case GameEventType.EventRevealed:
                    ActiveEventId = gameEvent.Id;
                    return true;
                case GameEventType.TurnStarted when seat != null:
                    CurrentPlayer = seat.Seat;
                    return true;
                case GameEventType.HpLost when seat != null:
                    seat.Hp = Math.Max(0, seat.Hp - gameEvent.Amount);
                    return true;
                case GameEventType.HpGained when seat != null:
                    seat.Hp += gameEvent.Amount;
                    return true;
                case GameEventType.ShieldChanged when seat != null:
                    seat.Shield = gameEvent.Value;
                    return true;
                case GameEventType.OverchargeChanged when seat != null:
                    seat.Overcharge = gameEvent.Value;
                    return true;
                case GameEventType.PlayerEliminated when seat != null:
                    seat.Eliminated = true;
                    return true;
                case GameEventType.TechnologyActivated when seat != null:
                    seat.AddTechnology((TechColor)gameEvent.Value);
                    return true;
                case GameEventType.GameOver:
                    Outcome = new GameOutcome { Winner = gameEvent.Player, Condition = (WinCondition)gameEvent.Value, Round = Round };
                    return true;
                default:
                    return false;
            }
        }
    }
}
