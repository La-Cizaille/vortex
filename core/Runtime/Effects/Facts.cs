using System.Collections.Generic;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Core.Effects
{
    /// <summary>An attack being resolved (RULES A6). Transient: lives only during the command's resolution.</summary>
    internal sealed class AttackInfo
    {
        public AttackInfo(int attacker, int target)
        {
            Attacker = attacker;
            Target = target;
        }

        /// <summary>Attacking seat.</summary>
        public int Attacker { get; }

        /// <summary>Current target (may change once through redirection).</summary>
        public int Target { get; set; }

        /// <summary>Target before redirection, or -1.</summary>
        public int OriginalTarget { get; set; } = -1;

        /// <summary>True once redirected (an attack is redirected at most once).</summary>
        public bool Redirected => OriginalTarget >= 0;

        /// <summary>True when the attack is overcharged (token consumed or granted by an effect).</summary>
        public bool Overcharged { get; set; }

        /// <summary>Value announced before the roll by a betting effect, or null.</summary>
        public int? Bet { get; set; }

        /// <summary>Every die rolled, in order (both throws with advantage/disadvantage).</summary>
        public List<int> Rolls { get; } = new List<int>();

        /// <summary>Dice kept for the attack value.</summary>
        public List<int> Kept { get; } = new List<int>();

        /// <summary>At least one kept die shows the highest face (RULES A6 step 5).</summary>
        public bool Critical { get; set; }

        /// <summary>Attack value after modifiers (step 6).</summary>
        public int Value { get; set; }

        /// <summary>Target's effective shield (step 7).</summary>
        public int EffectiveShield { get; set; }

        /// <summary>Damage after modifiers and critical effect (steps 8-9).</summary>
        public int Damage { get; set; }

        /// <summary>HP actually lost by the target (step 10).</summary>
        public int HpLost { get; set; }

        /// <summary>Sum of kept dice.</summary>
        public int KeptSum
        {
            get
            {
                int s = 0;
                foreach (int d in Kept)
                {
                    s += d;
                }

                return s;
            }
        }
    }

    /// <summary>Dice rules of an attack (calculation "lot de dés d'attaque", RULES B2.1).</summary>
    internal sealed class DicePool
    {
        /// <summary>Dice rolled per throw.</summary>
        public int Count { get; set; }

        /// <summary>Highest dice kept per throw.</summary>
        public int Keep { get; set; }

        /// <summary>Advantage sources; they offset disadvantages one for one (RULES B4).</summary>
        public int Advantage { get; set; }

        /// <summary>Disadvantage sources.</summary>
        public int Disadvantage { get; set; }
    }

    /// <summary>An HP loss being applied (RULES A1 "perte de PV").</summary>
    internal sealed class HpLossInfo
    {
        public HpLossInfo(int player, int amount, HpLossCause cause, int sourcePlayer, EffectSource? causeEffect, AttackInfo? attack)
        {
            Player = player;
            Amount = amount;
            Cause = cause;
            SourcePlayer = sourcePlayer;
            CauseEffect = causeEffect;
            Attack = attack;
        }

        /// <summary>Player losing HP.</summary>
        public int Player { get; }

        /// <summary>Amount before the "perte de PV" calculation.</summary>
        public int Amount { get; }

        /// <summary>Cause of the loss.</summary>
        public HpLossCause Cause { get; }

        /// <summary>Player responsible (attacker, effect holder), or -1.</summary>
        public int SourcePlayer { get; }

        /// <summary>Effect that caused the loss, if any (never reacts to it).</summary>
        public EffectSource? CauseEffect { get; }

        /// <summary>Attack, when <see cref="Cause"/> is <see cref="HpLossCause.Attack"/>.</summary>
        public AttackInfo? Attack { get; }

        /// <summary>HP actually lost, set once applied.</summary>
        public int Lost { get; set; }
    }

    /// <summary>Lower and upper bounds of a player's shield (calculation "bornes du bouclier").</summary>
    internal sealed class ShieldBounds
    {
        public ShieldBounds(int min, int max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>Lower bound; the highest registered wins.</summary>
        public int Min { get; private set; }

        /// <summary>Upper bound; the lowest registered wins.</summary>
        public int Max { get; private set; }

        public void RaiseMin(int min)
        {
            if (min > Min)
            {
                Min = min;
            }
        }

        public void LowerMax(int max)
        {
            if (max < Max)
            {
                Max = max;
            }
        }

        /// <summary>Clamps a value; if bounds cross, the upper bound wins.</summary>
        public int Clamp(int value)
        {
            int v = value < Min ? Min : value;
            return v > Max ? System.Math.Max(Max, 0) : v;
        }
    }

    /// <summary>A player's elimination (RULES A9).</summary>
    internal sealed class EliminationInfo
    {
        public EliminationInfo(int player, int responsible, HpLossCause? lastCause)
        {
            Player = player;
            Responsible = responsible;
            LastCause = lastCause;
        }

        /// <summary>Eliminated seat.</summary>
        public int Player { get; }

        /// <summary>Player whose loss brought the victim to 0, or -1.</summary>
        public int Responsible { get; }

        /// <summary>Cause of that last loss.</summary>
        public HpLossCause? LastCause { get; }
    }

    /// <summary>A shield value change (reaction "bouclier modifié").</summary>
    internal sealed class ShieldChangeInfo
    {
        public ShieldChangeInfo(int player, int oldValue, int newValue, int sourcePlayer, EffectSource? causeEffect)
        {
            Player = player;
            OldValue = oldValue;
            NewValue = newValue;
            SourcePlayer = sourcePlayer;
            CauseEffect = causeEffect;
        }

        public int Player { get; }

        public int OldValue { get; }

        public int NewValue { get; }

        public int SourcePlayer { get; }

        public EffectSource? CauseEffect { get; }
    }

    /// <summary>A card leaving or entering a slot.</summary>
    internal sealed class SlotChangeInfo
    {
        public SlotChangeInfo(int player, CardSlot slot, CardInstance card)
        {
            Player = player;
            Slot = slot;
            Card = card;
        }

        public int Player { get; }

        public CardSlot Slot { get; }

        public CardInstance Card { get; }
    }
}
