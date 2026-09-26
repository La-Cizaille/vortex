using System;
using System.Collections.Generic;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Decisions;
using Vortex.Core.Events;
using Vortex.Core.Projection;
using Vortex.Core.Rules;

namespace Vortex.Client.Session
{
    /// <summary>
    /// What the presentation knows of a game (ADR-0014): the public view, who must act, the legal commands, and a way to
    /// submit one. Local hot-seat today, network session in phase 2; the presentation never sees the full state, so the
    /// deck order and the random generator stay out of the client (docs/SECURITY.md).
    /// </summary>
    public interface IGameSession
    {
        /// <summary>Public view of the game, refreshed after each accepted command.</summary>
        GameView View { get; }

        /// <summary>Rules of this game (public): HP, doom round, technologies to win, rule options.</summary>
        GameConfig Rules { get; }

        /// <summary>Seat expected to act: the pending decision's player, else the current player; -1 once the game is over.</summary>
        int Actor { get; }

        /// <summary>Decision awaited from <see cref="Actor"/>, or null.</summary>
        DecisionRequest? Decision { get; }

        /// <summary>True once the game has an outcome.</summary>
        bool IsOver { get; }

        /// <summary>Commands the seat may submit now (empty when it is not its turn).</summary>
        IReadOnlyList<Command> LegalCommands(int seat);

        /// <summary>Submits a command. An illegal command is refused with a typed error and changes nothing.</summary>
        SessionResult Submit(int seat, Command command);

        /// <summary>
        /// What a command of the seat would likely do, estimated by the engine on guessed games (ADR-0018): it never
        /// reveals a coming die or draw. Null when the command is not legal now.
        /// </summary>
        CommandPreview? Preview(int seat, Command command);

        /// <summary>Why the seat may not submit a command now, or null when it may (the reason shown next to a greyed-out move).</summary>
        CommandError? Explain(int seat, Command command);

        /// <summary>
        /// Protection of the seat: its effective shield (RULES A6 step 7) against a plain attack, averaged over its alive
        /// opponents, with every active effect. 0 while its shield is disabled, whatever its stored value.
        /// </summary>
        double Protection(int seat);
    }

    /// <summary>Outcome of a submitted command.</summary>
    public sealed class SessionResult
    {
        private SessionResult(CommandError? error, IReadOnlyList<GameEvent> events)
        {
            Error = error;
            Events = events;
        }

        /// <summary>True when the command was accepted (possibly suspended on a decision).</summary>
        public bool Accepted => Error == null;

        /// <summary>Why the command was refused, or null.</summary>
        public CommandError? Error { get; }

        /// <summary>Facts to play, in order.</summary>
        public IReadOnlyList<GameEvent> Events { get; }

        /// <summary>Wraps an engine result.</summary>
        public static SessionResult From(EngineResult result)
        {
            if (result is null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            return new SessionResult(result.Error, result.Events);
        }
    }

    /// <summary>Who plays a seat.</summary>
    public enum SeatKind
    {
        /// <summary>A person on this device.</summary>
        Human = 0,

        /// <summary>A generic bot (ADR-0010).</summary>
        Bot = 1,
    }

    /// <summary>A seat of a new game.</summary>
    public sealed class SeatSetup
    {
        /// <summary>Creates a seat.</summary>
        public SeatSetup(string name, SeatKind kind, BotLevel botLevel = BotLevel.Normal)
        {
            Name = name;
            Kind = kind;
            BotLevel = botLevel;
        }

        /// <summary>Display name (validated by the engine).</summary>
        public string Name { get; }

        /// <summary>Human or bot.</summary>
        public SeatKind Kind { get; }

        /// <summary>Level of the bot, when <see cref="Kind"/> is <see cref="SeatKind.Bot"/>.</summary>
        public BotLevel BotLevel { get; }
    }
}
