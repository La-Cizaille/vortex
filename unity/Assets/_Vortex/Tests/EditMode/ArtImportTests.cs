using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;
using Vortex.Core.Content;
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

        [TestCase("__test_BaseColor", TextureImporterType.Default, true)]
        [TestCase("__test_Normal", TextureImporterType.NormalMap, false)]
        [TestCase("__test_MetallicSmoothness", TextureImporterType.Default, false)]
        public void A_model_texture_is_mipmapped_and_a_normal_or_metal_map_is_known_by_its_name(string name, TextureImporterType type, bool colour)
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
                Assert.That(importer.sRGBTexture, Is.EqualTo(colour), "Only a colour is read with the sRGB conversion.");
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
            Assert.That(Vector3.Distance(mesh.bounds.size, ThemeAssets.CardSize), Is.LessThan(0.001f), "1 x 1.4 x 0.04, width along X and height along Y.");
            Assert.That(mesh.bounds.center.magnitude, Is.LessThan(0.001f), "Origin at the centre of the card.");
            Assert.That(Triangles(mesh), Is.LessThanOrEqualTo(1000), "Triangle budget (docs/ASSETS.md §2).");
            foreach (Transform part in model.GetComponentsInChildren<Transform>())
            {
                // A zone's scale is its size (ARB-85); every other part arrives at scale 1.
                Assert.That(Quaternion.Angle(part.localRotation, Quaternion.identity), Is.LessThan(0.01f), part.name + " arrives without rotation.");
                if (!part.name.StartsWith(CardPrefabSync.ZonePrefix, System.StringComparison.Ordinal))
                {
                    Assert.That(part.localScale, Is.EqualTo(Vector3.one), part.name + " arrives at scale 1.");
                }
            }

            Transform body = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath).transform.Find(ThemeAssets.CardBodyPath);
            Assert.That(body.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
            Assert.That(body.localScale, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void The_card_model_places_the_illustration_and_the_texts_on_the_card()
        {
            // The model's Zone_* empties say where each part goes (ARB-85); the prefab follows them.
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardModelPath);
            Transform visual = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath).transform.Find("Visuel");
            Transform[] zones = model.GetComponentsInChildren<Transform>(true);
            foreach (string part in CardPrefabSync.Zoned)
            {
                Transform zone = zones.Single(candidate => candidate.name == CardPrefabSync.ZonePrefix + part);
                Transform placed = visual.Find(part);
                Assert.That(Vector2.Distance(placed.localPosition, zone.position), Is.LessThan(0.001f), part + " sits on its zone.");
                Assert.That(placed.localPosition.z, Is.LessThan(zone.position.z), part + " lies in front of the surface.");
                if (part != "Tourments")
                {
                    // The Torment badge overhangs the corner on purpose, to be read on a small card.
                    Assert.That(Mathf.Abs(zone.position.x) + (zone.lossyScale.x / 2f), Is.LessThanOrEqualTo(0.5f), part + " stays on the card.");
                }
            }

            Assert.That(visual.Find("Fond").gameObject.activeSelf, Is.False, "The model has panels of its own.");
            var illustration = visual.Find("Illustration");
            Assert.That(illustration.localScale.x / illustration.localScale.y, Is.EqualTo(16f / 9f).Within(0.01f), "Illustrations stay 16:9 (docs/ASSETS.md §3).");
        }

        [TestCase(TechColor.Blue)]
        [TestCase(TechColor.Green)]
        [TestCase(TechColor.Yellow)]
        public void A_technology_card_gem_glows_above_the_bloom_threshold_and_a_mark_colours_the_trims(TechColor color)
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(ThemeAssets.CardArtPath);
            var card = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath)).GetComponent<CardDisplay>();
            try
            {
                Renderer body = card.transform.Find(ThemeAssets.CardBodyPath).GetComponent<Renderer>();
                int gem = System.Array.FindIndex(body.sharedMaterials, material => material.name == "CardGem");
                Assume.That(gem, Is.GreaterThanOrEqualTo(0), "The card model is in the project.");
                var block = new MaterialPropertyBlock();

                card.Show(new CardFace("X_001", "Nom", "ATK", "Durable", "Texte", color), theme, catalog);
                body.GetPropertyBlock(block, gem);
                Vector4 glow = block.GetVector("_EmissionColor");
                Assert.That(Mathf.Max(glow.x, glow.y, glow.z), Is.GreaterThan(1.5f), "Every technology's gem blooms alike.");
                Assert.That(card.TrimColor, Is.EqualTo(theme.Technology(color)));

                card.Mark(theme.Loss, theme);
                Assert.That(card.TrimColor, Is.EqualTo(theme.Loss), "A marked card shows it on its trims.");

                card.Show(new CardFace("X_002", "Nom", "ÉVT", "Événement", "Texte", TechColor.Neutral), theme, catalog);
                body.GetPropertyBlock(block, gem);
                Assert.That((Vector3)block.GetVector("_EmissionColor"), Is.EqualTo(Vector3.zero), "A neutral card's gem is off.");
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void A_card_shows_its_slot_icon_or_else_the_short_text()
        {
            // The icons of tools/blender/build_icons.py are in the icons folder (ARB-86); a name without an image
            // falls back to the short text, so a missing icon never blocks the game.
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(ThemeAssets.CardArtPath);
            var icons = AssetDatabase.LoadAssetAtPath<IconCatalog>(ThemeAssets.IconsPath);
            foreach (string icon in new[] { CardFace.AttackIcon, CardFace.DefenseIcon, CardFace.EventIcon, CardFace.TechnologyIcon })
            {
                Assert.That(icons.Find(icon), Is.Not.Null, icon + " is in the icon catalog.");
            }

            var card = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath)).GetComponent<CardDisplay>();
            try
            {
                card.Show(new CardFace("X_001", "Nom", "ATK", "Durable", "Texte", TechColor.Red, CardFace.AttackIcon), theme, catalog);
                Assert.That(card.ShowsBadgeIcon, Is.True);

                card.Show(new CardFace("X_002", "Nom", "ATK", "Durable", "Texte", TechColor.Red, "Slot_Missing"), theme, catalog);
                Assert.That(card.ShowsBadgeIcon, Is.False, "Without its image, the disc shows the short text.");
            }
            finally
            {
                Object.DestroyImmediate(card.gameObject);
            }
        }

        [Test]
        public void Every_ship_model_follows_the_art_conventions()
        {
            // docs/ASSETS.md §2: about 2 m long and 2.4 m wide at most, 5,000 triangles at most, three materials at
            // most including the seat paint, and no rotation or scale on arrival.
            string[] paths = AssetDatabase.FindAssets("t:Model", new[] { ArtImportRules.ShipsFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            if (paths.Length == 0)
            {
                Assert.Ignore("No ship model yet.");
                return;
            }

            foreach (string path in paths)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Transform part in model.GetComponentsInChildren<Transform>())
                {
                    Assert.That(Quaternion.Angle(part.localRotation, Quaternion.identity), Is.LessThan(0.01f), path + ": " + part.name + " arrives without rotation.");
                    Assert.That(part.localScale, Is.EqualTo(Vector3.one), path + ": " + part.name + " arrives at scale 1.");
                }

                MeshFilter[] meshes = model.GetComponentsInChildren<MeshFilter>();
                Bounds bounds = meshes[0].sharedMesh.bounds;
                foreach (MeshFilter mesh in meshes)
                {
                    bounds.Encapsulate(mesh.sharedMesh.bounds);
                }

                Assert.That(bounds.size.x, Is.LessThanOrEqualTo(2.4f), path + ": width.");
                Assert.That(bounds.size.z, Is.InRange(1.5f, 2.5f), path + ": length, nose along +Z.");
                Assert.That(bounds.size.z, Is.GreaterThan(bounds.size.y), path + ": lies flat, top along +Y.");
                Assert.That(meshes.Sum(mesh => Triangles(mesh.sharedMesh)), Is.LessThanOrEqualTo(5000), path + ": triangle budget.");

                string[] materials = model.GetComponentsInChildren<Renderer>()
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Select(material => material.name)
                    .Distinct()
                    .ToArray();
                Assert.That(materials.Length, Is.LessThanOrEqualTo(3), path + ": " + string.Join(", ", materials));
                Assert.That(materials, Has.Some.StartsWith(ShipCatalog.SeatMaterialName), path + ": a part takes the seat colour.");

                // Markers (ANIMATIONS §5), when the model has them: on the ship, the muzzle ahead of its centre and the
                // engines behind it. A wrong axis at the export would throw them off.
                Bounds around = bounds;
                around.Expand(0.1f);
                foreach (Transform marker in model.GetComponentsInChildren<Transform>())
                {
                    bool muzzle = marker.name == ShipParts.Muzzle;
                    if (!muzzle && !marker.name.StartsWith(ShipParts.Engine, System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Assert.That(around.Contains(marker.position), Is.True, path + ": " + marker.name + " sits on the ship.");
                    Assert.That(marker.position.z, muzzle ? Is.GreaterThan(bounds.center.z) : Is.LessThan(bounds.center.z), path + ": " + marker.name + " is on its end of the ship.");
                }
            }
        }

        // Every material of a mesh is a sub-mesh of its own.
        private static int Triangles(Mesh mesh) =>
            Enumerable.Range(0, mesh.subMeshCount).Sum(subMesh => (int)mesh.GetIndexCount(subMesh) / 3);

        private static string PathOf(string id) => ArtImportRules.CardsFolder + "/" + id + ".png";
    }
}
