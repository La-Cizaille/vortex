using UnityEngine;
using Vortex.Client.Theme;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A visual or audio reaction to one game event (ADR-0014). Feedbacks are assets edited in Unity, not code in the
    /// game logic: replacing a placeholder by a Timeline sequence, an animation or a VFX never touches the rules.
    /// </summary>
    public interface IFeedback
    {
        /// <summary>Starts the reaction; returns how long to wait before the next event, in seconds (0 for none).</summary>
        float Play(GameEvent gameEvent, IFeedbackStage stage);
    }

    /// <summary>Where feedbacks happen: the scene gives anchors, the feedback decides what to show there.</summary>
    public interface IFeedbackStage
    {
        /// <summary>The transform for <paramref name="anchor"/> about <paramref name="gameEvent"/>, or null when the scene has none.</summary>
        Transform? AnchorFor(FeedbackAnchor anchor, GameEvent gameEvent);

        /// <summary>Playback speed (1 = normal): a feedback that animates by itself runs this much faster.</summary>
        float PlaybackSpeed { get; }

        /// <summary>The camera of the table, or null (3D effects placed over the interface, like the dice, need it).</summary>
        Camera? View { get; }

        /// <summary>The theme of the game (seat and technology colours), or null when the scene has none.</summary>
        ThemeSettings? Theme { get; }

        /// <summary>A new 3D card showing <paramref name="cardId"/>, for an animation to move and then destroy; null without cards.</summary>
        CardDisplay? NewCard(string cardId);

        /// <summary>Whether the person dropped this card where it goes (then its trip is not animated); asking forgets it.</summary>
        bool TakePlacedByHand(int uid);

        /// <summary>What the stage remembers of the attack being played (deflection, critical hit).</summary>
        AttackMemory Attack { get; }

        /// <summary>How the ship of a seat moves (recoil, knockback), or null when the seat has no ship.</summary>
        ShipMotion? MotionOf(int seat);
    }

    /// <summary>Places a feedback can target; the scene resolves them for each event.</summary>
    public enum FeedbackAnchor
    {
        /// <summary>The centre of the table.</summary>
        Table = 0,

        /// <summary>The ship of the event's <see cref="GameEvent.Player"/>.</summary>
        Player = 1,

        /// <summary>The ship of the event's <see cref="GameEvent.Other"/> (target, responsible player...).</summary>
        Other = 2,

        /// <summary>The market of the event's slot.</summary>
        Market = 3,

        /// <summary>The banner showing the round and the active event.</summary>
        Banner = 4,

        /// <summary>The middle of the foreground layer, drawn above the table and its cards (dice, messages).</summary>
        Foreground = 5,
    }
}
