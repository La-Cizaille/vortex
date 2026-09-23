namespace Vortex.Core.Content
{
    /// <summary>Technology colour of a modifier (RULES.md §1, §8). <see cref="Neutral"/> never forms a combo.</summary>
    public enum TechColor
    {
        /// <summary>No technology.</summary>
        Neutral = 0,
        /// <summary>"Ordre".</summary>
        Blue = 1,
        /// <summary>"Rebelles".</summary>
        Red = 2,
        /// <summary>"Abomination organique".</summary>
        Green = 3,
        /// <summary>"Casino Cosmique".</summary>
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

    /// <summary>How a modifier's effect is used (RULES.md §5.3).</summary>
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
