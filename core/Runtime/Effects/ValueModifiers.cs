using System;
using System.Collections.Generic;

namespace Vortex.Core.Effects
{
    /// <summary>
    /// Accumulates the modifications several effects make to one calculated value, then applies them in the
    /// fixed stacking order of RULES B4: replace → add → multiply → bound → cancel.
    /// </summary>
    /// <remarks>
    /// Effects only register intentions; nothing is applied until <see cref="Apply"/>, so the result never
    /// depends on the order in which effects were asked (except "last replacement wins" and "first matching
    /// cancellation wins", both following the resolution order of RULES B7).
    /// </remarks>
    internal sealed class ValueModifiers
    {
        private readonly List<Cancellation> _cancellations = new List<Cancellation>();
        private readonly List<Action> _deferred = new List<Action>();
        private int? _replacement;
        private int _addition;
        private long _multiplier = 1;
        private int? _cap;
        private int? _floor;

        /// <summary>1. Replace the base value. The last replacement registered wins (RULES B4).</summary>
        public void Replace(int value)
        {
            _replacement = value;
        }

        /// <summary>2. Add (or subtract with a negative amount).</summary>
        public void Add(int amount)
        {
            _addition += amount;
        }

        /// <summary>3. Multiply. Multipliers compound: two ×2 give ×4.</summary>
        public void Multiply(int factor)
        {
            if (factor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(factor), "Multipliers cannot be negative.");
            }

            _multiplier *= factor;
        }

        /// <summary>4. Upper bound. The lowest cap wins.</summary>
        public void Cap(int max)
        {
            _cap = _cap.HasValue ? Math.Min(_cap.Value, max) : max;
        }

        /// <summary>4. Lower bound. The highest floor wins.</summary>
        public void Floor(int min)
        {
            _floor = _floor.HasValue ? Math.Max(_floor.Value, min) : min;
        }

        /// <summary>
        /// 5. Cancel: the value becomes 0 when <paramref name="condition"/> holds on the bounded value.
        /// Only the first matching cancellation applies; its <paramref name="onApplied"/> action then runs.
        /// </summary>
        public void Cancel(Func<int, bool>? condition = null, Action? onApplied = null)
        {
            _cancellations.Add(new Cancellation(condition, onApplied));
        }

        /// <summary>Action to run once the value has been applied (e.g. discard a card whose condition was met).</summary>
        public void Defer(Action action)
        {
            _deferred.Add(action ?? throw new ArgumentNullException(nameof(action)));
        }

        /// <summary>Computes the final value. Deferred and cancellation actions are returned, not run.</summary>
        public int Apply(int baseValue, List<Action> actionsToRun)
        {
            long value = _replacement ?? baseValue;
            value += _addition;
            value *= _multiplier;
            if (_cap.HasValue)
            {
                value = Math.Min(value, _cap.Value);
            }

            if (_floor.HasValue)
            {
                value = Math.Max(value, _floor.Value);
            }

            int result = (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, value));
            foreach (Cancellation c in _cancellations)
            {
                if (c.Condition == null || c.Condition(result))
                {
                    result = 0;
                    if (c.OnApplied != null)
                    {
                        actionsToRun.Add(c.OnApplied);
                    }

                    break;
                }
            }

            actionsToRun.AddRange(_deferred);
            return result;
        }

        private sealed class Cancellation
        {
            public Cancellation(Func<int, bool>? condition, Action? onApplied)
            {
                Condition = condition;
                OnApplied = onApplied;
            }

            public Func<int, bool>? Condition { get; }

            public Action? OnApplied { get; }
        }
    }
}
