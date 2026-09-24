using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Content;
using Vortex.Core.Effects.Bricks;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Rules;

namespace Vortex.Core.Effects
{
    /// <summary>
    /// Production effect catalog: compiles the effect bricks declared in the content files (ADR-0007) and knows
    /// every status behaviour. Built once per engine; immutable afterwards.
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

        /// <summary>Compiles the effects of all content.</summary>
        /// <exception cref="GameDataException">A brick is unknown, badly parameterized, or inconsistent with its carrier.</exception>
        public static EffectCatalog Build(GameData data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Dictionary<string, BrickInfo> bricks = BrickCatalog.Entries().ToDictionary(b => b.Name, StringComparer.Ordinal);
            var errors = new List<string>();
            var cards = new Dictionary<string, IReadOnlyList<Effect>>(StringComparer.Ordinal);
            var events = new Dictionary<string, IReadOnlyList<Effect>>(StringComparer.Ordinal);
            var techs = new Dictionary<TechColor, IReadOnlyList<Effect>>();

            foreach (CardDefinition card in data.Modifiers)
            {
                List<Effect> effects = CompileAll(bricks, card.Id, card.Effects, errors);
                CheckUsage(card, effects, errors);
                cards[card.Id] = effects;
            }

            foreach (EventDefinition evt in data.Events)
            {
                events[evt.Id] = CompileAll(bricks, evt.Id, evt.Effects, errors);
            }

            foreach (TechnologyDefinition tech in data.Technologies)
            {
                List<Effect> effects = CompileAll(bricks, tech.Id, tech.Effects, errors);
                if (effects.Any(e => !e.IsActivation))
                {
                    errors.Add(tech.Id + ": a technology only has activation bricks.");
                }

                techs[tech.Color] = effects;
            }

            if (errors.Count > 0)
            {
                throw new GameDataException("Invalid effects:\n - " + string.Join("\n - ", errors));
            }

            return new EffectCatalog(cards, events, techs, AllStatuses());
        }

        /// <summary>Status behaviours implemented natively by the engine.</summary>
        public static Dictionary<string, Effect> EngineStatuses()
        {
            return new Dictionary<string, Effect>(StringComparer.Ordinal)
            {
                [StatusKinds.ForcedCrewAction] = new ForcedCrewActionStatus(),
                [StatusKinds.PreRolledDie] = new PreRolledDieStatus(),
                [StatusKinds.DefensivePosture] = new DefensivePostureStatus(),
            };
        }

        /// <summary>Engine and brick statuses.</summary>
        public static Dictionary<string, Effect> AllStatuses()
        {
            Dictionary<string, Effect> all = EngineStatuses();
            foreach (KeyValuePair<string, Effect> s in BrickStatuses.All())
            {
                all.Add(s.Key, s.Value);
            }

            return all;
        }

        public IReadOnlyList<Effect> ForCard(string cardId) => _cards.TryGetValue(cardId, out IReadOnlyList<Effect>? e) ? e : None;

        public IReadOnlyList<Effect> ForEvent(string eventId) => _events.TryGetValue(eventId, out IReadOnlyList<Effect>? e) ? e : None;

        public IReadOnlyList<Effect> ForTechnology(TechColor color) => _technologies.TryGetValue(color, out IReadOnlyList<Effect>? e) ? e : None;

        public Effect ForStatus(string kind)
        {
            return _statuses.TryGetValue(kind, out Effect? e) ? e : throw new EngineException("Unknown status kind '" + kind + "'.");
        }

        private static List<Effect> CompileAll(IReadOnlyDictionary<string, BrickInfo> bricks, string owner, IReadOnlyList<EffectSpec> specs, List<string> errors)
        {
            var effects = new List<Effect>();
            for (int i = 0; i < specs.Count; i++)
            {
                try
                {
                    effects.Add(BrickCatalog.Compile(bricks, specs[i]));
                }
                catch (BrickParamException ex)
                {
                    errors.Add(owner + ".effects[" + i + "] " + specs[i]?.Brick + ": " + ex.Message);
                }
            }

            return effects;
        }

        // Card usage and brick kinds must agree (RULES A5.3): single-use cards only activate, durable cards never do.
        private static void CheckUsage(CardDefinition card, List<Effect> effects, List<string> errors)
        {
            if (card.Effects.Count > 0 && effects.Count == 0)
            {
                return; // compilation errors already reported
            }

            switch (card.Usage)
            {
                case CardUsage.SingleUse:
                    if (effects.Count == 0 || effects.Any(e => !e.IsActivation))
                    {
                        errors.Add(card.Id + ": a single-use card needs activation bricks only.");
                    }

                    break;
                case CardUsage.Durable:
                    if (effects.Any(e => e.IsActivation))
                    {
                        errors.Add(card.Id + ": a durable card cannot have activation bricks.");
                    }

                    break;
                default:
                    break;
            }
        }
    }
}
