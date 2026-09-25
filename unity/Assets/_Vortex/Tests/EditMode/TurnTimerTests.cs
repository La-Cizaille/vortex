using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Vortex.Client.Menus;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The turn timer (INTERFACE.md 3.9, ARB-80).</summary>
    public class TurnTimerTests
    {
        [Test]
        public void The_clock_counts_a_turn_and_a_shorter_decision_asked_during_it()
        {
            var clock = new TurnClock(60f, 15f);
            clock.Follow(round: 1, currentPlayer: 0, actor: 0, decisionId: null);
            clock.Tick(20f);
            Assert.That(clock.Remaining, Is.EqualTo(40f));

            // Another person decides during that turn: 15 seconds of their own, the turn keeps its time.
            clock.Follow(1, 0, actor: 2, decisionId: "7.0");
            Assert.That((clock.ForDecision, clock.Remaining), Is.EqualTo((true, 15f)));
            clock.Tick(15f);
            Assert.That(clock.Expired, Is.True);
            clock.Follow(1, 0, actor: 0, decisionId: null);
            Assert.That(clock.Remaining, Is.EqualTo(40f), "Back to the turn, where it was.");

            // A decision of the current player counts on the turn's time.
            clock.Follow(1, 0, actor: 0, decisionId: "8.0");
            Assert.That(clock.ForDecision, Is.False);

            clock.Stop();
            clock.Tick(30f);
            Assert.That(clock.Remaining, Is.EqualTo(40f), "Stopped while nobody can act (animations, bots, pause).");

            clock.Follow(1, 1, actor: 1, decisionId: null);
            Assert.That(clock.Remaining, Is.EqualTo(60f), "A new turn gets the full time.");
            Assert.That(new TurnClock(0f, 15f).Enabled, Is.False, "0: no limit.");
        }

        [Test]
        public void When_the_time_is_up_the_turn_ends_by_itself_with_the_last_seconds_signalled()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
                director.ShowCommandPanel = false;
                director.Begin(new MatchSetup(
                    new List<SeatSetup> { new SeatSetup("Vous", SeatKind.Human), new SeatSetup("Bot", SeatKind.Bot, BotLevel.Random) },
                    seed: 4,
                    turnSeconds: 60));
                for (int frame = 0; frame < 20000 && !director.Controls.Offered; frame++)
                {
                    director.Advance(2f);
                }

                Assume.That(director.Session!.Decision, Is.Null, "The person's turn, no decision pending.");
                int round = director.Session.View.Round;
                float waited = 0f;
                bool warned = false;
                for (int second = 0; second < 600 && director.Session.View.CurrentPlayer == 0 && director.Session.View.Round == round; second++)
                {
                    if (director.Controls.Offered)
                    {
                        Assert.That(director.Timer.Shown, Is.True, "The time left shows while the person can act.");
                        waited += 1f;
                        warned |= director.Timer.Warning;
                    }

                    director.Advance(1f);
                }

                Assert.That(director.Session.View.CurrentPlayer, Is.Not.EqualTo(0), "The turn ended by itself.");
                Assert.That(waited, Is.EqualTo(60f).Within(2f), "After the whole time, counted only while the person could act.");
                Assert.That(warned, Is.True);
                Assert.That(director.Timer.Ticks, Is.EqualTo(10), "A tick for each of the last ten seconds.");
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void The_local_game_menu_offers_no_limit_by_default_then_60_90_120_seconds()
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScene.ScenePath, OpenSceneMode.Single);
            try
            {
                MainMenu menu = scene.GetRootGameObjects().Select(o => o.GetComponent<MainMenu>()).Single(m => m != null);
                menu.Setup(_ => { });
                LocalGameMenu local = menu.LocalGame;
                Assert.That(local.BuildSetup().TurnSeconds, Is.Zero, "Not timed by default in a local game.");
                var offered = new List<int>();
                for (int i = 0; i < 4; i++)
                {
                    local.NextTurnTime();
                    offered.Add(local.BuildSetup().TurnSeconds);
                }

                Assert.That(offered, Is.EqualTo(new[] { 60, 90, 120, 0 }));
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
