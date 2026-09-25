using System;
using System.Linq;
using UnityEditor;

namespace Vortex.Editor
{
    /// <summary>
    /// Import conventions of the art folders (docs/ASSETS.md). An image dropped in <c>Art/Cards/</c>, named after a
    /// content id (<c>A_005.png</c>), gets mobile import settings and joins the card art catalog; deleting it brings the
    /// placeholder back. An image in <c>Art/Icons/</c> becomes a small sprite. A model anywhere under <c>Art/</c> gets
    /// lean import settings. Settings are applied on the first import only, so the designer can fine-tune them
    /// afterwards.
    /// </summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        /// <summary>Folder of the card, event and technology illustrations.</summary>
        public const string CardsFolder = "Assets/_Vortex/Art/Cards";

        /// <summary>Folder of the ship models.</summary>
        public const string ShipsFolder = "Assets/_Vortex/Art/Ships";

        /// <summary>Folder of the icons (actions, card text icons, technologies, tokens).</summary>
        public const string IconsFolder = "Assets/_Vortex/Art/Icons";

        /// <summary>Root of every art folder: models anywhere below get the model settings.</summary>
        public const string ArtFolder = "Assets/_Vortex/Art";

        /// <summary>Largest side of an icon, in pixels.</summary>
        public const int IconMaxSize = 256;

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

        /// <summary>Settings of a new icon: a sprite without mipmaps, 256 pixels at most (ASTC 4x4 on Android, sharp edges).</summary>
        public static void ConfigureIconTexture(TextureImporter importer)
        {
            if (importer is null)
            {
                throw new ArgumentNullException(nameof(importer));
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = IconMaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = IconMaxSize,
                format = TextureImporterFormat.ASTC_4x4,
                compressionQuality = 50,
            });
        }

        /// <summary>Settings of a new model: no cameras or lights from the file, compressed meshes kept off the CPU.</summary>
        public static void ConfigureModel(ModelImporter importer)
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
            string[][] changes = { importedAssets, deletedAssets, movedAssets, movedFromAssetPaths };
            if (changes.Any(paths => paths.Any(p => IsIn(p, CardsFolder))))
            {
                ThemeAssets.SyncCardArt();
            }

            if (changes.Any(paths => paths.Any(p => IsIn(p, IconsFolder))))
            {
                ThemeAssets.SyncIcons();
            }
        }

        private void OnPreprocessTexture()
        {
            if (!assetImporter.importSettingsMissing)
            {
                return;
            }

            if (IsIn(assetPath, CardsFolder))
            {
                ConfigureCardTexture((TextureImporter)assetImporter);
            }
            else if (IsIn(assetPath, IconsFolder))
            {
                ConfigureIconTexture((TextureImporter)assetImporter);
            }
        }

        private void OnPreprocessModel()
        {
            if (IsIn(assetPath, ArtFolder) && assetImporter.importSettingsMissing)
            {
                ConfigureModel((ModelImporter)assetImporter);
            }
        }
    }
}
