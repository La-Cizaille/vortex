using System.IO;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Core.Config;
using Vortex.Core.Content;

namespace Vortex.Editor
{
    /// <summary>
    /// Creates the project's base assets when they are missing (menu, or batch mode with
    /// -executeMethod Vortex.Editor.ProjectAssets.EnsureAll). Existing assets are never overwritten: once created,
    /// they belong to the designer.
    /// </summary>
    public static class ProjectAssets
    {
        /// <summary>The game content asset (references the engine's content files).</summary>
        public const string ContentPath = "Assets/_Vortex/Content/GameContent.asset";

        /// <summary>The feedback profile used by the game scene.</summary>
        public const string ProfilePath = "Assets/_Vortex/Presentation/Feedback/DefaultFeedbackProfile.asset";

        private const string PausePath = "Assets/_Vortex/Presentation/Feedback/Pause.asset";
        private const string DataFolder = "Packages/com.vortex.core/Runtime/Data/";

        /// <summary>Creates every missing base asset.</summary>
        [MenuItem("Vortex/Développement/Créer les assets de base manquants")]
        public static void EnsureAll()
        {
            EnsureContent();
            EnsureFeedbackProfile();
            ThemeAssets.EnsureAll();
            GalleryScene.Ensure();
            AssetDatabase.SaveAssets();
        }

        private static void EnsureContent()
        {
            if (AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath) != null)
            {
                return;
            }

            var content = ScriptableObject.CreateInstance<GameContent>();
            content.Assign(Data(CardsFile.FileName), Data(EventsFile.FileName), Data(TechnologiesFile.FileName), Data(GameConfig.FileName));
            Create(content, ContentPath);
        }

        private static void EnsureFeedbackProfile()
        {
            if (AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProfilePath) != null)
            {
                return;
            }

            PauseFeedback pause = AssetDatabase.LoadAssetAtPath<PauseFeedback>(PausePath);
            if (pause == null)
            {
                pause = ScriptableObject.CreateInstance<PauseFeedback>();
                pause.Configure(0.15f);
                Create(pause, PausePath);
            }

            var profile = ScriptableObject.CreateInstance<FeedbackProfile>();
            profile.Configure(pause);
            Create(profile, ProfilePath);
        }

        private static TextAsset Data(string file)
        {
            TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder + file);
            if (asset == null)
            {
                throw new FileNotFoundException("Engine content file not found: " + DataFolder + file);
            }

            return asset;
        }

        /// <summary>Saves a new asset, creating its folder if needed.</summary>
        internal static void Create(Object asset, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log("Created " + path);
        }
    }
}
