using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>
    /// A touch screen has no hover: a long press replaces it (INTERFACE.md 1). A tap never shows the hover help or zoom,
    /// and the release after a long press never triggers the button.
    /// </summary>
    public class TouchTests
    {
        private const float Long = HoverIntent.HoldSeconds + 0.05f;
        private const float Short = HoverIntent.HoldSeconds / 3f;

        private Scene _scene;
        private GameDirector _director = null!;

        private PlayerControls Controls => _director.Controls;

        [TearDown]
        public void CloseGame() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void The_mouse_shows_at_once_and_a_finger_only_after_a_long_press()
        {
            int shown = 0;
            int hidden = 0;
            var intent = new HoverIntent(() => shown++, () => hidden++);

            intent.Enter(Mouse());
            Assert.That(shown, Is.EqualTo(1), "Hovering with the mouse.");
            intent.Exit(Mouse());
            Assert.That(hidden, Is.EqualTo(1));

            intent.Enter(Finger());
            intent.Down(Finger());
            intent.Tick(Short);
            intent.Up(Finger());
            Assert.That(shown, Is.EqualTo(1), "A tap shows nothing.");
            Assert.That(intent.WasHeld, Is.False, "The tap's click goes through.");

            intent.Down(Finger());
            intent.Tick(Short);
            intent.Tick(Long - Short);
            Assert.That(shown, Is.EqualTo(2), "A long press shows it.");
            intent.Up(Finger());
            Assert.That(hidden, Is.EqualTo(2), "Lifting the finger hides it.");
            Assert.That(intent.WasHeld, Is.True, "The click after a long press is ignored.");

            intent.Down(Finger());
            intent.Tick(Long);
            intent.Cancel();
            Assert.That(hidden, Is.EqualTo(3), "A drag that starts hides it.");
        }

        [Test]
        public void A_long_press_on_a_table_card_enlarges_it()
        {
            OpenGame(humanFirstSeat: false);
            CardHover card = Object.FindObjectsByType<CardHover>().First();
            CardZoom zoom = Object.FindAnyObjectByType<CardZoom>();

            card.OnPointerEnter(Finger());
            card.OnPointerDown(Finger());
            card.Tick(Short);
            Assert.That(zoom.Shown, Is.Null, "Not on a tap.");
            card.Tick(Long);
            Assert.That(zoom.Shown, Is.Not.Null);
            card.OnPointerUp(Finger());
            Assert.That(zoom.Shown, Is.Null);
        }

        [Test]
        public void A_long_press_on_an_opponent_grows_the_panel()
        {
            OpenGame(humanFirstSeat: false);
            SeatDisplay opponent = _director.Seats.First(s => s.Key != _director.Viewer).Value;

            opponent.OnPointerEnter(Finger());
            opponent.OnPointerDown(Finger());
            Assert.That(opponent.transform.localScale.x, Is.EqualTo(1f));
            opponent.Tick(Long);
            Assert.That(opponent.transform.localScale.x, Is.GreaterThan(1f));
            opponent.OnPointerUp(Finger());
            Assert.That(opponent.transform.localScale.x, Is.EqualTo(1f));
        }

        [Test]
        public void A_long_press_on_an_action_explains_it_without_using_it()
        {
            OpenGame(humanFirstSeat: true);
            ReachActionPhase();
            ActionButton? action = Controls.Actions.FirstOrDefault(a => a.Available && !a.NeedsTarget);
            Assume.That(action, Is.Not.Null, "An action without a target is allowed.");
            var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);

            action!.OnPointerDown(Finger());
            action.Tick(Long);
            Assert.That(Controls.Help.Text, Is.EqualTo(texts.Get(TextKeys.ActionHelp(action.Action))));
            action.OnPointerUp(Finger());
            action.OnPointerClick(Finger());
            Assert.That(Controls.Offered, Is.True, "The long press only explained the action.");
            Assert.That(Controls.Help.Text, Is.Empty);

            action.OnPointerDown(Finger());
            action.Tick(Short);
            action.OnPointerUp(Finger());
            action.OnPointerClick(Finger());
            Assert.That(Controls.Offered, Is.False, "A tap uses it.");
        }

        [Test]
        public void The_overcharge_token_and_the_combo_explain_themselves()
        {
            OpenGame(humanFirstSeat: true);
            PlayUntilOffered();
            var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);

            Controls.OverchargeHelp!.OnPointerEnter(Mouse());
            Assert.That(Controls.Help.Text, Is.EqualTo(texts.Get(TextKeys.HelpOvercharge)));
            Controls.OverchargeHelp.OnPointerExit(Mouse());
            Assert.That(Controls.Help.Text, Is.Empty);

            Controls.ComboHelp!.OnPointerDown(Finger());
            Controls.ComboHelp.Tick(Long);
            Assert.That(Controls.Help.Text, Does.StartWith("Combo"));
            Controls.ComboHelp.OnPointerUp(Finger());
            Assert.That(Controls.ComboHelp.WasHeld, Is.True, "The click that follows does not play the combo.");
        }

        private static PointerEventData Finger() => new ExtendedPointerEventData(null) { pointerType = UIPointerType.Touch };

        private static PointerEventData Mouse() => new ExtendedPointerEventData(null) { pointerType = UIPointerType.MouseOrPen };

        private void OpenGame(bool humanFirstSeat)
        {
            _scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            _director = _scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            _director.HumanFirstSeat = humanFirstSeat;
            _director.ShowCommandPanel = false;
            _director.Begin();
            _director.Advance(0f);
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
