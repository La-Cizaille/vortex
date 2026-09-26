using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A loss an effect spared (<see cref="GameEventType.HpLossPrevented"/>, ANIMATIONS.md §2). When an attack's loss is
    /// spared entirely, the ship dodges: it swerves aside, rolling, at the moment the bolt arrives, then comes back. A loss
    /// only reduced, or not from an attack, shows a short flare of its shield.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Esquive", fileName = "DodgeFeedback")]
    public sealed class DodgeFeedback : FeedbackAsset
    {
        [Tooltip("Écart de l'esquive, en unités de la scène.")]
        [SerializeField, Min(0f)] private float swerve = 1.1f;
        [Tooltip("Roulis de l'esquive, en degrés.")]
        [SerializeField] private float roll = 55f;
        [Tooltip("Durée de l'écart, puis du retour, en secondes à vitesse normale.")]
        [SerializeField, Min(0.01f)] private float outSeconds = 0.14f;
        [SerializeField, Min(0.01f)] private float backSeconds = 0.6f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.45f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? ship = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            if (ship == null)
            {
                return wait;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);

            // The whole damage of the attack spared: no loss follows, the ship dodged the bolt.
            bool spared = gameEvent.Cause == HpLossCause.Attack && gameEvent.Amount >= stage.Attack.LandedDamage;
            if (!spared)
            {
                ShieldBubble.Show(ship, (stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.white) * 2f, 0.6f, 0.35f / speed, stage.Theme != null ? stage.Theme.ShieldLook : null);
                return wait * 0.5f;
            }

            // Aside, to the left or the right of the ship at random: cosmetic only.
            float side = Random.value < 0.5f ? -1f : 1f;
            Vector3 aside = ship.right * side * swerve * ship.lossyScale.x;
            stage.MotionOf(gameEvent.Player)?.Push(aside + (Vector3.up * 0.2f * ship.lossyScale.x), new Vector3(0f, 10f * side, -roll * side), outSeconds / speed, backSeconds / speed);
            return wait;
        }
    }
}
