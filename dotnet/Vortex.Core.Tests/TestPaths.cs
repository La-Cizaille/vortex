using System.IO;
using NUnit.Framework;
using Vortex.Core.Content;

namespace Vortex.Core.Tests
{
    /// <summary>Locations of repository files used by tests.</summary>
    internal static class TestPaths
    {
        /// <summary>Copy of core/Runtime/Data next to the test assembly.</summary>
        public static string DataDir => Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData");

        public static string CardsJson => File.ReadAllText(Path.Combine(DataDir, CardsFile.FileName));

        public static string EventsJson => File.ReadAllText(Path.Combine(DataDir, EventsFile.FileName));

        public static string TechnologiesJson => File.ReadAllText(Path.Combine(DataDir, TechnologiesFile.FileName));

        public static string ConfigJson => File.ReadAllText(Path.Combine(DataDir, Vortex.Core.Config.GameConfig.FileName));

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

        public static GameData LoadRealContent()
        {
            return GameDataLoader.Load(CardsJson, EventsJson, TechnologiesJson);
        }

        public static Vortex.Core.Config.GameConfig LoadRealConfig()
        {
            return GameDataLoader.LoadConfig(ConfigJson, LoadRealContent());
        }
    }
}
