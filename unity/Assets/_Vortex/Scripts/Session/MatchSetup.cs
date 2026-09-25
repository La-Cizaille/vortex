using System;
using System.Collections.Generic;
using System.Linq;

namespace Vortex.Client.Session
{
    /// <summary>
    /// A game to start, as chosen in the local game menu (INTERFACE.md 5): the seats in table order, and in development
    /// only, the seed and the rule options to try.
    /// </summary>
    public sealed class MatchSetup
    {
        /// <summary>Creates a setup.</summary>
        /// <param name="seats">The seats, in table order (2 to 5; the engine checks the count and the names).</param>
        /// <param name="seed">Seed of the game; 0: a new one for each game.</param>
        /// <param name="rules">Rule options to try, or null for the current rules.</param>
        public MatchSetup(IReadOnlyList<SeatSetup> seats, ulong seed = 0, RuleOptions? rules = null)
        {
            Seats = seats ?? throw new ArgumentNullException(nameof(seats));
            Seed = seed;
            Rules = rules;
        }

        /// <summary>The seats, in table order.</summary>
        public IReadOnlyList<SeatSetup> Seats { get; }

        /// <summary>Seed of the game; 0: a new one for each game (a fixed seed replays the same draws).</summary>
        public ulong Seed { get; }

        /// <summary>Rule options to try, or null for the current rules.</summary>
        public RuleOptions? Rules { get; }

        /// <summary>Number of seats played by people on this device.</summary>
        public int Humans => Seats.Count(s => s.Kind == SeatKind.Human);
    }
}
