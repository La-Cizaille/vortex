using System.Collections.Generic;
using System.Linq;

namespace Vortex.Core.Decisions
{
    /// <summary>What kind of choice is asked (for UI presentation; the engine only uses option keys).</summary>
    public enum DecisionKind
    {
        /// <summary>Pick a player.</summary>
        ChoosePlayer = 0,
        /// <summary>Pick an equipped modifier.</summary>
        ChooseModifier = 1,
        /// <summary>Pick a card in a market.</summary>
        ChooseMarketCard = 2,
        /// <summary>Pick a number.</summary>
        ChooseNumber = 3,
        /// <summary>Yes or no.</summary>
        YesNo = 4,
        /// <summary>Clockwise or counter-clockwise.</summary>
        ChooseDirection = 5,
        /// <summary>Pick a crew action.</summary>
        ChooseCrewAction = 6,
        /// <summary>Pick among labelled alternatives.</summary>
        ChooseOption = 7,
    }

    /// <summary>
    /// A choice the engine needs from a specific player while resolving a command (RULES B6, ADR-0009).
    /// Always a single pick among <see cref="Options"/>; multi-step choices are asked one at a time.
    /// </summary>
    public sealed class DecisionRequest
    {
        /// <summary>Unique id; an answer must quote it (stale answers are rejected).</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Seat of the player who must answer.</summary>
        public int Player { get; set; } = -1;

        /// <summary>Kind of choice.</summary>
        public DecisionKind Kind { get; set; }

        /// <summary>Stable key describing the question, e.g. <c>critical.discard</c> (UI localisation key).</summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>Card, event or technology that asks, if any (for display).</summary>
        public string? SourceId { get; set; }

        /// <summary>Legal answers.</summary>
        public List<DecisionOption> Options { get; set; } = new List<DecisionOption>();

        /// <summary>True when <paramref name="key"/> is one of the options.</summary>
        public bool Accepts(string? key)
        {
            return key != null && Options.Any(o => o.Key == key);
        }

        /// <summary>Deep copy.</summary>
        public DecisionRequest Clone()
        {
            return new DecisionRequest
            {
                Id = Id,
                Player = Player,
                Kind = Kind,
                Prompt = Prompt,
                SourceId = SourceId,
                Options = Options.Select(o => o.Clone()).ToList(),
            };
        }
    }

    /// <summary>One possible answer.</summary>
    public sealed class DecisionOption
    {
        /// <summary>Answer key sent back by the client, e.g. <c>p2</c>, <c>c17</c>, <c>n3</c>, <c>yes</c>, <c>none</c>.</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Referenced player, or -1.</summary>
        public int Player { get; set; } = -1;

        /// <summary>Referenced card uid, or -1.</summary>
        public int CardUid { get; set; } = -1;

        /// <summary>Referenced number (value, index), if relevant.</summary>
        public int Number { get; set; }

        /// <summary>Referenced content id (e.g. an event), or null.</summary>
        public string? ContentId { get; set; }

        /// <summary>Copy.</summary>
        public DecisionOption Clone()
        {
            return new DecisionOption { Key = Key, Player = Player, CardUid = CardUid, Number = Number, ContentId = ContentId };
        }
    }
}
