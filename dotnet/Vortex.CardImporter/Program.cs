using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Vortex.Core.Content;

namespace Vortex.CardImporter
{
    /// <summary>
    /// Usage:
    ///   Vortex.CardImporter &lt;workbook.xlsx&gt; &lt;output-dir&gt;           write gamedata.json
    ///   Vortex.CardImporter &lt;workbook.xlsx&gt; &lt;output-dir&gt; --check   fail if gamedata.json is stale (CI)
    /// Exit codes: 0 ok, 1 stale output (--check), 2 invalid input or usage.
    /// </summary>
    internal static class Program
    {
        internal const string OutputFileName = "gamedata.json";
        private const long MaxWorkbookBytes = 10L * 1024 * 1024;

        private static int Main(string[] args)
        {
            if (args.Length < 2 || args.Length > 3 || (args.Length == 3 && args[2] != "--check"))
            {
                Console.Error.WriteLine("Usage: Vortex.CardImporter <workbook.xlsx> <output-dir> [--check]");
                return 2;
            }

            string workbookPath = args[0];
            string outputPath = Path.Combine(args[1], OutputFileName);
            bool check = args.Length == 3;

            string json;
            GameData data;
            try
            {
                (data, json) = Import(workbookPath);
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is GameDataException || ex is UnauthorizedAccessException || ex is System.Xml.XmlException)
            {
                Console.Error.WriteLine("Import failed: " + ex.Message);
                return 2;
            }

            PrintSummary(data);

            if (check)
            {
                string existing = File.Exists(outputPath) ? File.ReadAllText(outputPath) : string.Empty;
                if (!string.Equals(existing, json, StringComparison.Ordinal))
                {
                    Console.Error.WriteLine(outputPath + " is out of date. Run the importer and commit the result.");
                    return 1;
                }

                Console.WriteLine(outputPath + " is up to date.");
                return 0;
            }

            File.WriteAllText(outputPath, json);
            Console.WriteLine("Wrote " + outputPath);
            return 0;
        }

        /// <summary>Reads, converts and validates the workbook; returns the content and its serialized form.</summary>
        internal static (GameData Data, string Json) Import(string workbookPath)
        {
            var info = new FileInfo(workbookPath);
            if (!info.Exists)
            {
                throw new IOException("Workbook not found: " + workbookPath);
            }

            if (info.Length > MaxWorkbookBytes)
            {
                throw new InvalidDataException("Workbook is larger than " + MaxWorkbookBytes + " bytes.");
            }

            byte[] bytes = File.ReadAllBytes(workbookPath);
            string sha = Convert.ToHexStringLower(SHA256.HashData(bytes));

            XlsxReader workbook;
            using (var stream = new MemoryStream(bytes, writable: false))
            {
                workbook = XlsxReader.Load(stream);
            }

            GameData data = WorkbookParser.Parse(workbook, sha);
            IReadOnlyList<string> errors = GameDataValidator.Validate(data);
            if (errors.Count > 0)
            {
                throw new GameDataException("Workbook content is invalid:\n - " + string.Join("\n - ", errors));
            }

            string json = GameDataSerializer.Serialize(data);

            // Round-trip through the hardened loader: what we write is exactly what the game can read.
            GameDataSerializer.Deserialize(json);
            return (data, json);
        }

        private static void PrintSummary(GameData data)
        {
            int atk = data.Modifiers.Where(m => m.Slot == CardSlot.Attack).Sum(m => m.Copies);
            int def = data.Modifiers.Where(m => m.Slot == CardSlot.Defense).Sum(m => m.Copies);
            int evt = data.Events.Sum(e => e.Copies);
            Console.WriteLine($"Modifiers: {data.Modifiers.Count} distinct (ATK {atk} copies, DEF {def} copies)");
            Console.WriteLine($"Events: {data.Events.Count} distinct ({evt} copies), Technologies: {data.Technologies.Count}");
            string[] review = data.Modifiers.Where(m => m.NeedsReview).Select(m => m.Id).ToArray();
            if (review.Length > 0)
            {
                Console.WriteLine("Flagged for review: " + string.Join(", ", review));
            }
        }
    }
}
