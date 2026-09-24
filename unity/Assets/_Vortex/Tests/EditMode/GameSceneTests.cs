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
                director.HumanFirstSeat = false;
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
        public void A_person_plays_a_whole_game_with_the_command_panel_in_test_mode()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
                director.HumanFirstSeat = true;
                director.Begin();
                CommandPanel panel = Object.FindAnyObjectByType<CommandPanel>(FindObjectsInactive.Include);
                int choices = 0;
                int frames = 0;
                while ((!director.Session!.IsOver || director.IsPlaying) && frames < 100000)
                {
                    director.Advance(2f);
                    frames++;
                    if (panel.gameObject.activeSelf && panel.Options.Count > 0)
                    {
                        // The person always takes the last move offered: it ends the market phase, then the turn.
                        Assert.That(director.Session.Actor, Is.EqualTo(0), "Only the person's moves are offered.");
                        Assert.That(panel.Options.Any(o => o.StartsWith("#", System.StringComparison.Ordinal)), Is.False, string.Join(" | ", panel.Options));
                        panel.Choose(panel.Options.Count - 1);
                        choices++;
                    }
                }

                Assert.That(director.Session.IsOver, Is.True, "The game ends.");
                Assert.That(choices, Is.GreaterThan(0));
                Assert.That(panel.gameObject.activeSelf, Is.False, "The panel is hidden once the game is over.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void Every_decision_and_option_of_the_engine_has_words()
        {
            // The panel describes each answer through the interface texts; bots play many games and every decision
            // they meet must have its question in the table.
            var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var seats = Enumerable.Range(1, 5).Select(n => new Vortex.Client.Session.SeatSetup("Bot " + n, Vortex.Client.Session.SeatKind.Bot)).ToList();
                var session = new Vortex.Client.Session.LocalHotSeatSession(content.CreateEngine(), seed, seats);
                while (!session.IsOver)
                {
                    if (session.Decision != null)
                    {
                        Assert.That(texts.Contains(TextKeys.Decision(session.Decision.Prompt)), session.Decision.Prompt);
                    }

                    session.PlayBotStep();
                }
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
