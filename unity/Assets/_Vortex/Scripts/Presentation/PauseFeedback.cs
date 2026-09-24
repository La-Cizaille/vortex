using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>Only waits: the simplest placeholder, which gives the rhythm of the game.</summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Pause", fileName = "Pause")]
    public sealed class PauseFeedback : FeedbackAsset
    {
        [SerializeField, Min(0f)] private float seconds = 0.2f;

        /// <summary>Sets the pause (editor setup and tests).</summary>
        public void Configure(float pauseSeconds) => seconds = Mathf.Max(0f, pauseSeconds);

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage) => seconds;
    }
}
