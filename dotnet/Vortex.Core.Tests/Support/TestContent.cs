using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Effects;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Rules;

namespace Vortex.Core.Tests.Support
{
    /// <summary>
    /// Synthetic content for engine tests (ADR-0007: engine tests never depend on the real cards).
    /// Ten attack and ten defense cards with neutral/blue/red colours, a calm event and a doom event.
    /// </summary>
    internal static class TestContent
    {
        public const string CalmEvent = "EVT_TEST_CALM";
        public const string DoomEvent = "EVT_TEST_DOOM";

        public static GameData Data(int copies = 2)
        {
            var cards = new List<CardDefinition>();
            for (int i = 1; i <= 10; i++)
            {
                TechColor color = i <= 6 ? TechColor.Neutral : (i <= 8 ? TechColor.Blue : TechColor.Red);
                cards.Add(Card(string.Format(System.Globalization.CultureInfo.InvariantCulture, "A_{0:000}", 900 + i), CardSlot.Attack, color, copies));
                cards.Add(Card(string.Format(System.Globalization.CultureInfo.InvariantCulture, "D_{0:000}", 900 + i), CardSlot.Defense, color, copies));
            }

            var events = new List<EventDefinition>
            {
                new EventDefinition(CalmEvent, "Calme", 3, "Rien.", "Rien.", null),
                new EventDefinition(DoomEvent, "Fin", 1, "Fin.", "Fin.", null),
            };
            var techs = new[] { TechColor.Blue, TechColor.Red, TechColor.Green, TechColor.Yellow }
                .Select(c => new TechnologyDefinition("TECH_" + c.ToString().ToUpperInvariant(), c, c.ToString(), "Test.", "Test.", null))
                .ToList();
            return new GameData(cards.OrderBy(c => c.Id, StringComparer.Ordinal).ToList(), events, techs);
        }

        public static CardDefinition Card(string id, CardSlot slot, TechColor color, int copies)
        {
            return new CardDefinition(id, "Test " + id, slot, color, CardUsage.Durable, copies, false, "Test.", "Test.", null);
        }

        public static GameConfig Config(int eventFrequency = 1, int doomRound = 50, int startShield = 4, int startingHp = 30, RoundStartRotation rotation = RoundStartRotation.None, int technologiesToWin = 4)
        {
            var counts = Enumerable.Range(2, 4).Select(n => new PlayerCountSettings(n, startShield, doomRound)).ToList();
            return new GameConfig(null, ContentFile.CurrentSchemaVersion, startingHp, 30, 0, 8, 8, 5, 1, 1, 1, eventFrequency, DoomEvent, rotation, technologiesToWin, counts, 16, 64);
        }
    }

    /// <summary>Effect catalog whose content is chosen by each test.</summary>
    internal sealed class TestCatalog : IEffectCatalog
    {
        private readonly Dictionary<string, List<Effect>> _cards = new Dictionary<string, List<Effect>>(StringComparer.Ordinal);
        private readonly Dictionary<string, List<Effect>> _events = new Dictionary<string, List<Effect>>(StringComparer.Ordinal);
        private readonly Dictionary<TechColor, List<Effect>> _techs = new Dictionary<TechColor, List<Effect>>();
        private readonly Dictionary<string, Effect> _statuses = EffectCatalog.AllStatuses();

        public TestCatalog Card(string id, params Effect[] effects)
        {
            _cards[id] = effects.ToList();
            return this;
        }

        public TestCatalog Event(string id, params Effect[] effects)
        {
            _events[id] = effects.ToList();
            return this;
        }

        public TestCatalog Technology(TechColor color, params Effect[] effects)
        {
            _techs[color] = effects.ToList();
            return this;
        }

        public TestCatalog Status(string kind, Effect effect)
        {
            _statuses[kind] = effect;
            return this;
        }

        public IReadOnlyList<Effect> ForCard(string cardId) => _cards.TryGetValue(cardId, out List<Effect>? e) ? e : new List<Effect>();

        public IReadOnlyList<Effect> ForEvent(string eventId) => _events.TryGetValue(eventId, out List<Effect>? e) ? e : new List<Effect>();

        public IReadOnlyList<Effect> ForTechnology(TechColor color) => _techs.TryGetValue(color, out List<Effect>? e) ? e : new List<Effect>();

        public Effect ForStatus(string kind) => _statuses.TryGetValue(kind, out Effect? e) ? e : throw new EngineException("Unknown status " + kind);
    }

    /// <summary>Dice results imposed by a test, in order.</summary>
    internal sealed class ScriptedDice : IDiceSource
    {
        private readonly Queue<int> _values;

        public ScriptedDice(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        public int Remaining => _values.Count;

        public int Next(int faces)
        {
            if (_values.Count == 0)
            {
                throw new InvalidOperationException("The test scripted fewer dice than the engine rolled.");
            }

            return _values.Dequeue();
        }
    }
}
