using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// An attack declared (<see cref="GameEventType.AttackDeclared"/>, ANIMATIONS.md §2): the attacker turns its nose
    /// towards its target and holds it while the dice roll; <see cref="LaserFeedback"/> fires, then turns it back.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Visée", fileName = "AimFeedback")]
    public sealed class AimFeedback : FeedbackAsset
    {
        [Tooltip("Temps pour se tourner vers la cible, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float turnSeconds = 0.4f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.4f;
        [Tooltip("Si le tir ne vient pas, le vaisseau revient à sa place après ce temps, en secondes à vitesse normale.")]
        [SerializeField, Min(0.5f)] private float giveUpSeconds = 5f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? target = stage.AnchorFor(FeedbackAnchor.Other, gameEvent);
            if (target != null)
            {
                float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
                ShipMotion? motion = stage.MotionOf(gameEvent.Player);
                motion?.Aim(ShipParts.HullOf(target), turnSeconds / speed);

                // Should the attack never land, the ship turns back by itself; the shot takes this over when it comes.
                motion?.Release(turnSeconds / speed, giveUpSeconds / speed);
            }

            return wait;
        }
    }
}
