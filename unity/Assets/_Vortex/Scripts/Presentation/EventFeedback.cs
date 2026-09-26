using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The round's event revealed (<see cref="GameEventType.EventRevealed"/>, ANIMATIONS.md §2): the theme's effect for
    /// that event, if the designer gave one (a storm, a black hole, the end of times…), plays in the middle of the table;
    /// otherwise a wave of light sweeps the table. The effects are looked up by content id, like the illustrations: the
    /// code names no event.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Événement de manche", fileName = "EventFeedback")]
    public sealed class EventFeedback : FeedbackAsset
    {
        [Tooltip("Durée de l'effet de l'événement, en secondes à vitesse normale.")]
        [SerializeField, Min(0.1f)] private float seconds = 3f;
        [Tooltip("Rayon de la vague provisoire, en unités de la scène.")]
        [SerializeField, Min(0.5f)] private float radius = 9f;
        [Tooltip("Couleur de la vague provisoire ; au-dessus de 1,5, elle rayonne.")]
        [SerializeField, ColorUsage(false, true)] private Color wave = new Color(1.6f, 1.3f, 2.4f);
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.8f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? centre = stage.AnchorFor(FeedbackAnchor.Table, gameEvent);
            if (centre == null)
            {
                return wait;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            GameObject? effect = stage.Theme != null && gameEvent.Id != null ? stage.Theme.EventEffectFor(gameEvent.Id) : null;
            if (effect != null)
            {
                TimedRemoval.After(Instantiate(effect, centre.position, Quaternion.identity), seconds / speed);
                return wait;
            }

            PlaceholderEffect.Create("Vague d'événement", centre.position + (Vector3.up * 0.2f), 1.2f / speed, stage.Theme != null ? stage.Theme.GlowMaterial : null)
                .AddRing(wave, 48, radius, 0.09f, 1.2f / speed);
            return wait;
        }
    }
}
