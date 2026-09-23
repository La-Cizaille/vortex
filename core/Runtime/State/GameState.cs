using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Decisions;
using Vortex.Core.Dice;

namespace Vortex.Core.State
{
    /// <summary>Phase of the current player's turn (RULES A5).</summary>
    public enum TurnPhase
    {
        /// <summary>Game created but not started.</summary>
        NotStarted = 0,
        /// <summary>Black market (RULES A5.2).</summary>
        Market = 1,
        /// <summary>Activation window and crew actions (RULES A5.3, A5.4).</summary>
        Main = 2,
        /// <summary>The game is over.</summary>
        GameOver = 3,
    }

    /// <summary>How the game ended (RULES A9).</summary>
    public enum WinCondition
    {
        /// <summary>Last ship standing.</summary>
        Domination = 0,
        /// <summary>All four technologies obtained.</summary>
        GalacticElection = 1,
        /// <summary>All remaining ships eliminated at once.</summary>
        Draw = 2,
    }

    /// <summary>
    /// Complete, authoritative state of a game. Plain data: serializable, cloneable, and owned by the
    /// engine only (clients receive a projection, never this object). See ADR-0005/ADR-0009.
    /// </summary>
    public sealed class GameState
    {
        /// <summary>Serialized format version of this class.</summary>
        public const int CurrentFormatVersion = 1;

        /// <summary>Format version of this instance.</summary>
        public int FormatVersion { get; set; } = CurrentFormatVersion;

        /// <summary>Deterministic random generator (ADR-0004). Hidden from clients.</summary>
        public Pcg32 Rng { get; set; } = new Pcg32();

        /// <summary>Players indexed by seat, in clockwise order.</summary>
        public List<PlayerState> Players { get; set; } = new List<PlayerState>();

        /// <summary>Current round number, starting at 1.</summary>
        public int Round { get; set; }

        /// <summary>Seat that won the initiative roll; rounds start from this seat (RULES A4).</summary>
        public int InitiativeSeat { get; set; }

        /// <summary>Seat of the player whose turn it is.</summary>
        public int CurrentPlayer { get; set; }

        /// <summary>Seats that already played in the current round.</summary>
        public List<int> PlayedThisRound { get; set; } = new List<int>();

        /// <summary>Current phase.</summary>
        public TurnPhase Phase { get; set; }

        /// <summary>Market picks still allowed this turn.</summary>
        public int MarketPicksLeft { get; set; }

        /// <summary>True once the player has taken at least one market card this turn (recycling is then forbidden).</summary>
        public bool PickedThisTurn { get; set; }

        /// <summary>Crew actions already performed this turn, in order.</summary>
        public List<CrewAction> CrewActionsTaken { get; set; } = new List<CrewAction>();

        /// <summary>Attack modifier deck, market and discard.</summary>
        public MarketState AttackMarket { get; set; } = new MarketState();

        /// <summary>Defense modifier deck, market and discard.</summary>
        public MarketState DefenseMarket { get; set; } = new MarketState();

        /// <summary>Event draw pile (top = last element). Hidden from clients.</summary>
        public List<string> EventDeck { get; set; } = new List<string>();

        /// <summary>Revealed events.</summary>
        public List<string> EventDiscard { get; set; } = new List<string>();

        /// <summary>Event active for the current round, if any.</summary>
        public string? ActiveEventId { get; set; }

        /// <summary>Effects that belong to no player (e.g. a permanent rule change), in creation order.</summary>
        public List<StatusState> GlobalStatuses { get; set; } = new List<StatusState>();

        /// <summary>Next unique id for cards and statuses.</summary>
        public int NextUid { get; set; } = 1;

        /// <summary>Number of commands applied so far (used to build decision ids).</summary>
        public int CommandCount { get; set; }

        /// <summary>Command waiting for a decision (ADR-0009), or null.</summary>
        public PendingState? Pending { get; set; }

        /// <summary>Result, once the game is over.</summary>
        public GameOutcome? Outcome { get; set; }

        /// <summary>Market for a slot.</summary>
        public MarketState Market(CardSlot slot)
        {
            return slot == CardSlot.Attack ? AttackMarket : DefenseMarket;
        }

        /// <summary>Deep copy. Every field must be copied (checked by a serialization round-trip test).</summary>
        public GameState Clone()
        {
            return new GameState
            {
                FormatVersion = FormatVersion,
                Rng = Rng.Clone(),
                Players = Players.Select(p => p.Clone()).ToList(),
                Round = Round,
                InitiativeSeat = InitiativeSeat,
                CurrentPlayer = CurrentPlayer,
                PlayedThisRound = new List<int>(PlayedThisRound),
                Phase = Phase,
                MarketPicksLeft = MarketPicksLeft,
                PickedThisTurn = PickedThisTurn,
                CrewActionsTaken = new List<CrewAction>(CrewActionsTaken),
                AttackMarket = AttackMarket.Clone(),
                DefenseMarket = DefenseMarket.Clone(),
                EventDeck = new List<string>(EventDeck),
                EventDiscard = new List<string>(EventDiscard),
                ActiveEventId = ActiveEventId,
                GlobalStatuses = GlobalStatuses.Select(s => s.Clone()).ToList(),
                NextUid = NextUid,
                CommandCount = CommandCount,
                Pending = Pending?.Clone(),
                Outcome = Outcome?.Clone(),
            };
        }
    }

    /// <summary>One ship and its owner (RULES A2).</summary>
    public sealed class PlayerState
    {
        /// <summary>Seat index, also the player id.</summary>
        public int Seat { get; set; }

        /// <summary>Display name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Hit points.</summary>
        public int Hp { get; set; }

        /// <summary>Stored shield value (the effective value also applies bounds, RULES B2.1).</summary>
        public int Shield { get; set; }

        /// <summary>Overcharge tokens held.</summary>
        public int Overcharge { get; set; }

        /// <summary>Equipped attack modifier.</summary>
        public CardInstance? AttackSlot { get; set; }

        /// <summary>Equipped defense modifier.</summary>
        public CardInstance? DefenseSlot { get; set; }

        /// <summary>Technologies obtained through combos (RULES A8).</summary>
        public List<TechColor> Technologies { get; set; } = new List<TechColor>();

        /// <summary>Temporary effects held by this player, in creation order.</summary>
        public List<StatusState> Statuses { get; set; } = new List<StatusState>();

        /// <summary>True once eliminated.</summary>
        public bool Eliminated { get; set; }

        /// <summary>Card in a slot.</summary>
        public CardInstance? Slot(CardSlot slot)
        {
            return slot == CardSlot.Attack ? AttackSlot : DefenseSlot;
        }

        /// <summary>Equipped modifiers, ATK first.</summary>
        public IEnumerable<CardInstance> Modifiers()
        {
            if (AttackSlot != null)
            {
                yield return AttackSlot;
            }

            if (DefenseSlot != null)
            {
                yield return DefenseSlot;
            }
        }

        /// <summary>Deep copy.</summary>
        public PlayerState Clone()
        {
            return new PlayerState
            {
                Seat = Seat,
                Name = Name,
                Hp = Hp,
                Shield = Shield,
                Overcharge = Overcharge,
                AttackSlot = AttackSlot?.Clone(),
                DefenseSlot = DefenseSlot?.Clone(),
                Technologies = new List<TechColor>(Technologies),
                Statuses = Statuses.Select(s => s.Clone()).ToList(),
                Eliminated = Eliminated,
            };
        }
    }

    /// <summary>A physical copy of a modifier card.</summary>
    public sealed class CardInstance
    {
        /// <summary>Unique id of this copy within the game.</summary>
        public int Uid { get; set; }

        /// <summary>Definition id, e.g. <c>A_005</c>.</summary>
        public string CardId { get; set; } = string.Empty;

        /// <summary>Torment tokens on this card (RULES A7).</summary>
        public int Torments { get; set; }

        /// <summary>Per-copy data kept by the card's effects (e.g. a chosen enemy).</summary>
        public SortedDictionary<string, int> Vars { get; set; } = new SortedDictionary<string, int>();

        /// <summary>Deep copy.</summary>
        public CardInstance Clone()
        {
            return new CardInstance { Uid = Uid, CardId = CardId, Torments = Torments, Vars = new SortedDictionary<string, int>(Vars) };
        }
    }

    /// <summary>Deck, visible market and discard of one modifier type.</summary>
    public sealed class MarketState
    {
        /// <summary>Face-up cards, in order.</summary>
        public List<CardInstance> Visible { get; set; } = new List<CardInstance>();

        /// <summary>Draw pile (top = last element). Hidden from clients.</summary>
        public List<CardInstance> Deck { get; set; } = new List<CardInstance>();

        /// <summary>Discard pile.</summary>
        public List<CardInstance> Discard { get; set; } = new List<CardInstance>();

        /// <summary>Deep copy.</summary>
        public MarketState Clone()
        {
            return new MarketState
            {
                Visible = Visible.Select(c => c.Clone()).ToList(),
                Deck = Deck.Select(c => c.Clone()).ToList(),
                Discard = Discard.Select(c => c.Clone()).ToList(),
            };
        }
    }

    /// <summary>When a status ends (RULES B5).</summary>
    public enum StatusExpiry
    {
        /// <summary>Never expires on its own.</summary>
        Permanent = 0,
        /// <summary>At the end of <see cref="StatusState.ExpiryPlayer"/>'s turn.</summary>
        EndOfTurn = 1,
        /// <summary>At the start of <see cref="StatusState.ExpiryPlayer"/>'s turn.</summary>
        StartOfTurn = 2,
        /// <summary>At the start of the next round.</summary>
        EndOfRound = 3,
    }

    /// <summary>A temporary or rule-changing effect created during the game (e.g. "next attack ×2").</summary>
    public sealed class StatusState
    {
        /// <summary>Unique id.</summary>
        public int Uid { get; set; }

        /// <summary>Status behaviour key, resolved through the effect catalog.</summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>Player who created it (-1: the game).</summary>
        public int Owner { get; set; } = -1;

        /// <summary>Expiry rule.</summary>
        public StatusExpiry Expiry { get; set; }

        /// <summary>Player whose turn boundary triggers the expiry.</summary>
        public int ExpiryPlayer { get; set; } = -1;

        /// <summary>When set (&gt;= 0), the status is dormant until that player's next turn starts.</summary>
        public int DormantUntilTurnOf { get; set; } = -1;

        /// <summary>Parameters and data of the status.</summary>
        public SortedDictionary<string, int> Vars { get; set; } = new SortedDictionary<string, int>();

        /// <summary>True when the status currently has an effect.</summary>
        public bool IsActive => DormantUntilTurnOf < 0;

        /// <summary>Reads a variable, or a default.</summary>
        public int Var(string name, int fallback = 0)
        {
            return Vars.TryGetValue(name, out int v) ? v : fallback;
        }

        /// <summary>Deep copy.</summary>
        public StatusState Clone()
        {
            return new StatusState
            {
                Uid = Uid,
                Kind = Kind,
                Owner = Owner,
                Expiry = Expiry,
                ExpiryPlayer = ExpiryPlayer,
                DormantUntilTurnOf = DormantUntilTurnOf,
                Vars = new SortedDictionary<string, int>(Vars),
            };
        }
    }

    /// <summary>A command suspended on a decision (ADR-0009).</summary>
    public sealed class PendingState
    {
        /// <summary>Command being resolved.</summary>
        public Command Command { get; set; } = new Command();

        /// <summary>Player who submitted it.</summary>
        public int Player { get; set; }

        /// <summary>Answers already given, in order.</summary>
        public List<string> Answers { get; set; } = new List<string>();

        /// <summary>Decision currently awaited.</summary>
        public DecisionRequest Decision { get; set; } = new DecisionRequest();

        /// <summary>Events already delivered to clients for this command (a deterministic prefix).</summary>
        public int DeliveredEvents { get; set; }

        /// <summary>Deep copy.</summary>
        public PendingState Clone()
        {
            return new PendingState
            {
                Command = Command.Clone(),
                Player = Player,
                Answers = new List<string>(Answers),
                Decision = Decision.Clone(),
                DeliveredEvents = DeliveredEvents,
            };
        }
    }

    /// <summary>Final result of a game.</summary>
    public sealed class GameOutcome
    {
        /// <summary>Winning seat, or -1 for a draw.</summary>
        public int Winner { get; set; } = -1;

        /// <summary>How the game ended.</summary>
        public WinCondition Condition { get; set; }

        /// <summary>Round in which the game ended.</summary>
        public int Round { get; set; }

        /// <summary>Deep copy.</summary>
        public GameOutcome Clone()
        {
            return new GameOutcome { Winner = Winner, Condition = Condition, Round = Round };
        }
    }
}
