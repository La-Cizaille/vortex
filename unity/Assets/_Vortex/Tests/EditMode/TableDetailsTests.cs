using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Projection;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Details of the table (INTERFACE.md 3.1, 3.3 and 3.8).</summary>
    public class TableDetailsTests
    {
        private GameDirector _director = null!;

        private PlayerControls Controls => _director.Controls;

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
        public void Pointing_at_the_banner_enlarges_the_event_of_the_round()
        {
            _director.HumanFirstSeat = false;
            _director.Begin();
            for (int frame = 0; frame < 200 && _director.Model!.ActiveEventId is null; frame++)
            {
                _director.Advance(2f);
            }

            Assume.That(_director.Model!.ActiveEventId, Is.Not.Null, "An event is on.");
            _director.Advance(0f);
            RoundBanner banner = Object.FindAnyObjectByType<RoundBanner>();
            CardZoom zoom = Object.FindAnyObjectByType<CardZoom>();

            banner.OnPointerEnter(null!);
            Assert.That(zoom.Shown, Is.Not.Null);
            Assert.That(zoom.Shown!.Face.Id, Is.EqualTo(_director.Model.ActiveEventId));
            banner.OnPointerExit(null!);
            Assert.That(zoom.Shown, Is.Null);
        }

        [Test]
        public void Dragging_a_market_card_marks_the_card_it_would_replace()
        {
            _director.Begin(new MatchSetup(new List<SeatSetup>
            {
                new SeatSetup("Vous", SeatKind.Human),
                new SeatSetup("Bot", SeatKind.Bot, BotLevel.Random),
            }, seed: 8));

            // First market: buy a card, then play on until the next market with that card equipped.
            PlayUntilOffered();
            Assume.That(Controls.CanEndMarket, Is.True);
            Command pick = _director.Session!.LegalCommands(0).First(c => c.Type == CommandType.PickMarket);
            Rect side = CardAnchor.ScreenRectOf((RectTransform)_director.Seats[0].transform);
            Assert.That(Controls.Buy(pick.Slot, pick.MarketIndex, side.center), Is.True);
            for (int frame = 0; frame < 20000 && !(Controls.Offered && Controls.CanEndMarket && Equipped(pick.Slot) != null); frame++)
            {
                _director.Advance(2f);
                if (Controls.Offered && !Controls.CanEndMarket)
                {
                    if (Controls.Decision.gameObject.activeSelf && Controls.Decision.Options.Count > 0)
                    {
                        Controls.Decision.Choose(0);
                    }
                    else
                    {
                        Controls.EndTurn();
                    }
                }
            }

            Assume.That(Equipped(pick.Slot), Is.Not.Null, "The bought card is still equipped at the next market.");
            HashSet<string> sameSlot = new HashSet<string>(Market(pick.Slot).Visible.Select(c => c.CardId));
            Rect market = CardAnchor.ScreenRectOf((RectTransform)Object.FindAnyObjectByType<MarketDisplay>().transform);
            CardDrag dragged = Object.FindObjectsByType<CardDrag>()
                .First(d => market.Contains(d.GetComponent<CardAnchor>().ScreenRect.center) && sameSlot.Contains(d.GetComponent<CardDisplay>().Face.Id));
            CardDisplay mine = Equipped(pick.Slot)!;

            dragged.OnBeginDrag(new PointerEventData(null) { position = market.center });
            Assert.That(dragged.Dragging, Is.True);
            Assert.That(mine.Marked, Is.True, "The card the purchase would replace is marked.");
            Assert.That(dragged.Drop(new Vector2(-1000f, -1000f)), Is.False, "Dropped outside: no purchase.");
            Assert.That(mine.Marked, Is.False);
        }

        [Test]
        public void With_the_ghosts_option_a_wreck_says_it_chooses_the_event()
        {
            _director.Begin(new MatchSetup(
                Enumerable.Range(1, 5).Select(n => new SeatSetup("Bot " + n, SeatKind.Bot, BotLevel.Random)).ToList(),
                seed: 12,
                rules: new RuleOptions(0, 0, true)));
            for (int frame = 0; frame < 100000 && !_director.Model!.Seats.Any(s => s.Eliminated) && !_director.Session!.IsOver; frame++)
            {
                _director.Advance(2f);
            }

            _director.Advance(0f);
            SeatModel? wreck = _director.Model!.Seats.FirstOrDefault(s => s.Eliminated);
            Assume.That(wreck, Is.Not.Null, "Someone was eliminated.");
            var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
            Assert.That(_director.Seats[wreck!.Seat].StatusText, Is.EqualTo(texts.Get(TextKeys.SeatGhost)));
        }

        private CardDisplay? Equipped(CardSlot slot)
        {
            SeatDisplay mine = _director.Seats[_director.Viewer];
            CardDisplay? card = slot == CardSlot.Attack ? mine.AttackCard : mine.DefenseCard;
            return card != null && card.gameObject.activeSelf ? card : null;
        }

        private MarketView Market(CardSlot slot) => slot == CardSlot.Attack ? _director.Session!.View.AttackMarket : _director.Session!.View.DefenseMarket;

        private void PlayUntilOffered()
        {
            for (int frame = 0; frame < 20000 && !Controls.Offered && !_director.Session!.IsOver; frame++)
            {
                _director.Advance(2f);
            }

            Assume.That(Controls.Offered, Is.True, "The person's turn came.");
        }
    }
}
