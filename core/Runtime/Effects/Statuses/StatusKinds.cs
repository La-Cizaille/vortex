using Vortex.Core.Commands;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Effects.Statuses
{
    /// <summary>Status kinds implemented natively by the engine (generic base-rule mechanisms, RULES B3).</summary>
    internal static class StatusKinds
    {
        /// <summary>Imposed crew action for the holder's next turn ("imposer l'action d'équipage").</summary>
        public const string ForcedCrewAction = "ForcedCrewAction";

        /// <summary>A die rolled in advance, used as the first die of the holder's next crew action roll.</summary>
        public const string PreRolledDie = "PreRolledDie";

        /// <summary>Defensive posture: bonus to the holder's effective shield until the start of their next turn.</summary>
        public const string DefensivePosture = "DefensivePosture";

        /// <summary>Variable: amount.</summary>
        public const string VarAmount = "amount";

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

    /// <summary>
    /// Behaviour of <see cref="StatusKinds.DefensivePosture"/> (RULES A5.4): adds its amount to the holder's effective
    /// shield (calculation "bouclier effectif"). The shield value itself does not change, so the permission
    /// "modifier un bouclier" does not apply, and the bonus counts even when the shield is disabled or ignored.
    /// </summary>
    internal sealed class DefensivePostureStatus : Effect
    {
        public override string Name => StatusKinds.DefensivePosture;

        public override void ModifyEffectiveShield(Game game, EffectSource self, AttackInfo attack, ValueModifiers shield)
        {
            if (attack.Target == self.Holder && game.FindStatus(self.StatusUid) is StatusState status)
            {
                shield.Add(status.Var(StatusKinds.VarAmount));
            }
        }
    }

    /// <summary>Behaviour of <see cref="StatusKinds.PreRolledDie"/>: consumed by <see cref="Game.ThrowDice"/>.</summary>
    internal sealed class PreRolledDieStatus : Effect
    {
        public override string Name => StatusKinds.PreRolledDie;
    }
}
