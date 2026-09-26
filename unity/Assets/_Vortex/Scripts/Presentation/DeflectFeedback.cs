using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// An attack deflected (<see cref="GameEventType.AttackRedirected"/>: Player = new target, Other = the deflecting
    /// ship, ARB-88): the deflecting ship's shield flares up, and the attack remembers it, so that the bolt, fired after
    /// the dice (<see cref="LaserFeedback"/>), bounces off that shield towards the new target.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Déviation", fileName = "DeflectFeedback")]
    public sealed class DeflectFeedback : FeedbackAsset
    {
        [Tooltip("Durée de l'éclat du bouclier qui dévie, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 0.6f;
        [Tooltip("Éclat du bouclier : au-dessus du seuil du Bloom (1,5), il rayonne.")]
        [SerializeField, Min(0f)] private float brightness = 3f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.5f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            stage.Attack.Deflect(gameEvent.Other);
            Transform? deflector = stage.AnchorFor(FeedbackAnchor.Other, gameEvent);
            if (deflector != null)
            {
                Color color = (stage.Theme != null ? stage.Theme.Seat(gameEvent.Other) : Color.white) * brightness;
                ShieldBubble.Show(deflector, color, 1f, seconds / Mathf.Max(0.01f, stage.PlaybackSpeed), stage.Theme != null ? stage.Theme.ShieldLook : null, 0f, 0.5f);
            }

            return wait;
        }
    }
}
