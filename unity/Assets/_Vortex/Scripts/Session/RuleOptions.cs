using System;
using Vortex.Core.Config;

namespace Vortex.Client.Session
{
    /// <summary>
    /// Rule options under study (ADR-0011) chosen in the development menu, which is absent from published builds
    /// (INTERFACE.md 5, ARB-65). The engine validates them when it is created.
    /// </summary>
    public sealed class RuleOptions
    {
        /// <summary>Creates a set of options.</summary>
        public RuleOptions(int defensivePostureBonus, int leaderBounty, bool ghostsChooseEvent)
        {
            DefensivePostureBonus = defensivePostureBonus;
            LeaderBounty = leaderBounty;
            GhostsChooseEvent = ghostsChooseEvent;
        }

        /// <summary>Shield bonus of the defensive posture; 0: no such action (RULES A5.4).</summary>
        public int DefensivePostureBonus { get; }

        /// <summary>Attack bonus against the sole HP leader; 0: no bounty (RULES A6).</summary>
        public int LeaderBounty { get; }

        /// <summary>Whether an eliminated player chooses the round's event (RULES A4.1).</summary>
        public bool GhostsChooseEvent { get; }

        /// <summary>The options of a configuration (the current rules).</summary>
        public static RuleOptions Of(GameConfig config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new RuleOptions(config.DefensivePostureBonus, config.LeaderBounty, config.GhostsChooseEvent);
        }

        /// <summary>The configuration with these options instead of its own.</summary>
        public GameConfig ApplyTo(GameConfig config)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return config.WithRuleOptions(DefensivePostureBonus, LeaderBounty, GhostsChooseEvent);
        }
    }
}
