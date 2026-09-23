using Vortex.Core.Content;

namespace Vortex.Core.Commands
{
    /// <summary>What a player asks the engine to do.</summary>
    public enum CommandType
    {
        /// <summary>Take the card at <see cref="Command.MarketIndex"/> from the <see cref="Command.Slot"/> market (RULES A5.2).</summary>
        PickMarket = 0,
        /// <summary>Recycle the <see cref="Command.Slot"/> market instead of picking.</summary>
        RecycleMarket = 1,
        /// <summary>End the market phase (pass, or stop after at least one pick).</summary>
        EndMarket = 2,
        /// <summary>Activate the equipped card <see cref="Command.CardUid"/> (RULES A5.3).</summary>
        ActivateCard = 3,
        /// <summary>Activate the technology combo of the equipped pair (RULES A8).</summary>
        ActivateTechnology = 4,
        /// <summary>Crew action: attack <see cref="Command.Target"/> (RULES A6).</summary>
        Attack = 5,
        /// <summary>Crew action: reroll own shield (RULES A5.4).</summary>
        RerollShield = 6,
        /// <summary>Crew action: reroll <see cref="Command.Target"/>'s shield.</summary>
        Sabotage = 7,
        /// <summary>Crew action: gain an overcharge token.</summary>
        Overcharge = 8,
        /// <summary>End the turn.</summary>
        EndTurn = 9,
        /// <summary>Answer the pending decision <see cref="Command.DecisionId"/> with <see cref="Command.Option"/>.</summary>
        AnswerDecision = 10,
    }

    /// <summary>Crew actions (RULES A5.4).</summary>
    public enum CrewAction
    {
        /// <summary>Attack an opponent.</summary>
        Attack = 0,
        /// <summary>Reroll own shield.</summary>
        RerollShield = 1,
        /// <summary>Reroll an opponent's shield.</summary>
        Sabotage = 2,
        /// <summary>Gain an overcharge token.</summary>
        Overcharge = 3,
    }

    /// <summary>
    /// A player's intention. Deliberately a flat record of optional fields rather than a class hierarchy:
    /// no polymorphic deserialization is ever needed (docs/SECURITY.md). Unused fields keep their defaults.
    /// </summary>
    public sealed class Command
    {
        /// <summary>Kind of command.</summary>
        public CommandType Type { get; set; }

        /// <summary>Target seat for Attack / Sabotage, or -1.</summary>
        public int Target { get; set; } = -1;

        /// <summary>Card uid for ActivateCard, or -1.</summary>
        public int CardUid { get; set; } = -1;

        /// <summary>Market for PickMarket / RecycleMarket.</summary>
        public CardSlot Slot { get; set; }

        /// <summary>Index in the market for PickMarket, or -1.</summary>
        public int MarketIndex { get; set; } = -1;

        /// <summary>Consume an overcharge token (Attack, RerollShield).</summary>
        public bool UseOvercharge { get; set; }

        /// <summary>Decision id for AnswerDecision.</summary>
        public string? DecisionId { get; set; }

        /// <summary>Chosen option key for AnswerDecision.</summary>
        public string? Option { get; set; }

        /// <summary>Take a market card.</summary>
        public static Command PickMarket(CardSlot slot, int index) => new Command { Type = CommandType.PickMarket, Slot = slot, MarketIndex = index };

        /// <summary>Recycle a market.</summary>
        public static Command RecycleMarket(CardSlot slot) => new Command { Type = CommandType.RecycleMarket, Slot = slot };

        /// <summary>End the market phase.</summary>
        public static Command EndMarket() => new Command { Type = CommandType.EndMarket };

        /// <summary>Activate an equipped card.</summary>
        public static Command ActivateCard(int cardUid) => new Command { Type = CommandType.ActivateCard, CardUid = cardUid };

        /// <summary>Activate the technology combo.</summary>
        public static Command ActivateTechnology() => new Command { Type = CommandType.ActivateTechnology };

        /// <summary>Attack a player.</summary>
        public static Command Attack(int target, bool useOvercharge = false) => new Command { Type = CommandType.Attack, Target = target, UseOvercharge = useOvercharge };

        /// <summary>Reroll own shield.</summary>
        public static Command RerollShield(bool useOvercharge = false) => new Command { Type = CommandType.RerollShield, UseOvercharge = useOvercharge };

        /// <summary>Reroll an opponent's shield.</summary>
        public static Command Sabotage(int target) => new Command { Type = CommandType.Sabotage, Target = target };

        /// <summary>Gain an overcharge token.</summary>
        public static Command Overcharge() => new Command { Type = CommandType.Overcharge };

        /// <summary>End the turn.</summary>
        public static Command EndTurn() => new Command { Type = CommandType.EndTurn };

        /// <summary>Answer a decision.</summary>
        public static Command Answer(string decisionId, string option) => new Command { Type = CommandType.AnswerDecision, DecisionId = decisionId, Option = option };

        /// <summary>Copy.</summary>
        public Command Clone()
        {
            return new Command
            {
                Type = Type,
                Target = Target,
                CardUid = CardUid,
                Slot = Slot,
                MarketIndex = MarketIndex,
                UseOvercharge = UseOvercharge,
                DecisionId = DecisionId,
                Option = Option,
            };
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Type + "(target=" + Target + ", card=" + CardUid + ", slot=" + Slot + ", index=" + MarketIndex + ", overcharge=" + UseOvercharge + ", option=" + Option + ")";
        }
    }
}
