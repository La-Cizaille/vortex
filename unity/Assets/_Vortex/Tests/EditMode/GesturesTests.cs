using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Presentation;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The person plays with gestures (M4.5, INTERFACE.md 3): every gesture picks one of the engine's legal commands.</summary>
    public class GesturesTests
    {
        private Scene _scene;
        private GameDirector _director = null!;

        private PlayerControls Controls => _director.Controls;

        [SetUp]
        public void OpenGame()
        {
            _scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            _director = _scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            _director.HumanFirstSeat = true;
            _director.ShowCommandPanel = false;
            _director.Begin();
        }

        [TearDown]
        public void CloseGame() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void A_person_plays_a_whole_game_with_gestures_only()
        {
            int gestures = 0;
            for (int frame = 0; frame < 100000 && (!_director.Session!.IsOver || _director.IsPlaying); frame++)
            {
                _director.Advance(2f);
                if (!Controls.Offered)
                {
                    continue;
                }

                gestures++;
                if (TableAnswers.Answer(Controls))
                {
                    // A decision, answered on the table (ARB-82).
                }
                else if (Controls.CanEndMarket)
                {
                    Controls.EndMarket();
                }
                else if (Controls.CanEndTurn)
                {
                    Controls.EndTurn();
                }
                else
                {
                    // An action is imposed (for instance by a card): take it, on its first allowed target.
                    ActionButton action = Controls.Actions.First(a => a.Available);
                    Command imposed = _director.Session.LegalCommands(0).First(c => c.Type == PlayerControls.TypeOf(action.Action));
                    Assert.That(action.NeedsTarget ? Controls.UseActionOn(action.Action, imposed.Target) : Controls.UseAction(action.Action), Is.True);
                }
            }

            Assert.That(_director.Session!.IsOver, Is.True, "The game ends.");
            Assert.That(gestures, Is.GreaterThan(0));
        }

        [Test]
        public void An_attack_goes_to_an_opponent_the_engine_allows_and_never_elsewhere()
        {
            ReachActionPhase();
            Command attack = _director.Session!.LegalCommands(0).First(c => c.Type == CommandType.Attack);
            Assert.That(Controls.Actions.Single(a => a.Action == CrewAction.Attack).Available, Is.True);

            Assert.That(Controls.UseActionOn(CrewAction.Attack, 0), Is.False, "Not oneself.");
            Assert.That(Controls.UseActionOn(CrewAction.Attack, -1), Is.False, "Not the empty table.");
            Assert.That(Controls.UseActionOn(CrewAction.Attack, attack.Target), Is.True);
            Assert.That(Controls.Offered, Is.False, "The controls are taken back while the attack plays.");
            PlayUntilOffered();
            Assert.That(_director.Session.View.CrewActionsTaken, Does.Contain(CrewAction.Attack));
        }

        [Test]
        public void Aiming_an_attack_over_a_target_shows_the_engine_preview_next_to_it()
        {
            ReachActionPhase();
            Command attack = _director.Session!.LegalCommands(0).First(c => c.Type == CommandType.Attack);
            string? preview = Controls.PreviewOn(CrewAction.Attack, attack.Target);
            Assert.That(preview, Does.StartWith("Dés : ").And.Contains("Touche : "));
            Assert.That(Controls.PreviewOn(CrewAction.Attack, 0), Is.Null, "Not oneself.");
            Assert.That(Controls.PreviewOn(CrewAction.Attack, -1), Is.Null, "Not the empty table.");

            var from = (RectTransform)Controls.Actions.Single(a => a.Action == CrewAction.Attack).transform;
            Rect target = CardAnchor.ScreenRectOf((RectTransform)_director.Seats[attack.Target].transform);
            Controls.BeginAim(CrewAction.Attack, from, CardAnchor.ScreenRectOf(from).center);
            Assert.That(Controls.Help.Text, Is.Empty, "Nothing before a target is reached.");
            Controls.Aim(target.center);
            Assert.That(Controls.Help.Text, Is.EqualTo(preview), "The same preview, computed once per offer.");
            AssertInside(Controls.Help.Bubble);

            Assert.That(Controls.EndAim(target.center), Is.True);
            Assert.That(Controls.Help.Text, Is.Empty, "The preview goes with the aim.");
        }

        [Test]
        public void A_market_card_is_bought_only_when_dropped_on_the_players_side()
        {
            PlayUntilOffered();
            Assume.That(Controls.CanEndMarket, Is.True, "The person starts at the market.");
            Command pick = _director.Session!.LegalCommands(0).First(c => c.Type == CommandType.PickMarket);
            Assert.That(Controls.CanBuy(pick.Slot, pick.MarketIndex), Is.True);

            Rect side = CardAnchor.ScreenRectOf((RectTransform)_director.Seats[0].transform);
            Assert.That(Controls.Buy(pick.Slot, pick.MarketIndex, new Vector2(side.xMax + 400f, side.yMax + 400f)), Is.False, "Dropped elsewhere: back to the market.");
            Assert.That(Controls.Buy(pick.Slot, pick.MarketIndex, side.center), Is.True);
        }

        [Test]
        public void The_action_buttons_light_up_only_when_the_engine_allows_them()
        {
            PlayUntilOffered();
            Assume.That(Controls.CanEndMarket, Is.True);
            Assert.That(Controls.Actions.Any(a => a.Available), Is.False, "No crew action during the market phase.");
            Assert.That(Controls.CanEndTurn, Is.False);

            ReachActionPhase();
            foreach (ActionButton action in Controls.Actions.Where(a => a.gameObject.activeSelf))
            {
                bool allowed = _director.Session!.LegalCommands(0).Any(c => c.Type == PlayerControls.TypeOf(action.Action));
                Assert.That(action.Available, Is.EqualTo(allowed), action.Action.ToString());
            }
        }

        [Test]
        public void An_armed_overcharge_is_spent_and_otherwise_kept()
        {
            Command[] legal = { Command.Attack(2), Command.Attack(2, true), Command.Attack(3), Command.RerollShield(), Command.RerollShield(true) };
            Assert.That(PlayerControls.Pick(legal, CommandType.Attack, 2, armed: true)!.UseOvercharge, Is.True);
            Assert.That(PlayerControls.Pick(legal, CommandType.Attack, 2, armed: false)!.UseOvercharge, Is.False);
            Assert.That(PlayerControls.Pick(legal, CommandType.Attack, 3, armed: true)!.Target, Is.EqualTo(3), "No overcharged attack on 3: the plain one.");
            Assert.That(PlayerControls.Pick(legal, CommandType.RerollShield, -1, armed: true)!.UseOvercharge, Is.True);
            Assert.That(PlayerControls.Pick(legal, CommandType.Sabotage, 2, armed: false), Is.Null);
        }

        [Test]
        public void An_opponent_panel_grows_while_hovered()
        {
            SeatDisplay opponent = _director.Seats.First(s => s.Key != 0).Value;
            opponent.OnPointerEnter(null!);
            Assert.That(opponent.transform.localScale.x, Is.GreaterThan(1f));
            opponent.OnPointerExit(null!);
            opponent.Tick(0f);
            Assert.That(opponent.transform.localScale.x, Is.EqualTo(1f), "Closed at the next frame.");
        }

        // The bubble lies inside the layer it is drawn on (the screen).
        private static void AssertInside(RectTransform bubble)
        {
            var layer = (RectTransform)bubble.parent;
            var corners = new Vector3[4];
            bubble.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 local = layer.InverseTransformPoint(corner);
                Assert.That(local.x, Is.InRange(layer.rect.xMin - 0.5f, layer.rect.xMax + 0.5f), "Inside horizontally.");
                Assert.That(local.y, Is.InRange(layer.rect.yMin - 0.5f, layer.rect.yMax + 0.5f), "Inside vertically.");
            }
        }

        [Test]
        public void An_armed_overcharge_is_disarmed_once_the_action_is_sent()
        {
            // First turn: gain the token; next turn: arm it, then attack.
            ReachActionPhase();
            Assume.That(Controls.UseAction(CrewAction.Overcharge), Is.True);
            PlayUntilOffered();
            if (Controls.CanEndTurn)
            {
                Controls.EndTurn();
            }

            ReachActionPhase();
            Assume.That(_director.Session!.View.Players[0].Overcharge, Is.GreaterThan(0), "The token is still held.");
            Controls.ToggleOvercharge();
            Assert.That(Controls.OverchargeArmed, Is.True);
            Command attack = _director.Session.LegalCommands(0).First(c => c.Type == CommandType.Attack);
            Assert.That(Controls.UseActionOn(CrewAction.Attack, attack.Target), Is.True);
            Assert.That(Controls.OverchargeArmed, Is.False, "Arming is a choice for one action (ARB-67): never left armed.");
        }

        private void PlayUntilOffered()
        {
            for (int frame = 0; frame < 20000 && !Controls.Offered && !_director.Session!.IsOver; frame++)
            {
                _director.Advance(2f);
            }

            Assume.That(Controls.Offered, Is.True, "The person's turn came.");
        }

        private void ReachActionPhase()
        {
            PlayUntilOffered();
            if (Controls.CanEndMarket)
            {
                Controls.EndMarket();
                PlayUntilOffered();
            }

            Assume.That(Controls.Decision.gameObject.activeSelf, Is.False, "No decision pending.");
        }
    }
}
