using System;

namespace Vortex.Core.Effects
{
    /// <summary>What carries an effect (RULES B1).</summary>
    internal enum EffectOrigin
    {
        /// <summary>An equipped modifier card.</summary>
        Card = 0,
        /// <summary>A status (temporary or rule-changing effect).</summary>
        Status = 1,
        /// <summary>The active event of the round.</summary>
        Event = 2,
        /// <summary>A technology combo being activated.</summary>
        Technology = 3,
    }

    /// <summary>
    /// Identity of one active effect instance: where it comes from and who holds it. Two sources are equal
    /// when they designate the same card copy, status or event, which lets the engine apply
    /// "an effect never reacts to a fact it caused" (RULES B7).
    /// </summary>
    internal sealed class EffectSource : IEquatable<EffectSource>
    {
        public EffectSource(EffectOrigin origin, string id, int holder, int cardUid = -1, int statusUid = -1)
        {
            Origin = origin;
            Id = id;
            Holder = holder;
            CardUid = cardUid;
            StatusUid = statusUid;
        }

        /// <summary>Kind of carrier.</summary>
        public EffectOrigin Origin { get; }

        /// <summary>Card id, status kind, event id or technology id.</summary>
        public string Id { get; }

        /// <summary>Player holding the effect (card holder, status holder, technology owner), or -1 for global effects.</summary>
        public int Holder { get; }

        /// <summary>Card copy uid for <see cref="EffectOrigin.Card"/>, else -1.</summary>
        public int CardUid { get; }

        /// <summary>Status uid for <see cref="EffectOrigin.Status"/>, else -1.</summary>
        public int StatusUid { get; }

        /// <summary>True for effects that belong to no player (events, global statuses).</summary>
        public bool IsGlobal => Holder < 0;

        public bool Equals(EffectSource? other)
        {
            return other != null
                && Origin == other.Origin
                && Holder == other.Holder
                && CardUid == other.CardUid
                && StatusUid == other.StatusUid
                && string.Equals(Id, other.Id, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as EffectSource);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int h = (int)Origin;
                h = (h * 397) ^ Holder;
                h = (h * 397) ^ CardUid;
                h = (h * 397) ^ StatusUid;
                h = (h * 397) ^ StringComparer.Ordinal.GetHashCode(Id);
                return h;
            }
        }

        public override string ToString()
        {
            return Origin + ":" + Id + "@" + Holder + (CardUid >= 0 ? "#c" + CardUid : string.Empty) + (StatusUid >= 0 ? "#s" + StatusUid : string.Empty);
        }
    }
}
