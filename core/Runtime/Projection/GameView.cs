using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.State;

namespace Vortex.Core.Projection
{
    /// <summary>
    /// Public view of a game: everything any player may see, and nothing else (docs/SECURITY.md,
    /// "fuite d'information"). Deck order and the random generator state are never included, only deck sizes.
    /// Clients (Unity, network) work exclusively on this view.
    /// </summary>
    public sealed class GameView
    {
        private GameView()
        {
        }

        /// <summary>Round number.</summary>
        public int Round { get; private set; }

        /// <summary>Seat whose turn it is.</summary>
        public int CurrentPlayer { get; private set; }

        /// <summary>Seat that starts each round.</summary>
        public int InitiativeSeat { get; private set; }

        /// <summary>Current phase.</summary>
        public TurnPhase Phase { get; private set; }

        /// <summary>Market picks left this turn.</summary>
        public int MarketPicksLeft { get; private set; }

        /// <summary>True once a market card was taken this turn.</summary>
        public bool PickedThisTurn { get; private set; }

        /// <summary>Crew actions already performed this turn.</summary>
        public IReadOnlyList<CrewAction> CrewActionsTaken { get; private set; } = Array.Empty<CrewAction>();

        /// <summary>Players by seat.</summary>
        public IReadOnlyList<PlayerView> Players { get; private set; } = Array.Empty<PlayerView>();

        /// <summary>Attack market (visible cards and pile sizes).</summary>
        public MarketView AttackMarket { get; private set; } = new MarketView();

        /// <summary>Defense market.</summary>
        public MarketView DefenseMarket { get; private set; } = new MarketView();

        /// <summary>Event active this round, if any.</summary>
        public string? ActiveEventId { get; private set; }

        /// <summary>Cards left in the event pile.</summary>
        public int EventDeckCount { get; private set; }

        /// <summary>Global statuses (kinds), in creation order.</summary>
        public IReadOnlyList<StatusView> GlobalStatuses { get; private set; } = Array.Empty<StatusView>();

        /// <summary>Decision awaited, if any (public: every player sees who must choose what).</summary>
        public DecisionRequest? PendingDecision { get; private set; }

        /// <summary>Result once the game is over.</summary>
        public GameOutcome? Outcome { get; private set; }

        /// <summary>Builds the view of a state.</summary>
        public static GameView Of(GameState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new GameView
            {
                Round = state.Round,
                CurrentPlayer = state.CurrentPlayer,
                InitiativeSeat = state.InitiativeSeat,
                Phase = state.Phase,
                MarketPicksLeft = state.MarketPicksLeft,
                PickedThisTurn = state.PickedThisTurn,
                CrewActionsTaken = state.CrewActionsTaken.ToList(),
                Players = state.Players.Select(PlayerView.Of).ToList(),
                AttackMarket = MarketView.Of(state.AttackMarket),
                DefenseMarket = MarketView.Of(state.DefenseMarket),
                ActiveEventId = state.ActiveEventId,
                EventDeckCount = state.EventDeck.Count,
                GlobalStatuses = state.GlobalStatuses.Select(StatusView.Of).ToList(),
                PendingDecision = state.Pending?.Decision.Clone(),
                Outcome = state.Outcome?.Clone(),
            };
        }

        /// <summary>Market for a slot.</summary>
        public MarketView Market(CardSlot slot) => slot == CardSlot.Attack ? AttackMarket : DefenseMarket;
    }

    /// <summary>Public view of a player.</summary>
    public sealed class PlayerView
    {
        private PlayerView()
        {
        }

        /// <summary>Seat.</summary>
        public int Seat { get; private set; }

        /// <summary>Display name.</summary>
        public string Name { get; private set; } = string.Empty;

        /// <summary>Hit points.</summary>
        public int Hp { get; private set; }

        /// <summary>Stored shield value (effective value may be lower under a bound).</summary>
        public int Shield { get; private set; }

        /// <summary>Overcharge tokens.</summary>
        public int Overcharge { get; private set; }

        /// <summary>Equipped attack modifier.</summary>
        public CardView? AttackSlot { get; private set; }

        /// <summary>Equipped defense modifier.</summary>
        public CardView? DefenseSlot { get; private set; }

        /// <summary>Technologies obtained.</summary>
        public IReadOnlyList<TechColor> Technologies { get; private set; } = Array.Empty<TechColor>();

        /// <summary>Statuses held.</summary>
        public IReadOnlyList<StatusView> Statuses { get; private set; } = Array.Empty<StatusView>();

        /// <summary>True once eliminated.</summary>
        public bool Eliminated { get; private set; }

        internal static PlayerView Of(PlayerState p)
        {
            return new PlayerView
            {
                Seat = p.Seat,
                Name = p.Name,
                Hp = p.Hp,
                Shield = p.Shield,
                Overcharge = p.Overcharge,
                AttackSlot = p.AttackSlot == null ? null : CardView.Of(p.AttackSlot),
                DefenseSlot = p.DefenseSlot == null ? null : CardView.Of(p.DefenseSlot),
                Technologies = p.Technologies.ToList(),
                Statuses = p.Statuses.Select(StatusView.Of).ToList(),
                Eliminated = p.Eliminated,
            };
        }
    }

    /// <summary>Public view of a card copy.</summary>
    public sealed class CardView
    {
        private CardView()
        {
        }

        /// <summary>Copy uid.</summary>
        public int Uid { get; private set; }

        /// <summary>Definition id.</summary>
        public string CardId { get; private set; } = string.Empty;

        /// <summary>Torment tokens.</summary>
        public int Torments { get; private set; }

        internal static CardView Of(CardInstance c) => new CardView { Uid = c.Uid, CardId = c.CardId, Torments = c.Torments };
    }

    /// <summary>Public view of a market: face-up cards and pile sizes only.</summary>
    public sealed class MarketView
    {
        /// <summary>Face-up cards.</summary>
        public IReadOnlyList<CardView> Visible { get; private set; } = Array.Empty<CardView>();

        /// <summary>Cards left in the draw pile.</summary>
        public int DeckCount { get; private set; }

        /// <summary>Cards in the discard pile.</summary>
        public int DiscardCount { get; private set; }

        internal static MarketView Of(MarketState m)
        {
            return new MarketView { Visible = m.Visible.Select(CardView.Of).ToList(), DeckCount = m.Deck.Count, DiscardCount = m.Discard.Count };
        }
    }

    /// <summary>Public view of a status.</summary>
    public sealed class StatusView
    {
        private StatusView()
        {
        }

        /// <summary>Status kind.</summary>
        public string Kind { get; private set; } = string.Empty;

        /// <summary>Creator seat, or -1.</summary>
        public int Owner { get; private set; }

        /// <summary>False while dormant.</summary>
        public bool Active { get; private set; }

        /// <summary>Public parameters.</summary>
        public IReadOnlyDictionary<string, int> Vars { get; private set; } = new Dictionary<string, int>();

        internal static StatusView Of(StatusState s)
        {
            return new StatusView { Kind = s.Kind, Owner = s.Owner, Active = s.IsActive, Vars = new Dictionary<string, int>(s.Vars) };
        }
    }
}
