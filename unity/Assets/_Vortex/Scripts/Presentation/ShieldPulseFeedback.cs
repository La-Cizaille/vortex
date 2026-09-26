using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A shield that changes (<see cref="GameEventType.ShieldChanged"/>: Value = new, Amount = old, ANIMATIONS.md §3),
    /// whatever changed it (a reroll, a sabotage, a card): its plasma sphere recharges brightly when it rises, and
    /// crackles and dims when it falls. The brighter, the bigger the change.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Bouclier", fileName = "ShieldPulseFeedback")]
    public sealed class ShieldPulseFeedback : FeedbackAsset
    {
        [Tooltip("Durée de l'effet, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 0.7f;
        [Tooltip("Éclat de la sphère : au-dessus du seuil du Bloom (1,5), elle rayonne.")]
        [SerializeField, Min(0f)] private float brightness = 3f;
        [Tooltip("Écart de bouclier qui donne l'effet le plus fort.")]
        [SerializeField, Min(1)] private int bigChange = 6;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.35f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            int change = gameEvent.Value - gameEvent.Amount;
            Transform? ship = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            if (ship == null || change == 0)
            {
                return change == 0 ? 0f : wait;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            float strength = Mathf.Clamp01(Mathf.Abs(change) / (float)bigChange);
            Color seat = stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.white;
            Material? glow = stage.Theme != null ? stage.Theme.ShieldLook : null;
            if (change > 0)
            {
                ShieldBubble.Show(ship, Color.Lerp(seat, Color.white, 0.3f) * brightness, Mathf.Lerp(0.4f, 1f, strength), seconds / speed, glow);
            }
            else
            {
                // A falling shield crackles, tinted towards red.
                ShieldBubble.Show(ship, Color.Lerp(seat, new Color(1f, 0.25f, 0.2f), 0.5f) * brightness, Mathf.Lerp(0.3f, 0.8f, strength), seconds / speed, glow, 0f, 0.9f);
            }

            return wait;
        }
    }
}
