using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Vortex.Core.Content
{
    /// <summary>Common shape of a content file: editor schema pointer, format version, items.</summary>
    public abstract class ContentFile
    {
        /// <summary>Content file format understood by this code.</summary>
        public const int CurrentSchemaVersion = 2;

        /// <summary>Creates the file header.</summary>
        protected ContentFile(string? schema, int schemaVersion)
        {
            Schema = schema;
            SchemaVersion = schemaVersion;
        }

        /// <summary>Relative path to the JSON Schema used by editors (VS Code) for completion and live validation.</summary>
        [JsonProperty("$schema", Order = -3)]
        public string? Schema { get; }

        /// <summary>Content file format version.</summary>
        [JsonProperty(Order = -2)]
        public int SchemaVersion { get; }
    }

    /// <summary><c>cards.json</c>: every attack and defense modifier.</summary>
    public sealed class CardsFile : ContentFile
    {
        /// <summary>File name inside the data folder.</summary>
        public const string FileName = "cards.json";

        /// <summary>Creates the file.</summary>
        [JsonConstructor]
        public CardsFile(string? schema, int schemaVersion, IReadOnlyList<CardDefinition> cards)
            : base(schema, schemaVersion)
        {
            Cards = cards ?? Array.Empty<CardDefinition>();
        }

        /// <summary>Modifier cards.</summary>
        public IReadOnlyList<CardDefinition> Cards { get; }
    }

    /// <summary><c>events.json</c>: every event card.</summary>
    public sealed class EventsFile : ContentFile
    {
        /// <summary>File name inside the data folder.</summary>
        public const string FileName = "events.json";

        /// <summary>Creates the file.</summary>
        [JsonConstructor]
        public EventsFile(string? schema, int schemaVersion, IReadOnlyList<EventDefinition> events)
            : base(schema, schemaVersion)
        {
            Events = events ?? Array.Empty<EventDefinition>();
        }

        /// <summary>Event cards.</summary>
        public IReadOnlyList<EventDefinition> Events { get; }
    }

    /// <summary><c>technologies.json</c>: the technology combos.</summary>
    public sealed class TechnologiesFile : ContentFile
    {
        /// <summary>File name inside the data folder.</summary>
        public const string FileName = "technologies.json";

        /// <summary>Creates the file.</summary>
        [JsonConstructor]
        public TechnologiesFile(string? schema, int schemaVersion, IReadOnlyList<TechnologyDefinition> technologies)
            : base(schema, schemaVersion)
        {
            Technologies = technologies ?? Array.Empty<TechnologyDefinition>();
        }

        /// <summary>Technology combos.</summary>
        public IReadOnlyList<TechnologyDefinition> Technologies { get; }
    }
}
