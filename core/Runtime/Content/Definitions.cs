using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Vortex.Core.Content
{
    /// <summary>
    /// Static description of a modifier card, read from <c>core/Runtime/Data/cards.json</c> (the source of truth, ADR-0008).
    /// Holds data only; behaviour is built from effect bricks or a card class (ADR-0007).
    /// </summary>
    public sealed class CardDefinition
    {
        /// <summary>Creates a card definition. Values are validated by <see cref="GameDataValidator"/>, not here.</summary>
        [JsonConstructor]
        public CardDefinition(string id, string name, CardSlot slot, TechColor color, CardUsage usage, int copies, bool needsReview, string text, string ruling, IReadOnlyList<EffectSpec>? effects, string? flavor = null)
        {
            Id = id;
            Name = name;
            Slot = slot;
            Color = color;
            Usage = usage;
            Copies = copies;
            NeedsReview = needsReview;
            Text = text;
            Ruling = ruling;
            Effects = effects ?? Array.Empty<EffectSpec>();
            Flavor = flavor;
        }

        /// <summary>Stable identifier, e.g. <c>A_005</c> or <c>D_012</c>.</summary>
        public string Id { get; }

        /// <summary>Display name (French).</summary>
        public string Name { get; }

        /// <summary>Slot and market of the card.</summary>
        public CardSlot Slot { get; }

        /// <summary>Technology colour.</summary>
        public TechColor Color { get; }

        /// <summary>Usage mode.</summary>
        public CardUsage Usage { get; }

        /// <summary>Number of copies in the deck.</summary>
        public int Copies { get; }

        /// <summary>True when the designer flagged the card for rework.</summary>
        public bool NeedsReview { get; }

        /// <summary>Rules text printed on the card, including icon tags such as <c>&lt;ATQ&gt;</c>.</summary>
        public string Text { get; }

        /// <summary>
        /// Precise interpretation of <see cref="Text"/> in terms of the effect model (docs/RULES.md part B).
        /// Never names another card.
        /// </summary>
        public string Ruling { get; }

        /// <summary>Effect bricks implementing the ruling (ADR-0007), in order.</summary>
        public IReadOnlyList<EffectSpec> Effects { get; }

        /// <summary>
        /// Flavour text shown under the rules text (ARB-95), or null. It never states a rule: the rules text alone does
        /// (docs/DIRECTION_ARTISTIQUE.md section 5.3). Plain text, without markup.
        /// </summary>
        public string? Flavor { get; }
    }

    /// <summary>Static description of an event card (docs/RULES.md A4).</summary>
    public sealed class EventDefinition
    {
        /// <summary>Creates an event definition.</summary>
        [JsonConstructor]
        public EventDefinition(string id, string name, int copies, string text, string ruling, IReadOnlyList<EffectSpec>? effects, string? flavor = null)
        {
            Id = id;
            Name = name;
            Copies = copies;
            Text = text;
            Ruling = ruling;
            Effects = effects ?? Array.Empty<EffectSpec>();
            Flavor = flavor;
        }

        /// <summary>Stable identifier, e.g. <c>EVT_TROU_NOIR</c>.</summary>
        public string Id { get; }

        /// <summary>Display name (French).</summary>
        public string Name { get; }

        /// <summary>Number of copies in the event deck.</summary>
        public int Copies { get; }

        /// <summary>Rules text.</summary>
        public string Text { get; }

        /// <summary>Precise interpretation of <see cref="Text"/>.</summary>
        public string Ruling { get; }

        /// <summary>Effect bricks (ADR-0007), in order.</summary>
        public IReadOnlyList<EffectSpec> Effects { get; }

        /// <summary>
        /// Flavour text shown under the rules text (ARB-95), or null. It never states a rule: the rules text alone does
        /// (docs/DIRECTION_ARTISTIQUE.md section 5.3). Plain text, without markup.
        /// </summary>
        public string? Flavor { get; }
    }

    /// <summary>Static description of a technology combo (docs/RULES.md A8).</summary>
    public sealed class TechnologyDefinition
    {
        /// <summary>Creates a technology definition.</summary>
        [JsonConstructor]
        public TechnologyDefinition(string id, TechColor color, string name, string text, string ruling, IReadOnlyList<EffectSpec>? effects, string? flavor = null)
        {
            Id = id;
            Color = color;
            Name = name;
            Text = text;
            Ruling = ruling;
            Effects = effects ?? Array.Empty<EffectSpec>();
            Flavor = flavor;
        }

        /// <summary>Stable identifier, e.g. <c>TECH_BLUE</c>.</summary>
        public string Id { get; }

        /// <summary>Colour required on both equipped modifiers.</summary>
        public TechColor Color { get; }

        /// <summary>Display name (French).</summary>
        public string Name { get; }

        /// <summary>Rules text.</summary>
        public string Text { get; }

        /// <summary>Precise interpretation of <see cref="Text"/>.</summary>
        public string Ruling { get; }

        /// <summary>Effect bricks (ADR-0007), in order.</summary>
        public IReadOnlyList<EffectSpec> Effects { get; }

        /// <summary>
        /// Flavour text shown under the rules text (ARB-95), or null. It never states a rule: the rules text alone does
        /// (docs/DIRECTION_ARTISTIQUE.md section 5.3). Plain text, without markup.
        /// </summary>
        public string? Flavor { get; }
    }

    /// <summary>All static game content, assembled from the content files by <see cref="GameDataLoader"/>.</summary>
    public sealed class GameData
    {
        /// <summary>Creates the content set.</summary>
        public GameData(IReadOnlyList<CardDefinition> modifiers, IReadOnlyList<EventDefinition> events, IReadOnlyList<TechnologyDefinition> technologies)
        {
            Modifiers = modifiers ?? throw new ArgumentNullException(nameof(modifiers));
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Technologies = technologies ?? throw new ArgumentNullException(nameof(technologies));
        }

        /// <summary>Attack and defense modifiers.</summary>
        public IReadOnlyList<CardDefinition> Modifiers { get; }

        /// <summary>Event cards, including the doom event which the engine keeps out of the deck.</summary>
        public IReadOnlyList<EventDefinition> Events { get; }

        /// <summary>Technology combos, one per non-neutral colour.</summary>
        public IReadOnlyList<TechnologyDefinition> Technologies { get; }
    }
}
