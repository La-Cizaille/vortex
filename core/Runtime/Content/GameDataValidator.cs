using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Vortex.Core.Content
{
    /// <summary>
    /// Structural validation of <see cref="GameData"/>. Used both by the importer (fail the build on bad
    /// spreadsheet data) and at load time (never trust a file on disk, docs/SECURITY.md S2).
    /// </summary>
    /// <remarks>
    /// Checks shape only (ids, enums, bounds, uniqueness). Balance-level expectations such as
    /// "54 attack copies" are asserted by tests, because the designer may legitimately change them.
    /// </remarks>
    public static class GameDataValidator
    {
        /// <summary>Maximum length of a display name.</summary>
        public const int MaxNameLength = 64;

        /// <summary>Maximum length of a rules text.</summary>
        public const int MaxTextLength = 600;

        /// <summary>Longest flavour text, in characters: one or two lines under the rules text.</summary>
        public const int MaxFlavorLength = 200;

        /// <summary>Maximum length of a ruling.</summary>
        public const int MaxRulingLength = 1000;

        /// <summary>Maximum copies of a single card in a deck.</summary>
        public const int MaxCopies = 8;

        private static readonly Regex AttackId = new Regex("^A_[0-9]{3}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private static readonly Regex DefenseId = new Regex("^D_[0-9]{3}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private static readonly Regex EventId = new Regex("^EVT_[A-Z0-9_]{1,48}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        private static readonly Regex TechnologyId = new Regex("^TECH_[A-Z]{1,16}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

        /// <summary>Returns every problem found; an empty list means the data is valid.</summary>
        public static IReadOnlyList<string> Validate(GameData data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var errors = new List<string>();

            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < data.Modifiers.Count; i++)
            {
                CardDefinition? card = data.Modifiers[i];
                string where = "modifiers[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (card is null)
                {
                    errors.Add(where + " is null.");
                    continue;
                }

                Regex idPattern = card.Slot == CardSlot.Attack ? AttackId : DefenseId;
                CheckId(card.Id, idPattern, where, ids, errors);
                CheckEnum(card.Slot, where + ".slot", errors);
                CheckEnum(card.Color, where + ".color", errors);
                CheckEnum(card.Usage, where + ".usage", errors);
                CheckText(card.Name, MaxNameLength, where + ".name", errors);
                CheckText(card.Text, MaxTextLength, where + ".text", errors);
                CheckText(card.Ruling, MaxRulingLength, where + ".ruling", errors);
                CheckFlavor(card.Flavor, where + ".flavor", errors);
                CheckCopies(card.Copies, where, errors);
            }

            for (int i = 0; i < data.Events.Count; i++)
            {
                EventDefinition? evt = data.Events[i];
                string where = "events[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (evt is null)
                {
                    errors.Add(where + " is null.");
                    continue;
                }

                CheckId(evt.Id, EventId, where, ids, errors);
                CheckText(evt.Name, MaxNameLength, where + ".name", errors);
                CheckText(evt.Text, MaxTextLength, where + ".text", errors);
                CheckText(evt.Ruling, MaxRulingLength, where + ".ruling", errors);
                CheckFlavor(evt.Flavor, where + ".flavor", errors);
                CheckCopies(evt.Copies, where, errors);
            }

            var colors = new HashSet<TechColor>();
            for (int i = 0; i < data.Technologies.Count; i++)
            {
                TechnologyDefinition? tech = data.Technologies[i];
                string where = "technologies[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                if (tech is null)
                {
                    errors.Add(where + " is null.");
                    continue;
                }

                CheckId(tech.Id, TechnologyId, where, ids, errors);
                CheckEnum(tech.Color, where + ".color", errors);
                if (tech.Id != null && tech.Id != "TECH_" + tech.Color.ToString().ToUpperInvariant())
                {
                    errors.Add(where + ": id '" + tech.Id + "' does not match its colour (expected TECH_" + tech.Color.ToString().ToUpperInvariant() + ").");
                }

                if (tech.Color == TechColor.Neutral)
                {
                    errors.Add(where + ": a technology cannot be Neutral.");
                }
                else if (!colors.Add(tech.Color))
                {
                    errors.Add(where + ": duplicate technology colour " + tech.Color + ".");
                }

                CheckText(tech.Name, MaxNameLength, where + ".name", errors);
                CheckText(tech.Text, MaxTextLength, where + ".text", errors);
                CheckText(tech.Ruling, MaxRulingLength, where + ".ruling", errors);
                CheckFlavor(tech.Flavor, where + ".flavor", errors);
            }

            if (colors.Count != 4)
            {
                errors.Add("Exactly one technology per non-neutral colour is required (found " + colors.Count.ToString(CultureInfo.InvariantCulture) + ").");
            }

            return errors;
        }

        private static void CheckId(string? id, Regex pattern, string where, HashSet<string> seen, List<string> errors)
        {
            if (id is null || !pattern.IsMatch(id))
            {
                errors.Add(where + ": invalid id '" + id + "' (expected " + pattern + ").");
                return;
            }

            if (!seen.Add(id))
            {
                errors.Add(where + ": duplicate id '" + id + "'.");
            }
        }

        private static void CheckEnum<T>(T value, string where, List<string> errors)
            where T : struct, Enum
        {
            if (!Enum.IsDefined(typeof(T), value))
            {
                errors.Add(where + ": undefined value '" + value + "'.");
            }
        }

        // Flavour text is optional; when present it is plain text: no markup, since the client shows it inside rich text.
        private static void CheckFlavor(string? flavor, string where, List<string> errors)
        {
            if (flavor is null)
            {
                return;
            }

            CheckText(flavor, MaxFlavorLength, where, errors);
            if (flavor.Contains('<') || flavor.Contains('>'))
            {
                errors.Add(where + " contains markup ('<' or '>'), which flavour text may not.");
            }
        }

        private static void CheckText(string? text, int maxLength, string where, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                errors.Add(where + " is empty.");
                return;
            }

            if (text!.Length > maxLength)
            {
                errors.Add(where + " exceeds " + maxLength.ToString(CultureInfo.InvariantCulture) + " characters.");
            }

            foreach (char c in text)
            {
                if (IsForbiddenTextCharacter(c))
                {
                    errors.Add(where + string.Format(CultureInfo.InvariantCulture, " contains forbidden character U+{0:X4}.", (int)c));
                    return;
                }
            }
        }

        private static void CheckCopies(int copies, string where, List<string> errors)
        {
            if (copies < 1 || copies > MaxCopies)
            {
                errors.Add(where + string.Format(CultureInfo.InvariantCulture, ": copies must be between 1 and {0} (got {1}).", MaxCopies, copies));
            }
        }

        /// <summary>
        /// True for a character refused in any displayed text: a control character other than '\n' (no '\r', tab or NUL),
        /// or a bidirectional override. Shared with the tools so that every text shown to a reader follows one policy.
        /// </summary>
        public static bool IsForbiddenTextCharacter(char c)
        {
            return (char.IsControl(c) && c != '\n') || IsBidiControl(c);
        }

        // Bidirectional overrides can make displayed text differ from stored text (CVE-2021-42574 "Trojan Source").
        private static bool IsBidiControl(char c)
        {
            return (c >= (char)0x202A && c <= (char)0x202E) || (c >= (char)0x2066 && c <= (char)0x2069) || c == (char)0x200E || c == (char)0x200F;
        }
    }
}
