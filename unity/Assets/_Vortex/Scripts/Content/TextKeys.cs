using System.Collections.Generic;

namespace Vortex.Client.Content
{
    /// <summary>
    /// Keys of the interface texts, with their default French text. The defaults only fill a new or incomplete
    /// <see cref="TextTable"/> asset; what the game shows is the asset's text.
    /// </summary>
    public static class TextKeys
    {
        /// <summary>Attack modifier (card caption).</summary>
        public const string SlotAttack = "card.slot.attack";

        /// <summary>Defense modifier (card caption).</summary>
        public const string SlotDefense = "card.slot.defense";

        /// <summary>Durable card (card caption).</summary>
        public const string UsageDurable = "card.usage.durable";

        /// <summary>Single-use card (card caption).</summary>
        public const string UsageSingleUse = "card.usage.single-use";

        /// <summary>Triggered card (card caption).</summary>
        public const string UsageTriggered = "card.usage.triggered";

        /// <summary>Event card (card caption).</summary>
        public const string KindEvent = "card.kind.event";

        /// <summary>Technology card (card caption).</summary>
        public const string KindTechnology = "card.kind.technology";

        /// <summary>Gallery section: attack modifiers.</summary>
        public const string GalleryAttack = "gallery.attack";

        /// <summary>Gallery section: defense modifiers.</summary>
        public const string GalleryDefense = "gallery.defense";

        /// <summary>Gallery section: events.</summary>
        public const string GalleryEvents = "gallery.events";

        /// <summary>Gallery section: technologies.</summary>
        public const string GalleryTechnologies = "gallery.technologies";

        /// <summary>Every key with its default text.</summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string>> Defaults = new[]
        {
            Text(SlotAttack, "ATK"),
            Text(SlotDefense, "DEF"),
            Text(UsageDurable, "Durable"),
            Text(UsageSingleUse, "Usage unique"),
            Text(UsageTriggered, "Déclenchement"),
            Text(KindEvent, "Événement"),
            Text(KindTechnology, "Technologie"),
            Text(GalleryAttack, "Modificateurs d'attaque"),
            Text(GalleryDefense, "Modificateurs de défense"),
            Text(GalleryEvents, "Événements"),
            Text(GalleryTechnologies, "Technologies"),
        };

        private static KeyValuePair<string, string> Text(string key, string text) => new KeyValuePair<string, string>(key, text);
    }
}
