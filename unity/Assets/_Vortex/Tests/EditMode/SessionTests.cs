using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using Vortex.Client.Content;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Rules;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The local hot-seat session (ADR-0014): public view only, typed refusals, bots on any seat.</summary>
    public class SessionTests
    {
        private static GameEngine Engine()
        {
            GameContent content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            Assert.That(content, Is.Not.Null, "Run Vortex > Développement > Créer les assets de base manquants.");
            return content.CreateEngine();
        }

        private static IReadOnlyList<SeatSetup> Seats(int humans, int bots)
        {
            return Enumerable.Range(0, humans).Select(i => new SeatSetup("Humain " + (i + 1), SeatKind.Human))
                .Concat(Enumerable.Range(0, bots).Select(i => new SeatSetup("Bot " + (i + 1), SeatKind.Bot, BotLevel.Naive)))
                .ToList();
        }

        [Test]
        public void A_new_session_exposes_the_opening_events_and_who_acts()
        {
            var session = new LocalHotSeatSession(Engine(), 3, Seats(2, 0));
            Assert.That(session.OpeningEvents, Is.Not.Empty);
            Assert.That(session.View.Players, Has.Count.EqualTo(2));
            Assert.That(session.Actor, Is.EqualTo(session.View.CurrentPlayer));
            Assert.That(session.LegalCommands(session.Actor), Is.Not.Empty);
            Assert.That(session.LegalCommands(1 - session.Actor), Is.Empty, "Not the other player's turn.");
            Assert.That(session.IsBotTurn, Is.False);
        }

        [Test]
        public void An_illegal_command_is_refused_and_changes_nothing()
        {
            var session = new LocalHotSeatSession(Engine(), 3, Seats(2, 0));
            string before = JsonConvert.SerializeObject(session.View);
            SessionResult result = session.Submit(1 - session.Actor, Command.EndTurn());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Error!.Code, Is.EqualTo(CommandErrorCode.NotYourTurn));
            Assert.That(result.Events, Is.Empty);
            Assert.That(JsonConvert.SerializeObject(session.View), Is.EqualTo(before));
        }

        [Test]
        public void A_legal_command_updates_the_view_and_returns_its_events()
        {
            var session = new LocalHotSeatSession(Engine(), 3, Seats(2, 0));
            int actor = session.Actor;
            SessionResult result = session.Submit(actor, Command.EndMarket());
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(session.View.Phase, Is.EqualTo(Vortex.Core.State.TurnPhase.Main));
        }

        [Test]
        public void Bots_play_a_whole_game_through_the_session()
        {
            var session = new LocalHotSeatSession(Engine(), 11, Seats(0, 5));
            int steps = 0;
            while (!session.IsOver && steps < 20_000)
            {
                Assert.That(session.IsBotTurn, Is.True);
                Assert.That(session.PlayBotStep().Accepted, Is.True);
                steps++;
            }

            Assert.That(session.IsOver, Is.True);
            Assert.That(session.Actor, Is.EqualTo(-1));
            Assert.That(session.View.Outcome, Is.Not.Null);
        }

        [Test]
        public void A_bot_never_plays_for_a_human()
        {
            var session = new LocalHotSeatSession(Engine(), 5, new[] { new SeatSetup("Humain", SeatKind.Human), new SeatSetup("Bot", SeatKind.Bot, BotLevel.Naive) });
            Assert.That(session.IsBot(0), Is.False);
            Assert.That(session.IsBot(1), Is.True);
            if (!session.IsBotTurn)
            {
                Assert.That(() => session.PlayBotStep(), Throws.InvalidOperationException);
            }
        }
    }
}
