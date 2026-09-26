namespace Vortex.Core.Content
{
    /// <summary>Technology colour of a modifier (RULES A1, A8). <see cref="Neutral"/> never forms a combo.</summary>
    public enum TechColor
    {
        /// <summary>No technology.</summary>
        Neutral = 0,
        /// <summary>"Légion de l'Ordre" (technology Ordre).</summary>
        Blue = 1,
        /// <summary>"Front Syndical des Mutins" (technology Rebelles).</summary>
        Red = 2,
        /// <summary>"Adeptes de la Corruption" (technology Abomination organique).</summary>
        Green = 3,
        /// <summary>"La Maison" (technology Casino Cosmique).</summary>
        Yellow = 4,
    }

    /// <summary>Ship slot a modifier goes into; also identifies which black market it comes from.</summary>
    public enum CardSlot
    {
        /// <summary>Offensive modifier (ATK market).</summary>
        Attack = 0,
        /// <summary>Defensive modifier (DEF market).</summary>
        Defense = 1,
    }

    /// <summary>How a modifier's effect is used (RULES A5.3).</summary>
    public enum CardUsage
    {
        /// <summary>Effect applies as long as the card is equipped.</summary>
        Durable = 0,
        /// <summary>Activated once by the owner during the activation window, then discarded.</summary>
        SingleUse = 1,
        /// <summary>Fires automatically when the condition printed on the card occurs.</summary>
        Triggered = 2,
    }
}
