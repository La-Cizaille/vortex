using System;
using System.Linq;
using UnityEditor;

namespace Vortex.Editor
{
    /// <summary>
    /// Import conventions of the art folders (CONTRIBUTING.md). An image dropped in <c>Art/Cards/</c>, named after a
    /// content id (<c>A_005.png</c>), gets mobile import settings and joins the card art catalog; deleting it brings the
    /// placeholder back. A model dropped in <c>Art/Ships/</c> gets lean import settings. Settings are applied on the
    /// first import only, so the designer can fine-tune them afterwards.
    /// </summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        /// <summary>Folder of the card, event and technology illustrations.</summary>
        public const string CardsFolder = "Assets/_Vortex/Art/Cards";

        /// <summary>Folder of the ship models.</summary>
        public const string ShipsFolder = "Assets/_Vortex/Art/Ships";

        /// <summary>Largest side of an illustration, in pixels.</summary>
        public const int CardMaxSize = 1024;

        /// <summary>Settings of a new illustration: a sprite without mipmaps, compressed (ASTC 6x6 on Android).</summary>
        public static void ConfigureCardTexture(TextureImporter importer)
        {
            if (importer is null)
            {
                throw new ArgumentNullException(nameof(importer));
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = CardMaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = CardMaxSize,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = 50,
            });
        }

        /// <summary>Settings of a new ship model: no cameras or lights from the file, compressed meshes kept off the CPU.</summary>
        public static void ConfigureShipModel(ModelImporter importer)
        {
            if (importer is null)
            {
                throw new ArgumentNullException(nameof(importer));
            }

            importer.importCameras = false;
            importer.importLights = false;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
        }

        private static bool IsIn(string path, string folder) => path.StartsWith(folder + "/", StringComparison.Ordinal);

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (new[] { importedAssets, deletedAssets, movedAssets, movedFromAssetPaths }.Any(paths => paths.Any(p => IsIn(p, CardsFolder))))
            {
                ThemeAssets.SyncCardArt();
            }
        }

        private void OnPreprocessTexture()
        {
            if (IsIn(assetPath, CardsFolder) && assetImporter.importSettingsMissing)
            {
                ConfigureCardTexture((TextureImporter)assetImporter);
            }
        }

        private void OnPreprocessModel()
        {
            if (IsIn(assetPath, ShipsFolder) && assetImporter.importSettingsMissing)
            {
                ConfigureShipModel((ModelImporter)assetImporter);
            }
        }
    }
}
