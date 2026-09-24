using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Dropping an image in the art folder is enough to change a card's look (CONTRIBUTING.md).</summary>
    public class ArtImportTests
    {
        [Test]
        public void A_dropped_image_gets_mobile_settings_and_replaces_the_placeholder()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(ThemeAssets.CardArtPath);
            string? id = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath).LoadData().Modifiers
                .Select(c => c.Id)
                .FirstOrDefault(candidate => catalog.Find(candidate) == null && !File.Exists(PathOf(candidate)));
            if (id is null)
            {
                Assert.Ignore("Every modifier already has its art: no free id to test with.");
                return;
            }

            string path = PathOf(id);
            try
            {
                var image = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.mipmapEnabled, Is.False);
                Assert.That(importer.maxTextureSize, Is.EqualTo(ArtImportRules.CardMaxSize));
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Assert.That(android.overridden, Is.True);
                Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_6x6));

                Assert.That(catalog.ArtFor(id).IsPlaceholder, Is.False, "The image joined the catalog.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }

            Assert.That(catalog.ArtFor(id).IsPlaceholder, Is.True, "Deleting the image brings the placeholder back.");
        }

        [Test]
        public void A_file_named_after_no_content_is_left_out()
        {
            const string path = ArtImportRules.CardsFolder + "/PAS_UNE_CARTE.png";
            var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(ThemeAssets.CardArtPath);
            int before = catalog.Entries.Count;
            try
            {
                var image = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("PAS_UNE_CARTE"));
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                Assert.That(catalog.Entries.Count, Is.EqualTo(before));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void A_dropped_icon_becomes_a_small_sprite()
        {
            string path = ArtImportRules.IconsFolder + "/__test_icone.png";
            Assume.That(File.Exists(path), Is.False);
            try
            {
                var image = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
                Assert.That(importer.maxTextureSize, Is.EqualTo(ArtImportRules.IconMaxSize));
                Assert.That(importer.GetPlatformTextureSettings("Android").format, Is.EqualTo(TextureImporterFormat.ASTC_4x4));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static string PathOf(string id) => ArtImportRules.CardsFolder + "/" + id + ".png";
    }
}
