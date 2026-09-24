using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vortex.Core.Bots;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Tests.EditMode
{
    /// <summary>
    /// The rules engine package runs inside Unity with the real content (ADR-0002): same code, same data,
    /// and the same results as in the .NET test suite.
    /// </summary>
    public class CoreInUnityTests
    {
        private const string DataFolder = "Packages/com.vortex.core/Runtime/Data/";

        [Test]
        public void The_real_content_loads_in_unity()
        {
            (GameData data, GameConfig config) = LoadContent();
            Assert.That(data.Modifiers.Where(c => c.Slot == CardSlot.Attack).Sum(c => c.Copies), Is.EqualTo(54));
            Assert.That(data.Technologies, Has.Count.EqualTo(4));
            Assert.That(config.ForPlayers(5), Is.Not.Null, "5 players is the standard table (ARB-52).");
        }

        [Test]
        public void A_full_bot_game_plays_in_unity()
        {
            (GameData data, GameConfig config) = LoadContent();
            var engine = new GameEngine(data, config);
            var bots = Enumerable.Range(0, 5).Select(seat => BotFactory.Create(BotLevel.Naive, (ulong)(100 + seat))).ToList();
            GameState state = engine.NewGame(7, bots.Select((_, i) => "Bot" + i).ToList()).State;

            for (int i = 0; i < 20_000 && state.Outcome == null; i++)
            {
                int actor = state.Pending?.Decision.Player ?? state.CurrentPlayer;
                IReadOnlyList<Command> legal = engine.LegalCommands(state, actor);
                EngineResult result = engine.Submit(state, actor, bots[actor].Choose(engine, state, actor, legal));
                Assert.That(result.Accepted, Is.True, () => "Rejected: " + result.Error);
                state = result.State;
            }

            Assert.That(state.Outcome, Is.Not.Null, "The game ends.");
            Assert.That(engine.ValidateState(state), Is.Empty);
        }

        private static (GameData Data, GameConfig Config) LoadContent()
        {
            GameData data = GameDataLoader.Load(Text(CardsFile.FileName), Text(EventsFile.FileName), Text(TechnologiesFile.FileName));
            return (data, GameDataLoader.LoadConfig(Text(GameConfig.FileName), data));
        }

        private static string Text(string file)
        {
            TextAsset? asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DataFolder + file);
            Assert.That(asset, Is.Not.Null, "Content file not imported: " + file);
            return asset!.text;
        }
    }
}
