using System.Collections.Generic;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Rules;
using Vortex.Core.State;

namespace Vortex.Core.Effects
{
    /// <summary>
    /// Base class of every effect (card brick, event, technology, status). An effect acts on the game
    /// <b>only</b> through the interception points below (RULES B2) and the elementary actions exposed by
    /// <see cref="Game"/> (RULES B3). It never names a card (ADR-0007).
    /// </summary>
    /// <remarks>
    /// Effects are stateless and shared between games: per-instance data lives in the card copy
    /// (<see cref="CardInstance.Vars"/>) or the status (<see cref="StatusState.Vars"/>). All hooks default
    /// to "no effect", so an effect overrides only what it needs.
    /// </remarks>
    internal abstract class Effect
    {
        /// <summary>Catalog name (brick name or status kind), for diagnostics.</summary>
        public abstract string Name { get; }

        // ---------------------------------------------------------------- Activation (RULES A5.3, A8, A4)

        /// <summary>
        /// True when the effect runs once when its carrier is activated (single-use card, manual trigger,
        /// technology, event reveal) instead of acting passively while present.
        /// </summary>
        public virtual bool IsActivation => false;

        /// <summary>Activation permission ("activer"): false makes the card not activatable now.</summary>
        public virtual bool CanActivate(Game game, EffectSource self) => true;

        /// <summary>Runs the activation.</summary>
        public virtual void Activate(Game game, EffectSource self)
        {
        }

        // ---------------------------------------------------------------- Calculations (RULES B2.1)

        /// <summary>Attack set-up: may mark the attack overcharged.</summary>
        public virtual void ConfigureAttack(Game game, EffectSource self, AttackInfo attack)
        {
        }

        /// <summary>Calculation "lot de dés d'attaque".</summary>
        public virtual void ModifyAttackDice(Game game, EffectSource self, AttackInfo attack, DicePool pool)
        {
        }

        /// <summary>Calculation "valeur d'attaque".</summary>
        public virtual void ModifyAttackValue(Game game, EffectSource self, AttackInfo attack, ValueModifiers value)
        {
        }

        /// <summary>Calculation "bouclier effectif".</summary>
        public virtual void ModifyEffectiveShield(Game game, EffectSource self, AttackInfo attack, ValueModifiers shield)
        {
        }

        /// <summary>Calculation "dégâts".</summary>
        public virtual void ModifyDamage(Game game, EffectSource self, AttackInfo attack, ValueModifiers damage)
        {
        }

        /// <summary>Calculation "perte de PV" (any cause).</summary>
        public virtual void ModifyHpLoss(Game game, EffectSource self, HpLossInfo loss, ValueModifiers amount)
        {
        }

        /// <summary>Calculation "gain de PV".</summary>
        public virtual void ModifyHpGain(Game game, EffectSource self, int player, ValueModifiers amount)
        {
        }

        /// <summary>Calculation "bornes du bouclier" of <paramref name="player"/>.</summary>
        public virtual void ModifyShieldBounds(Game game, EffectSource self, int player, ShieldBounds bounds)
        {
        }

        /// <summary>Calculation "valeur d'un Tourment".</summary>
        public virtual void ModifyTormentValue(Game game, EffectSource self, ValueModifiers value)
        {
        }

        /// <summary>Calculation "cartes prenables au marché".</summary>
        public virtual void ModifyMarketPicks(Game game, EffectSource self, int player, ValueModifiers picks)
        {
        }

        /// <summary>Calculation "actions d'équipage".</summary>
        public virtual void ModifyCrewActions(Game game, EffectSource self, int player, ValueModifiers actions)
        {
        }

        // ---------------------------------------------------------------- Permissions (RULES B2.2)

        /// <summary>Permission "cibler".</summary>
        public virtual bool CanTarget(Game game, EffectSource self, int actor, int target, CrewAction action) => true;

        /// <summary>Permission "modifier un bouclier". <paramref name="sourcePlayer"/> is -1 for the game itself.</summary>
        public virtual bool CanChangeShield(Game game, EffectSource self, int sourcePlayer, int target) => true;

        /// <summary>Permission "perdre la surcharge".</summary>
        public virtual bool CanLoseOvercharge(Game game, EffectSource self, int player) => true;

        // ---------------------------------------------------------------- Reactions (RULES B2.3)

        public virtual void OnRoundStart(Game game, EffectSource self)
        {
        }

        public virtual void OnTurnStart(Game game, EffectSource self, int player)
        {
        }

        public virtual void OnMarketEnd(Game game, EffectSource self, int player)
        {
        }

        public virtual void OnEquipped(Game game, EffectSource self, SlotChangeInfo change)
        {
        }

        public virtual void OnLeftSlot(Game game, EffectSource self, SlotChangeInfo change)
        {
        }

        public virtual void OnAttackDeclared(Game game, EffectSource self, AttackInfo attack)
        {
        }

        public virtual void OnBeforeRoll(Game game, EffectSource self, AttackInfo attack)
        {
        }

        public virtual void OnAttackResolved(Game game, EffectSource self, AttackInfo attack)
        {
        }

        public virtual void OnHpLost(Game game, EffectSource self, HpLossInfo loss)
        {
        }

        public virtual void OnShieldChanged(Game game, EffectSource self, ShieldChangeInfo change)
        {
        }

        public virtual void OnTormentPlaced(Game game, EffectSource self, int cardOwner, CardInstance card)
        {
        }

        public virtual void OnPlayerEliminated(Game game, EffectSource self, EliminationInfo elimination)
        {
        }

        public virtual void OnTurnEnd(Game game, EffectSource self, int player)
        {
        }

        // ---------------------------------------------------------------- Status lifecycle

        /// <summary>For "next attack" statuses: true when this attack uses the status up.</summary>
        public virtual bool IsConsumedBy(Game game, EffectSource self, AttackInfo attack) => false;
    }

    /// <summary>Resolves effects for content ids. Built from the content files (ADR-0007) or injected by tests.</summary>
    internal interface IEffectCatalog
    {
        /// <summary>Effects of a modifier card, in declaration order.</summary>
        IReadOnlyList<Effect> ForCard(string cardId);

        /// <summary>Effects of an event card.</summary>
        IReadOnlyList<Effect> ForEvent(string eventId);

        /// <summary>Effects of a technology combo.</summary>
        IReadOnlyList<Effect> ForTechnology(TechColor color);

        /// <summary>Behaviour of a status kind; unknown kinds are an engine bug.</summary>
        Effect ForStatus(string kind);
    }
}
