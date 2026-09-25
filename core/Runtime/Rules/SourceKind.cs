using Vortex.Core.Effects;

namespace Vortex.Core.Rules
{
    /// <summary>
    /// Where an effect comes from, as a player can see it: a card, a status, the event of the round, a technology, or the
    /// base rules. Previews name the source of each bonus with it, and refusals the effect that forbids a move.
    /// </summary>
    public enum SourceKind
    {
        /// <summary>The base rules (a rule option).</summary>
        Rule = 0,

        /// <summary>An equipped modifier card.</summary>
        Card = 1,

        /// <summary>A status, often left by a card already used.</summary>
        Status = 2,

        /// <summary>The event of the round.</summary>
        Event = 3,

        /// <summary>A technology.</summary>
        Technology = 4,
    }

    /// <summary>Conversion from the engine's own effect sources.</summary>
    internal static class SourceKinds
    {
        /// <summary>The kind and id of a source (the base rules when null).</summary>
        public static (SourceKind Kind, string? Id) Of(EffectSource? source) => source is null
            ? (SourceKind.Rule, null)
            : (source.Origin switch
            {
                EffectOrigin.Card => SourceKind.Card,
                EffectOrigin.Status => SourceKind.Status,
                EffectOrigin.Event => SourceKind.Event,
                _ => SourceKind.Technology,
            }, source.Id);
    }
}
