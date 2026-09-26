using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The black markets' decks (ANIMATIONS.md §2): cards leave them when revealed and go back when recycled.</summary>
    public class MarketDeckTests
    {
        [TearDown]
        public void CloseScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [Test]
        public void A_revealed_card_flies_from_the_deck_to_its_hidden_place_and_a_recycle_sends_the_cards_back()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
            director.ShowCommandPanel = false;
            director.Begin(new MatchSetup(
                new List<SeatSetup> { new SeatSetup("Bot 1", SeatKind.Bot, BotLevel.Random), new SeatSetup("Bot 2", SeatKind.Bot, BotLevel.Random) },
                seed: 8));
            MarketDisplay market = director.Market;
            DeckDisplay? deck = market.DeckOf(CardSlot.Attack);
            Assert.That(deck, Is.Not.Null);
            Assert.That(deck!.Count, Is.GreaterThan(0), "The deck shows the cards left.");
            CleanUpTrips();

            IReadOnlyList<(Vortex.Core.Projection.CardView Card, RectTransform Place)> shown = market.Shown(CardSlot.Attack);
            Assume.That(shown.Count, Is.GreaterThan(2));
            IFeedbackStage stage = director;
            MarketDeckFeedback feedback = ScriptableObject.CreateInstance<MarketDeckFeedback>();

            feedback.Play(new GameEvent { Type = GameEventType.MarketCardRevealed, Value = (int)CardSlot.Attack, Amount = 2, Id = shown[2].Card.CardId, CardUid = shown[2].Card.Uid }, stage);
            CardTrip trip = Object.FindObjectsByType<CardTrip>().Single();
            Assert.That(market.IsConcealed(CardSlot.Attack, 2), Is.True, "Its place waits, empty, for the card on its way.");
            Assert.That(trip.Tick(5f), Is.False);
            Assert.That(market.IsConcealed(CardSlot.Attack, 2), Is.False, "Arrived: the place shows its card.");

            feedback.Play(new GameEvent { Type = GameEventType.MarketRecycled, Value = (int)CardSlot.Attack }, stage);
            Assert.That(Object.FindObjectsByType<CardTrip>().Length, Is.EqualTo(shown.Count), "Every card of the market goes back to the deck.");
            CleanUpTrips();

            deck.Show(0);
            Assert.That(deck.GetComponent<CardDisplay>().Visible, Is.False, "An empty deck is not shown.");
        }

        private static void CleanUpTrips()
        {
            foreach (CardTrip trip in Object.FindObjectsByType<CardTrip>())
            {
                Object.DestroyImmediate(trip.gameObject);
            }
        }
    }
}
