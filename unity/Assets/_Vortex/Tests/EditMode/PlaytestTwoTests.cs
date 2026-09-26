using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Remarks of the second playtest: the zoom of an opponent's card, the log, the arc of the crew actions.</summary>
    public class PlaytestTwoTests
    {
        private GameDirector _director = null!;

        [SetUp]
        public void OpenGame()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            _director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            _director.ShowCommandPanel = false;
        }

        [TearDown]
        public void CloseGame() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void Moving_onto_an_opponents_card_keeps_its_panel_open_without_flicker()
        {
            _director.HumanFirstSeat = false;
            _director.Begin();
            SeatDisplay? opponent = null;
            for (int frame = 0; frame < 5000 && opponent is null; frame++)
            {
                _director.Advance(2f);
                opponent = _director.Seats.Where(s => s.Key != _director.Viewer).Select(s => s.Value)
                    .FirstOrDefault(s => s.AttackCard != null && s.AttackCard.gameObject.activeSelf);
            }

            Assume.That(opponent, Is.Not.Null, "An opponent has an attack card.");
            CardHover card = opponent!.AttackCard!.GetComponent<CardHover>();
            CardZoom zoom = Object.FindAnyObjectByType<CardZoom>();

            opponent.OnPointerEnter(null!);
            Assert.That(opponent.Grown, Is.True);

            // The pointer goes from the panel onto its card: the panel is left, then the card is entered, in one frame.
            opponent.OnPointerExit(null!);
            card.OnPointerEnter(null!);
            opponent.Tick(0f);
            Assert.That(opponent.Grown, Is.True, "The panel stays open under its card.");
            Assert.That(zoom.Shown, Is.Not.Null);

            // Back onto the panel, then away from both.
            card.OnPointerExit(null!);
            opponent.OnPointerEnter(null!);
            opponent.Tick(0f);
            Assert.That(opponent.Grown, Is.True);
            opponent.OnPointerExit(null!);
            opponent.Tick(0f);
            Assert.That(opponent.Grown, Is.False);
            Assert.That(zoom.Shown, Is.Null);
        }

        [Test]
        public void The_log_scrolls_back_and_follows_the_latest_line_only_at_the_end()
        {
            _director.HumanFirstSeat = false;
            _director.Begin();
            GameLogDisplay log = Object.FindAnyObjectByType<GameLogDisplay>();
            ScrollRect scroll = log.GetComponentInChildren<ScrollRect>(true);
            log.Toggle();
            for (int frame = 0; frame < 5000 && log.Lines.Count < 60; frame++)
            {
                _director.Advance(2f);
            }

            log.Refresh();
            Canvas.ForceUpdateCanvases();
            Assert.That(scroll.content.rect.height, Is.GreaterThan(scroll.viewport.rect.height), "More lines than the view shows.");
            Assert.That(log.AtEnd, Is.True, "It opens on the latest line.");

            scroll.verticalNormalizedPosition = 1f;
            Assert.That(log.AtEnd, Is.False, "Scrolled back to the start of the game.");
            log.Add("nouvelle ligne");
            log.Refresh();
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f), "Reading back: the view does not jump.");
            Assert.That(log.Text, Does.EndWith("nouvelle ligne"));

            scroll.verticalNormalizedPosition = 0f;
            log.Add("encore une");
            log.Refresh();
            Assert.That(log.AtEnd, Is.True, "At the end again: it follows.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void The_actions_form_an_arc_centred_on_the_ship_attack_left_shield_right(bool posture)
        {
            _director.Begin(new MatchSetup(
                new List<SeatSetup> { new SeatSetup("Vous", SeatKind.Human), new SeatSetup("Bot", SeatKind.Bot, BotLevel.Random) },
                seed: 3,
                rules: posture ? new RuleOptions(2, 0, false) : null));
            Dictionary<CrewAction, Vector2> at = _director.Controls.Actions
                .Where(a => a.InRules)
                .ToDictionary(a => a.Action, a => ((RectTransform)a.transform).anchoredPosition);

            Assert.That(at.ContainsKey(CrewAction.DefensivePosture), Is.EqualTo(posture));
            Assert.That(at[CrewAction.Attack].x, Is.LessThan(at[CrewAction.Overcharge].x), "Attack at the left end.");
            Assert.That(at[CrewAction.Overcharge].x, Is.LessThan(0f), "Attack actions on the left.");
            Assert.That(at[CrewAction.RerollShield].x, Is.GreaterThan(0f), "Shield actions on the right.");
            Assert.That(at[CrewAction.Sabotage].x, Is.GreaterThan(at.Values.Max(p => p.x) - 0.01f), "Sabotage at the right end.");
            Vector2 centre = new Vector2(0f, 270f);
            float radius = Vector2.Distance(at[CrewAction.Attack], centre);
            foreach (KeyValuePair<CrewAction, Vector2> action in at)
            {
                Assert.That(Vector2.Distance(action.Value, centre), Is.EqualTo(radius).Within(0.01f), action.Key + " on the arc.");
            }

            Assert.That(at[CrewAction.Attack].x, Is.EqualTo(-at[CrewAction.Sabotage].x).Within(0.01f), "Centred: both ends mirror each other.");
            if (!posture)
            {
                Assert.That(at[CrewAction.Overcharge].x, Is.EqualTo(-at[CrewAction.RerollShield].x).Within(0.01f), "Symmetric with four actions.");
            }
        }
    }
}
