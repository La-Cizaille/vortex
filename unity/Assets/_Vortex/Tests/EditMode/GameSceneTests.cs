using System.Globalization;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Core.Projection;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The game scene plays a whole game of bots and shows its final state (M4.4).</summary>
    public class GameSceneTests
    {
        [Test]
        public void Bots_play_a_whole_game_in_the_game_scene()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
                director.Begin();
                Assert.That(director.Ships, Has.Count.EqualTo(5));
                Assert.That(director.Seats, Has.Count.EqualTo(5));

                int frames = 0;
                while ((!director.Session!.IsOver || director.IsPlaying) && frames < 50000)
                {
                    director.Advance(2f);
                    frames++;
                }

                director.Advance(0f);
                Assert.That(director.Session.IsOver, Is.True, "The game ends.");
                var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
                foreach (PlayerView player in director.Session.View.Players)
                {
                    string expected = string.Format(CultureInfo.InvariantCulture, texts.Get(TextKeys.SeatHp), player.Hp);
                    Assert.That(director.Seats[player.Seat].HpText, Is.EqualTo(expected), "Seat " + player.Seat + " shows its final HP.");
                    Assert.That(director.Seats[player.Seat].AttackCard != null, Is.EqualTo(player.AttackSlot != null), "Seat " + player.Seat + " attack card.");
                }

                RoundBanner banner = Object.FindAnyObjectByType<RoundBanner>();
                Assert.That(banner.OutcomeText, Is.Not.Empty, "The banner shows the result.");
                GameLogDisplay log = Object.FindAnyObjectByType<GameLogDisplay>();
                Assert.That(log.Lines, Is.Not.Empty);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void The_game_scene_is_in_the_build_and_the_gallery_is_not()
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Assert.That(scenes, Does.Contain(GameScene.ScenePath));
            Assert.That(scenes, Does.Not.Contain(GalleryScene.ScenePath), "The gallery is a design tool, not part of the game.");
        }
    }
}
