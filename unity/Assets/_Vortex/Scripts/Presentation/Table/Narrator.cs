using System;
using System.Collections.Generic;
using Vortex.Client.Content;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// SINISTRA, the narrator (docs/DIRECTION_ARTISTIQUE.md 2.6 and 5.4): a cold remark after a fact of the log or an
    /// announcement. What she says lives in the interface texts, never in code: the line <see cref="TextKeys.Quip"/> of an
    /// event holds her variants, separated by <see cref="Separator"/>, and an empty line keeps her silent. She never names
    /// a card or states a rule: the fact before her remark does. She keeps quiet when the person turned her comments off.
    /// </summary>
    public sealed class Narrator
    {
        /// <summary>Separates the variants of a remark in one text.</summary>
        public const char Separator = '|';

        // Events she always comments; the others, about one time in two, so the log does not drown in her voice.
        private static readonly HashSet<string> Always = new HashSet<string>(StringComparer.Ordinal)
        {
            TextKeys.Quip(GameEventType.PlayerEliminated),
            TextKeys.Quip(GameEventType.CriticalHit),
            TextKeys.Quip(GameEventType.GameOver),
        };

        private readonly TextTable _texts;
        private readonly Func<float> _chance;
        private readonly Dictionary<string, int> _last = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>
        /// Creates the narrator. <paramref name="chance"/> returns a number from 0 to 1 at each call (a random source for
        /// the client, a fixed one in tests).
        /// </summary>
        public Narrator(TextTable texts, bool speaks, Func<float> chance)
        {
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _chance = chance ?? throw new ArgumentNullException(nameof(chance));
            Speaks = speaks;
        }

        /// <summary>Whether she comments (the option "Commentaires de SINISTRA").</summary>
        public bool Speaks { get; set; }

        /// <summary>Her remark on an event, or null when she keeps quiet.</summary>
        public string? RemarkOn(GameEvent gameEvent)
        {
            if (gameEvent is null)
            {
                throw new ArgumentNullException(nameof(gameEvent));
            }

            // An attack that did no damage has its own remarks.
            string key = gameEvent.Type == GameEventType.AttackResolved && gameEvent.Amount <= 0
                ? TextKeys.QuipMissed
                : TextKeys.Quip(gameEvent.Type);
            return Remark(key, always: Always.Contains(key));
        }

        /// <summary>Her remark for the announcement of a turn, or null.</summary>
        public string? RemarkOnTurn() => Remark(TextKeys.Quip(GameEventType.TurnStarted), always: true);

        private string? Remark(string key, bool always)
        {
            if (!Speaks)
            {
                return null;
            }

            if (!_texts.Contains(key))
            {
                return null;
            }

            string[] variants = _texts.Get(key).Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);
            if (variants.Length == 0 || (!always && _chance() >= 0.5f))
            {
                return null;
            }

            // A variant at random, never the one said last for this key.
            int pick = Math.Min((int)(_chance() * variants.Length), variants.Length - 1);
            if (variants.Length > 1 && _last.TryGetValue(key, out int last) && pick == last)
            {
                pick = (pick + 1) % variants.Length;
            }

            _last[key] = pick;
            return variants[pick].Trim();
        }
    }
}
