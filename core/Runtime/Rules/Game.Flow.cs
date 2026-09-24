using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Effects;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Core.Rules
{
    /// <summary>Setup, rounds and turns (RULES A3, A4, A5.1, A5.5).</summary>
    internal sealed partial class Game
    {
        /// <summary>Builds decks, markets and ships, rolls initiative and starts round 1 (RULES A3).</summary>
        public void Setup(IReadOnlyList<string> playerNames)
        {
            PlayerCountSettings settings = Config.ForPlayers(playerNames.Count)
                ?? throw new EngineException("Unsupported number of players: " + playerNames.Count + ".");

            for (int seat = 0; seat < playerNames.Count; seat++)
            {
                State.Players.Add(new PlayerState
                {
                    Seat = seat,
                    Name = playerNames[seat],
                    Hp = Config.StartingHp,
                    Shield = settings.StartShield,
                });
            }

            Emit(new GameEvent { Type = GameEventType.GameStarted, Amount = playerNames.Count });

            foreach (CardDefinition def in Data.Modifiers)
            {
                for (int i = 0; i < def.Copies; i++)
                {
                    State.Market(def.Slot).Deck.Add(new CardInstance { Uid = State.NextUid++, CardId = def.Id });
                }
            }

            Shuffle(State.AttackMarket.Deck);
            Shuffle(State.DefenseMarket.Deck);
            RefillMarket(CardSlot.Attack);
            RefillMarket(CardSlot.Defense);

            foreach (EventDefinition evt in Data.Events)
            {
                if (evt.Id == Config.DoomEventId)
                {
                    continue; // kept out of the deck (RULES A3.2)
                }

                for (int i = 0; i < evt.Copies; i++)
                {
                    State.EventDeck.Add(evt.Id);
                }
            }

            Shuffle(State.EventDeck);

            State.InitiativeSeat = RollInitiative(Enumerable.Range(0, playerNames.Count).ToList());
            State.CurrentPlayer = State.InitiativeSeat;
            Emit(new GameEvent { Type = GameEventType.InitiativeWon, Player = State.InitiativeSeat });

            BeginRound();
            if (!IsOver)
            {
                BeginTurn(FirstSeatOfRound());
            }
        }

        // Highest d8 wins; only tied players reroll (RULES A3.4).
        private int RollInitiative(List<int> contenders)
        {
            while (true)
            {
                var rolls = new Dictionary<int, int>();
                foreach (int seat in contenders)
                {
                    int v = NextDie();
                    rolls[seat] = v;
                    Emit(new GameEvent { Type = GameEventType.InitiativeRolled, Player = seat, Value = v });
                }

                int best = rolls.Values.Max();
                List<int> top = contenders.Where(s => rolls[s] == best).ToList();
                if (top.Count == 1)
                {
                    return top[0];
                }

                contenders = top;
            }
        }

        /// <summary>Starts a new round: expiries, then the round's event or the doom event (RULES A4).</summary>
        public void BeginRound()
        {
            State.Round++;
            State.PlayedThisRound.Clear();
            RemoveStatuses(s => s.Expiry == StatusExpiry.EndOfRound);
            if (State.ActiveEventId != null)
            {
                if (State.ActiveEventId != Config.DoomEventId)
                {
                    State.EventDiscard.Add(State.ActiveEventId);
                }

                State.ActiveEventId = null;
            }

            Emit(new GameEvent { Type = GameEventType.RoundStarted, Value = State.Round });

            string? eventId = null;
            PlayerCountSettings settings = Config.ForPlayers(State.Players.Count)!;
            if (State.Round == settings.DoomRound)
            {
                eventId = Config.DoomEventId;
            }
            else if (Config.EventFrequency > 0 && (State.Round - 1) % Config.EventFrequency == 0)
            {
                eventId = DrawEvent();
                int ghost = Config.GhostsChooseEvent ? GhostChoosingEvent() : -1;
                string? other = eventId != null && ghost >= 0 ? DrawEvent() : null;
                if (other != null)
                {
                    eventId = GhostChoosesEvent(ghost, eventId!, other);
                }
            }

            if (eventId != null)
            {
                State.ActiveEventId = eventId;
                Emit(new GameEvent { Type = GameEventType.EventRevealed, Id = eventId });
                var source = new EffectSource(EffectOrigin.Event, eventId, -1);
                foreach (Effect effect in Catalog.ForEvent(eventId))
                {
                    if (effect.IsActivation && !IsOver)
                    {
                        effect.Activate(this, source);
                    }
                }
            }

            Raise((e, s) => e.OnRoundStart(this, s));
            CheckEliminations();
        }

        // Eliminated players take turns, in seat order, to choose the event (RULES A4.1); -1 if nobody is eliminated.
        private int GhostChoosingEvent()
        {
            List<int> ghosts = State.Players.Where(p => p.Eliminated).Select(p => p.Seat).ToList();
            return ghosts.Count == 0 ? -1 : ghosts[State.Round % ghosts.Count];
        }

        // The ghost keeps one of the two drawn events; the other goes to the event discard pile.
        private string GhostChoosesEvent(int ghost, string first, string second)
        {
            var options = new List<Decisions.DecisionOption>
            {
                new Decisions.DecisionOption { Key = "first", ContentId = first },
                new Decisions.DecisionOption { Key = "second", ContentId = second },
            };
            string key = Ask(ghost, Decisions.DecisionKind.ChooseOption, "ghost.event", null, options);
            string kept = key == "first" ? first : second;
            string aside = key == "first" ? second : first;
            State.EventDiscard.Add(aside);
            Emit(new GameEvent { Type = GameEventType.EventSetAside, Player = ghost, Id = aside });
            return kept;
        }

        private string? DrawEvent()
        {
            if (State.EventDeck.Count == 0)
            {
                State.EventDeck.AddRange(State.EventDiscard);
                State.EventDiscard.Clear();
                Shuffle(State.EventDeck);
            }

            if (State.EventDeck.Count == 0)
            {
                return null;
            }

            string id = State.EventDeck[State.EventDeck.Count - 1];
            State.EventDeck.RemoveAt(State.EventDeck.Count - 1);
            return id;
        }

        /// <summary>
        /// First player of the current round (RULES A4.4): the initiative seat, moved by the configured rotation
        /// once per round, then the first alive seat clockwise from there. With two players left, nobody plays twice
        /// in a row (ARB-68): the round starts with the one who did not play last.
        /// </summary>
        public int FirstSeatOfRound()
        {
            int n = State.Players.Count;
            int last = State.CurrentPlayer;
            if (State.Round > 1 && last >= 0 && last < n && !State.Players[last].Eliminated && AliveFrom(0).Count() == 2)
            {
                return AliveFrom(last + 1).First();
            }

            int step = Config.RoundStartRotation == RoundStartRotation.Clockwise ? 1 : (Config.RoundStartRotation == RoundStartRotation.CounterClockwise ? -1 : 0);
            int offset = (step * (State.Round - 1)) % n;
            return AliveFrom(((State.InitiativeSeat + offset) % n + n) % n).First();
        }

        /// <summary>Starts a player's turn (RULES A5.1). Skips to the next player if they die during it.</summary>
        public void BeginTurn(int seat)
        {
            State.CurrentPlayer = seat;
            State.PlayedThisRound.Add(seat);
            State.CrewActionsTaken.Clear();
            State.PickedThisTurn = false;

            RemoveStatuses(s => s.IsActive && s.Expiry == StatusExpiry.StartOfTurn && s.ExpiryPlayer == seat);
            foreach (StatusState s in AllStatuses().Where(s => s.DormantUntilTurnOf == seat))
            {
                s.DormantUntilTurnOf = -1;
            }

            Emit(new GameEvent { Type = GameEventType.TurnStarted, Player = seat, Value = State.Round });
            ApplyTorments(seat, null);
            Raise((e, s) => e.OnTurnStart(this, s, seat));
            CheckEliminations();
            if (IsOver)
            {
                return;
            }

            if (State.Players[seat].Eliminated)
            {
                Advance();
                return;
            }

            State.Phase = TurnPhase.Market;
            State.MarketPicksLeft = Math.Max(0, Calculate(1, (e, s, m) => e.ModifyMarketPicks(this, s, seat, m)));
        }

        /// <summary>Ends the market phase (RULES A5.2).</summary>
        public void EndMarketPhase()
        {
            int seat = State.CurrentPlayer;
            State.Phase = TurnPhase.Main;
            State.MarketPicksLeft = 0;
            Emit(new GameEvent { Type = GameEventType.MarketEnded, Player = seat });
            Raise((e, s) => e.OnMarketEnd(this, s, seat));
            CheckEliminations();
        }

        /// <summary>Ends the current turn and starts the next one (RULES A5.5).</summary>
        public void EndTurn()
        {
            int seat = State.CurrentPlayer;
            Raise((e, s) => e.OnTurnEnd(this, s, seat));
            RemoveStatuses(s => s.IsActive && s.Expiry == StatusExpiry.EndOfTurn && s.ExpiryPlayer == seat);
            Emit(new GameEvent { Type = GameEventType.TurnEnded, Player = seat });
            CheckEliminations();
            if (!IsOver)
            {
                Advance();
            }
        }

        // Next alive player clockwise; a new round starts when that player already played this round.
        private void Advance()
        {
            // AliveFrom(current + 1) lists the other alive players first; the game is over when none is left.
            int next = AliveFrom(State.CurrentPlayer + 1).First();
            if (State.PlayedThisRound.Contains(next))
            {
                BeginRound();
                if (IsOver)
                {
                    return;
                }

                next = FirstSeatOfRound();
            }

            BeginTurn(next);
        }

        private List<StatusState> AllStatuses()
        {
            return State.GlobalStatuses.Concat(State.Players.SelectMany(p => p.Statuses)).ToList();
        }
    }
}
