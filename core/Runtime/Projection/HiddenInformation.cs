using System;
using System.Collections.Generic;
using Vortex.Core.Dice;
using Vortex.Core.State;

namespace Vortex.Core.Projection
{
    /// <summary>
    /// Replaces what no player may know (deck orders, the random generator) by a guess drawn from another
    /// generator (ADR-0010, ADR-0018). Whatever is computed on a guess (a bot's look-ahead, a preview shown to a
    /// player) therefore depends only on public information and can never reveal a coming draw or die.
    /// </summary>
    public static class HiddenInformation
    {
        /// <summary>
        /// A copy of <paramref name="state"/> whose hidden information is guessed with <paramref name="guesses"/>.
        /// Only valid when no command is suspended: a suspended command must be replayed with the very same
        /// generator and piles (ADR-0009).
        /// </summary>
        public static GameState Guess(GameState state, Pcg32 guesses)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (guesses is null)
            {
                throw new ArgumentNullException(nameof(guesses));
            }

            GameState guess = state.Clone();
            guess.Rng = Pcg32.Seeded(((ulong)guesses.NextUInt() << 32) | guesses.NextUInt(), 1);

            // Canonical order first, so the guess depends only on what is visible (the pile contents are public
            // knowledge through the card list; their order is not).
            guess.AttackMarket.Deck.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            guess.DefenseMarket.Deck.Sort((a, b) => a.Uid.CompareTo(b.Uid));
            guess.EventDeck.Sort(StringComparer.Ordinal);
            Shuffle(guess.AttackMarket.Deck, guesses);
            Shuffle(guess.DefenseMarket.Deck, guesses);
            Shuffle(guess.EventDeck, guesses);
            return guess;
        }

        private static void Shuffle<T>(List<T> list, Pcg32 guesses)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = guesses.NextInt(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
