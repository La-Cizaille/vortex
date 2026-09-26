using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>What shows around the person's ship (INTERFACE.md 3.2, 3.4, 3.7, ARB-87).</summary>
    public class LayoutRulesTests
    {
        [TearDown]
        public void CloseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void Only_possible_actions_and_a_ready_combo_show_and_the_end_of_turn_lights_once_no_crew_action_is_left()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            director.ShowCommandPanel = false;
            director.Begin(new MatchSetup(
                new List<SeatSetup> { new SeatSetup("Vous", SeatKind.Human), new SeatSetup("Bot", SeatKind.Bot, BotLevel.Random) },
                seed: 4));
            PlayerControls controls = director.Controls;
            Wait(director);
            Assume.That(controls.CanEndMarket, Is.True, "The person's market.");
            Assert.That(controls.Actions.Any(a => a.gameObject.activeSelf), Is.False, "No crew action during the market.");

            controls.EndMarket();
            Wait(director);
            Assume.That(controls.Choices, Is.Null);
            IReadOnlyList<Command> legal = director.Session!.LegalCommands(0);
            foreach (ActionButton action in controls.Actions)
            {
                bool possible = legal.Any(c => c.Type == PlayerControls.TypeOf(action.Action));
                Assert.That(action.gameObject.activeSelf, Is.EqualTo(possible && action.InRules), action.Action.ToString());
            }

            bool comboReady = legal.Any(c => c.Type == CommandType.ActivateTechnology);
            Assert.That(controls.ComboShown, Is.EqualTo(comboReady), "The combo shows only when ready.");
            Assert.That(controls.EndTurnLit, Is.False, "Crew actions are left.");

            // After the crew action, the end of the turn lights up, even if a card could still be used.
            ActionButton reroll = controls.Actions.Single(a => a.Action == CrewAction.RerollShield);
            Assume.That(reroll.Available, Is.True);
            controls.UseAction(CrewAction.RerollShield);
            Wait(director);
            Assume.That(director.Session.LegalCommands(0).Any(c => PlayerControls.IsCrewAction(c.Type)), Is.False);
            Assert.That(controls.Actions.Any(a => a.gameObject.activeSelf), Is.False, "No action left to show.");
            Assert.That(controls.EndTurnLit, Is.True);
        }

        private static void Wait(GameDirector director)
        {
            director.Advance(1f);
            for (int frame = 0; frame < 20000 && !director.Controls.Offered; frame++)
            {
                director.Advance(2f);
            }
        }
    }
}
