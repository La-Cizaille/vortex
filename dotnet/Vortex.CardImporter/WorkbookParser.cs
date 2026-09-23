using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Vortex.Core.Content;

namespace Vortex.CardImporter
{
    /// <summary>
    /// Maps the designer's spreadsheet layout onto <see cref="GameData"/>.
    /// Columns are located by header text, not position, so the designer can reorder them.
    /// </summary>
    internal static class WorkbookParser
    {
        internal const string AttackSheet = "Modificateurs attaque";
        internal const string DefenseSheet = "Modificateurs défense";
        internal const string EventsSheet = "Evènements";
        internal const string TechnologiesSheet = "Technologies";

        public static GameData Parse(XlsxReader workbook, string sourceSha256)
        {
            var modifiers = new List<CardDefinition>();
            modifiers.AddRange(ParseModifiers(workbook.GetSheet(AttackSheet), CardSlot.Attack, "technologie"));
            modifiers.AddRange(ParseModifiers(workbook.GetSheet(DefenseSheet), CardSlot.Defense, "couleur"));

            return new GameData(
                GameData.CurrentSchemaVersion,
                sourceSha256,
                modifiers,
                ParseEvents(workbook.GetSheet(EventsSheet)).ToList(),
                ParseTechnologies(workbook.GetSheet(TechnologiesSheet)).ToList());
        }

        private static IEnumerable<CardDefinition> ParseModifiers(IReadOnlyDictionary<string, string> cells, CardSlot slot, string colorHeader)
        {
            var sheet = new Sheet(cells);
            string idCol = sheet.Column("id");
            string nameCol = sheet.Column("nom");
            string usageCol = sheet.Column("mode");
            string textCol = sheet.Column("description");
            string colorCol = sheet.Column(colorHeader);
            string copiesCol = sheet.Column("distribution");
            string reviewCol = Sheet.NextColumn(copiesCol); // designer's "x" marker, unlabelled

            foreach (int row in sheet.DataRows)
            {
                string? id = sheet.Get(idCol, row);
                if (id is null)
                {
                    continue;
                }

                yield return new CardDefinition(
                    id: id,
                    name: Clean(sheet.Require(nameCol, row)),
                    text: Clean(sheet.Require(textCol, row)),
                    slot: slot,
                    color: ParseColor(sheet.Require(colorCol, row), id),
                    usage: ParseUsage(sheet.Require(usageCol, row), id),
                    copies: ParseCopies(sheet.Require(copiesCol, row), id),
                    needsReview: string.Equals(sheet.Get(reviewCol, row), "x", StringComparison.OrdinalIgnoreCase));
            }
        }

        private static IEnumerable<EventDefinition> ParseEvents(IReadOnlyDictionary<string, string> cells)
        {
            var sheet = new Sheet(cells);
            string nameCol = sheet.Column("nom");
            string textCol = sheet.Column("description");
            string copiesCol = sheet.Column("distribution");

            foreach (int row in sheet.DataRows)
            {
                string? rawName = sheet.Get(nameCol, row);
                if (rawName is null)
                {
                    continue;
                }

                string name = Clean(rawName);
                yield return new EventDefinition(
                    id: "EVT_" + Slug(name),
                    name: name,
                    text: Clean(sheet.Require(textCol, row)),
                    copies: ParseCopies(sheet.Require(copiesCol, row), name));
            }
        }

        private static IEnumerable<TechnologyDefinition> ParseTechnologies(IReadOnlyDictionary<string, string> cells)
        {
            var sheet = new Sheet(cells);
            string colorCol = sheet.Column("couleur");
            string nameCol = sheet.Column("nom");
            string textCol = sheet.Column("effet");

            foreach (int row in sheet.DataRows)
            {
                // Rows without a name are the designer's free-text notes below the table.
                string? name = sheet.Get(nameCol, row);
                string? color = sheet.Get(colorCol, row);
                if (name is null || color is null)
                {
                    continue;
                }

                yield return new TechnologyDefinition(ParseColor(color, name), Clean(name), Clean(sheet.Require(textCol, row)));
            }
        }

        internal static TechColor ParseColor(string value, string context)
        {
            switch (Normalise(value))
            {
                case "neutre": return TechColor.Neutral;
                case "bleu": return TechColor.Blue;
                case "rouge": return TechColor.Red;
                case "vert": return TechColor.Green;
                case "jaune": return TechColor.Yellow;
                default: throw new InvalidDataException(context + ": unknown colour '" + value + "'.");
            }
        }

        internal static CardUsage ParseUsage(string value, string context)
        {
            // Cells look like "<dur> Durable"; the tag is authoritative.
            string v = Normalise(value);
            if (v.StartsWith("<dur>", StringComparison.Ordinal))
            {
                return CardUsage.Durable;
            }

            if (v.StartsWith("<uni>", StringComparison.Ordinal))
            {
                return CardUsage.SingleUse;
            }

            if (v.StartsWith("<dec>", StringComparison.Ordinal))
            {
                return CardUsage.Triggered;
            }

            throw new InvalidDataException(context + ": unknown usage '" + value + "'.");
        }

        internal static int ParseCopies(string value, string context)
        {
            if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal d)
                || d != decimal.Truncate(d) || d < 1 || d > GameDataValidator.MaxCopies)
            {
                throw new InvalidDataException(context + ": invalid copy count '" + value + "'.");
            }

            return (int)d;
        }

        /// <summary>
        /// Normalises designer text: LF line endings, trimmed, and invisible format characters
        /// (e.g. the stray U+FE0F found in A_010) removed.
        /// </summary>
        internal static string Clean(string value)
        {
            var sb = new StringBuilder(value.Length);
            foreach (char c in value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n'))
            {
                // Format chars (zero-width, bidi controls) and emoji variation selectors are invisible noise.
                if (char.GetUnicodeCategory(c) == UnicodeCategory.Format || c == (char)0xFE0E || c == (char)0xFE0F)
                {
                    continue;
                }

                sb.Append(c == (char)0x00A0 ? ' ' : c);
            }

            // Trim trailing spaces on each line, then the whole text.
            string[] lines = sb.ToString().Split('\n');
            return string.Join("\n", lines.Select(l => l.TrimEnd())).Trim();
        }

        /// <summary>ASCII upper-case identifier: "tempête électro-magnetique" → "TEMPETE_ELECTRO_MAGNETIQUE".</summary>
        internal static string Slug(string value)
        {
            // Explicit folding table instead of string.Normalize(FormD): Unicode normalisation depends on
            // ICU/NLS availability, and ids must be identical on every machine and in CI.
            var sb = new StringBuilder();
            bool pendingUnderscore = false;
            foreach (char raw in value.ToLowerInvariant())
            {
                string folded = FoldFrench(raw);
                if (folded.Length == 0)
                {
                    pendingUnderscore = true;
                    continue;
                }

                if (pendingUnderscore && sb.Length > 0)
                {
                    sb.Append('_');
                }

                sb.Append(folded.ToUpperInvariant());
                pendingUnderscore = false;
            }

            return sb.ToString();
        }

        // Maps a lower-case char to its ASCII letters/digits, or "" for a separator.
        private static string FoldFrench(char c)
        {
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                return c.ToString();
            }

            switch ((int)c)
            {
                case 0xE0: case 0xE2: case 0xE4: return "a";               // à â ä
                case 0xE7: return "c";                                       // ç
                case 0xE8: case 0xE9: case 0xEA: case 0xEB: return "e";    // è é ê ë
                case 0xEE: case 0xEF: return "i";                           // î ï
                case 0xF4: case 0xF6: return "o";                           // ô ö
                case 0xF9: case 0xFB: case 0xFC: return "u";               // ù û ü
                case 0xFF: return "y";                                       // ÿ
                case 0x153: return "oe";                                     // œ
                case 0xE6: return "ae";                                      // æ
                default: return string.Empty;
            }
        }

        private static string Normalise(string value)
        {
            return Clean(value).ToLowerInvariant();
        }

        /// <summary>Row/column view over a sheet's cells, with header lookup on row 1.</summary>
        private sealed class Sheet
        {
            private readonly IReadOnlyDictionary<string, string> _cells;
            private readonly Dictionary<string, string> _headerToColumn = new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly int _maxRow;

            public Sheet(IReadOnlyDictionary<string, string> cells)
            {
                _cells = cells;
                foreach (KeyValuePair<string, string> cell in cells)
                {
                    (string col, int row) = Split(cell.Key);
                    _maxRow = Math.Max(_maxRow, row);
                    if (row == 1)
                    {
                        // Header cells can hold a second line of notes: only the first line names the column.
                        string header = Slug(Clean(cell.Value).Split('\n')[0]).ToLowerInvariant();
                        _headerToColumn[header] = col;
                    }
                }
            }

            public IEnumerable<int> DataRows => Enumerable.Range(2, Math.Max(0, _maxRow - 1));

            public string Column(string headerPrefix)
            {
                foreach (KeyValuePair<string, string> h in _headerToColumn.OrderBy(h => h.Value.Length).ThenBy(h => h.Value, StringComparer.Ordinal))
                {
                    if (h.Key.StartsWith(headerPrefix, StringComparison.Ordinal))
                    {
                        return h.Value;
                    }
                }

                throw new InvalidDataException("Column starting with '" + headerPrefix + "' not found. Headers: " + string.Join(", ", _headerToColumn.Keys));
            }

            public string? Get(string col, int row)
            {
                return _cells.TryGetValue(col + row.ToString(CultureInfo.InvariantCulture), out string? v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;
            }

            public string Require(string col, int row)
            {
                return Get(col, row) ?? throw new InvalidDataException("Cell " + col + row.ToString(CultureInfo.InvariantCulture) + " is empty.");
            }

            public static string NextColumn(string col)
            {
                char[] chars = col.ToCharArray();
                for (int i = chars.Length - 1; i >= 0; i--)
                {
                    if (chars[i] != 'Z')
                    {
                        chars[i]++;
                        return new string(chars);
                    }

                    chars[i] = 'A';
                }

                return "A" + new string(chars);
            }

            private static (string Col, int Row) Split(string reference)
            {
                int i = 0;
                while (i < reference.Length && char.IsAsciiLetterUpper(reference[i]))
                {
                    i++;
                }

                if (i == 0 || !int.TryParse(reference.AsSpan(i), NumberStyles.None, CultureInfo.InvariantCulture, out int row))
                {
                    throw new InvalidDataException("Invalid cell reference '" + reference + "'.");
                }

                return (reference.Substring(0, i), row);
            }
        }
    }
}
