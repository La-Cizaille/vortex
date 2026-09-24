using UnityEngine;
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
    }
}
