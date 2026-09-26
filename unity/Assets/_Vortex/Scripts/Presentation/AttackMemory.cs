namespace Vortex.Client.Presentation
{
    /// <summary>
    /// What the stage remembers of the attack being played (ANIMATIONS.md §2). The engine tells an attack in several
    /// events (declared, deflected, critical, resolved); the shot is drawn at the last one and needs the others: where it
    /// is deflected, whether it is a critical hit. It only holds what the events already said.
    /// </summary>
    public sealed class AttackMemory
    {
        /// <summary>The seat whose ship deflected the attack, or -1.</summary>
        public int Deflector { get; private set; } = -1;

        /// <summary>Whether the attack is a critical hit.</summary>
        public bool Critical { get; private set; }

        /// <summary>The damage of the last attack drawn, as the engine announced it (before the target's protections).</summary>
        public int LandedDamage { get; private set; }

        /// <summary>A new attack is declared: nothing is known about it yet.</summary>
        public void Begin()
        {
            Deflector = -1;
            Critical = false;
            LandedDamage = 0;
        }

        /// <summary>The attack was deflected by <paramref name="seat"/>'s ship towards another target.</summary>
        public void Deflect(int seat) => Deflector = seat;

        /// <summary>The attack is a critical hit.</summary>
        public void MarkCritical() => Critical = true;

        /// <summary>
        /// The attack is drawn with <paramref name="damage"/>: what was remembered is given and forgotten, and the damage
        /// kept for the protections that may spare it.
        /// </summary>
        public (int Deflector, bool Critical) Land(int damage)
        {
            (int, bool) taken = (Deflector, Critical);
            Begin();
            LandedDamage = damage;
            return taken;
        }
    }
}
