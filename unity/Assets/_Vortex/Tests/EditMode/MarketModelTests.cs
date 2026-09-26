using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>
    /// The table's markets follow their events (ADR-0014): a decision among newly revealed cards finds them on the table,
    /// while the public view is still the state before the command (ADR-0009). Playtest bug: the market cards offered
    /// after a card recycled a market could be neither touched nor dragged.
    /// </summary>
    public class MarketModelTests
    {
        [TestCase(CardSlot.Attack)]
        [TestCase(CardSlot.Defense)]
        public void Following_a_recycle_and_a_pick_gives_the_engines_markets(CardSlot slot)
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            var seats = Enumerable.Range(0, 3).Select(i => new SeatSetup("Siège " + (i + 1), SeatKind.Human)).ToList();
            var session = new LocalHotSeatSession(content.CreateEngine(), 7UL, seats);

            TableModel model = TableModel.From(session.View, session.Rules);
            Play(model, session.Submit(session.Actor, Command.RecycleMarket(slot)));
            Assert.That(Uids(model, slot), Is.EqualTo(Uids(session, slot)), "After the recycle: the new cards, in order.");

            // The next seat picks the middle card: it leaves its place, a new one comes at the end.
            Play(model, session.Submit(session.Actor, Command.EndTurn()));
            Assume.That(session.LegalCommands(session.Actor).Any(c => c.Type == CommandType.PickMarket && c.Slot == slot && c.MarketIndex == 2), Is.True);
            Play(model, session.Submit(session.Actor, Command.PickMarket(slot, 2)));
            Assert.That(Uids(model, slot), Is.EqualTo(Uids(session, slot)), "After a pick.");
            Assert.That(Market(model, slot).DeckCount, Is.EqualTo(slot == CardSlot.Attack ? session.View.AttackMarket.DeckCount : session.View.DefenseMarket.DeckCount));
        }

        private static void Play(TableModel model, SessionResult result)
        {
            Assert.That(result.Accepted, Is.True, result.Error?.ToString());
            foreach (GameEvent gameEvent in result.Events)
            {
                model.Apply(gameEvent);
            }
        }

        private static Vortex.Core.Projection.MarketView Market(TableModel model, CardSlot slot) => slot == CardSlot.Attack ? model.AttackMarket : model.DefenseMarket;

        private static int[] Uids(TableModel model, CardSlot slot) => Market(model, slot).Visible.Select(c => c.Uid).ToArray();

        private static int[] Uids(LocalHotSeatSession session, CardSlot slot) =>
            (slot == CardSlot.Attack ? session.View.AttackMarket : session.View.DefenseMarket).Visible.Select(c => c.Uid).ToArray();
    }
}
