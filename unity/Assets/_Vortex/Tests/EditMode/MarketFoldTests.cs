using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The market folds outside the person's market phase (INTERFACE.md 3.3, ARB-81).</summary>
    public class MarketFoldTests
    {
        [Test]
        public void The_market_opens_for_the_persons_market_then_folds_and_the_button_opens_it_again()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
                director.ShowCommandPanel = false;
                director.Begin(new MatchSetup(
                    new List<SeatSetup> { new SeatSetup("Vous", SeatKind.Human), new SeatSetup("Bot", SeatKind.Bot, BotLevel.Random) },
                    seed: 4));
                MarketDisplay market = director.Market;
                var shape = (RectTransform)market.transform;
                Assert.That(market.Open, Is.False, "Folded until the person's market.");
                float folded = shape.localScale.x;

                Wait(director);
                Assume.That(director.Controls.CanEndMarket, Is.True, "The person's market.");
                Assert.That(market.Open, Is.True);
                Assert.That(shape.localScale.x, Is.EqualTo(1f), "Fully open once it has moved.");

                director.Controls.EndMarket();
                Wait(director);
                Assume.That(director.Controls.CanEndMarket, Is.False, "The person's actions, after the market.");
                Assert.That(market.Open, Is.False, "Folded after the market phase.");
                Assert.That(shape.localScale.x, Is.EqualTo(folded).Within(0.001f));

                market.Toggle();
                director.Advance(1f);
                Assert.That(market.Open, Is.True, "The button opens it; the person's choice stands while the phase lasts.");
                Assert.That(shape.localScale.x, Is.EqualTo(1f));
                market.Toggle();
                Assert.That(market.Open, Is.False);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void Wait(GameDirector director)
        {
            director.Advance(1f);
            for (int frame = 0; frame < 20000 && !director.Controls.Offered; frame++)
            {
                director.Advance(2f);
            }

            director.Advance(1f);
        }
    }
}
