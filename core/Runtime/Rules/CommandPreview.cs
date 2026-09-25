using System;
using System.Collections.Generic;
using System.Linq;

namespace Vortex.Core.Rules
{
    /// <summary>
    /// What a command is likely to do, estimated by playing it on many guessed copies of the game (ADR-0018).
    /// Figures come from samples: they are estimates, never a promise about the coming dice.
    /// </summary>
    public sealed class CommandPreview
    {
        internal CommandPreview(int samples, IReadOnlyList<SeatPreview> seats, AttackPreview? attack)
        {
            Samples = samples;
            Seats = seats;
            Attack = attack;
        }

        /// <summary>Number of guessed games played.</summary>
        public int Samples { get; }

        /// <summary>Every seat after the command, indexed by seat.</summary>
        public IReadOnlyList<SeatPreview> Seats { get; }

        /// <summary>The attack the command starts, or null when it starts none.</summary>
        public AttackPreview? Attack { get; }
    }

    /// <summary>One seat after the command.</summary>
    public sealed class SeatPreview
    {
        internal SeatPreview(int seat, Estimate hp, Estimate shield, double eliminated)
        {
            Seat = seat;
            Hp = hp;
            Shield = shield;
            Eliminated = eliminated;
        }

        /// <summary>Seat.</summary>
        public int Seat { get; }

        /// <summary>Hit points after the command.</summary>
        public Estimate Hp { get; }

        /// <summary>Stored shield after the command (RULES A1: 0 to 8).</summary>
        public Estimate Shield { get; }

        /// <summary>Chance (0 to 1) that the seat is eliminated by the command.</summary>
        public double Eliminated { get; }
    }

    /// <summary>The attack started by a command (RULES A6), over the samples where it reached the chosen target.</summary>
    public sealed class AttackPreview
    {
        internal AttackPreview(int dicePerThrow, int diceKept, int netAdvantage, Estimate bonus, IReadOnlyList<BonusPreview> bonuses, Estimate effectiveShield, Estimate damage, double hit, double critical, double redirected)
        {
            DicePerThrow = dicePerThrow;
            DiceKept = diceKept;
            NetAdvantage = netAdvantage;
            Bonus = bonus;
            Bonuses = bonuses;
            EffectiveShield = effectiveShield;
            Damage = damage;
            Hit = hit;
            Critical = critical;
            Redirected = redirected;
        }

        /// <summary>Dice rolled per throw (step 4).</summary>
        public int DicePerThrow { get; }

        /// <summary>Highest dice kept per throw.</summary>
        public int DiceKept { get; }

        /// <summary>1: thrown twice, best kept (advantage); -1: worst kept (disadvantage); 0: thrown once.</summary>
        public int NetAdvantage { get; }

        /// <summary>Attack value minus the kept dice (step 6): every bonus and malus together.</summary>
        public Estimate Bonus { get; }

        /// <summary>Each bonus or malus with where it comes from, in the order the effects apply (RULES B7).</summary>
        public IReadOnlyList<BonusPreview> Bonuses { get; }

        /// <summary>Effective shield of the target (step 7).</summary>
        public Estimate EffectiveShield { get; }

        /// <summary>Hit points the target loses (step 10).</summary>
        public Estimate Damage { get; }

        /// <summary>Chance (0 to 1) that the target loses at least one hit point.</summary>
        public double Hit { get; }

        /// <summary>Chance (0 to 1) of a critical hit (step 5).</summary>
        public double Critical { get; }

        /// <summary>Chance (0 to 1) that the attack is redirected to another seat (step 2), answering its choices at random.</summary>
        public double Redirected { get; }
    }

    /// <summary>What one source adds to the attack value; 0 in the samples where it adds nothing.</summary>
    public sealed class BonusPreview
    {
        internal BonusPreview(SourceKind origin, string? id, Estimate amount)
        {
            Origin = origin;
            Id = id;
            Amount = amount;
        }

        /// <summary>Kind of source.</summary>
        public SourceKind Origin { get; }

        /// <summary>Card id, status kind, event id or technology id; null for the base rules.</summary>
        public string? Id { get; }

        /// <summary>Amount added (negative: a malus).</summary>
        public Estimate Amount { get; }
    }

    /// <summary>Lowest, highest and average value seen over the samples.</summary>
    public readonly struct Estimate : IEquatable<Estimate>
    {
        /// <summary>Creates an estimate.</summary>
        public Estimate(int min, int max, double average)
        {
            Min = min;
            Max = max;
            Average = average;
        }

        /// <summary>Lowest value seen.</summary>
        public int Min { get; }

        /// <summary>Highest value seen.</summary>
        public int Max { get; }

        /// <summary>Average value.</summary>
        public double Average { get; }

        /// <summary>True when every sample gave the same value.</summary>
        public bool IsExact => Min == Max;

        /// <summary>Equality of two estimates.</summary>
        public static bool operator ==(Estimate left, Estimate right) => left.Equals(right);

        /// <summary>Inequality of two estimates.</summary>
        public static bool operator !=(Estimate left, Estimate right) => !left.Equals(right);

        /// <inheritdoc/>
        public bool Equals(Estimate other) => Min == other.Min && Max == other.Max && Average.Equals(other.Average);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Estimate other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Min, Max, Average);

        /// <inheritdoc/>
        public override string ToString() => IsExact ? Min.ToString(System.Globalization.CultureInfo.InvariantCulture) : Min + ".." + Max + " (" + Average.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + ")";

        internal static Estimate Of(IReadOnlyCollection<int> values)
        {
            return values.Count == 0 ? default : new Estimate(values.Min(), values.Max(), values.Average());
        }
    }
}
