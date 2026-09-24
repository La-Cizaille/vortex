using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Vortex.Editor
{
    /// <summary>
    /// Packages allowed for development only (docs/SECURITY.md section 0): they may be installed for comfort,
    /// but no release build may contain them.
    /// </summary>
    public static class DevOnlyPackages
    {
        /// <summary>Unity MCP: its runtime helpers would be compiled into player builds.</summary>
        public static readonly IReadOnlyList<string> Names = new[] { "com.coplaydev.unity-mcp" };

        /// <summary>Dev-only packages listed in a Packages/manifest.json content.</summary>
        public static IReadOnlyList<string> FindIn(string manifestJson)
        {
            using var reader = new JsonTextReader(new StringReader(manifestJson)) { MaxDepth = 16 };
            JObject manifest = JObject.Load(reader);
            if (!(manifest["dependencies"] is JObject dependencies))
            {
                return Array.Empty<string>();
            }

            return Names.Where(name => dependencies[name] != null).ToList();
        }
    }

    /// <summary>
    /// Refuses a release (non-development) player build while a dev-only package is installed, so that the shipped
    /// product never embeds development tooling. Development builds stay possible for testing on devices.
    /// </summary>
    public sealed class ReleaseBuildGuard : IPreprocessBuildWithReport
    {
        /// <inheritdoc/>
        public int callbackOrder => 0;

        /// <inheritdoc/>
        public void OnPreprocessBuild(BuildReport report)
        {
            if ((report.summary.options & UnityEditor.BuildOptions.Development) != 0)
            {
                return;
            }

            IReadOnlyList<string> found = DevOnlyPackages.FindIn(File.ReadAllText(Path.Combine("Packages", "manifest.json")));
            if (found.Count > 0)
            {
                throw new BuildFailedException(
                    "Release build refused: development-only packages are installed (" + string.Join(", ", found) + "). " +
                    "Remove them from Packages/manifest.json, or make a Development build (docs/SECURITY.md section 0).");
            }
        }
    }
}
