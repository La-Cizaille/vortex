using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vortex.Client.Gallery;
using Vortex.Client.Presentation;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Cards are 3D objects that follow their place in the interface (ADR-0017).</summary>
    public class CardsTests
    {
        [Test]
        public void Table_cards_are_3D_objects_in_front_of_the_interface_facing_the_camera()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
                director.HumanFirstSeat = false;
                director.Begin();
                director.Advance(0f);
                Camera camera = Object.FindAnyObjectByType<Camera>();
                Canvas canvas = Object.FindObjectsByType<Canvas>().Single(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceCamera);

                CardAnchor[] cards = Object.FindObjectsByType<CardAnchor>();
                Assert.That(cards.Length, Is.GreaterThanOrEqualTo(10), "At least the ten market cards.");
                foreach (CardAnchor card in cards)
                {
                    card.Place();
                    Assert.That(card.GetComponent<RectTransform>(), Is.Null, card.name + " is a scene object, not an interface element.");
                    Assert.That(Quaternion.Angle(card.transform.rotation, camera.transform.rotation), Is.LessThan(0.01f), "It faces the camera.");
                    float depth = Vector3.Dot(card.transform.position - camera.transform.position, camera.transform.forward);
                    Assert.That(depth, Is.LessThan(canvas.planeDistance), "Nearer than the interface, so drawn in front of its panels.");
                    Assert.That(card.GetComponent<Collider>().enabled, Is.EqualTo(card.GetComponent<CardDisplay>().Visible), "It answers the pointer when shown.");
                }
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void A_gallery_card_scrolled_out_of_view_is_hidden()
        {
            Scene scene = EditorSceneManager.OpenScene(GalleryScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GalleryController gallery = scene.GetRootGameObjects().Select(o => o.GetComponent<GalleryController>()).Single(g => g != null);
                gallery.Build();
                Canvas.ForceUpdateCanvases();
                foreach (LayoutGroup group in Object.FindObjectsByType<LayoutGroup>())
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
                }

                foreach (CardAnchor card in Object.FindObjectsByType<CardAnchor>())
                {
                    card.Place();
                }

                Assert.That(gallery.Cards.Any(c => c.Visible), Is.True, "The first rows show.");
                Assert.That(gallery.Cards.Any(c => !c.Visible), Is.True, "Cards below the view are hidden, not drawn over the ships.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void The_enlarged_card_is_nearer_than_the_table_cards_and_ignores_the_pointer()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
                director.HumanFirstSeat = false;
                director.Begin();
                Camera camera = Object.FindAnyObjectByType<Camera>();
                CardHover pointed = Object.FindObjectsByType<CardHover>().First();
                pointed.OnPointerEnter(null!);

                CardDisplay zoomed = Object.FindAnyObjectByType<CardZoom>().Shown!;
                Assert.That(zoomed, Is.Not.Null);
                float Depth(Component c) => Vector3.Dot(c.transform.position - camera.transform.position, camera.transform.forward);
                Assert.That(Depth(zoomed), Is.LessThan(Depth(pointed)), "In front of the table cards.");
                Assert.That(zoomed.GetComponent<Collider>().enabled, Is.False, "It never answers the pointer.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
