using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A critical hit (<see cref="GameEventType.CriticalHit"/>, ANIMATIONS.md §3). The engine says it before the shot is
    /// drawn: the attack remembers it, and the bolt (<see cref="LaserFeedback"/>) strikes with a bigger flash and shakes
    /// the camera at the impact.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Coup critique", fileName = "CriticalFeedback")]
    public sealed class CriticalFeedback : FeedbackAsset
    {
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.1f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            stage.Attack.MarkCritical();
            return wait;
        }
    }
}
