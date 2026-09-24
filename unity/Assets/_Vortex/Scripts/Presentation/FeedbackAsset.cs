using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>Base of feedback assets, created from the Unity menu Assets &gt; Create &gt; Vortex.</summary>
    public abstract class FeedbackAsset : ScriptableObject, IFeedback
    {
        /// <inheritdoc/>
        public abstract float Play(GameEvent gameEvent, IFeedbackStage stage);
    }
}
