using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Content;
using Vortex.Client.Gallery;
using Vortex.Client.Presentation;
using Vortex.Core.Content;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The Gallery scene shows every card and every seat's ship, built from the real content and theme.</summary>
    public class GalleryTests
    {
        [Test]
        public void The_gallery_shows_every_card_event_technology_and_seat()
        {
            Scene scene = EditorSceneManager.OpenScene(GalleryScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GalleryController gallery = scene.GetRootGameObjects().Select(o => o.GetComponent<GalleryController>()).Single(g => g != null);
                gallery.Build();

                GameData data = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath).LoadData();
                int expected = data.Modifiers.Count + data.Events.Count + data.Technologies.Count;
                Assert.That(gallery.Cards, Has.Count.EqualTo(expected));
                Assert.That(gallery.Ships, Has.Count.EqualTo(5));
                foreach (CardDisplay card in gallery.Cards)
                {
                    Assert.That(card.Face.Title, Is.Not.Empty, card.Face.Id);
                    Assert.That(card.Face.Caption, Does.Not.StartWith("#"), "Every caption text is in the table.");
                    Assert.That(card.BodyText, Does.Not.Contain("**"), card.Face.Id);
                }

                gallery.Build();
                Assert.That(gallery.Cards, Has.Count.EqualTo(expected), "Building again replaces the cards.");
            }
            finally
            {
                // Leave the built gallery without saving it.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void The_card_prefab_is_wired()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var view = prefab.GetComponent<CardDisplay>();
            Assert.That(view, Is.Not.Null);
            var serialized = new SerializedObject(view);
            foreach (string field in new[] { "frame", "background", "art", "title", "caption", "body", "id", "tormentBadge", "tormentCount", "visual", "pointerArea" })
            {
                Assert.That(serialized.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            }
        }
    }
}
