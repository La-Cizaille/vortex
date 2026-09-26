using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Decisions;
using Vortex.Core.Dice;
using Vortex.Core.Events;
using Vortex.Core.Projection;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Client.Session
{
    /// <summary>
    /// A game played on this device: several people share the screen, and any seat may be a bot. The full state stays
    /// private to this class; bots receive it only through their own interface, and they never read hidden information
    /// (ADR-0010).
    /// </summary>
    public sealed class LocalHotSeatSession : IGameSession
    {
        private const ulong PreviewStream = 0x9E3779B97F4A7C15UL;
        private readonly GameEngine _engine;
        private readonly Pcg32 _guesses;
        private readonly IBot _standIn;
        private readonly Dictionary<int, IBot> _bots = new Dictionary<int, IBot>();
        private GameState _state;
        private GameView _view;

        /// <summary>Starts a new game.</summary>
        /// <param name="engine">Rules engine with the game content.</param>
        /// <param name="seed">Seed of the game: same seed, same seats and same commands give the same game.</param>
        /// <param name="seats">The seats, in table order (the engine checks the player count and names).</param>
        public LocalHotSeatSession(GameEngine engine, ulong seed, IReadOnlyList<SeatSetup> seats)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            if (seats is null)
            {
                throw new ArgumentNullException(nameof(seats));
            }

            EngineResult start = engine.NewGame(seed, seats.Select(s => s.Name).ToList());

            // Previews guess the hidden information with a generator of their own (ADR-0018), never the game's.
            _guesses = Pcg32.Seeded(seed, PreviewStream);

            // Answers for a person whose time is up (ARB-80): a generic bot, which never reads hidden information.
            _standIn = BotFactory.Create(BotLevel.Normal, (seed * 31UL) + 97UL);
            _state = start.State;
            _view = GameView.Of(_state);
            OpeningEvents = start.Events;
            for (int seat = 0; seat < seats.Count; seat++)
            {
                if (seats[seat].Kind == SeatKind.Bot)
                {
                    // Each bot has its own random stream, derived from the game seed (as in the simulator).
                    _bots[seat] = BotFactory.Create(seats[seat].BotLevel, (seed * 31UL) + (ulong)seat);
                }
            }
        }

        /// <summary>Events of the setup (initiative, first round, first event), to play before anything else.</summary>
        public IReadOnlyList<GameEvent> OpeningEvents { get; }

        /// <inheritdoc/>
        public GameView View => _view;

        /// <inheritdoc/>
        public GameConfig Rules => _engine.Config;

        /// <inheritdoc/>
        public int Actor => _state.Outcome != null ? -1 : (_state.Pending?.Decision.Player ?? _state.CurrentPlayer);

        /// <inheritdoc/>
        public DecisionRequest? Decision => _state.Pending?.Decision;

        /// <inheritdoc/>
        public bool IsOver => _state.Outcome != null;

        /// <summary>True when the seat expected to act is a bot.</summary>
        public bool IsBotTurn => Actor >= 0 && _bots.ContainsKey(Actor);

        /// <summary>True when the seat is played by a bot.</summary>
        public bool IsBot(int seat) => _bots.ContainsKey(seat);

        /// <inheritdoc/>
        public IReadOnlyList<Command> LegalCommands(int seat) => _engine.LegalCommands(_state, seat);

        /// <inheritdoc/>
        public CommandPreview? Preview(int seat, Command command) => _engine.Preview(_state, seat, command, _guesses);

        /// <inheritdoc/>
        public CommandError? Explain(int seat, Command command) => _engine.Explain(_state, seat, command);

        /// <inheritdoc/>
        public double Protection(int seat) => _engine.AverageEffectiveShield(_state, seat);

        /// <inheritdoc/>
        public SessionResult Submit(int seat, Command command)
        {
            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            EngineResult result = _engine.Submit(_state, seat, command);
            if (result.Accepted)
            {
                _state = result.State;
                _view = GameView.Of(_state);
            }

            return SessionResult.From(result);
        }

        /// <summary>
        /// The time of <paramref name="seat"/> is up (turn timer, ARB-80): plays one step for it. A decision awaited from the
        /// seat is answered with the choice a bot judges best for it; otherwise its turn simply ends: the market phase is
        /// left, then the turn is ended, or, when the turn cannot end yet (an imposed crew action), a bot plays the step
        /// the rules require. Call it again, once the events are played, until the seat no longer has to act.
        /// </summary>
        public SessionResult Expire(int seat)
        {
            if (seat != Actor)
            {
                throw new InvalidOperationException("Only the seat that has to act can run out of time.");
            }

            IReadOnlyList<Command> legal = LegalCommands(seat);
            Command? step = _state.Pending != null
                ? null
                : legal.FirstOrDefault(c => c.Type == CommandType.EndMarket) ?? legal.FirstOrDefault(c => c.Type == CommandType.EndTurn);
            return Submit(seat, step ?? _standIn.Choose(_engine, _state, seat, legal));
        }

        /// <summary>Lets the bot whose turn it is play one command. Call it when the presentation is idle, to pace bots.</summary>
        public SessionResult PlayBotStep()
        {
            if (!IsBotTurn)
            {
                throw new InvalidOperationException("It is not a bot's turn.");
            }

            int seat = Actor;
            Command command = _bots[seat].Choose(_engine, _state, seat, LegalCommands(seat));
            return Submit(seat, command);
        }
    }
}
