using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vortex.Core.Content;

namespace Vortex.ContentTool
{
    /// <summary>
    /// Usage:
    ///   Vortex.ContentTool validate &lt;data-dir&gt;
    ///   Vortex.ContentTool format   &lt;data-dir&gt; [--check]
    ///   Vortex.ContentTool docs     &lt;data-dir&gt; &lt;output.md&gt; [--check]
    /// Exit codes: 0 ok, 1 check failed (file not canonical / doc stale), 2 invalid content or usage.
    /// </summary>
    internal static class Program
    {
        private const string Usage =
            "Usage:\n" +
            "  Vortex.ContentTool validate <data-dir>\n" +
            "  Vortex.ContentTool format   <data-dir> [--check]\n" +
            "  Vortex.ContentTool docs     <data-dir> <output.md> [--check]";

        private static int Main(string[] args)
        {
            try
            {
                return Run(args, Console.Out, Console.Error);
            }
            catch (Exception ex) when (ex is GameDataException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 2;
            }
        }

        internal static int Run(string[] args, TextWriter output, TextWriter error)
        {
            if (args.Length < 2)
            {
                error.WriteLine(Usage);
                return 2;
            }

            bool check = args.Contains("--check");
            string[] positional = args.Where(a => a != "--check").ToArray();

            switch (positional[0])
            {
                case "validate" when positional.Length == 2:
                    return Validate(new ContentFolder(positional[1]), output);
                case "format" when positional.Length == 2:
                    return Format(new ContentFolder(positional[1]), check, output, error);
                case "docs" when positional.Length == 3:
                    return Docs(new ContentFolder(positional[1]), positional[2], check, output, error);
                default:
                    error.WriteLine(Usage);
                    return 2;
            }
        }

        private static int Validate(ContentFolder folder, TextWriter output)
        {
            GameData data = folder.Load();
            int atk = data.Modifiers.Where(m => m.Slot == CardSlot.Attack).Sum(m => m.Copies);
            int def = data.Modifiers.Where(m => m.Slot == CardSlot.Defense).Sum(m => m.Copies);
            output.WriteLine($"OK: {data.Modifiers.Count} modifiers (ATK {atk} copies, DEF {def} copies), {data.Events.Count} events ({data.Events.Sum(e => e.Copies)} copies), {data.Technologies.Count} technologies.");
            string[] review = data.Modifiers.Where(m => m.NeedsReview).Select(m => m.Id).ToArray();
            if (review.Length > 0)
            {
                output.WriteLine("Flagged for review: " + string.Join(", ", review));
            }

            return 0;
        }

        private static int Format(ContentFolder folder, bool check, TextWriter output, TextWriter error)
        {
            // Validate the whole set first: never rewrite files into an invalid state.
            folder.Load();

            var files = new List<(string Name, string Current, string Canonical)>
            {
                (CardsFile.FileName, folder.CardsJson, ContentJson.Serialize(ContentJson.Parse<CardsFile>(folder.CardsJson, CardsFile.FileName))),
                (EventsFile.FileName, folder.EventsJson, ContentJson.Serialize(ContentJson.Parse<EventsFile>(folder.EventsJson, EventsFile.FileName))),
                (TechnologiesFile.FileName, folder.TechnologiesJson, ContentJson.Serialize(ContentJson.Parse<TechnologiesFile>(folder.TechnologiesJson, TechnologiesFile.FileName))),
                (Vortex.Core.Config.GameConfig.FileName, folder.ConfigJson, ContentJson.Serialize(ContentJson.Parse<Vortex.Core.Config.GameConfig>(folder.ConfigJson, Vortex.Core.Config.GameConfig.FileName))),
            };

            int stale = 0;
            foreach ((string name, string current, string canonical) in files)
            {
                if (string.Equals(current, canonical, StringComparison.Ordinal))
                {
                    continue;
                }

                stale++;
                if (check)
                {
                    error.WriteLine(name + " is not in canonical form. Run: dotnet run --project dotnet/Vortex.ContentTool -- format core/Runtime/Data");
                }
                else
                {
                    File.WriteAllText(folder.PathOf(name), canonical);
                    output.WriteLine("Formatted " + name);
                }
            }

            if (stale == 0)
            {
                output.WriteLine("All content files are canonical.");
            }

            return check && stale > 0 ? 1 : 0;
        }

        private static int Docs(ContentFolder folder, string outputPath, bool check, TextWriter output, TextWriter error)
        {
            string markdown = CardsDocGenerator.Generate(folder.Load());
            if (check)
            {
                string existing = File.Exists(outputPath) ? File.ReadAllText(outputPath) : string.Empty;
                if (!string.Equals(existing, markdown, StringComparison.Ordinal))
                {
                    error.WriteLine(outputPath + " is out of date. Run: dotnet run --project dotnet/Vortex.ContentTool -- docs core/Runtime/Data docs/CARDS.md");
                    return 1;
                }

                output.WriteLine(outputPath + " is up to date.");
                return 0;
            }

            File.WriteAllText(outputPath, markdown);
            output.WriteLine("Wrote " + outputPath);
            return 0;
        }
    }
}
