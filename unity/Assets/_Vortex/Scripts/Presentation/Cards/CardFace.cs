using System;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Core.Content;

namespace Vortex.Client.Presentation
{
    /// <summary>What a card view shows, whatever the kind of card: modifier, event or technology.</summary>
    public readonly struct CardFace
    {
        /// <summary>Creates a face.</summary>
        public CardFace(string id, string title, string caption, string text, TechColor color)
        {
            Id = id;
            Title = title;
            Caption = caption;
            Text = text;
            Color = color;
        }

        /// <summary>Content id, which also names the illustration.</summary>
        public string Id { get; }

        /// <summary>Card name, without markup (plain text).</summary>
        public string Title { get; }

        /// <summary>Kind of card, e.g. "ATK · Usage unique".</summary>
        public string Caption { get; }

        /// <summary>Printed text, in the content format (bold and icon tags).</summary>
        public string Text { get; }

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
            return new CardFace(card.Id, CardText.ToPlainText(card.Name), slot + " · " + usage, card.Text, card.Color);
        }

        /// <summary>Face of an event.</summary>
        public static CardFace Of(EventDefinition gameEvent, TextTable texts)
        {
            if (gameEvent is null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            return new CardFace(gameEvent.Id, CardText.ToPlainText(gameEvent.Name), texts.Get(TextKeys.KindEvent), gameEvent.Text, TechColor.Neutral);
        }

        /// <summary>Face of a technology.</summary>
        public static CardFace Of(TechnologyDefinition technology, TextTable texts)
        {
            if (technology is null)
            {
                throw new ArgumentNullException(nameof(technology));
            }

            return new CardFace(technology.Id, CardText.ToPlainText(technology.Name), texts.Get(TextKeys.KindTechnology), technology.Text, technology.Color);
        }
    }
}
