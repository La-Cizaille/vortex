using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Vortex.Core.Content
{
    /// <summary>
    /// Static description of a modifier card, generated from <c>design/Vortex.xlsx</c>.
    /// Holds data only; behaviour lives in the card's behaviour class (ADR-0006).
    /// </summary>
    public sealed class CardDefinition
    {
        /// <summary>Creates a card definition. Values are validated by <see cref="GameDataValidator"/>, not here.</summary>
        [JsonConstructor]
        public CardDefinition(string id, string name, string text, CardSlot slot, TechColor color, CardUsage usage, int copies, bool needsReview)
        {
            Id = id;
            Name = name;
            Text = text;
            Slot = slot;
            Color = color;
            Usage = usage;
            Copies = copies;
            NeedsReview = needsReview;
        }

        /// <summary>Stable identifier, e.g. <c>A_005</c> or <c>D_012</c>.</summary>
        public string Id { get; }

        /// <summary>Display name (French).</summary>
        public string Name { get; }

        /// <summary>Rules text as written by the designer, including icon tags such as <c>&lt;ATQ&gt;</c>.</summary>
        public string Text { get; }

        /// <summary>Slot and market of the card.</summary>
        public CardSlot Slot { get; }

        /// <summary>Technology colour.</summary>
        public TechColor Color { get; }

        /// <summary>Usage mode.</summary>
        public CardUsage Usage { get; }

        /// <summary>Number of copies in the deck.</summary>
        public int Copies { get; }

        /// <summary>True when the designer flagged the card for rework ("x" column).</summary>
        public bool NeedsReview { get; }
    }

    /// <summary>Static description of an event card (RULES.md §4, §10.3).</summary>
    public sealed class EventDefinition
    {
        /// <summary>Creates an event definition.</summary>
        [JsonConstructor]
        public EventDefinition(string id, string name, string text, int copies)
        {
            Id = id;
            Name = name;
            Text = text;
            Copies = copies;
        }

        /// <summary>Stable identifier derived from the name, e.g. <c>EVT_TROU_NOIR</c>.</summary>
        public string Id { get; }

        /// <summary>Display name (French).</summary>
        public string Name { get; }

        /// <summary>Rules text.</summary>
        public string Text { get; }

        /// <summary>Number of copies in the event deck.</summary>
        public int Copies { get; }
    }

    /// <summary>Static description of a technology combo (RULES.md §8).</summary>
    public sealed class TechnologyDefinition
    {
        /// <summary>Creates a technology definition.</summary>
        [JsonConstructor]
        public TechnologyDefinition(TechColor color, string name, string text)
        {
            Color = color;
            Name = name;
            Text = text;
        }

        /// <summary>Colour required on both equipped modifiers.</summary>
        public TechColor Color { get; }

        /// <summary>Display name (French).</summary>
        public string Name { get; }

        /// <summary>Rules text.</summary>
        public string Text { get; }
    }

    /// <summary>All static game content, as loaded from <c>core/Runtime/Data/gamedata.json</c>.</summary>
    public sealed class GameData
    {
        /// <summary>Schema version this code understands.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Creates the content set.</summary>
        [JsonConstructor]
        public GameData(
            int schemaVersion,
            string sourceSha256,
            IReadOnlyList<CardDefinition> modifiers,
            IReadOnlyList<EventDefinition> events,
            IReadOnlyList<TechnologyDefinition> technologies)
        {
            SchemaVersion = schemaVersion;
            SourceSha256 = sourceSha256;
            Modifiers = modifiers ?? Array.Empty<CardDefinition>();
            Events = events ?? Array.Empty<EventDefinition>();
            Technologies = technologies ?? Array.Empty<TechnologyDefinition>();
        }

        /// <summary>Schema version of the file.</summary>
        public int SchemaVersion { get; }

        /// <summary>SHA-256 of the spreadsheet the file was generated from (traceability).</summary>
        public string SourceSha256 { get; }

        /// <summary>Attack and defense modifiers.</summary>
        public IReadOnlyList<CardDefinition> Modifiers { get; }

        /// <summary>Event cards, including the doom event which the engine removes from the deck.</summary>
        public IReadOnlyList<EventDefinition> Events { get; }

        /// <summary>Technology combos, one per non-neutral colour.</summary>
        public IReadOnlyList<TechnologyDefinition> Technologies { get; }
    }
}
