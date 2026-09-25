using System.IO;
using System.Linq;
using UnityEditor;

namespace Vortex.Editor
{
    /// <summary>
    /// Keeps the scenes of the build current: the menu first (the game opens on it), then the game scene, each under its
    /// current id (a regenerated scene gets a new one). Other listed scenes that still exist are kept after them; scenes
    /// that no longer exist are dropped.
    /// </summary>
    internal static class BuildSceneList
    {
        /// <summary>Updates the list when it differs from the wanted one.</summary>
        public static void Update()
        {
            string[] first = new[] { MenuScene.ScenePath, GameScene.ScenePath }.Where(File.Exists).ToArray();
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            EditorBuildSettingsScene[] wanted = first.Select(path => new EditorBuildSettingsScene(path, true))
                .Concat(current.Where(s => !first.Contains(s.path) && File.Exists(s.path)))
                .ToArray();
            bool same = wanted.Length == current.Length
                && wanted.Zip(current, (a, b) => a.path == b.path && a.guid == b.guid && a.enabled == b.enabled).All(equal => equal);
            if (!same)
            {
                EditorBuildSettings.scenes = wanted;
            }
        }
    }
}
