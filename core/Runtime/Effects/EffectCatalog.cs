using System;
using System.Collections.Generic;
using Vortex.Core.Content;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Rules;

namespace Vortex.Core.Effects
{
    /// <summary>
    /// Production effect catalog: resolves the effects of cards, events and technologies declared in the
    /// content files, plus every status behaviour. Built once per engine; immutable afterwards.
    /// </summary>
    internal sealed class EffectCatalog : IEffectCatalog
    {
        private static readonly IReadOnlyList<Effect> None = Array.Empty<Effect>();

        private readonly Dictionary<string, IReadOnlyList<Effect>> _cards;
        private readonly Dictionary<string, IReadOnlyList<Effect>> _events;
        private readonly Dictionary<TechColor, IReadOnlyList<Effect>> _technologies;
        private readonly Dictionary<string, Effect> _statuses;

        private EffectCatalog(
            Dictionary<string, IReadOnlyList<Effect>> cards,
            Dictionary<string, IReadOnlyList<Effect>> events,
            Dictionary<TechColor, IReadOnlyList<Effect>> technologies,
            Dictionary<string, Effect> statuses)
        {
            _cards = cards;
            _events = events;
            _technologies = technologies;
            _statuses = statuses;
        }

        /// <summary>Builds the catalog for validated content.</summary>
        public static EffectCatalog Build(GameData data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Effect bricks are wired from the content files in milestone M2; until then cards have no effect.
            return new EffectCatalog(
                new Dictionary<string, IReadOnlyList<Effect>>(StringComparer.Ordinal),
                new Dictionary<string, IReadOnlyList<Effect>>(StringComparer.Ordinal),
                new Dictionary<TechColor, IReadOnlyList<Effect>>(),
                EngineStatuses());
        }

        /// <summary>Status behaviours implemented natively by the engine.</summary>
        public static Dictionary<string, Effect> EngineStatuses()
        {
            return new Dictionary<string, Effect>(StringComparer.Ordinal)
            {
                [StatusKinds.ForcedCrewAction] = new ForcedCrewActionStatus(),
                [StatusKinds.PreRolledDie] = new PreRolledDieStatus(),
            };
        }

        public IReadOnlyList<Effect> ForCard(string cardId) => _cards.TryGetValue(cardId, out IReadOnlyList<Effect>? e) ? e : None;

        public IReadOnlyList<Effect> ForEvent(string eventId) => _events.TryGetValue(eventId, out IReadOnlyList<Effect>? e) ? e : None;

        public IReadOnlyList<Effect> ForTechnology(TechColor color) => _technologies.TryGetValue(color, out IReadOnlyList<Effect>? e) ? e : None;

        public Effect ForStatus(string kind)
        {
            return _statuses.TryGetValue(kind, out Effect? e) ? e : throw new EngineException("Unknown status kind '" + kind + "'.");
        }
    }
}
