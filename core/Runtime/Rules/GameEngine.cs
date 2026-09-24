using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Effects;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Core.Rules
{
    /// <summary>
    /// Public entry point of the rules engine. Stateless: every call takes a <see cref="GameState"/> and returns
    /// a new one, so the same engine instance can serve many games (locally or on a server).
    /// </summary>
    /// <remarks>
    /// Decisions use deterministic re-execution (ADR-0009): when a command needs a player's choice, the run is
    /// abandoned and the pre-command state is returned with a <see cref="PendingState"/>. Each answer re-runs the
    /// command from that state with all answers so far; since randomness comes from the state's own generator,
    /// the run reaches the same point and continues. The state is therefore serializable at any time.
    /// </remarks>
    public sealed class GameEngine
    {
        private const ulong RngStream = 0xDA3E39CB94B95BDBUL;
        private const int MaxNameLength = 24;
        private readonly IEffectCatalog _catalog;

        /// <summary>Creates an engine for validated content and configuration.</summary>
        public GameEngine(GameData data, GameConfig config)
            : this(data, config, EffectCatalog.Build(data))
        {
        }

        internal GameEngine(GameData data, GameConfig config, IEffectCatalog catalog)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            IReadOnlyList<string> errors = config.Validate();
            if (errors.Count > 0)
            {
                throw new GameDataException("Invalid configuration:\n - " + string.Join("\n - ", errors));
            }
        }

        /// <summary>Game content.</summary>
        public GameData Data { get; }

        /// <summary>Balancing configuration.</summary>
        public GameConfig Config { get; }

        /// <summary>Supported player counts.</summary>
        public IEnumerable<int> SupportedPlayerCounts => Config.PlayerCounts.Select(p => p.Players).OrderBy(n => n);

        /// <summary>Creates and starts a game (RULES A3). Same seed and names give the same game.</summary>
        /// <exception cref="ArgumentException">Unsupported player count or invalid name.</exception>
        public EngineResult NewGame(ulong seed, IReadOnlyList<string> playerNames)
        {
            if (playerNames is null)
            {
                throw new ArgumentNullException(nameof(playerNames));
            }

            if (Config.ForPlayers(playerNames.Count) == null)
            {
                throw new ArgumentException("Unsupported number of players: " + playerNames.Count + ".", nameof(playerNames));
            }

            foreach (string name in playerNames)
            {
                if (string.IsNullOrWhiteSpace(name) || name.Length > MaxNameLength || name.Any(char.IsControl))
                {
                    throw new ArgumentException("Player names must be 1-" + MaxNameLength + " printable characters.", nameof(playerNames));
                }
            }

            var state = new GameState { Rng = Dice.Pcg32.Seeded(seed, RngStream) };
            var game = new Game(state, Data, Config, _catalog, Array.Empty<string>());
            try
            {
                game.Setup(playerNames);
            }
            catch (DecisionNeededException)
            {
                throw new EngineException("Game setup cannot require a player decision (check first-round event effects).");
            }

            return EngineResult.Ok(state, game.Events, null);
        }

        /// <summary>
        /// Submits a command from <paramref name="player"/>. Illegal commands are rejected with a
        /// <see cref="CommandError"/> and the returned state is the unchanged input.
        /// </summary>
        public EngineResult Submit(GameState state, int player, Command command)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (state.Outcome != null)
            {
                return EngineResult.Rejected(state, CommandErrorCode.GameOver, "The game is over.");
            }

            PendingState? pending = state.Pending;
            if (pending == null)
            {
                return Run(state, player, command, Array.Empty<string>(), 0);
            }

            if (command.Type != CommandType.AnswerDecision)
            {
                return EngineResult.Rejected(state, CommandErrorCode.DecisionPending, "A decision is pending.");
            }

            if (player != pending.Decision.Player)
            {
                return EngineResult.Rejected(state, CommandErrorCode.NotYourDecision, "The pending decision belongs to another player.");
            }

            if (command.DecisionId != pending.Decision.Id)
            {
                return EngineResult.Rejected(state, CommandErrorCode.WrongDecision, "Unknown or stale decision id.");
            }

            if (!pending.Decision.Accepts(command.Option))
            {
                return EngineResult.Rejected(state, CommandErrorCode.InvalidOption, "The answer is not one of the options.");
            }

            var answers = new List<string>(pending.Answers) { command.Option! };
            return Run(state, pending.Player, pending.Command, answers, pending.DeliveredEvents);
        }

        /// <summary>
        /// Structural invariants of a state (empty when valid). Call it before trusting a state that was saved or
        /// received from outside the engine (docs/SECURITY.md S2).
        /// </summary>
        public IReadOnlyList<string> ValidateState(GameState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return GameStateValidator.Validate(state, Data, Config, _catalog);
        }

        /// <summary>
        /// Every command <paramref name="player"/> may submit now: the answers to a decision addressed to them,
        /// or the legal actions of their turn. Used by the UI (enable buttons) and bots.
        /// </summary>
        public IReadOnlyList<Command> LegalCommands(GameState state, int player)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.Outcome != null)
            {
                return Array.Empty<Command>();
            }

            if (state.Pending != null)
            {
                DecisionRequest d = state.Pending.Decision;
                return d.Player == player ? d.Options.Select(o => Command.Answer(d.Id, o.Key)).ToList() : (IReadOnlyList<Command>)Array.Empty<Command>();
            }

            var game = new Game(state.Clone(), Data, Config, _catalog, Array.Empty<string>());
            return Candidates(game, player).Where(c => game.Validate(player, c) == null).ToList();
        }

        private EngineResult Run(GameState original, int player, Command command, IReadOnlyList<string> answers, int delivered)
        {
            GameState work = original.Clone();
            work.Pending = null;
            var game = new Game(work, Data, Config, _catalog, answers);

            CommandError? error = game.Validate(player, command);
            if (error != null)
            {
                return EngineResult.Rejected(original, error.Code, error.Message);
            }

            try
            {
                game.Execute(player, command);
            }
            catch (DecisionNeededException needed)
            {
                GameState suspended = original.Clone();
                suspended.Pending = new PendingState
                {
                    Command = command.Clone(),
                    Player = player,
                    Answers = answers.ToList(),
                    Decision = needed.Request,
                    DeliveredEvents = game.Events.Count,
                };
                return EngineResult.Ok(suspended, game.Events.Skip(delivered).ToList(), needed.Request);
            }

            work.CommandCount++;
            return EngineResult.Ok(work, game.Events.Skip(delivered).ToList(), null);
        }

        /// <summary>
        /// Protection of <paramref name="target"/>: its effective shield (RULES A6 step 7) against a plain attack,
        /// averaged over its alive opponents. It includes every active effect (disabled shield, temporary bonus...)
        /// and never changes <paramref name="state"/>. Returns the stored shield when no opponent is alive.
        /// </summary>
        public double AverageEffectiveShield(GameState state, int target)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (target < 0 || target >= state.Players.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(target));
            }

            var game = new Game(state.Clone(), Data, Config, _catalog, Array.Empty<string>());
            List<int> attackers = state.Players.Where(p => p.Seat != target && !p.Eliminated).Select(p => p.Seat).ToList();
            return attackers.Count == 0 ? state.Players[target].Shield : attackers.Average(a => game.PreviewEffectiveShield(target, a));
        }

        // Every command shape that could be legal for the player; validation filters them.
        private static IEnumerable<Command> Candidates(Game game, int player)
        {
            GameState s = game.State;
            if (player != s.CurrentPlayer)
            {
                yield break;
            }

            if (s.Phase == TurnPhase.Market)
            {
                foreach (CardSlot slot in new[] { CardSlot.Attack, CardSlot.Defense })
                {
                    for (int i = 0; i < s.Market(slot).Visible.Count; i++)
                    {
                        yield return Command.PickMarket(slot, i);
                    }

                    yield return Command.RecycleMarket(slot);
                }

                yield return Command.EndMarket();
                yield break;
            }

            if (s.Phase != TurnPhase.Main)
            {
                yield break;
            }

            foreach (CardInstance card in s.Players[player].Modifiers())
            {
                yield return Command.ActivateCard(card.Uid);
            }

            yield return Command.ActivateTechnology();
            foreach (int target in game.Opponents(player))
            {
                yield return Command.Attack(target, false);
                yield return Command.Attack(target, true);
                yield return Command.Sabotage(target);
            }

            yield return Command.RerollShield(false);
            yield return Command.RerollShield(true);
            yield return Command.Overcharge();
            yield return Command.DefensivePosture();
            yield return Command.EndTurn();
        }
    }

    /// <summary>Outcome of an engine call.</summary>
    public sealed class EngineResult
    {
        private EngineResult(GameState state, IReadOnlyList<GameEvent> events, DecisionRequest? decision, CommandError? error)
        {
            State = state;
            Events = events;
            Decision = decision;
            Error = error;
        }

        /// <summary>True when the command was accepted (possibly suspended on a decision).</summary>
        public bool Accepted => Error == null;

        /// <summary>Rejection reason, or null.</summary>
        public CommandError? Error { get; }

        /// <summary>New state (or the unchanged input when rejected).</summary>
        public GameState State { get; }

        /// <summary>Facts that happened, in order, to animate or log.</summary>
        public IReadOnlyList<GameEvent> Events { get; }

        /// <summary>Decision now awaited, if the command is suspended.</summary>
        public DecisionRequest? Decision { get; }

        internal static EngineResult Ok(GameState state, IReadOnlyList<GameEvent> events, DecisionRequest? decision)
        {
            return new EngineResult(state, events, decision, null);
        }

        internal static EngineResult Rejected(GameState state, CommandErrorCode code, string message)
        {
            return new EngineResult(state, Array.Empty<GameEvent>(), state.Pending?.Decision, new CommandError(code, message));
        }
    }
}
