using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Catalogs give each content id and seat its visual, or a generated placeholder.</summary>
    public class CatalogTests
    {
        [Test]
        public void Missing_art_is_a_generated_placeholder_reused_for_the_same_card()
        {
            var catalog = ScriptableObject.CreateInstance<CardArtCatalog>();
            CardArt first = catalog.ArtFor("A_001");
            Assert.That(first.IsPlaceholder, Is.True);
            Assert.That(first.Sprite, Is.Not.Null);
            Assert.That(catalog.ArtFor("A_001").Sprite, Is.SameAs(first.Sprite));
            Assert.That(catalog.ArtFor("A_002").Sprite, Is.Not.SameAs(first.Sprite));
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Placeholders_differ_from_one_card_to_another_and_never_change()
        {
            Assert.That(PlaceholderArt.Pattern("A_001"), Is.Not.EqualTo(PlaceholderArt.Pattern("D_014")));
            Assert.That(PlaceholderArt.Pattern("A_001"), Is.EqualTo(PlaceholderArt.Pattern("A_001")));
        }

        [Test]
        public void Art_in_the_catalog_replaces_the_placeholder()
        {
            var catalog = ScriptableObject.CreateInstance<CardArtCatalog>();
            var texture = new Texture2D(4, 4);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.zero);

            Assert.That(catalog.Replace(new[] { ("A_001", sprite) }), Is.True);
            Assert.That(catalog.Replace(new[] { ("A_001", sprite) }), Is.False, "Same art, nothing to save.");
            CardArt art = catalog.ArtFor("A_001");
            Assert.That(art.IsPlaceholder, Is.False);
            Assert.That(art.Sprite, Is.SameAs(sprite));

            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void The_project_catalog_only_names_game_content()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(ThemeAssets.CardArtPath);
            Assert.That(catalog, Is.Not.Null);
            HashSet<string> ids = ThemeAssets.ContentIds(AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath).LoadData());
            Assert.That(catalog.Entries.Select(e => e.Id).Where(id => !ids.Contains(id)), Is.Empty);
            List<string> order = catalog.Entries.Select(e => e.Id).ToList();
            Assert.That(order, Is.EqualTo(order.OrderBy(id => id, System.StringComparer.Ordinal)), "Sorted, so that the asset diffs cleanly.");
        }

        [Test]
        public void A_seat_without_a_ship_gets_a_placeholder_in_its_colour()
        {
            var catalog = ScriptableObject.CreateInstance<ShipCatalog>();
            var parent = new GameObject("Parent");
            GameObject ship = catalog.Spawn(2, parent.transform, Color.red);

            Assert.That(ship.name, Is.EqualTo(PlaceholderShip.Name));
            Assert.That(ship.transform.parent, Is.SameAs(parent.transform));
            Assert.That(ship.GetComponentsInChildren<Renderer>(), Is.Not.Empty);
            Assert.That(ship.GetComponentsInChildren<Collider>(), Is.Empty, "A placeholder is only something to look at.");

            Object.DestroyImmediate(parent);
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void A_seat_ship_overrides_the_default_ship()
        {
            var fallback = new GameObject("Défaut");
            var own = new GameObject("Siège 2");
            var catalog = ScriptableObject.CreateInstance<ShipCatalog>();
            catalog.Configure(fallback, null, own);

            Assert.That(catalog.PrefabFor(0), Is.SameAs(fallback));
            Assert.That(catalog.PrefabFor(1), Is.SameAs(own));
            Assert.That(catalog.PrefabFor(4), Is.SameAs(fallback));

            Object.DestroyImmediate(fallback);
            Object.DestroyImmediate(own);
            Object.DestroyImmediate(catalog);
        }
    }
}
