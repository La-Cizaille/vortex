using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vortex.Editor
{
    /// <summary>Creates a scene file from code, without disturbing the scene open in the editor.</summary>
    internal static class SceneFiles
    {
        /// <summary>
        /// Builds a new scene with <paramref name="build"/> and saves it at <paramref name="path"/>. The scene is built
        /// next to the open one, which stays untouched. An untitled scene (batch mode, or a new editor session) cannot
        /// have a scene added next to it: the new scene replaces it, once saved if the user wants. Returns false when
        /// the user kept an unsaved scene open.
        /// </summary>
        public static bool Create(string path, Action build)
        {
            Scene previous = SceneManager.GetActiveScene();
            bool additive = !string.IsNullOrEmpty(previous.path);
            if (!additive && previous.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.Log(path + " not created: the current scene was kept open.");
                return false;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, additive ? NewSceneMode.Additive : NewSceneMode.Single);
            if (additive)
            {
                SceneManager.SetActiveScene(scene);
            }

            try
            {
                build();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log("Created " + path);
                return true;
            }
            finally
            {
                if (additive)
                {
                    SceneManager.SetActiveScene(previous);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
