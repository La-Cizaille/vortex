using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Projection;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>A game started from the menu (M4.6): pause, end of game, restart, and several people on one device.</summary>
    public class MatchTests
    {
        private Scene _scene;
        private GameDirector _director = null!;

        [SetUp]
        public void OpenGame()
        {
            _scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            _director = _scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            _director.ShowCommandPanel = false;
        }

        [TearDown]
        public void CloseGame() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void The_game_of_the_menu_is_played_with_its_seats_and_rules()
        {
            _director.Begin(new MatchSetup(Seats(SeatKind.Bot, SeatKind.Bot, SeatKind.Human), seed: 3, rules: new RuleOptions(2, 0, false)));
            Assert.That(_director.Session!.View.Players.Select(p => p.Name), Is.EqualTo(new[] { "Siège A", "Siège B", "Siège C" }));
            Assert.That(_director.Session.Rules.DefensivePostureBonus, Is.EqualTo(2));
            Assert.That(_director.Viewer, Is.EqualTo(2), "The person's side of the table is at the bottom.");
            Assert.That(_director.Ships, Has.Count.EqualTo(3));
        }

        [Test]
        public void Nothing_moves_while_the_game_is_paused()
        {
            _director.Begin(new MatchSetup(Seats(SeatKind.Bot, SeatKind.Bot, SeatKind.Bot, SeatKind.Bot, SeatKind.Bot), seed: 5));
            AdvanceFrames(50);
            _director.Pause.Open();
            Assert.That(_director.Paused, Is.True);
            GameView before = _director.Session!.View;
            AdvanceFrames(500);
            Assert.That(_director.Session.View, Is.SameAs(before), "No bot plays during the pause.");

            _director.Pause.Close();
            Assert.That(_director.Paused, Is.False);
            AdvanceFrames(500);
            Assert.That(_director.Session.View, Is.Not.SameAs(before));
        }

        [Test]
        public void The_end_of_the_game_offers_to_play_again_or_leave()
        {
            _director.Begin(new MatchSetup(Seats(SeatKind.Bot, SeatKind.Bot), seed: 11));
            for (int frame = 0; frame < 50000 && !_director.GameOver.Shown; frame++)
            {
                _director.Advance(2f);
            }

            Assert.That(_director.Session!.IsOver, Is.True);
            Assert.That(_director.GameOver.Shown, Is.True);
            Assert.That(_director.GameOver.Text, Is.EqualTo(Object.FindAnyObjectByType<RoundBanner>().OutcomeText).And.Not.Empty);

            _director.Restart();
            Assert.That(_director.GameOver.Shown, Is.False);
            Assert.That(_director.Session.IsOver, Is.False, "A new game with the same seats.");
            Assert.That(_director.Session.View.Players.Select(p => p.Name), Is.EqualTo(new[] { "Siège A", "Siège B" }));
        }

        [Test]
        public void Restarting_leaves_no_card_behind()
        {
            Transform cards = _scene.GetRootGameObjects().Single(o => o.name == "Cartes 3D").transform;
            var setup = new MatchSetup(Seats(SeatKind.Human, SeatKind.Bot, SeatKind.Bot, SeatKind.Bot, SeatKind.Bot), seed: 9);
            _director.Begin(setup);
            int first = cards.GetComponentsInChildren<CardDisplay>(true).Length;
            _director.Restart();
            _director.Restart();
            Assert.That(cards.GetComponentsInChildren<CardDisplay>(true), Has.Length.EqualTo(first));
        }

        [Test]
        public void With_two_people_the_view_turns_to_each_one_at_the_start_of_their_turn()
        {
            _director.Begin(new MatchSetup(Seats(SeatKind.Human, SeatKind.Bot, SeatKind.Human, SeatKind.Bot), seed: 21));
            var viewed = new HashSet<int>();
            PlayerControls controls = _director.Controls;
            for (int frame = 0; frame < 20000 && viewed.Count < 2 && !_director.Session!.IsOver; frame++)
            {
                _director.Advance(2f);
                if (!controls.Offered)
                {
                    continue;
                }

                if (_director.Session!.Decision is null)
                {
                    Assert.That(_director.Viewer, Is.EqualTo(_director.Session.Actor), "Each person plays from their own side.");
                    if (viewed.Add(_director.Viewer) && _director.Viewer == 2)
                    {
                        Assert.That(_director.Announcement.Text, Is.EqualTo("Tour de Siège C"), "The banner says whose turn it is.");
                    }
                }

                if (TableAnswers.Answer(controls))
                {
                    // A decision, answered on the table (ARB-82).
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
                    Command imposed = _director.Session.LegalCommands(_director.Session.Actor).First(c => c.Type == PlayerControls.TypeOf(action.Action));
                    Assert.That(action.NeedsTarget ? controls.UseActionOn(action.Action, imposed.Target) : controls.UseAction(action.Action), Is.True);
                }
            }

            Assert.That(viewed, Is.EquivalentTo(new[] { 0, 2 }));
            Assert.That(_director.Seats[_director.Viewer], Is.Not.Null);
        }

        private static List<SeatSetup> Seats(params SeatKind[] kinds) =>
            kinds.Select((kind, i) => new SeatSetup("Siège " + (char)('A' + i), kind, BotLevel.Random)).ToList();

        private void AdvanceFrames(int frames)
        {
            for (int frame = 0; frame < frames; frame++)
            {
                _director.Advance(2f);
            }
        }
    }
}
