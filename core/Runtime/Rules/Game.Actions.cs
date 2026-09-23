using System;
using System.Collections.Generic;
using System.Linq;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Effects;
using Vortex.Core.Effects.Statuses;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Core.Rules
{
    /// <summary>
    /// Elementary actions (RULES B3). Each action applies by itself the calculations and permissions it is
    /// subject to, so no effect has to know which other effects exist.
    /// </summary>
    internal sealed partial class Game
    {
        // ---------------------------------------------------------------- Shields

        /// <summary>Current bounds of a player's shield (calculation "bornes du bouclier").</summary>
        public ShieldBounds ShieldBoundsOf(int player)
        {
            var bounds = new ShieldBounds(Config.MinShield, Config.MaxShield);
            foreach (KeyValuePair<Effect, EffectSource> pair in ActiveEffects())
            {
                pair.Key.ModifyShieldBounds(this, pair.Value, player, bounds);
            }

            return bounds;
        }

        /// <summary>Shield value of a player: the stored value within its current bounds.</summary>
        public int ShieldOf(int player)
        {
            return ShieldBoundsOf(player).Clamp(State.Players[player].Shield);
        }

        /// <summary>Permission "modifier un bouclier" (RULES B2.2). <paramref name="sourcePlayer"/> is -1 for the game.</summary>
        public bool CanChangeShield(int sourcePlayer, int target)
        {
            return Permits((e, s) => e.CanChangeShield(this, s, sourcePlayer, target));
        }

        /// <summary>Sets a shield value if permitted; the value is bounded. Returns false when refused.</summary>
        public bool TrySetShield(int sourcePlayer, int target, int value, EffectSource? cause)
        {
            if (!CanChangeShield(sourcePlayer, target))
            {
                Emit(new GameEvent { Type = GameEventType.ShieldChangeRefused, Player = target, Other = sourcePlayer, Id = cause?.Id });
                return false;
            }

            ApplyShield(sourcePlayer, target, value, cause);
            return true;
        }

        /// <summary>Changes a shield by a delta if permitted.</summary>
        public bool TryAddShield(int sourcePlayer, int target, int delta, EffectSource? cause)
        {
            return TrySetShield(sourcePlayer, target, ShieldOf(target) + delta, cause);
        }

        /// <summary>Swaps two shields; nothing happens unless both changes are permitted.</summary>
        public bool TrySwapShields(int sourcePlayer, int a, int b, EffectSource? cause)
        {
            if (!CanChangeShield(sourcePlayer, a) || !CanChangeShield(sourcePlayer, b))
            {
                Emit(new GameEvent { Type = GameEventType.ShieldChangeRefused, Player = a, Other = sourcePlayer, Id = cause?.Id });
                return false;
            }

            int va = ShieldOf(a);
            int vb = ShieldOf(b);
            ApplyShield(sourcePlayer, a, vb, cause);
            ApplyShield(sourcePlayer, b, va, cause);
            return true;
        }

        private void ApplyShield(int sourcePlayer, int target, int value, EffectSource? cause)
        {
            int old = ShieldOf(target);
            int bounded = ShieldBoundsOf(target).Clamp(value);
            State.Players[target].Shield = bounded;
            Emit(new GameEvent { Type = GameEventType.ShieldChanged, Player = target, Other = sourcePlayer, Value = bounded, Amount = old, Id = cause?.Id });
            var change = new ShieldChangeInfo(target, old, bounded, sourcePlayer, cause);
            Raise((e, s) => e.OnShieldChanged(this, s, change), cause);
        }

        // ---------------------------------------------------------------- Hit points

        /// <summary>
        /// Applies an HP loss (RULES A1): calculation "perte de PV", then the base overcharge rule (RULES A5.4),
        /// then reaction "perte de PV subie" (except for <see cref="HpLossCause.Reflect"/>). Returns HP lost.
        /// An attack loss is always calculated, even at 0, so "next attack suffered" effects see it.
        /// </summary>
        public int LoseHp(HpLossInfo loss)
        {
            PlayerState p = State.Players[loss.Player];
            if (p.Eliminated || (loss.Amount <= 0 && loss.Cause != HpLossCause.Attack))
            {
                return 0;
            }

            int amount = Math.Max(0, Calculate(Math.Max(0, loss.Amount), (e, s, m) => e.ModifyHpLoss(this, s, loss, m)));
            amount = Math.Min(amount, p.Hp);
            loss.Lost = amount;
            if (amount == 0)
            {
                return 0;
            }

            p.Hp -= amount;
            _lastLoss[loss.Player] = (loss.SourcePlayer, loss.Cause);
            Emit(new GameEvent { Type = GameEventType.HpLost, Player = loss.Player, Other = loss.SourcePlayer, Amount = amount, Cause = loss.Cause, Id = loss.CauseEffect?.Id });

            bool losesOvercharge = loss.Cause == HpLossCause.Attack || loss.Cause == HpLossCause.Reflect || loss.Cause == HpLossCause.Event;
            if (losesOvercharge && p.Overcharge > 0 && Permits((e, s) => e.CanLoseOvercharge(this, s, loss.Player)))
            {
                p.Overcharge = 0;
                Emit(new GameEvent { Type = GameEventType.OverchargeChanged, Player = loss.Player, Value = 0 });
            }

            if (loss.Cause != HpLossCause.Reflect)
            {
                Raise((e, s) => e.OnHpLost(this, s, loss), loss.CauseEffect);
            }

            return amount;
        }

        /// <summary>Heals a player (calculation "gain de PV"), bounded by the maximum. Returns HP gained.</summary>
        public int Heal(int player, int amount, EffectSource? cause)
        {
            PlayerState p = State.Players[player];
            if (p.Eliminated || amount <= 0)
            {
                return 0;
            }

            int gain = Math.Max(0, Calculate(amount, (e, s, m) => e.ModifyHpGain(this, s, player, m)));
            gain = Math.Min(gain, Config.MaxHp - p.Hp);
            if (gain <= 0)
            {
                return 0;
            }

            p.Hp += gain;
            Emit(new GameEvent { Type = GameEventType.HpGained, Player = player, Amount = gain, Id = cause?.Id });
            return gain;
        }

        // ---------------------------------------------------------------- Overcharge

        public void GainOvercharge(int player, int amount)
        {
            PlayerState p = State.Players[player];
            int value = Math.Min(Config.MaxOvercharge, p.Overcharge + amount);
            if (value != p.Overcharge)
            {
                p.Overcharge = value;
                Emit(new GameEvent { Type = GameEventType.OverchargeChanged, Player = player, Value = value });
            }
        }

        public void ConsumeOvercharge(int player)
        {
            PlayerState p = State.Players[player];
            if (p.Overcharge <= 0)
            {
                throw new EngineException("No overcharge token to consume.");
            }

            p.Overcharge--;
            Emit(new GameEvent { Type = GameEventType.OverchargeChanged, Player = player, Value = p.Overcharge });
        }

        // ---------------------------------------------------------------- Torments (RULES A7)

        /// <summary>Current value of one torment token (calculation "valeur d'un Tourment").</summary>
        public int TormentValue()
        {
            return Math.Max(0, Calculate(Config.TormentValue, (e, s, m) => e.ModifyTormentValue(this, s, m)));
        }

        /// <summary>
        /// Places a torment token on a card: on an equipped card its owner immediately loses one torment value;
        /// on a market card nothing else happens. Returns false if the card is neither equipped nor in a market.
        /// </summary>
        public bool PlaceTorment(int sourcePlayer, CardInstance card, EffectSource? cause)
        {
            int owner = OwnerOf(card);
            if (owner < 0 && !IsInMarket(card))
            {
                return false;
            }

            card.Torments++;
            Emit(new GameEvent { Type = GameEventType.TormentPlaced, Player = owner, Other = sourcePlayer, CardUid = card.Uid, Id = card.CardId, Value = card.Torments });
            if (owner >= 0)
            {
                LoseHp(new HpLossInfo(owner, TormentValue(), HpLossCause.Torment, sourcePlayer, cause, null));
                Raise((e, s) => e.OnTormentPlaced(this, s, owner, card), cause);
            }

            return true;
        }

        /// <summary>Removes every token from a card; returns how many were removed.</summary>
        public int RemoveTorments(CardInstance card)
        {
            int n = card.Torments;
            if (n > 0)
            {
                card.Torments = 0;
                Emit(new GameEvent { Type = GameEventType.TormentsRemoved, Player = OwnerOf(card), CardUid = card.Uid, Id = card.CardId, Amount = n });
            }

            return n;
        }

        /// <summary>Torment tokens on a player's equipped modifiers.</summary>
        public int TormentsOn(int player)
        {
            return State.Players[player].Modifiers().Sum(c => c.Torments);
        }

        /// <summary>Applies the loss of every token on a player's modifiers (turn start, reactivation).</summary>
        public void ApplyTorments(int player, EffectSource? cause)
        {
            int tokens = TormentsOn(player);
            if (tokens > 0)
            {
                LoseHp(new HpLossInfo(player, tokens * TormentValue(), HpLossCause.Torment, -1, cause, null));
            }
        }

        /// <summary>"Réactiver les Tourments" (RULES A7): every player, in resolution order.</summary>
        public void ReactivateTorments(EffectSource? cause)
        {
            foreach (int seat in AliveInTurnOrder().ToList())
            {
                ApplyTorments(seat, cause);
            }
        }

        // ---------------------------------------------------------------- Modifiers

        /// <summary>Seat holding the card, or -1.</summary>
        public int OwnerOf(CardInstance card)
        {
            foreach (PlayerState p in State.Players)
            {
                if (p.AttackSlot == card || p.DefenseSlot == card)
                {
                    return p.Seat;
                }
            }

            return -1;
        }

        public bool IsInMarket(CardInstance card)
        {
            return State.AttackMarket.Visible.Contains(card) || State.DefenseMarket.Visible.Contains(card);
        }

        /// <summary>Removes a modifier from its slot to the discard pile, with its tokens and data (RULES A7).</summary>
        public void DiscardModifier(int player, CardSlot slot, EffectSource? cause)
        {
            CardInstance? card = Detach(player, slot);
            if (card == null)
            {
                return;
            }

            ToDiscard(card);
            Emit(new GameEvent { Type = GameEventType.CardDiscarded, Player = player, CardUid = card.Uid, Id = card.CardId });
            var change = new SlotChangeInfo(player, slot, card);
            Raise((e, s) => e.OnLeftSlot(this, s, change), cause);
        }

        /// <summary>Discards every equipped modifier of a player.</summary>
        public void DiscardAllModifiers(int player, EffectSource? cause)
        {
            DiscardModifier(player, CardSlot.Attack, cause);
            DiscardModifier(player, CardSlot.Defense, cause);
        }

        /// <summary>Equips a card in its slot; the previous card of that slot is discarded without effect.</summary>
        public void Equip(int player, CardInstance card, EffectSource? cause)
        {
            CardSlot slot = Definition(card).Slot;
            DiscardModifier(player, slot, cause);
            PlayerState p = State.Players[player];
            if (slot == CardSlot.Attack)
            {
                p.AttackSlot = card;
            }
            else
            {
                p.DefenseSlot = card;
            }

            Emit(new GameEvent { Type = GameEventType.CardEquipped, Player = player, CardUid = card.Uid, Id = card.CardId });
            var change = new SlotChangeInfo(player, slot, card);
            Raise((e, s) => e.OnEquipped(this, s, change), cause);
        }

        /// <summary>Moves a modifier from one player to another (steal): tokens follow the card, per-copy data is reset.</summary>
        public void MoveModifier(int from, CardSlot slot, int to, EffectSource? cause)
        {
            CardInstance? card = Detach(from, slot);
            if (card == null)
            {
                return;
            }

            card.Vars.Clear();
            Emit(new GameEvent { Type = GameEventType.CardStolen, Player = to, Other = from, CardUid = card.Uid, Id = card.CardId });
            var left = new SlotChangeInfo(from, slot, card);
            Raise((e, s) => e.OnLeftSlot(this, s, left), cause);
            Equip(to, card, cause);
        }

        /// <summary>Exchanges a player's modifier with a visible market card of the same type.</summary>
        public void SwapWithMarket(int player, CardSlot slot, CardInstance marketCard, EffectSource? cause)
        {
            MarketState market = State.Market(slot);
            int index = market.Visible.IndexOf(marketCard);
            CardInstance? own = Detach(player, slot);
            if (index < 0 || own == null)
            {
                throw new EngineException("Invalid market swap.");
            }

            own.Vars.Clear();
            market.Visible[index] = own;
            Emit(new GameEvent { Type = GameEventType.MarketCardRevealed, CardUid = own.Uid, Id = own.CardId, Value = (int)slot });
            var left = new SlotChangeInfo(player, slot, own);
            Raise((e, s) => e.OnLeftSlot(this, s, left), cause);
            Equip(player, marketCard, cause);
        }

        private CardInstance? Detach(int player, CardSlot slot)
        {
            PlayerState p = State.Players[player];
            CardInstance? card = p.Slot(slot);
            if (slot == CardSlot.Attack)
            {
                p.AttackSlot = null;
            }
            else
            {
                p.DefenseSlot = null;
            }

            return card;
        }

        private void ToDiscard(CardInstance card)
        {
            card.Torments = 0;
            card.Vars.Clear();
            State.Market(Definition(card).Slot).Discard.Add(card);
        }

        // ---------------------------------------------------------------- Markets (RULES A5.2)

        /// <summary>Draws the top card of a deck, reshuffling the discard pile when empty (RULES A4.3).</summary>
        public CardInstance? Draw(MarketState market)
        {
            if (market.Deck.Count == 0)
            {
                if (market.Discard.Count == 0)
                {
                    return null;
                }

                market.Deck.AddRange(market.Discard);
                market.Discard.Clear();
                Shuffle(market.Deck);
            }

            CardInstance card = market.Deck[market.Deck.Count - 1];
            market.Deck.RemoveAt(market.Deck.Count - 1);
            return card;
        }

        /// <summary>Completes a market up to its size.</summary>
        public void RefillMarket(CardSlot slot)
        {
            MarketState market = State.Market(slot);
            while (market.Visible.Count < Config.MarketSize)
            {
                CardInstance? card = Draw(market);
                if (card == null)
                {
                    return;
                }

                market.Visible.Add(card);
                Emit(new GameEvent { Type = GameEventType.MarketCardRevealed, CardUid = card.Uid, Id = card.CardId, Value = (int)slot });
            }
        }

        /// <summary>Discards a market and reveals new cards.</summary>
        public void RecycleMarket(CardSlot slot)
        {
            MarketState market = State.Market(slot);
            foreach (CardInstance card in market.Visible)
            {
                ToDiscard(card);
            }

            market.Visible.Clear();
            Emit(new GameEvent { Type = GameEventType.MarketRecycled, Value = (int)slot });
            RefillMarket(slot);
        }

        /// <summary>Takes a visible market card into the player's slot and completes the market.</summary>
        public void TakeFromMarket(int player, CardSlot slot, int index, EffectSource? cause)
        {
            MarketState market = State.Market(slot);
            CardInstance card = market.Visible[index];
            market.Visible.RemoveAt(index);
            Emit(new GameEvent { Type = GameEventType.MarketCardTaken, Player = player, CardUid = card.Uid, Id = card.CardId, Value = (int)slot });
            Equip(player, card, cause);
            RefillMarket(slot);
        }

        /// <summary>Visible market cards of both markets whose colour is neutral.</summary>
        public int NeutralCardsInMarkets()
        {
            return State.AttackMarket.Visible.Concat(State.DefenseMarket.Visible).Count(c => Definition(c).Color == TechColor.Neutral);
        }

        /// <summary>Fisher-Yates shuffle with the game's generator.</summary>
        public void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = State.Rng.NextInt(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        // ---------------------------------------------------------------- Targeting and crew actions

        /// <summary>Permission "cibler" (RULES B2.2).</summary>
        public bool CanTarget(int actor, int target, CrewAction action)
        {
            return Permits((e, s) => e.CanTarget(this, s, actor, target, action));
        }

        /// <summary>Imposes a player's crew action for their next turn (RULES B3 "imposer l'action d'équipage").</summary>
        public void ImposeCrewAction(int player, CrewAction action, int target, int owner)
        {
            RemoveStatuses(s => s.Kind == StatusKinds.ForcedCrewAction && StatusesOf(player).Contains(s));
            AddStatus(player, StatusKinds.ForcedCrewAction, owner, StatusExpiry.EndOfTurn, player, dormantUntilExpiryPlayersTurn: true, vars: new[]
            {
                new KeyValuePair<string, int>(StatusKinds.VarAction, (int)action),
                new KeyValuePair<string, int>(StatusKinds.VarTarget, target),
            });
        }

        // ---------------------------------------------------------------- Eliminations and victory (RULES A9)

        /// <summary>
        /// Eliminates every alive player at 0 HP, in resolution order, then checks victory. Called at the end
        /// of each resolution step, so a player brought to 0 and healed within the same step survives (RULES A9).
        /// </summary>
        public void CheckEliminations()
        {
            if (IsOver)
            {
                return;
            }

            bool any = true;
            while (any && !IsOver)
            {
                any = false;
                foreach (int seat in AliveInTurnOrder().ToList())
                {
                    PlayerState p = State.Players[seat];
                    if (p.Eliminated || p.Hp > 0)
                    {
                        continue;
                    }

                    any = true;
                    (int source, HpLossCause cause) = _lastLoss.TryGetValue(seat, out var last) ? last : (-1, HpLossCause.Self);
                    DiscardAllModifiers(seat, null);
                    p.Eliminated = true;
                    p.Overcharge = 0;
                    foreach (StatusState s in p.Statuses.ToList())
                    {
                        RemoveStatus(seat, s);
                    }

                    Emit(new GameEvent { Type = GameEventType.PlayerEliminated, Player = seat, Other = source, Cause = cause });
                    var info = new EliminationInfo(seat, source, _lastLoss.ContainsKey(seat) ? cause : (HpLossCause?)null);
                    Raise((e, s) => e.OnPlayerEliminated(this, s, info));
                }

                CheckDomination();
            }
        }

        private void CheckDomination()
        {
            if (IsOver)
            {
                return;
            }

            List<int> alive = AliveFrom(0).ToList();
            if (alive.Count == 1)
            {
                EndGame(alive[0], WinCondition.Domination);
            }
            else if (alive.Count == 0)
            {
                EndGame(-1, WinCondition.Draw);
            }
        }

        public void EndGame(int winner, WinCondition condition)
        {
            if (IsOver)
            {
                return;
            }

            State.Outcome = new GameOutcome { Winner = winner, Condition = condition, Round = State.Round };
            State.Phase = TurnPhase.GameOver;
            Emit(new GameEvent { Type = GameEventType.GameOver, Player = winner, Value = (int)condition });
        }
    }
}
