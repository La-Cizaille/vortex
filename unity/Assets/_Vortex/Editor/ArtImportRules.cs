using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Vortex.Editor
{
    /// <summary>
    /// Import conventions of the art folders (docs/ASSETS.md). An image dropped in <c>Art/Cards/</c>, named after a
    /// content id (<c>A_005.png</c>), gets mobile import settings and joins the card art catalog; deleting it brings the
    /// placeholder back. An image in <c>Art/Icons/</c> becomes a small sprite. A model anywhere under <c>Art/</c> gets
    /// lean import settings, and any other image there is a model texture; the card model in <c>Art/Cards3D/</c> becomes
    /// the body of the card prefab. Settings are applied on the first import only, so the designer can fine-tune them
    /// afterwards.
    /// </summary>
    public sealed class ArtImportRules : AssetPostprocessor
    {
        /// <summary>Folder of the card, event and technology illustrations.</summary>
        public const string CardsFolder = "Assets/_Vortex/Art/Cards";

        /// <summary>Folder of the card body model (ADR-0017).</summary>
        public const string CardModelsFolder = "Assets/_Vortex/Art/Cards3D";

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

        /// <summary>Largest side of a model texture, in pixels: 1024 for a ship, 2048 for a planet (docs/ASSETS.md §2).</summary>
        public const int ModelTextureMaxSize = 2048;

        /// <summary>End of a normal map's file name (<c>Ship_Faucon_Normal.png</c>), docs/ASSETS.md section 2.</summary>
        public const string NormalMapSuffix = "_Normal";

        /// <summary>
        /// Suffix of a metal map (metalness in red, smoothness in alpha, as URP Lit reads it): data, not a colour, so it is
        /// read without the sRGB conversion (docs/ASSETS.md section 2).
        /// </summary>
        public const string MetallicSmoothnessSuffix = "_MetallicSmoothness";

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

        /// <summary>
        /// Settings of a new model texture (any other image under <c>Art/</c>, such as the textures exported next to a
        /// model): mipmapped for 3D, compressed (ASTC 6x6 on Android). A file whose name ends with
        /// <see cref="NormalMapSuffix"/> is a normal map: Unity does not detect it from the model, and read as a colour
        /// image it would light the model wrongly.
        /// </summary>
        public static void ConfigureModelTexture(TextureImporter importer, string fileName)
        {
            if (importer is null)
            {
                throw new ArgumentNullException(nameof(importer));
            }

            string name = Path.GetFileNameWithoutExtension(fileName);
            bool normalMap = name.EndsWith(NormalMapSuffix, StringComparison.Ordinal);
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap && !name.EndsWith(MetallicSmoothnessSuffix, StringComparison.Ordinal);
            importer.mipmapEnabled = true;
            importer.maxTextureSize = ModelTextureMaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = ModelTextureMaxSize,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = 50,
            });
        }

        /// <summary>
        /// Settings of a new model: no cameras or lights from the file, compressed meshes kept off the CPU, and static
        /// (models have no animation yet, docs/ASSETS.md section 2), so no Animator is added to every ship.
        /// </summary>
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
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
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

            if (changes.Any(paths => paths.Any(p => IsIn(p, CardModelsFolder))))
            {
                CardPrefabSync.Sync();
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
            else if (IsIn(assetPath, ArtFolder))
            {
                ConfigureModelTexture((TextureImporter)assetImporter, assetPath);
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
