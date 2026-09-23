using System.IO;
using NUnit.Framework;

namespace Vortex.Core.Tests
{
    /// <summary>Locations of repository files used by tests.</summary>
    internal static class TestPaths
    {
        public static string GameDataJson => Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "gamedata.json");

        public static string DesignWorkbook => Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Vortex.xlsx");

        /// <summary>Repository root, found by walking up to the directory containing CLAUDE.md.</summary>
        public static string RepoRoot
        {
            get
            {
                DirectoryInfo? dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                while (dir != null && !File.Exists(Path.Combine(dir.FullName, "CLAUDE.md")))
                {
                    dir = dir.Parent;
                }

                Assert.That(dir, Is.Not.Null, "Repository root (CLAUDE.md) not found above the test directory.");
                return dir!.FullName;
            }
        }
    }
}
