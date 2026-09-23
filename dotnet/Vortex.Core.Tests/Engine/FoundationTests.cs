using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Dice;
using Vortex.Core.Effects;

namespace Vortex.Core.Tests.Engine
{
    [TestFixture]
    public class Pcg32Tests
    {
        [Test]
        public void Same_seed_gives_the_same_sequence()
        {
            Pcg32 a = Pcg32.Seeded(42, 7);
            Pcg32 b = Pcg32.Seeded(42, 7);
            Assert.That(Enumerable.Range(0, 100).Select(_ => a.NextUInt()), Is.EqualTo(Enumerable.Range(0, 100).Select(_ => b.NextUInt())));
        }

        [Test]
        public void Different_seeds_give_different_sequences()
        {
            Pcg32 a = Pcg32.Seeded(1, 7);
            Pcg32 b = Pcg32.Seeded(2, 7);
            Assert.That(Enumerable.Range(0, 10).Select(_ => a.NextUInt()), Is.Not.EqualTo(Enumerable.Range(0, 10).Select(_ => b.NextUInt())));
        }

        [Test]
        public void Reference_output_is_stable_across_runtimes()
        {
            // Pinned values: if this fails, saved games and replays would no longer reproduce (ADR-0004).
            Pcg32 rng = Pcg32.Seeded(42, 54);
            uint[] expected = { rng.NextUInt(), rng.NextUInt(), rng.NextUInt() };
            Pcg32 again = Pcg32.Seeded(42, 54);
            Assert.That(new[] { again.NextUInt(), again.NextUInt(), again.NextUInt() }, Is.EqualTo(expected));
            Assert.That(expected[0], Is.EqualTo(0xa15c02b7u), "Official PCG32 reference vector (seed 42, stream 54).");
        }

        [Test]
        public void Rolls_stay_in_range_and_cover_every_face()
        {
            Pcg32 rng = Pcg32.Seeded(3, 3);
            var seen = new HashSet<int>();
            for (int i = 0; i < 10_000; i++)
            {
                int v = rng.Roll(8);
                Assert.That(v, Is.InRange(1, 8));
                seen.Add(v);
            }

            Assert.That(seen, Has.Count.EqualTo(8));
        }

        [Test]
        public void Rolls_are_roughly_uniform()
        {
            Pcg32 rng = Pcg32.Seeded(9, 9);
            var counts = new int[8];
            for (int i = 0; i < 80_000; i++)
            {
                counts[rng.Roll(8) - 1]++;
            }

            Assert.That(counts, Has.All.InRange(9_500, 10_500));
        }

        [Test]
        public void Clone_is_independent()
        {
            Pcg32 a = Pcg32.Seeded(5, 5);
            Pcg32 b = a.Clone();
            a.NextUInt();
            Assert.That(b.State, Is.Not.EqualTo(a.State));
            Assert.That(b.NextUInt(), Is.EqualTo(Pcg32.Seeded(5, 5).NextUInt()));
        }

        [Test]
        public void Invalid_bound_is_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Pcg32.Seeded(1, 1).NextInt(0));
        }
    }

    /// <summary>Stacking order of RULES B4.</summary>
    [TestFixture]
    public class ValueModifiersTests
    {
        private static int Apply(ValueModifiers m, int baseValue, List<Action>? actions = null)
        {
            return m.Apply(baseValue, actions ?? new List<Action>());
        }

        [Test]
        public void Order_is_replace_then_add_then_multiply_then_bound()
        {
            var m = new ValueModifiers();
            m.Cap(9);
            m.Multiply(2);
            m.Add(3);
            m.Replace(1);
            Assert.That(Apply(m, 100), Is.EqualTo(8)); // (1 + 3) × 2 = 8, under the cap
        }

        [Test]
        public void Last_replacement_wins()
        {
            var m = new ValueModifiers();
            m.Replace(8);
            m.Replace(0);
            Assert.That(Apply(m, 5), Is.EqualTo(0));
        }

        [Test]
        public void Multipliers_compound()
        {
            var m = new ValueModifiers();
            m.Multiply(2);
            m.Multiply(2);
            Assert.That(Apply(m, 3), Is.EqualTo(12));
        }

        [Test]
        public void Lowest_cap_and_highest_floor_win()
        {
            var caps = new ValueModifiers();
            caps.Cap(5);
            caps.Cap(1);
            caps.Cap(3);
            Assert.That(Apply(caps, 10), Is.EqualTo(1));

            var floors = new ValueModifiers();
            floors.Floor(2);
            floors.Floor(4);
            Assert.That(Apply(floors, 0), Is.EqualTo(4));
        }

        [Test]
        public void Cancellation_wins_over_everything_and_only_the_first_triggers()
        {
            var ran = new List<string>();
            var m = new ValueModifiers();
            m.Add(10);
            m.Cancel(onApplied: () => ran.Add("first"));
            m.Cancel(onApplied: () => ran.Add("second"));
            var actions = new List<Action>();
            Assert.That(Apply(m, 5, actions), Is.Zero);
            actions.ForEach(a => a());
            Assert.That(ran, Is.EqualTo(new[] { "first" }));
        }

        [Test]
        public void Conditional_cancellation_sees_the_bounded_value()
        {
            var m = new ValueModifiers();
            m.Cap(3);
            m.Cancel(v => v >= 5);
            Assert.That(Apply(m, 10), Is.EqualTo(3), "Capped to 3, so the '>= 5' cancellation does not apply.");
        }

        [Test]
        public void Deferred_actions_are_returned_not_run()
        {
            bool ran = false;
            var m = new ValueModifiers();
            m.Defer(() => ran = true);
            var actions = new List<Action>();
            Apply(m, 1, actions);
            Assert.That(ran, Is.False);
            Assert.That(actions, Has.Count.EqualTo(1));
        }

        [Test]
        public void Negative_multiplier_is_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ValueModifiers().Multiply(-1));
        }
    }
}
