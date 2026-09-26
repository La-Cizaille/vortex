using System.Collections.Generic;

namespace Vortex.Core.Events
{
    /// <summary>Kinds of facts reported by the engine (for animation, logs and replays).</summary>
    public enum GameEventType
    {
        /// <summary>Game created: Amount = players.</summary>
        GameStarted = 0,
        /// <summary>Initiative roll: Player, Value = die.</summary>
        InitiativeRolled = 1,
        /// <summary>Initiative winner: Player.</summary>
        InitiativeWon = 2,
        /// <summary>New round: Value = round number.</summary>
        RoundStarted = 3,
        /// <summary>Event revealed: Id = event id.</summary>
        EventRevealed = 4,
        /// <summary>Turn start: Player.</summary>
        TurnStarted = 5,
        /// <summary>Market phase over: Player.</summary>
        MarketEnded = 6,
        /// <summary>Card taken from a market: Player, Id = card id, CardUid, Value = slot, Amount = its position in the market.</summary>
        MarketCardTaken = 7,
        /// <summary>Market recycled: Value = slot (0 ATK, 1 DEF).</summary>
        MarketRecycled = 8,
        /// <summary>
        /// Card put into a market: Id, CardUid, Value = slot, Amount = its position (the end of the market, or the place of
        /// the card it replaces).
        /// </summary>
        MarketCardRevealed = 9,
        /// <summary>Card equipped: Player, Id, CardUid.</summary>
        CardEquipped = 10,
        /// <summary>Card left its slot to the discard pile: Player, Id, CardUid.</summary>
        CardDiscarded = 11,
        /// <summary>Card moved from Other to Player: Id, CardUid.</summary>
        CardStolen = 12,
        /// <summary>Card activated: Player, Id, CardUid.</summary>
        CardActivated = 13,
        /// <summary>Technology combo activated: Player, Value = colour.</summary>
        TechnologyActivated = 14,
        /// <summary>Attack declared: Player = attacker, Other = target, Value = 1 when overcharged.</summary>
        AttackDeclared = 15,
        /// <summary>Attack redirected: Player = new target, Other = previous target.</summary>
        AttackRedirected = 16,
        /// <summary>Dice rolled: Player, Values = all dice, Amount = sum kept.</summary>
        DiceRolled = 17,
        /// <summary>Critical hit on Other.</summary>
        CriticalHit = 18,
        /// <summary>Attack computed: Player = attacker, Other = target, Value = attack value, Amount = damage.</summary>
        AttackResolved = 19,
        /// <summary>HP lost: Player, Amount, Cause.</summary>
        HpLost = 20,
        /// <summary>HP gained: Player, Amount.</summary>
        HpGained = 21,
        /// <summary>Shield changed: Player, Value = new value, Amount = old value.</summary>
        ShieldChanged = 22,
        /// <summary>Shield change refused by a permission: Player = target, Other = source.</summary>
        ShieldChangeRefused = 23,
        /// <summary>Overcharge tokens changed: Player, Value = new count.</summary>
        OverchargeChanged = 24,
        /// <summary>Torment placed: Player = card owner (-1: market), CardUid, Value = tokens on card.</summary>
        TormentPlaced = 25,
        /// <summary>Torments removed: Player (-1: market), CardUid, Amount.</summary>
        TormentsRemoved = 26,
        /// <summary>Status added: Player (-1: global), Id = kind.</summary>
        StatusAdded = 27,
        /// <summary>Status ended: Player (-1: global), Id = kind.</summary>
        StatusEnded = 28,
        /// <summary>Player eliminated: Player, Other = responsible player or -1.</summary>
        PlayerEliminated = 29,
        /// <summary>Crew action performed: Player, Value = CrewAction.</summary>
        CrewActionPerformed = 30,
        /// <summary>Turn ended: Player.</summary>
        TurnEnded = 31,
        /// <summary>Game over: Player = winner (-1 draw), Value = WinCondition.</summary>
        GameOver = 32,
        /// <summary>A die was rolled for an effect: Player, Value, Id = source.</summary>
        DieRolled = 33,
        /// <summary>An effect did something noteworthy: Player, Id = source, Text = key.</summary>
        EffectTriggered = 34,
        /// <summary>Bounty on the HP leader added to an attack: Player = attacker, Other = leader, Amount = bonus.</summary>
        LeaderBountyApplied = 35,
        /// <summary>An event drawn for an eliminated player's choice was set aside: Player = chooser, Id = event.</summary>
        EventSetAside = 36,
    }

    /// <summary>
    /// A fact that happened. Flat by design (no polymorphism, safe to serialize); unused fields keep defaults.
    /// </summary>
    public sealed class GameEvent
    {
        /// <summary>Kind of fact.</summary>
        public GameEventType Type { get; set; }

        /// <summary>Main player concerned, or -1.</summary>
        public int Player { get; set; } = -1;

        /// <summary>Other player concerned, or -1.</summary>
        public int Other { get; set; } = -1;

        /// <summary>Quantity (damage, HP, tokens…).</summary>
        public int Amount { get; set; }

        /// <summary>Secondary value (die, new shield, enum value…).</summary>
        public int Value { get; set; }

        /// <summary>Card, event or status id.</summary>
        public string? Id { get; set; }

        /// <summary>Card uid, or -1.</summary>
        public int CardUid { get; set; } = -1;

        /// <summary>List of values (dice).</summary>
        public List<int>? Values { get; set; }

        /// <summary>Cause of an HP loss.</summary>
        public HpLossCause? Cause { get; set; }

        /// <summary>Free key for UI text.</summary>
        public string? Text { get; set; }
    }

    /// <summary>Cause of an HP loss (RULES A1).</summary>
    public enum HpLossCause
    {
        /// <summary>Damage from an attack.</summary>
        Attack = 0,
        /// <summary>A loss sent back by an effect; never triggers reactions (RULES B7).</summary>
        Reflect = 1,
        /// <summary>Torment tokens.</summary>
        Torment = 2,
        /// <summary>An event card.</summary>
        Event = 3,
        /// <summary>A cost paid by the player or an effect on themselves.</summary>
        Self = 4,
    }
}
