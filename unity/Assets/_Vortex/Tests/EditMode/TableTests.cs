using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Session;
using Vortex.Core.Bots;
using Vortex.Core.Events;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The table model, the seat layout and the game log, without a scene.</summary>
    public class TableTests
    {
        [Test]
        public void Opponents_sit_in_turn_order_from_left_to_right()
        {
            Assert.That(SeatLayout.Opponents(5, 0), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(SeatLayout.Opponents(5, 2), Is.EqualTo(new[] { 3, 4, 0, 1 }), "The seat playing after the viewer is on the left.");
            Assert.That(SeatLayout.Opponents(2, 1), Is.EqualTo(new[] { 0 }));
            Assert.That(SeatLayout.ArcAngle(0, 4, 10f), Is.EqualTo(170f));
            Assert.That(SeatLayout.ArcAngle(3, 4, 10f), Is.EqualTo(10f));
            Assert.That(SeatLayout.ArcAngle(0, 1, 10f), Is.EqualTo(90f));
        }

        [Test]
        public void The_table_model_follows_every_event_of_a_game_to_the_same_figures_as_the_engine()
        {
            // Five bots play a whole game. After each command, the model patched event by event must show the same
            // figures as a model rebuilt from the engine's view: what the player sees during playback is never wrong.
            // The opening events are not replayed: the session only has the view after them. While a command waits for
            // a decision, the view is still the state before it (ADR-0009): the comparison waits for its completion.
            LocalHotSeatSession session = NewBotGame(5, 11);
            TableModel? patched = null;
            int commands = 0;
            int suspended = 0;
            while (!session.IsOver && commands < 5000)
            {
                patched ??= TableModel.From(session.View, session.Rules);
                SessionResult result = session.PlayBotStep();
                Assert.That(result.Accepted, Is.True);
                foreach (GameEvent gameEvent in result.Events)
                {
                    patched.Apply(gameEvent);
                }

                if (session.Decision is null)
                {
                    AssertSameFigures(patched, TableModel.From(session.View, session.Rules), "command " + commands);
                    patched = null;
                }
                else
                {
                    suspended++;
                }

                commands++;
            }

            Assert.That(suspended, Is.GreaterThan(0), "The game went through decisions, the case that needs care.");

            Assert.That(session.IsOver, Is.True);
        }

        [Test]
        public void The_log_writes_every_logged_event_of_a_game()
        {
            var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            var log = new GameLogFormatter(texts, content.LoadData());
            LocalHotSeatSession session = NewBotGame(5, 12);
            TableModel model = TableModel.From(session.View, session.Rules);
            var lines = new List<string>();
            IEnumerable<GameEvent> events = session.OpeningEvents;
            while (true)
            {
                foreach (GameEvent gameEvent in events)
                {
                    string? line = log.Describe(gameEvent, model.Seats);
                    if (line != null)
                    {
                        Assert.That(line, Does.Not.StartWith("#"), gameEvent.Type.ToString());
                        Assert.That(line, Does.Not.Contain("{"), gameEvent.Type.ToString());
                        lines.Add(line);
                    }
                }

                if (session.IsOver)
                {
                    break;
                }

                events = session.PlayBotStep().Events;
            }

            Assert.That(lines, Is.Not.Empty);
            Assert.That(lines.Last(), Does.StartWith("Fin de partie"));
        }

        [Test]
        public void An_unknown_placeholder_in_a_log_text_shows_a_gap_instead_of_failing()
        {
            var table = UnityEngine.ScriptableObject.CreateInstance<TextTable>();
            table.AddMissing(new[] { new KeyValuePair<string, string>(TextKeys.Log(GameEventType.HpLost), "{0} perd {9} PV.") });
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            var log = new GameLogFormatter(table, content.LoadData());
            string? line = log.Describe(new GameEvent { Type = GameEventType.HpLost, Player = 0, Amount = 3 }, new List<SeatModel>());
            Assert.That(line, Is.EqualTo("#" + TextKeys.Log(GameEventType.HpLost)));
            UnityEngine.Object.DestroyImmediate(table);
        }

        private static LocalHotSeatSession NewBotGame(int players, ulong seed)
        {
            var content = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath);
            List<SeatSetup> seats = Enumerable.Range(1, players).Select(n => new SeatSetup("Bot " + n, SeatKind.Bot, BotLevel.Normal)).ToList();
            return new LocalHotSeatSession(content.CreateEngine(), seed, seats);
        }

        private static void AssertSameFigures(TableModel shown, TableModel engine, string when)
        {
            Assert.That(shown.Round, Is.EqualTo(engine.Round), when + ": round");
            Assert.That(shown.CurrentPlayer, Is.EqualTo(engine.CurrentPlayer), when + ": current player");
            Assert.That(shown.ActiveEventId, Is.EqualTo(engine.ActiveEventId), when + ": event");
            Assert.That(shown.Outcome?.Winner, Is.EqualTo(engine.Outcome?.Winner), when + ": winner");
            for (int i = 0; i < engine.Seats.Count; i++)
            {
                SeatModel a = shown.Seats[i];
                SeatModel b = engine.Seats[i];
                string seat = when + ", seat " + i;
                Assert.That(a.Hp, Is.EqualTo(b.Hp), seat + ": HP");
                Assert.That(a.Shield, Is.EqualTo(b.Shield), seat + ": shield");
                Assert.That(a.Overcharge, Is.EqualTo(b.Overcharge), seat + ": overcharge");
                Assert.That(a.Eliminated, Is.EqualTo(b.Eliminated), seat + ": eliminated");
                Assert.That(a.Technologies, Is.EqualTo(b.Technologies), seat + ": technologies");
            }
        }
    }
}
