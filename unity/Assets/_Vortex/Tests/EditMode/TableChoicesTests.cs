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
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Projection;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Decisions are answered on the table, with no button (INTERFACE.md 3.6, ARB-82).</summary>
    public class TableChoicesTests
    {
        [TearDown]
        public void CloseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void Players_are_seats_and_the_pass_is_the_deciders_own_seat_when_it_is_not_an_answer()
        {
            GameView view = View();
            DecisionChoices choices = DecisionChoices.From(Request(DecisionKind.ChoosePlayer, 0, null, Pass(), Player(1), Player(2)), view);
            Assert.That(choices.Seats, Is.EquivalentTo(new Dictionary<int, string> { [1] = "p1", [2] = "p2", [0] = DecisionChoices.Pass }));
            Assert.That((choices.PassOnOwnSeat, choices.Unplaced), Is.EqualTo((true, 0)));

            // The decider is an answer too: the pass goes to the card that asks, shown in the middle.
            choices = DecisionChoices.From(Request(DecisionKind.ChoosePlayer, 0, "A_001", Pass(), Player(0), Player(1)), view);
            Assert.That(choices.Seats.Keys, Is.EquivalentTo(new[] { 0, 1 }));
            Assert.That(choices.Middle, Is.EqualTo(new[] { ("A_001", DecisionChoices.Pass) }));
        }

        [Test]
        public void Only_a_card_a_player_has_equipped_takes_the_pass()
        {
            GameView view = View();
            CardView market = view.Market(CardSlot.Attack).Visible[0];
            CardView other = view.Market(CardSlot.Attack).Visible[1];

            // A card on the table: the market's, as no seat shows it.
            DecisionChoices choices = DecisionChoices.From(Request(DecisionKind.ChooseMarketCard, 0, other.CardId, Pass(), Card(market.Uid)), view);
            Assert.That(choices.Cards[market.Uid], Is.EqualTo("c" + market.Uid));
            Assert.That(choices.PassCard, Is.EqualTo(-1), "Only a card equipped by a player asks.");
            Assert.That(choices.PassOnOwnSeat, Is.True);
        }

        [Test]
        public void Numbers_events_crew_actions_and_drags_have_their_place()
        {
            GameView view = View();
            DecisionChoices numbers = DecisionChoices.From(Request(DecisionKind.ChooseNumber, 0, null, Number(1), Number(2), Number(3)), view);
            Assert.That(numbers.Numbers.Select(n => n.Number), Is.EqualTo(new[] { 1, 2, 3 }));

            DecisionChoices events = DecisionChoices.From(Request(DecisionKind.ChooseOption, 0, null, new DecisionOption { Key = "first", ContentId = "E_001" }, new DecisionOption { Key = "second", ContentId = "E_002" }), view);
            Assert.That(events.Middle, Is.EqualTo(new[] { ("E_001", "first"), ("E_002", "second") }));

            DecisionChoices actions = DecisionChoices.From(Request(
                DecisionKind.ChooseCrewAction,
                0,
                null,
                new DecisionOption { Key = "attack:1", Player = 1, Number = (int)CrewAction.Attack },
                new DecisionOption { Key = "reroll", Number = (int)CrewAction.RerollShield }), view);
            Assert.That(actions.AimedActions[(CrewAction.Attack, 1)], Is.EqualTo("attack:1"));
            Assert.That(actions.Actions[CrewAction.RerollShield], Is.EqualTo("reroll"));

            DecisionChoices drags = DecisionChoices.From(Request(
                DecisionKind.ChooseOption,
                0,
                null,
                new DecisionOption { Key = "steal", CardUid = 7, Player = 0 },
                new DecisionOption { Key = "destroy", CardUid = 7 }), view);
            Assert.That(drags.Drops[(7, 0)], Is.EqualTo("steal"), "Dragged onto the decider's side.");
            Assert.That(drags.DropsInMiddle[7], Is.EqualTo("destroy"), "Dragged into the middle.");

            DecisionChoices directions = DecisionChoices.From(Request(
                DecisionKind.ChooseDirection,
                0,
                null,
                new DecisionOption { Key = "clockwise", Number = 1, Player = 1 },
                new DecisionOption { Key = "counterclockwise", Number = -1, Player = 1 }), view);
            Assert.That(directions.Seats, Is.EquivalentTo(new Dictionary<int, string> { [1] = "clockwise" }), "Two seats: both directions give the same neighbour.");
        }

        [Test]
        public void An_answer_the_table_cannot_show_leaves_the_decision_to_the_window()
        {
            DecisionChoices choices = DecisionChoices.From(Request(DecisionKind.YesNo, 0, null, new DecisionOption { Key = "yes" }, new DecisionOption { Key = "no" }), View());
            Assert.That(choices.Unplaced, Is.EqualTo(2));
        }

        [Test]
        public void A_persons_decision_shows_its_question_and_lights_its_answers_with_no_button()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            director.ShowCommandPanel = false;
            director.Begin(new MatchSetup(
                Enumerable.Range(0, 5).Select(i => new SeatSetup("Siège " + (i + 1), i == 0 ? SeatKind.Human : SeatKind.Bot, BotLevel.Normal)).ToList(),
                104UL));

            PlayerControls controls = director.Controls;
            DecisionChoices? choices = null;
            for (int frame = 0; frame < 200000 && !director.Session!.IsOver && choices is null; frame++)
            {
                director.Advance(2f);
                if (!controls.Offered)
                {
                    continue;
                }

                choices = controls.Choices;
                if (choices != null)
                {
                    break;
                }

                if (controls.Decision.gameObject.activeSelf)
                {
                    controls.Decision.Choose(0);
                }
                else if (controls.CanEndMarket)
                {
                    // The person equips cards, so that attacks and their own cards ask them things.
                    Rect side = CardAnchor.ScreenRectOf((RectTransform)director.Seats[director.Viewer].transform);
                    CardSlot slot = director.Session.View.Round % 2 == 0 ? CardSlot.Attack : CardSlot.Defense;
                    if (!(controls.CanBuy(slot, 0) && controls.Buy(slot, 0, side.center)))
                    {
                        controls.EndMarket();
                    }
                }
                else if (UseACard(director))
                {
                    // One of the person's cards, used: it may ask them something.
                }
                else if (controls.CanEndTurn)
                {
                    controls.EndTurn();
                }
                else
                {
                    ActionButton action = controls.Actions.First(a => a.Available);
                    Command imposed = director.Session.LegalCommands(0).First(c => c.Type == PlayerControls.TypeOf(action.Action));
                    Assume.That(action.NeedsTarget ? controls.UseActionOn(action.Action, imposed.Target) : controls.UseAction(action.Action), Is.True);
                }
            }

            Assume.That(choices, Is.Not.Null, "The person was asked something during the game.");
            var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
            Assert.That(controls.Board.Question, Does.Contain(texts.Get(TextKeys.Decision(choices!.Request.Prompt))));
            Assert.That(controls.Decision.gameObject.activeSelf, Is.False, "No window of buttons.");
            int answering = choices.Cards.Count + choices.Drops.Count + choices.DropsInMiddle.Count + choices.Middle.Count;
            Assert.That(Object.FindObjectsByType<CardDisplay>().Count(c => c.gameObject.activeSelf && c.Marked), Is.GreaterThanOrEqualTo(answering), "The cards that answer are marked.");

            Assert.That(controls.Board.Faces.Count(f => f.Allowed), Is.EqualTo(choices.Numbers.Count));
            Assert.That(controls.Board.MiddleCards.Count, Is.EqualTo(choices.Middle.Count));

            Assert.That(TableAnswers.Answer(controls), Is.True);
            Assert.That(controls.Board.Question, Is.Empty, "Answered: the question goes.");
            Assert.That(controls.Choices, Is.Null);
        }

        // Drags one of the person's usable cards into the middle; false when none can be used.
        private static bool UseACard(GameDirector director)
        {
            PlayerView me = director.Session!.View.Players[director.Viewer];
            PlayerControls controls = director.Controls;
            Vector2 middle = CardAnchor.ScreenRectOf(controls.ActivationZone).center;
            return new[] { me.AttackSlot, me.DefenseSlot }.Any(card => card != null && controls.CanUse(card.Uid) && controls.UseCard(card.Uid, middle));
        }

        private static GameView View()
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            var seats = Enumerable.Range(0, 3).Select(i => new SeatSetup("Siège " + (i + 1), SeatKind.Human)).ToList();
            return new LocalHotSeatSession(content.CreateEngine(), 5UL, seats).View;
        }

        private static DecisionRequest Request(DecisionKind kind, int player, string? source, params DecisionOption[] options) =>
            new DecisionRequest { Id = "1.0", Player = player, Kind = kind, Prompt = "test", SourceId = source, Options = options.ToList() };

        private static DecisionOption Pass() => new DecisionOption { Key = DecisionChoices.Pass };

        private static DecisionOption Player(int seat) => new DecisionOption { Key = "p" + seat, Player = seat };

        private static DecisionOption Card(int uid) => new DecisionOption { Key = "c" + uid, CardUid = uid };

        private static DecisionOption Number(int n) => new DecisionOption { Key = "n" + n, Number = n };
    }
}
