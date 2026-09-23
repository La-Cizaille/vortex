namespace Vortex.Core.Rules
{
    /// <summary>Why a command was rejected. A rejected command never changes the state.</summary>
    public enum CommandErrorCode
    {
        /// <summary>The game is over.</summary>
        GameOver = 0,
        /// <summary>A decision is pending: only its answer is accepted.</summary>
        DecisionPending = 1,
        /// <summary>No decision is pending.</summary>
        NoDecisionPending = 2,
        /// <summary>The pending decision belongs to another player.</summary>
        NotYourDecision = 3,
        /// <summary>The answer quotes a stale or unknown decision id.</summary>
        WrongDecision = 4,
        /// <summary>The answer is not one of the options.</summary>
        InvalidOption = 5,
        /// <summary>It is not this player's turn.</summary>
        NotYourTurn = 6,
        /// <summary>The command is not allowed in the current phase.</summary>
        WrongPhase = 7,
        /// <summary>No such market card.</summary>
        InvalidMarketCard = 8,
        /// <summary>A market cannot be recycled after taking a card.</summary>
        CannotRecycleAfterPick = 9,
        /// <summary>No market pick left.</summary>
        NoPicksLeft = 10,
        /// <summary>The card is not equipped by this player.</summary>
        InvalidCard = 11,
        /// <summary>The card has no activation, or its activation is not allowed now.</summary>
        CardNotActivatable = 12,
        /// <summary>The equipped modifiers do not form a technology combo.</summary>
        NoTechnologyCombo = 13,
        /// <summary>All crew actions of the turn are used.</summary>
        NoCrewActionLeft = 14,
        /// <summary>This crew action was already performed this turn.</summary>
        CrewActionAlreadyUsed = 15,
        /// <summary>An imposed crew action must be performed (or no crew action is allowed if it is impossible).</summary>
        ForcedActionRequired = 16,
        /// <summary>Invalid target (self, unknown or eliminated).</summary>
        InvalidTarget = 17,
        /// <summary>The permission "cibler" is refused.</summary>
        TargetNotAllowed = 18,
        /// <summary>No overcharge token to consume.</summary>
        NoOvercharge = 19,
        /// <summary>Overcharge tokens are already at the maximum.</summary>
        OverchargeFull = 20,
        /// <summary>The permission "modifier un bouclier" is refused.</summary>
        ShieldChangeNotAllowed = 21,
        /// <summary>Unknown command type or malformed command.</summary>
        MalformedCommand = 22,
    }

    /// <summary>A rejected command.</summary>
    public sealed class CommandError
    {
        /// <summary>Creates the error.</summary>
        public CommandError(CommandErrorCode code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>Machine-readable reason.</summary>
        public CommandErrorCode Code { get; }

        /// <summary>Developer-facing explanation (not localized).</summary>
        public string Message { get; }

        /// <inheritdoc/>
        public override string ToString() => Code + ": " + Message;
    }
}
