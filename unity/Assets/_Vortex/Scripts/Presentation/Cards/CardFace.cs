using System;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Core.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>What a card view shows, whatever the kind of card: modifier, event or technology.</summary>
    public readonly struct CardFace
    {
        /// <summary>Icon of an attack modifier's slot, in the icon catalog (docs/ASSETS.md section 3).</summary>
        public const string AttackIcon = "Slot_ATK";

        /// <summary>Icon of a defense modifier's slot.</summary>
        public const string DefenseIcon = "Slot_DEF";

        /// <summary>Icon of an event.</summary>
        public const string EventIcon = "Slot_EVT";

        /// <summary>Icon of a technology.</summary>
        public const string TechnologyIcon = "Slot_TECH";

        /// <summary>Creates a face.</summary>
        public CardFace(string id, string title, string badge, string usage, string text, TechColor color, string badgeIcon = "", string? flavor = null)
        {
            Flavor = flavor ?? string.Empty;
            Id = id;
            Title = title;
            Badge = badge;
            Usage = usage;
            Text = text;
            Color = color;
            BadgeIcon = badgeIcon;
        }

        /// <summary>Content id, which also names the illustration.</summary>
        public string Id { get; }

        /// <summary>Card name, without markup (plain text).</summary>
        public string Title { get; }

        /// <summary>Short kind shown in the card's disc: "ATK", "DEF", or the kind of an event or a technology.</summary>
        public string Badge { get; }

        /// <summary>Name of the icon shown in the disc instead of <see cref="Badge"/>, when the icon catalog has it.</summary>
        public string BadgeIcon { get; }

        /// <summary>Usage of a modifier ("Durable", "Usage unique"...), or the kind of an event or a technology.</summary>
        public string Usage { get; }

        /// <summary>Kind of card in one line, e.g. "ATK · Usage unique".</summary>
        public string Caption => string.IsNullOrEmpty(Badge) ? Usage : Badge + " · " + Usage;

        /// <summary>Printed text, in the content format (bold and icon tags).</summary>
        public string Text { get; }

        /// <summary>Flavour text under the printed text (ARB-95), plain; empty when the card has none.</summary>
        public string Flavor { get; }

        /// <summary>Technology colour (neutral for events).</summary>
        public TechColor Color { get; }

        /// <summary>Face of a modifier.</summary>
        public static CardFace Of(CardDefinition card, TextTable texts)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            string slot = texts.Get(card.Slot == CardSlot.Attack ? TextKeys.SlotAttack : TextKeys.SlotDefense);
            string usage = texts.Get(card.Usage switch
            {
                CardUsage.SingleUse => TextKeys.UsageSingleUse,
                CardUsage.Triggered => TextKeys.UsageTriggered,
                _ => TextKeys.UsageDurable,
            });
            return new CardFace(card.Id, CardText.ToPlainText(card.Name), slot, usage, card.Text, card.Color, card.Slot == CardSlot.Attack ? AttackIcon : DefenseIcon, card.Flavor);
        }

        /// <summary>Face of an event.</summary>
        public static CardFace Of(EventDefinition gameEvent, TextTable texts)
        {
            if (gameEvent is null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            return new CardFace(gameEvent.Id, CardText.ToPlainText(gameEvent.Name), texts.Get(TextKeys.BadgeEvent), texts.Get(TextKeys.KindEvent), gameEvent.Text, TechColor.Neutral, EventIcon, gameEvent.Flavor);
        }

        /// <summary>Face of a technology.</summary>
        public static CardFace Of(TechnologyDefinition technology, TextTable texts)
        {
            if (technology is null)
            {
                throw new ArgumentNullException(nameof(technology));
            }

            return new CardFace(technology.Id, CardText.ToPlainText(technology.Name), texts.Get(TextKeys.BadgeTechnology), texts.Get(TextKeys.KindTechnology), technology.Text, technology.Color, TechnologyIcon, technology.Flavor);
        }
    }
}
