using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vortex.Client.Content;
using Vortex.Client.Menus;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The menus before a game (M4.6, INTERFACE.md 5, ARB-65): home, local game, development menu.</summary>
    public class MenuTests
    {
        private readonly List<MatchSetup> _launched = new List<MatchSetup>();
        private MainMenu _menu = null!;

        private LocalGameMenu Local => _menu.LocalGame;

        [SetUp]
        public void OpenMenu()
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScene.ScenePath, OpenSceneMode.Single);
            _menu = scene.GetRootGameObjects().Select(o => o.GetComponent<MainMenu>()).Single(m => m != null);
            _launched.Clear();
            _menu.Setup(_launched.Add);
        }

        [TearDown]
        public void CloseMenu() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void Home_offers_the_local_game_and_greys_out_what_is_not_there_yet()
        {
            Assert.That(_menu.AtHome, Is.True);
            Button[] buttons = Object.FindObjectsByType<Button>();
            Assert.That(buttons.Single(b => b.name == "Partie locale").interactable, Is.True);
            Assert.That(buttons.Single(b => b.name == "Trouver une partie").interactable, Is.False, "Online play comes in phase 2.");
            Assert.That(buttons.Single(b => b.name == "Social").interactable, Is.False);

            _menu.ShowLocalGame();
            Assert.That(_menu.AtHome, Is.False);
            Assert.That(Local.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void The_local_game_has_five_seats_by_default_the_first_a_persons()
        {
            MatchSetup setup = Local.BuildSetup();
            Assert.That(setup.Seats, Has.Count.EqualTo(5), "The standard table (ARB-52).");
            Assert.That(setup.Seats.Select(s => s.Kind), Is.EqualTo(new[] { SeatKind.Human, SeatKind.Bot, SeatKind.Bot, SeatKind.Bot, SeatKind.Bot }));
            Assert.That(setup.Seats.Select(s => s.Name), Is.EqualTo(new[] { "Joueur 1", "Joueur 2", "Joueur 3", "Joueur 4", "Joueur 5" }));
            Assert.That(setup.Seed, Is.Zero, "A new seed for each game.");
            Assert.That(setup.Rules, Is.Null, "The current rules.");
        }

        [Test]
        public void The_number_of_players_stays_between_two_and_five()
        {
            Local.SetPlayers(1);
            Assert.That(Local.Players, Is.EqualTo(2));
            Assert.That(Local.Rows.Count(r => r.gameObject.activeSelf), Is.EqualTo(2));
            Assert.That(Local.BuildSetup().Seats, Has.Count.EqualTo(2));
            Local.SetPlayers(9);
            Assert.That(Local.Players, Is.EqualTo(5));
        }

        [Test]
        public void Each_seat_chooses_a_person_or_a_bot_its_level_and_a_name()
        {
            SeatRow second = Local.Rows[1];
            second.NextLevel();
            Assert.That(second.Level, Is.EqualTo(BotLevel.Strong));
            second.ToggleKind();
            second.NameField.text = "  Alice  ";
            Local.Launch();

            MatchSetup setup = _launched.Single();
            Assert.That(setup.Humans, Is.EqualTo(2));
            Assert.That(setup.Seats[1].Name, Is.EqualTo("Alice"));
            Assert.That(setup.Seats[2].BotLevel, Is.EqualTo(BotLevel.Normal));
        }

        [TestCase("Bob", "Bob")]
        [TestCase("   ", "Joueur 3")]
        [TestCase("Ev‮il", "Evil")]
        [TestCase("Tab\there", "Tabhere")]
        [TestCase("A very long name that goes on and on", "A very long name that go")]
        [TestCase("​", "Joueur 3")]
        public void Names_are_cleaned_before_they_reach_the_engine(string typed, string expected)
        {
            Assert.That(SeatRow.CleanName(typed, "Joueur 3"), Is.EqualTo(expected));
        }

        [Test]
        public void Any_setup_of_the_menu_is_accepted_by_the_engine()
        {
            Local.Rows[0].NameField.text = "‮\u0007";
            Local.Rows[4].NameField.text = new string('x', 80);
            MatchSetup setup = Local.BuildSetup();
            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            Assert.DoesNotThrow(() => _ = new LocalHotSeatSession(content.CreateEngine(setup.Rules), 1, setup.Seats));
        }

        [Test]
        public void The_development_menu_tries_rule_options_and_a_seed()
        {
            DevMenu dev = Local.Development!;
            Assert.That(DevMenu.Available, Is.True, "The editor is a development environment.");
            dev.NextPosture();
            dev.NextBounty();
            dev.NextBounty();
            dev.ToggleGhosts();
            dev.SeedField.text = "42";
            MatchSetup setup = Local.BuildSetup();
            Assert.That(setup.Seed, Is.EqualTo(42UL));
            Assert.That((setup.Rules!.DefensivePostureBonus, setup.Rules.LeaderBounty, setup.Rules.GhostsChooseEvent), Is.EqualTo((1, 2, true)));

            var content = UnityEditor.AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            Assert.That(content.CreateEngine(setup.Rules).Config.LeaderBounty, Is.EqualTo(2), "The game is played with them.");

            dev.SeedField.text = "not a number";
            Assert.That(Local.BuildSetup().Seed, Is.Zero);
        }

        [Test]
        public void Options_keep_the_animation_speed()
        {
            float before = UserOptions.Load(1f).Speed;
            try
            {
                _menu.ShowOptions();
                OptionsMenu options = _menu.Options;
                float shown = options.Options!.Speed;
                options.NextSpeed();
                Assert.That(UserOptions.Load(1f).Speed, Is.EqualTo(UserOptions.NextSpeed(shown)), "Kept on this device.");
                options.Close();
                Assert.That(_menu.AtHome, Is.True);
            }
            finally
            {
                UserOptions kept = UserOptions.Load(1f);
                kept.Speed = before;
                kept.Save();
            }
        }
    }
}
