using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Effects;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Core.Rules
{
    /// <summary>
    /// Resolution context for one command: the mutable state being worked on plus everything effects may
    /// use (RULES B3 elementary actions, decisions, dice, events). Created per command run and discarded.
    /// </summary>
    /// <remarks>
    /// Split in partial files: this one (infrastructure), <c>Game.Actions.cs</c> (elementary actions),
    /// <c>Game.Flow.cs</c> (rounds and turns), <c>Game.Commands.cs</c> (validation and execution),
    /// <c>Game.Attack.cs</c> (attack pipeline).
    /// </remarks>
    internal sealed partial class Game
    {
        private readonly IReadOnlyList<string> _answers;
        private readonly Dictionary<int, (int Source, HpLossCause Cause)> _lastLoss = new Dictionary<int, (int, HpLossCause)>();
        private int _answerIndex;
        private int _depth;

        public Game(GameState state, GameData data, GameConfig config, IEffectCatalog catalog, IReadOnlyList<string> answers)
        {
            State = state;
            Data = data;
            Config = config;
            Catalog = catalog;
            _answers = answers;
            Cards = data.Modifiers.ToDictionary(c => c.Id, StringComparer.Ordinal);
        }

        public GameState State { get; }

        public GameData Data { get; }

        public GameConfig Config { get; }

        public IEffectCatalog Catalog { get; }

        /// <summary>Card definitions by id.</summary>
        public IReadOnlyDictionary<string, CardDefinition> Cards { get; }

        /// <summary>Events emitted so far by this run.</summary>
        public List<GameEvent> Events { get; } = new List<GameEvent>();

        /// <summary>
        /// Test-only die source (scripted scenarios). Production runs leave it null so every roll comes from the
        /// state's generator, which keeps re-execution deterministic (ADR-0009).
        /// </summary>
        public IDiceSource? DiceOverride { get; set; }

        /// <summary>True once the game has ended.</summary>
        public bool IsOver => State.Outcome != null;

        public PlayerState Player(int seat) => State.Players[seat];

        public CardDefinition Definition(CardInstance card) => Cards[card.CardId];

        // ---------------------------------------------------------------- Players and seats

        /// <summary>Alive seats in clockwise order starting from <paramref name="from"/> (included).</summary>
        public IEnumerable<int> AliveFrom(int from)
        {
            int n = State.Players.Count;
            for (int i = 0; i < n; i++)
            {
                int seat = (from + i) % n;
                if (!State.Players[seat].Eliminated)
                {
                    yield return seat;
                }
            }
        }

        /// <summary>Alive seats, starting from the current player (resolution order, RULES B7).</summary>
        public IEnumerable<int> AliveInTurnOrder() => AliveFrom(State.CurrentPlayer);

        /// <summary>Alive opponents of <paramref name="seat"/>, clockwise from its left.</summary>
        public IEnumerable<int> Opponents(int seat) => AliveFrom(seat + 1).Where(s => s != seat);

        /// <summary>Nearest alive seat clockwise (+1) or counter-clockwise (-1), or -1 if alone.</summary>
        public int Neighbour(int seat, int direction)
        {
            int n = State.Players.Count;
            for (int i = 1; i < n; i++)
            {
                int s = ((seat + (direction * i)) % n + n) % n;
                if (!State.Players[s].Eliminated)
                {
                    return s;
                }
            }

            return -1;
        }

        // ---------------------------------------------------------------- Events and dice

        public void Emit(GameEvent e)
        {
            Events.Add(e);
        }

        /// <summary>Rolls one die for an effect or a rule; the roll is public.</summary>
        public int RollDie(int player, string? sourceId)
        {
            int v = NextDie();
            Emit(new GameEvent { Type = GameEventType.DieRolled, Player = player, Value = v, Id = sourceId });
            return v;
        }

        /// <summary>Next die result: the test override if any, else the state's generator.</summary>
        public int NextDie()
        {
            return DiceOverride?.Next(Config.DieFaces) ?? State.Rng.Roll(Config.DieFaces);
        }

        // ---------------------------------------------------------------- Decisions (ADR-0009)

        /// <summary>
        /// Asks <paramref name="player"/> to pick one option. A single option is chosen automatically.
        /// During a replay the recorded answer is returned; otherwise the run is suspended.
        /// </summary>
        public string Ask(int player, DecisionKind kind, string prompt, string? sourceId, IReadOnlyList<DecisionOption> options)
        {
            if (options.Count == 0)
            {
                throw new EngineException("Decision '" + prompt + "' has no option: the caller must check first.");
            }

            if (options.Count == 1)
            {
                return options[0].Key;
            }

            var request = new DecisionRequest
            {
                Player = player,
                Kind = kind,
                Prompt = prompt,
                SourceId = sourceId,
                Options = options.ToList(),
            };

            if (_answerIndex < _answers.Count)
            {
                string answer = _answers[_answerIndex++];
                if (!request.Accepts(answer))
                {
                    throw new EngineException("Replayed answer '" + answer + "' is not valid for '" + prompt + "': resolution is not deterministic.");
                }

                return answer;
            }

            if (_answerIndex >= Config.MaxDecisionsPerCommand)
            {
                throw new EngineException("Too many decisions for a single command.");
            }

            request.Id = State.CommandCount.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + _answerIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            throw new DecisionNeededException(request);
        }

        /// <summary>Asks for a player among <paramref name="candidates"/>; with <paramref name="optional"/>, "none" is allowed (returns -1).</summary>
        public int AskPlayer(int player, string prompt, string? sourceId, IEnumerable<int> candidates, bool optional = false)
        {
            var options = candidates.Select(p => new DecisionOption { Key = "p" + p, Player = p }).ToList();
            if (optional)
            {
                options.Insert(0, new DecisionOption { Key = "none" });
            }

            if (options.Count == 0)
            {
                return -1;
            }

            string key = Ask(player, DecisionKind.ChoosePlayer, prompt, sourceId, options);
            return key == "none" ? -1 : options.First(o => o.Key == key).Player;
        }

        /// <summary>Asks for a card among <paramref name="cards"/>; with <paramref name="optional"/>, returns null for "none".</summary>
        public CardInstance? AskCard(int player, DecisionKind kind, string prompt, string? sourceId, IReadOnlyList<CardInstance> cards, bool optional = false)
        {
            var options = cards.Select(c => new DecisionOption { Key = "c" + c.Uid, CardUid = c.Uid }).ToList();
            if (optional)
            {
                options.Insert(0, new DecisionOption { Key = "none" });
            }

            if (options.Count == 0)
            {
                return null;
            }

            string key = Ask(player, kind, prompt, sourceId, options);
            return key == "none" ? null : cards.First(c => "c" + c.Uid == key);
        }

        /// <summary>Asks for a number in [min, max].</summary>
        public int AskNumber(int player, string prompt, string? sourceId, int min, int max)
        {
            if (max < min)
            {
                throw new EngineException("Empty number range for '" + prompt + "'.");
            }

            var options = Enumerable.Range(min, max - min + 1).Select(n => new DecisionOption { Key = "n" + n, Number = n }).ToList();
            string key = Ask(player, DecisionKind.ChooseNumber, prompt, sourceId, options);
            return options.First(o => o.Key == key).Number;
        }

        /// <summary>Asks a yes/no question.</summary>
        public bool AskYesNo(int player, string prompt, string? sourceId)
        {
            var options = new List<DecisionOption> { new DecisionOption { Key = "yes" }, new DecisionOption { Key = "no" } };
            return Ask(player, DecisionKind.YesNo, prompt, sourceId, options) == "yes";
        }

        // ---------------------------------------------------------------- Effect traversal (RULES B7)

        /// <summary>
        /// Active effects in resolution order: global ones (active event, then global statuses), then each
        /// alive player from the current one clockwise: ATK slot, DEF slot, then statuses in creation order.
        /// Activation-only effects are not listed. The list is a snapshot.
        /// </summary>
        public List<KeyValuePair<Effect, EffectSource>> ActiveEffects()
        {
            var list = new List<KeyValuePair<Effect, EffectSource>>();
            if (State.ActiveEventId != null)
            {
                var src = new EffectSource(EffectOrigin.Event, State.ActiveEventId, -1);
                foreach (Effect e in Catalog.ForEvent(State.ActiveEventId))
                {
                    if (!e.IsActivation)
                    {
                        list.Add(new KeyValuePair<Effect, EffectSource>(e, src));
                    }
                }
            }

            AddStatuses(list, State.GlobalStatuses, -1);

            foreach (int seat in AliveInTurnOrder())
            {
                PlayerState p = State.Players[seat];
                foreach (CardInstance card in p.Modifiers())
                {
                    var src = new EffectSource(EffectOrigin.Card, card.CardId, seat, cardUid: card.Uid);
                    foreach (Effect e in Catalog.ForCard(card.CardId))
                    {
                        if (!e.IsActivation)
                        {
                            list.Add(new KeyValuePair<Effect, EffectSource>(e, src));
                        }
                    }
                }

                AddStatuses(list, p.Statuses, seat);
            }

            return list;
        }

        /// <summary>True while the effect instance still exists (it may disappear during a reaction chain).</summary>
        public bool IsStillActive(EffectSource source)
        {
            switch (source.Origin)
            {
                case EffectOrigin.Event:
                    return State.ActiveEventId == source.Id;
                case EffectOrigin.Card:
                    PlayerState holder = State.Players[source.Holder];
                    return !holder.Eliminated && holder.Modifiers().Any(c => c.Uid == source.CardUid);
                case EffectOrigin.Status:
                    return FindStatus(source.StatusUid) is StatusState s && s.IsActive;
                default:
                    return true;
            }
        }

        /// <summary>Reaction broadcast (RULES B2.3, B7). Effects equal to <paramref name="cause"/> are skipped.</summary>
        public void Raise(Action<Effect, EffectSource> reaction, EffectSource? cause = null)
        {
            if (IsOver)
            {
                return;
            }

            _depth++;
            try
            {
                if (_depth > Config.ReactionDepthLimit)
                {
                    throw new EngineException("Reaction chain deeper than " + Config.ReactionDepthLimit + ": card design loop (RULES B7).");
                }

                foreach (KeyValuePair<Effect, EffectSource> pair in ActiveEffects())
                {
                    if (IsOver)
                    {
                        return;
                    }

                    if ((cause != null && pair.Value.Equals(cause)) || !IsStillActive(pair.Value))
                    {
                        continue;
                    }

                    reaction(pair.Key, pair.Value);
                }
            }
            finally
            {
                _depth--;
            }
        }

        /// <summary>Calculation: every active effect registers its modifications; returns the stacked result (RULES B4).</summary>
        public int Calculate(int baseValue, Action<Effect, EffectSource, ValueModifiers> calculation)
        {
            var modifiers = new ValueModifiers();
            foreach (KeyValuePair<Effect, EffectSource> pair in ActiveEffects())
            {
                calculation(pair.Key, pair.Value, modifiers);
            }

            var actions = new List<Action>();
            int result = modifiers.Apply(baseValue, actions);
            foreach (Action a in actions)
            {
                a();
            }

            return result;
        }

        /// <summary>Permission: false as soon as one active effect refuses (RULES B4).</summary>
        public bool Permits(Func<Effect, EffectSource, bool> permission)
        {
            foreach (KeyValuePair<Effect, EffectSource> pair in ActiveEffects())
            {
                if (!permission(pair.Key, pair.Value))
                {
                    return false;
                }
            }

            return true;
        }

        // ---------------------------------------------------------------- Statuses

        public StatusState? FindStatus(int uid)
        {
            foreach (StatusState s in State.GlobalStatuses)
            {
                if (s.Uid == uid)
                {
                    return s;
                }
            }

            foreach (PlayerState p in State.Players)
            {
                foreach (StatusState s in p.Statuses)
                {
                    if (s.Uid == uid)
                    {
                        return s;
                    }
                }
            }

            return null;
        }

        /// <summary>Status list of a holder (-1: global).</summary>
        public List<StatusState> StatusesOf(int holder) => holder < 0 ? State.GlobalStatuses : State.Players[holder].Statuses;

        /// <summary>Creates a status (RULES B5). Its behaviour is <see cref="IEffectCatalog.ForStatus"/>(<paramref name="kind"/>).</summary>
        public StatusState AddStatus(int holder, string kind, int owner, StatusExpiry expiry, int expiryPlayer, bool dormantUntilExpiryPlayersTurn = false, IEnumerable<KeyValuePair<string, int>>? vars = null)
        {
            Catalog.ForStatus(kind); // fails fast on unknown kinds
            var status = new StatusState
            {
                Uid = State.NextUid++,
                Kind = kind,
                Owner = owner,
                Expiry = expiry,
                ExpiryPlayer = expiryPlayer,
                DormantUntilTurnOf = dormantUntilExpiryPlayersTurn ? expiryPlayer : -1,
            };
            if (vars != null)
            {
                foreach (KeyValuePair<string, int> v in vars)
                {
                    status.Vars[v.Key] = v.Value;
                }
            }

            StatusesOf(holder).Add(status);
            Emit(new GameEvent { Type = GameEventType.StatusAdded, Player = holder, Other = owner, Id = kind });
            return status;
        }

        public void RemoveStatus(int holder, StatusState status)
        {
            if (StatusesOf(holder).Remove(status))
            {
                Emit(new GameEvent { Type = GameEventType.StatusEnded, Player = holder, Id = status.Kind });
            }
        }

        /// <summary>Removes the statuses of every holder that match <paramref name="predicate"/>.</summary>
        public void RemoveStatuses(Func<StatusState, bool> predicate)
        {
            foreach (StatusState s in State.GlobalStatuses.Where(predicate).ToList())
            {
                RemoveStatus(-1, s);
            }

            foreach (PlayerState p in State.Players)
            {
                foreach (StatusState s in p.Statuses.Where(predicate).ToList())
                {
                    RemoveStatus(p.Seat, s);
                }
            }
        }

        private void AddStatuses(List<KeyValuePair<Effect, EffectSource>> list, List<StatusState> statuses, int holder)
        {
            foreach (StatusState s in statuses)
            {
                if (s.IsActive)
                {
                    list.Add(new KeyValuePair<Effect, EffectSource>(Catalog.ForStatus(s.Kind), new EffectSource(EffectOrigin.Status, s.Kind, holder, statusUid: s.Uid)));
                }
            }
        }
    }

    /// <summary>Source of die results that replaces the generator in scripted tests.</summary>
    internal interface IDiceSource
    {
        /// <summary>Next result in 1..faces.</summary>
        int Next(int faces);
    }

    /// <summary>Thrown to suspend a run that needs a player's decision (control flow of ADR-0009, never escapes the engine).</summary>
    internal sealed class DecisionNeededException : Exception
    {
        public DecisionNeededException(DecisionRequest request)
            : base("Decision needed: " + request.Prompt)
        {
            Request = request;
        }

        public DecisionNeededException()
        {
            Request = new DecisionRequest();
        }

        public DecisionNeededException(string message)
            : base(message)
        {
            Request = new DecisionRequest();
        }

        public DecisionNeededException(string message, Exception innerException)
            : base(message, innerException)
        {
            Request = new DecisionRequest();
        }

        public DecisionRequest Request { get; }
    }

    /// <summary>
    /// An engine invariant was broken (a bug in the engine or in content, e.g. a reaction loop).
    /// Never raised for an illegal command: those are rejected with a <see cref="CommandError"/>.
    /// </summary>
    public sealed class EngineException : Exception
    {
        /// <summary>Creates the exception.</summary>
        public EngineException(string message)
            : base(message)
        {
        }

        /// <summary>Creates the exception with a cause.</summary>
        public EngineException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>Creates the exception.</summary>
        public EngineException()
        {
        }
    }
}
