using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Client.Theme;
using Vortex.Core.Bots;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The player's cockpit (ARB-90): the Blender model under the ship, which shows the player's figures.</summary>
    public class CockpitTests
    {
        [TearDown]
        public void CloseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void The_cockpit_shows_the_players_name_hit_points_shield_overcharge_and_technologies()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            director.ShowCommandPanel = false;
            director.Begin(new MatchSetup(
                new List<SeatSetup> { new SeatSetup("Vous", SeatKind.Human), new SeatSetup("Bot", SeatKind.Bot, BotLevel.Random) },
                seed: 4));
            CockpitDisplay? cockpit = director.Cockpit;
            Assert.That(cockpit, Is.Not.Null, "The theme has the cockpit model.");
            Assert.That(cockpit!.Texts.Name, Is.EqualTo("Vous"));
            Assert.That(cockpit.Fill, Is.EqualTo(1f).Within(0.01f), "Full hit points at the start.");

            Assert.That(cockpit.Texts.Figures, Is.EqualTo("0 1 2 3 4 5 6 7 8"), "The dial's figures, from 0 to the full shield.");

            var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            cockpit.Show("Vous", Color.cyan, 15, 30, 8, 8, false, true, true, new[] { Color.red, Color.blue }, texts, theme);
            Assert.That(cockpit.Fill, Is.EqualTo(0.5f).Within(0.01f), "Half the hit points: half the gauge.");
            Assert.That(cockpit.Texts.Hp, Does.Contain("15"));
            Assert.That(cockpit.Needle, Is.EqualTo(CockpitDisplay.NeedleSweep).Within(0.5f), "A full shield: the needle all the way right.");
            Assert.That((cockpit.SwitchUp, cockpit.ChargeLit, cockpit.RedZoneLit, cockpit.DiodesLit), Is.EqualTo((true, true, false, 2)),
                "Armed token: switch up, its diode lit; a shield that protects: red zone off; two technologies: two diodes.");

            cockpit.Show("Vous", Color.cyan, 30, 30, 5, 8, true, false, false, new Color[0], texts, theme);
            Assert.That((cockpit.SwitchUp, cockpit.ChargeLit, cockpit.RedZoneLit, cockpit.DiodesLit), Is.EqualTo((false, false, true, 0)),
                "A disabled shield lights the red zone, whatever its value.");

            cockpit.Show("Vous", Color.cyan, 30, 30, 0, 8, false, false, false, new Color[0], texts, theme);
            Assert.That(cockpit.Needle, Is.EqualTo(-CockpitDisplay.NeedleSweep).Within(0.5f), "No shield: the needle all the way left.");
            Assert.That(cockpit.RedZoneLit, Is.True, "No shield: the red zone lit.");
        }
    }
}
