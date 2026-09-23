using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Core.Effects;
using Vortex.Core.State;

namespace Vortex.Core.Rules
{
    /// <summary>
    /// Structural invariants of a <see cref="GameState"/> between two commands. Used by tests and the simulator
    /// after every command, and before loading any saved state (untrusted input, docs/SECURITY.md S2).
    /// </summary>
    internal static class GameStateValidator
    {
        public static List<string> Validate(GameState state, GameData data, GameConfig config, IEffectCatalog catalog)
        {
            var errors = new List<string>();
            void Check(bool ok, string message)
            {
                if (!ok)
                {
                    errors.Add(message);
                }
            }

            Check(state.FormatVersion == GameState.CurrentFormatVersion, "Unsupported state format version.");
            Check(state.Rng != null && state.Rng.IsValid, "Invalid random generator state.");
            Check(config.ForPlayers(state.Players.Count) != null, "Unsupported player count.");
            if (errors.Count > 0)
            {
                return errors;
            }

            Dictionary<string, CardDefinition> cards = data.Modifiers.ToDictionary(c => c.Id, System.StringComparer.Ordinal);
            var uids = new HashSet<int>();
            int cardCount = 0;
            void CheckCard(CardInstance? card, CardSlot expectedSlot, bool mayHaveTokens, string where)
            {
                if (card == null)
                {
                    errors.Add(where + ": null card.");
                    return;
                }

                cardCount++;
                Check(uids.Add(card.Uid), where + ": duplicate card uid " + card.Uid.ToString(CultureInfo.InvariantCulture) + ".");
                Check(card.Uid > 0 && card.Uid < state.NextUid, where + ": card uid out of range.");
                if (!cards.TryGetValue(card.CardId, out CardDefinition? def))
                {
                    errors.Add(where + ": unknown card '" + card.CardId + "'.");
                    return;
                }

                Check(def.Slot == expectedSlot, where + ": " + card.CardId + " is in the wrong slot or market.");
                Check(card.Torments >= 0 && (mayHaveTokens || card.Torments == 0), where + ": invalid torment count.");
            }

            for (int seat = 0; seat < state.Players.Count; seat++)
            {
                PlayerState p = state.Players[seat];
                string where = "player " + seat.ToString(CultureInfo.InvariantCulture);
                Check(p.Seat == seat, where + ": seat mismatch.");
                Check(!string.IsNullOrWhiteSpace(p.Name) && p.Name.Length <= 24 && !p.Name.Any(char.IsControl), where + ": invalid name.");
                Check(p.Hp >= 0 && p.Hp <= config.MaxHp, where + ": HP out of range.");
                Check(p.Eliminated || p.Hp > 0, where + ": alive with 0 HP between commands.");
                Check(p.Shield >= config.MinShield && p.Shield <= config.MaxShield, where + ": shield out of range.");
                Check(p.Overcharge >= 0 && p.Overcharge <= config.MaxOvercharge, where + ": overcharge out of range.");
                Check(p.Technologies.Distinct().Count() == p.Technologies.Count && p.Technologies.All(c => c != TechColor.Neutral), where + ": invalid technologies.");
                Check(!p.Eliminated || (p.AttackSlot == null && p.DefenseSlot == null && p.Statuses.Count == 0), where + ": eliminated player still holds cards or statuses.");
                if (p.AttackSlot != null)
                {
                    CheckCard(p.AttackSlot, CardSlot.Attack, true, where + " ATK");
                }

                if (p.DefenseSlot != null)
                {
                    CheckCard(p.DefenseSlot, CardSlot.Defense, true, where + " DEF");
                }

                CheckStatuses(p.Statuses, where, catalog, errors, state.Players.Count);
            }

            foreach (CardSlot slot in new[] { CardSlot.Attack, CardSlot.Defense })
            {
                MarketState m = state.Market(slot);
                Check(m.Visible.Count <= config.MarketSize, slot + " market too large.");
                m.Visible.ForEach(c => CheckCard(c, slot, true, slot + " market"));
                m.Deck.ForEach(c => CheckCard(c, slot, false, slot + " deck"));
                m.Discard.ForEach(c => CheckCard(c, slot, false, slot + " discard"));
            }

            Check(cardCount == data.Modifiers.Sum(c => c.Copies), "Card count not conserved.");

            var eventIds = new HashSet<string>(data.Events.Select(e => e.Id), System.StringComparer.Ordinal);
            Check(state.EventDeck.Concat(state.EventDiscard).All(eventIds.Contains), "Unknown event id.");
            Check(!state.EventDeck.Contains(config.DoomEventId) && !state.EventDiscard.Contains(config.DoomEventId), "The doom event must stay out of the event piles.");
            Check(state.ActiveEventId == null || eventIds.Contains(state.ActiveEventId), "Unknown active event.");
            CheckStatuses(state.GlobalStatuses, "global", catalog, errors, state.Players.Count);

            bool over = state.Outcome != null;
            Check(state.CurrentPlayer >= 0 && state.CurrentPlayer < state.Players.Count, "Current player out of range.");
            if (!over && errors.Count == 0)
            {
                Check(!state.Players[state.CurrentPlayer].Eliminated, "Current player is eliminated.");
                Check(state.Phase == TurnPhase.Market || state.Phase == TurnPhase.Main, "Invalid phase for a running game.");
                Check(state.Players.Count(p => !p.Eliminated) >= 2, "Running game with fewer than two players alive.");
            }

            if (state.Pending != null)
            {
                Check(!over, "Pending decision on a finished game.");
                Check(state.Pending.Decision.Options.Count >= 2, "Pending decision with fewer than two options.");
                Check(state.Pending.Decision.Player >= 0 && state.Pending.Decision.Player < state.Players.Count, "Pending decision for an unknown player.");
            }

            return errors;
        }

        private static void CheckStatuses(List<StatusState> statuses, string where, IEffectCatalog catalog, List<string> errors, int players)
        {
            var seen = new HashSet<int>();
            foreach (StatusState s in statuses)
            {
                if (!seen.Add(s.Uid))
                {
                    errors.Add(where + ": duplicate status uid.");
                }

                try
                {
                    catalog.ForStatus(s.Kind);
                }
                catch (EngineException)
                {
                    errors.Add(where + ": unknown status kind '" + s.Kind + "'.");
                }

                if (s.Owner < -1 || s.Owner >= players || s.ExpiryPlayer < -1 || s.ExpiryPlayer >= players || s.DormantUntilTurnOf < -1 || s.DormantUntilTurnOf >= players)
                {
                    errors.Add(where + ": status references an unknown player.");
                }
            }
        }
    }
}
