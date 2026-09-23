using System;

namespace Vortex.Core.Dice
{
    /// <summary>
    /// PCG32 (XSH RR variant, M.E. O'Neill 2014): small, fast, statistically sound and fully
    /// deterministic across runtimes (ADR-0004). Not cryptographic: its state never leaves the
    /// authoritative engine (see <c>StateProjection</c>).
    /// </summary>
    /// <remarks>
    /// Plain data object stored inside <see cref="State.GameState"/>, so it is saved, cloned and
    /// replayed with the game. All engine randomness goes through it.
    /// </remarks>
    public sealed class Pcg32
    {
        private const ulong Multiplier = 6364136223846793005UL;

        /// <summary>Internal state (serialized).</summary>
        public ulong State { get; set; }

        /// <summary>Stream increment; must be odd (checked by <see cref="IsValid"/>).</summary>
        public ulong Increment { get; set; } = 1UL;

        /// <summary>Creates a generator from a seed and a stream selector (any values are valid).</summary>
        public static Pcg32 Seeded(ulong seed, ulong stream)
        {
            var rng = new Pcg32 { Increment = (stream << 1) | 1UL, State = 0UL };
            rng.NextUInt();
            rng.State += seed;
            rng.NextUInt();
            return rng;
        }

        /// <summary>True when the state can be used (the increment is odd).</summary>
        public bool IsValid => (Increment & 1UL) == 1UL;

        /// <summary>Next 32-bit output.</summary>
        public uint NextUInt()
        {
            ulong old = State;
            State = unchecked((old * Multiplier) + Increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>Uniform integer in [0, bound) without modulo bias (rejection sampling).</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), "Bound must be positive.");
            }

            uint b = (uint)bound;
            uint threshold = unchecked(0u - b) % b;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold)
                {
                    return (int)(r % b);
                }
            }
        }

        /// <summary>Roll of a die: 1..faces.</summary>
        public int Roll(int faces)
        {
            return NextInt(faces) + 1;
        }

        /// <summary>Independent copy with the same state.</summary>
        public Pcg32 Clone()
        {
            return new Pcg32 { State = State, Increment = Increment };
        }
    }
}
