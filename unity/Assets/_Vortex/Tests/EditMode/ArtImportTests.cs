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

        [TestCase("__test_BaseColor", TextureImporterType.Default)]
        [TestCase("__test_Normal", TextureImporterType.NormalMap)]
        public void A_model_texture_is_mipmapped_and_a_normal_map_is_known_by_its_name(string name, TextureImporterType type)
        {
            string path = ArtImportRules.ShipsFolder + "/" + name + ".png";
            Assume.That(File.Exists(path), Is.False);
            try
            {
                var image = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(type));
                Assert.That(importer.mipmapEnabled, Is.True);
                Assert.That(importer.maxTextureSize, Is.EqualTo(ArtImportRules.ModelTextureMaxSize));
                Assert.That(importer.GetPlatformTextureSettings("Android").format, Is.EqualTo(TextureImporterFormat.ASTC_6x6));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void A_dropped_model_is_imported_static_without_cameras_or_lights()
        {
            string path = ArtImportRules.ShipsFolder + "/__test_modele.fbx";
            Assume.That(File.Exists(path), Is.False);
            try
            {
                File.Copy(ThemeAssets.CardModelPath, path);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.importCameras, Is.False);
                Assert.That(importer.importLights, Is.False);
                Assert.That(importer.isReadable, Is.False);
                Assert.That(importer.importAnimation, Is.False);
                Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.None));
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Animator>(), Is.Empty, "No Animator on a static ship.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        [Test]
        public void The_card_model_keeps_the_card_size_and_axes_and_becomes_the_card_body()
        {
            // The model comes from Blender through tools/blender/export_unity.py: this checks the whole chain (scale,
            // axes, budget) on the one model the repository ships today.
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardModelPath);
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(ThemeAssets.CardModelPath);
            Assert.That(model, Is.Not.Null);
            Assert.That(Vector3.Distance(mesh.bounds.size, ThemeAssets.CardSize), Is.LessThan(0.001f), "1 x 1.4 x 0.02, width along X and height along Y.");
            Assert.That(mesh.bounds.center.magnitude, Is.LessThan(0.001f), "Origin at the centre of the card.");
            Assert.That(mesh.GetIndexCount(0) / 3, Is.LessThanOrEqualTo(500), "Triangle budget (docs/ASSETS.md §2).");
            foreach (Transform part in model.GetComponentsInChildren<Transform>())
            {
                Assert.That(Quaternion.Angle(part.localRotation, Quaternion.identity), Is.LessThan(0.01f), part.name + " arrives without rotation.");
                Assert.That(part.localScale, Is.EqualTo(Vector3.one), part.name + " arrives at scale 1.");
            }

            Transform body = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath).transform.Find(ThemeAssets.CardBodyPath);
            Assert.That(body.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            Assert.That(body.localScale, Is.EqualTo(Vector3.one));
        }

        private static string PathOf(string id) => ArtImportRules.CardsFolder + "/" + id + ".png";
    }
}
