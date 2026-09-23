using Vortex.Core.Commands;
using Vortex.Core.Rules;

namespace Vortex.Core.Effects.Statuses
{
    /// <summary>Status kinds implemented natively by the engine (generic base-rule mechanisms, RULES B3).</summary>
    internal static class StatusKinds
    {
        /// <summary>Imposed crew action for the holder's next turn ("imposer l'action d'équipage").</summary>
        public const string ForcedCrewAction = "ForcedCrewAction";

        /// <summary>A die rolled in advance, used as the first die of the holder's next crew action roll.</summary>
        public const string PreRolledDie = "PreRolledDie";

        /// <summary>Variable: crew action (int value of <see cref="CrewAction"/>).</summary>
        public const string VarAction = "action";

        /// <summary>Variable: target seat.</summary>
        public const string VarTarget = "target";

        /// <summary>Variable: die value.</summary>
        public const string VarValue = "value";
    }

    /// <summary>Behaviour of <see cref="StatusKinds.ForcedCrewAction"/>: enforced by command validation.</summary>
    internal sealed class ForcedCrewActionStatus : Effect
    {
        public override string Name => StatusKinds.ForcedCrewAction;
    }

    /// <summary>Behaviour of <see cref="StatusKinds.PreRolledDie"/>: consumed by <see cref="Game.ThrowDice"/>.</summary>
    internal sealed class PreRolledDieStatus : Effect
    {
        public override string Name => StatusKinds.PreRolledDie;
    }
}
