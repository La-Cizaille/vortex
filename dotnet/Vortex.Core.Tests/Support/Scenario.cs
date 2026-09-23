using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Vortex.Core.Commands;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Tests.Support
{
    /// <summary>Builds and drives a test game on synthetic content.</summary>
    internal sealed class Scenario
    {
        private Scenario(GameData data, GameConfig config, TestCatalog catalog, GameState state)
        {
            Data = data;
            Config = config;
            Catalog = catalog;
            Engine = new GameEngine(data, config, catalog);
            State = state;
        }

        public GameData Data { get; }

        public GameConfig Config { get; }

        public TestCatalog Catalog { get; }

        public GameEngine Engine { get; }

        public GameState State { get; set; }

        public static Scenario Start(int players = 2, TestCatalog? catalog = null, GameConfig? config = null, ulong seed = 1)
        {
            GameData data = TestContent.Data();
            config ??= TestContent.Config();
            catalog ??= new TestCatalog();
            var engine = new GameEngine(data, config, catalog);
            EngineResult result = engine.NewGame(seed, Enumerable.Range(0, players).Select(i => "P" + i).ToList());
            return new Scenario(data, config, catalog, result.State);
        }

        public int Current => State.CurrentPlayer;

        public PlayerState P(int seat) => State.Players[seat];

        /// <summary>Puts a copy of <paramref name="cardId"/> (taken from a pile or market) into the player's slot.</summary>
        public CardInstance Equip(int seat, string cardId)
        {
            foreach (MarketState m in new[] { State.AttackMarket, State.DefenseMarket })
            {
                foreach (List<CardInstance> pile in new[] { m.Deck, m.Discard, m.Visible })
                {
                    CardInstance? card = pile.FirstOrDefault(c => c.CardId == cardId);
                    if (card == null)
                    {
                        continue;
                    }

                    pile.Remove(card);
                    PlayerState p = P(seat);
                    CardSlot slot = cardId.StartsWith("A_", StringComparison.Ordinal) ? CardSlot.Attack : CardSlot.Defense;
                    CardInstance? old = p.Slot(slot);
                    if (old != null)
                    {
                        m.Discard.Add(old);
                    }

                    if (slot == CardSlot.Attack)
                    {
                        p.AttackSlot = card;
                    }
                    else
                    {
                        p.DefenseSlot = card;
                    }

                    if (pile == m.Visible)
                    {
                        m.Visible.Add(m.Deck[m.Deck.Count - 1]);
                        m.Deck.RemoveAt(m.Deck.Count - 1);
                    }

                    return card;
                }
            }

            throw new InvalidOperationException("No free copy of " + cardId);
        }

        /// <summary>Game-level access for scripted scenarios (mutates <see cref="State"/> directly).</summary>
        public Game Game(params int[] dice)
        {
            return new Game(State, Data, Config, Catalog, Array.Empty<string>()) { DiceOverride = dice.Length > 0 ? new ScriptedDice(dice) : null };
        }

        public EngineResult Submit(int seat, Command command)
        {
            EngineResult result = Engine.Submit(State, seat, command);
            State = result.State;
            return result;
        }

        public EngineResult MustAccept(int seat, Command command)
        {
            EngineResult result = Submit(seat, command);
            Assert.That(result.Accepted, Is.True, () => "Rejected " + command + ": " + result.Error);
            AssertValid();
            return result;
        }

        /// <summary>Ends the market phase of the current player if needed.</summary>
        public Scenario ToMain()
        {
            if (State.Phase == TurnPhase.Market)
            {
                MustAccept(Current, Command.EndMarket());
            }

            return this;
        }

        /// <summary>Ends turns until it is <paramref name="seat"/>'s main phase.</summary>
        public Scenario TurnOf(int seat)
        {
            for (int guard = 0; guard < 20 && (Current != seat || State.Phase != TurnPhase.Main); guard++)
            {
                ToMain();
                if (Current != seat)
                {
                    MustAccept(Current, Command.EndTurn());
                }
            }

            Assert.That(Current, Is.EqualTo(seat));
            return this;
        }

        public void AssertValid()
        {
            IReadOnlyList<string> errors = Engine.ValidateState(State);
            Assert.That(errors, Is.Empty, () => string.Join("\n", errors));
        }

        public static string Json(object value) => JsonConvert.SerializeObject(value, Formatting.None);
    }
}
