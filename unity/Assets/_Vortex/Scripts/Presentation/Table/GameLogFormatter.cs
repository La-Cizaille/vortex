using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Writes the game log line of an event from the interface texts (<see cref="TextKeys.Log"/>). An event whose text
    /// is empty is left out of the log, so the designer chooses what the log tells. The lines contain player names as
    /// typed: the log must display them as plain text.
    /// </summary>
    public sealed class GameLogFormatter
    {
        private readonly TextTable _texts;
        private readonly Dictionary<string, string> _names;
        private readonly Dictionary<TechColor, string> _technologies;

        /// <summary>Creates a formatter for the game content.</summary>
        public GameLogFormatter(TextTable texts, GameData data)
        {
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            _names = data.Modifiers.Select(c => (c.Id, c.Name))
                .Concat(data.Events.Select(e => (e.Id, e.Name)))
                .Concat(data.Technologies.Select(t => (t.Id, t.Name)))
                .ToDictionary(n => n.Id, n => CardText.ToPlainText(n.Name), StringComparer.Ordinal);
            _technologies = data.Technologies.ToDictionary(t => t.Color, t => CardText.ToPlainText(t.Name));
        }

        /// <summary>The log line of an event, or null when the event is not logged.</summary>
        public string? Describe(GameEvent gameEvent, IReadOnlyList<SeatModel> seats)
        {
            if (gameEvent is null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            string key = TextKeys.Log(gameEvent.Type);
            string template = _texts.Get(key);
            if (template.Length == 0)
            {
                return null;
            }

            string dice = gameEvent.Values is null ? string.Empty : string.Join(" ", gameEvent.Values);
            try
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    template,
                    Name(seats, gameEvent.Player),
                    Name(seats, gameEvent.Other),
                    gameEvent.Amount,
                    gameEvent.Value,
                    Subject(gameEvent),
                    dice);
            }
            catch (FormatException)
            {
                // A placeholder the event does not have: show the gap rather than fail.
                return "#" + key;
            }
        }

        // What {4} names for this kind of event.
        private string Subject(GameEvent gameEvent)
        {
            switch (gameEvent.Type)
            {
                case GameEventType.TechnologyActivated:
                    return _technologies.TryGetValue((TechColor)gameEvent.Value, out string? technology) ? technology : string.Empty;
                case GameEventType.CrewActionPerformed:
                    return _texts.Get(TextKeys.Crew((CrewAction)gameEvent.Value));
                case GameEventType.GameOver:
                    return _texts.Get(TextKeys.Win((WinCondition)gameEvent.Value));
                case GameEventType.MarketRecycled:
                    return _texts.Get(gameEvent.Value == (int)CardSlot.Attack ? TextKeys.SlotAttack : TextKeys.SlotDefense);
                case GameEventType.StatusAdded:
                case GameEventType.StatusEnded:
                    return gameEvent.Id is null ? string.Empty : _texts.Get(TextKeys.Status(gameEvent.Id));
                default:
                    return gameEvent.Id is null ? string.Empty : _names.TryGetValue(gameEvent.Id, out string? name) ? name : gameEvent.Id;
            }
        }

        private string Name(IReadOnlyList<SeatModel> seats, int seat) =>
            seat >= 0 && seat < seats.Count ? seats[seat].Name : _texts.Get(TextKeys.LogNobody);
    }
}
