using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Content;
using Vortex.Client.Gallery;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Client.Theme;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>
    /// Acceptance of the playable client (M4.7, README): whole games at every table size without an error, and a new
    /// image changes the look without any code. An error or exception logged during a test fails it (Unity test runner).
    /// </summary>
    public class MilestoneTests
    {
        [TearDown]
        public void CloseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [TestCase(2, 101UL)]
        [TestCase(3, 102UL)]
        [TestCase(4, 103UL)]
        [TestCase(5, 104UL)]
        public void A_person_plays_a_whole_game_against_bots_at_every_table_size(int players, ulong seed)
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            director.ShowCommandPanel = false;
            director.Begin(new MatchSetup(
                Enumerable.Range(0, players).Select(i => new SeatSetup("Siège " + (i + 1), i == 0 ? SeatKind.Human : SeatKind.Bot, BotLevel.Normal)).ToList(),
                seed));

            PlayerControls controls = director.Controls;
            int gestures = 0;
            for (int frame = 0; frame < 200000 && !director.GameOver.Shown; frame++)
            {
                director.Advance(2f);
                if (!controls.Offered)
                {
                    continue;
                }

                gestures++;
                if (controls.Decision.gameObject.activeSelf && controls.Decision.Options.Count > 0)
                {
                    controls.Decision.Choose(0);
                }
                else if (controls.CanEndMarket)
                {
                    controls.EndMarket();
                }
                else if (controls.CanEndTurn)
                {
                    controls.EndTurn();
                }
                else
                {
                    // An action is imposed (for instance by a card): take it, on its first allowed target.
                    ActionButton action = controls.Actions.First(a => a.Available);
                    Command imposed = director.Session!.LegalCommands(0).First(c => c.Type == PlayerControls.TypeOf(action.Action));
                    Assert.That(action.NeedsTarget ? controls.UseActionOn(action.Action, imposed.Target) : controls.UseAction(action.Action), Is.True);
                }
            }

            Assert.That(director.Session!.IsOver, Is.True, "The game ends.");
            Assert.That(director.GameOver.Shown, Is.True, "The end of game panel shows the result.");
            Assert.That(gestures, Is.GreaterThan(0), "The person played.");
            Assert.That(director.Ships, Has.Count.EqualTo(players));
        }

        [Test]
        public void A_new_image_shows_on_its_card_without_any_code()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(ThemeAssets.CardArtPath);
            string? id = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath).LoadData().Modifiers
                .Select(c => c.Id)
                .FirstOrDefault(candidate => catalog.Find(candidate) == null && !File.Exists(ArtPath(candidate)));
            if (id is null)
            {
                Assert.Ignore("Every modifier already has its art: no free id to test with.");
                return;
            }

            Assert.That(CardInGallery(id).ShowsPlaceholder, Is.True, "Before: the generated placeholder.");
            string path = ArtPath(id);
            try
            {
                var image = new Texture2D(8, 8, TextureFormat.RGBA32, false);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                Assert.That(CardInGallery(id).ShowsPlaceholder, Is.False, "The dropped image is on the 3D card.");
            }
            finally
            {
                AssetDatabase.DeleteAsset(path);
            }
        }

        private static string ArtPath(string id) => ArtImportRules.CardsFolder + "/" + id + ".png";

        // The card of the gallery (every card of the content) showing a content id.
        private static CardDisplay CardInGallery(string id)
        {
            Scene scene = EditorSceneManager.OpenScene(GalleryScene.ScenePath, OpenSceneMode.Single);
            GalleryController gallery = scene.GetRootGameObjects().Select(o => o.GetComponentInChildren<GalleryController>()).First(g => g != null);
            gallery.Build();
            return gallery.Cards.First(card => card.Face.Id == id);
        }
    }
}
