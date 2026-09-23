using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Vortex.Core.Tests
{
    /// <summary>
    /// Repository-wide guard: no invisible or bidirectional control characters in source files
    /// (Trojan Source, CVE-2021-42574). Such characters must be written as numeric codes, e.g. <c>(char)0x202E</c>.
    /// </summary>
    [TestFixture]
    public class SourceHygieneTests
    {
        private static readonly string[] Extensions = { ".cs", ".json", ".asmdef", ".csproj", ".props", ".yml", ".md" };

        [Test]
        public void Source_files_contain_no_invisible_or_bidi_characters()
        {
            string root = TestPaths.RepoRoot;
            var offenders = new List<string>();
            foreach (string file in EnumerateSources(root))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    char bad = lines[i].FirstOrDefault(IsForbidden);
                    if (bad != default)
                    {
                        offenders.Add($"{Path.GetRelativePath(root, file)}:{i + 1} U+{(int)bad:X4}");
                    }
                }
            }

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void Engine_code_never_references_a_specific_card()
        {
            // RULES.md B1 / ADR-0007: base rules are card-agnostic. Only card behaviour classes
            // (core/Runtime/Cards/) may name a card id. Comments are ignored (they may cite examples).
            var cardId = new System.Text.RegularExpressions.Regex(@"\b(A_[0-9]{3}|D_[0-9]{3}|EVT_[A-Z_]+|TECH_(BLUE|RED|GREEN|YELLOW))\b");
            string runtime = Path.Combine(TestPaths.RepoRoot, "core", "Runtime");
            var offenders = new List<string>();
            foreach (string file in Directory.EnumerateFiles(runtime, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(runtime, file).Replace('\\', '/');
                if (relative.StartsWith("Cards/", System.StringComparison.Ordinal))
                {
                    continue;
                }

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    int comment = lines[i].IndexOf("//", System.StringComparison.Ordinal);
                    string code = comment >= 0 ? lines[i].Substring(0, comment) : lines[i];
                    if (cardId.IsMatch(code))
                    {
                        offenders.Add($"core/Runtime/{relative}:{i + 1}");
                    }
                }
            }

            Assert.That(offenders, Is.Empty);
        }

        private static IEnumerable<string> EnumerateSources(string root)
        {
            foreach (string dir in new[] { "core", "dotnet", "docs", ".github" })
            {
                string path = Path.Combine(root, dir);
                if (!Directory.Exists(path))
                {
                    continue;
                }

                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    string normalised = file.Replace('\\', '/');
                    if (normalised.Contains("/bin/", System.StringComparison.Ordinal) || normalised.Contains("/obj/", System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (Extensions.Contains(Path.GetExtension(file)))
                    {
                        yield return file;
                    }
                }
            }
        }

        private static bool IsForbidden(char c)
        {
            int u = c;
            return (u >= 0x200B && u <= 0x200F)   // zero-width chars, LRM/RLM
                || (u >= 0x202A && u <= 0x202E)   // bidi embeddings/overrides
                || (u >= 0x2066 && u <= 0x2069)   // bidi isolates
                || u == 0xFEFF                    // BOM inside text
                || u == 0xFE0E || u == 0xFE0F;    // variation selectors
        }
    }
}
