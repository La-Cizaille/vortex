using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// An elimination (<see cref="GameEventType.PlayerEliminated"/>, ANIMATIONS.md §3): a flash and debris burst from the
    /// ship; the table then greys the wreck, which drifts slowly (<see cref="ShipMotion.Wreck"/>). With a prefab, the
    /// prefab is the explosion.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Explosion", fileName = "ExplosionFeedback")]
    public sealed class ExplosionFeedback : FeedbackAsset
    {
        [Tooltip("Explosion posée sur le vaisseau. Vide : une explosion provisoire.")]
        [SerializeField] private GameObject? prefab;
        [Tooltip("Durée de l'explosion, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 1.2f;
        [Tooltip("Nombre d'éclats de l'explosion provisoire.")]
        [SerializeField, Range(0, 40)] private int debris = 14;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 1f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? ship = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            if (ship == null)
            {
                return wait;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            Vector3 at = ShipParts.HullOf(ship);
            if (prefab != null)
            {
                TimedRemoval.After(Instantiate(prefab, at, Quaternion.identity), seconds / speed);
                return wait;
            }

            Color hull = stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.grey;
            float scale = ship.lossyScale.x;
            PlaceholderEffect effect = PlaceholderEffect.Create("Explosion", at, seconds / speed, stage.Theme != null ? stage.Theme.GlowMaterial : null)
                .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, new Color(3f, 2.4f, 1.5f), Vector3.zero, Quaternion.identity, Vector3.one * 1.3f * scale)
                { Duration = 0.3f / speed, Rise = 0.15f, Glow = true })
                .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, new Color(2.2f, 0.9f, 0.25f), Vector3.zero, Quaternion.identity, Vector3.one * 1f * scale)
                { Velocity = Vector3.up * 0.4f * speed, Glow = true })
                .AddRing(new Color(2.5f, 1.2f, 0.4f), 24, 3f * scale, 0.08f * scale, 0.7f / speed);
            for (int i = 0; i < debris; i++)
            {
                // Cosmetic scatter only, never a game value.
                Vector3 direction = Random.onUnitSphere;
                Color color = i % 2 == 0 ? hull * 0.6f : new Color(0.25f, 0.25f, 0.28f);
                effect.Add(PrimitiveType.Cube, color, Vector3.zero, Random.rotation, Vector3.one * Random.Range(0.08f, 0.2f) * scale, direction * Random.Range(1.5f, 3.5f) * speed);
            }

            return wait;
        }
    }
}
