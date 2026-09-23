using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vortex.Core.Content;

namespace Vortex.Core.Config
{
    /// <summary>
    /// Every balancing value of the base rules (values marked ⚙ in docs/RULES.md), read from
    /// <c>core/Runtime/Data/config.json</c>. Card-specific numbers live in the card's effect bricks instead.
    /// </summary>
    public sealed class GameConfig : ContentFile
    {
        /// <summary>File name inside the data folder.</summary>
        public const string FileName = "config.json";

        /// <summary>Creates the configuration. Values are validated by <see cref="Validate"/>.</summary>
        [JsonConstructor]
        public GameConfig(
            string? schema,
            int schemaVersion,
            int startingHp,
            int maxHp,
            int minShield,
            int maxShield,
            int dieFaces,
            int marketSize,
            int maxOvercharge,
            int criticalBonusWithoutModifier,
            int tormentValue,
            int eventFrequency,
            string doomEventId,
            IReadOnlyList<PlayerCountSettings> playerCounts,
            int reactionDepthLimit,
            int maxDecisionsPerCommand)
            : base(schema, schemaVersion)
        {
            StartingHp = startingHp;
            MaxHp = maxHp;
            MinShield = minShield;
            MaxShield = maxShield;
            DieFaces = dieFaces;
            MarketSize = marketSize;
            MaxOvercharge = maxOvercharge;
            CriticalBonusWithoutModifier = criticalBonusWithoutModifier;
            TormentValue = tormentValue;
            EventFrequency = eventFrequency;
            DoomEventId = doomEventId;
            PlayerCounts = playerCounts ?? Array.Empty<PlayerCountSettings>();
            ReactionDepthLimit = reactionDepthLimit;
            MaxDecisionsPerCommand = maxDecisionsPerCommand;
        }

        /// <summary>HP of every ship at setup (RULES A3).</summary>
        public int StartingHp { get; }

        /// <summary>Upper bound for HP gains.</summary>
        public int MaxHp { get; }

        /// <summary>Default lower shield bound (RULES B2.1 "bornes du bouclier").</summary>
        public int MinShield { get; }

        /// <summary>Default upper shield bound.</summary>
        public int MaxShield { get; }

        /// <summary>Number of faces of the die.</summary>
        public int DieFaces { get; }

        /// <summary>Visible cards per black market.</summary>
        public int MarketSize { get; }

        /// <summary>Maximum overcharge tokens a ship can hold.</summary>
        public int MaxOvercharge { get; }

        /// <summary>Extra damage of a critical hit against a ship without modifiers (RULES A6 step 9).</summary>
        public int CriticalBonusWithoutModifier { get; }

        /// <summary>Base HP loss per torment token (RULES A7).</summary>
        public int TormentValue { get; }

        /// <summary>An event is revealed every N rounds; 0 disables events (RULES A4).</summary>
        public int EventFrequency { get; }

        /// <summary>Event kept out of the deck and revealed at the doom round.</summary>
        public string DoomEventId { get; }

        /// <summary>Settings that depend on the number of players.</summary>
        public IReadOnlyList<PlayerCountSettings> PlayerCounts { get; }

        /// <summary>Safety limit on nested reactions (RULES B7).</summary>
        public int ReactionDepthLimit { get; }

        /// <summary>Safety limit on decisions requested while resolving a single command.</summary>
        public int MaxDecisionsPerCommand { get; }

        /// <summary>Settings for a given number of players, or null when that count is not supported.</summary>
        public PlayerCountSettings? ForPlayers(int players)
        {
            foreach (PlayerCountSettings s in PlayerCounts)
            {
                if (s.Players == players)
                {
                    return s;
                }
            }

            return null;
        }

        /// <summary>Returns every problem found; empty when valid.</summary>
        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            void Check(bool ok, string message)
            {
                if (!ok)
                {
                    errors.Add("config: " + message);
                }
            }

            Check(StartingHp >= 1 && StartingHp <= 999, "startingHp must be in 1..999");
            Check(MaxHp >= StartingHp && MaxHp <= 999, "maxHp must be in startingHp..999");
            Check(MinShield >= 0 && MinShield <= MaxShield, "minShield must be in 0..maxShield");
            Check(MaxShield >= 1 && MaxShield <= 99, "maxShield must be in 1..99");
            Check(DieFaces >= 2 && DieFaces <= 100, "dieFaces must be in 2..100");
            Check(MarketSize >= 1 && MarketSize <= 20, "marketSize must be in 1..20");
            Check(MaxOvercharge >= 1 && MaxOvercharge <= 10, "maxOvercharge must be in 1..10");
            Check(CriticalBonusWithoutModifier >= 0 && CriticalBonusWithoutModifier <= 99, "criticalBonusWithoutModifier must be in 0..99");
            Check(TormentValue >= 0 && TormentValue <= 99, "tormentValue must be in 0..99");
            Check(EventFrequency >= 0 && EventFrequency <= 99, "eventFrequency must be in 0..99");
            Check(!string.IsNullOrEmpty(DoomEventId), "doomEventId is required");
            Check(ReactionDepthLimit >= 4 && ReactionDepthLimit <= 64, "reactionDepthLimit must be in 4..64");
            Check(MaxDecisionsPerCommand >= 8 && MaxDecisionsPerCommand <= 512, "maxDecisionsPerCommand must be in 8..512");

            var seen = new HashSet<int>();
            foreach (PlayerCountSettings? s in PlayerCounts)
            {
                if (s is null)
                {
                    errors.Add("config: playerCounts contains null");
                    continue;
                }

                Check(s.Players >= 2 && s.Players <= 8, "playerCounts.players must be in 2..8");
                Check(seen.Add(s.Players), "playerCounts has a duplicate entry for " + s.Players + " players");
                Check(s.StartShield >= MinShield && s.StartShield <= MaxShield, "startShield must be within shield bounds");
                Check(s.DoomRound >= 1 && s.DoomRound <= 999, "doomRound must be in 1..999");
            }

            Check(seen.Count > 0, "playerCounts must not be empty");
            return errors;
        }
    }

    /// <summary>Setup values that depend on the number of players (RULES A3, A4).</summary>
    public sealed class PlayerCountSettings
    {
        /// <summary>Creates the settings.</summary>
        [JsonConstructor]
        public PlayerCountSettings(int players, int startShield, int doomRound)
        {
            Players = players;
            StartShield = startShield;
            DoomRound = doomRound;
        }

        /// <summary>Number of players these settings apply to.</summary>
        public int Players { get; }

        /// <summary><c>StartShield[n]</c>: shield of every ship at setup.</summary>
        public int StartShield { get; }

        /// <summary><c>DoomRound[n]</c>: round at which the doom event replaces the regular event.</summary>
        public int DoomRound { get; }
    }
}
