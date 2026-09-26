using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Hit points regained (<see cref="GameEventType.HpGained"/>, ANIMATIONS.md §3): green sparkles, like repair nanites,
    /// rise from the ship's hull, more of them the more it regains. With a prefab, the prefab is the repair effect.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Réparation", fileName = "RepairFeedback")]
    public sealed class RepairFeedback : FeedbackAsset
    {
        [Tooltip("Effet de réparation posé sur la coque. Vide : des étincelles provisoires.")]
        [SerializeField] private GameObject? prefab;
        [Tooltip("Couleur des étincelles ; au-dessus de 1,5, elles rayonnent.")]
        [SerializeField, ColorUsage(false, true)] private Color nanites = new Color(0.6f, 2.4f, 1f);
        [Tooltip("PV regagnés qui donnent l'effet le plus fort.")]
        [SerializeField, Min(1)] private int bigRepair = 8;
        [Tooltip("Durée de l'effet, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 1.1f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.4f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? ship = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            if (ship == null || gameEvent.Amount <= 0)
            {
                return wait;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            Vector3 hull = ShipParts.HullOf(ship);
            if (prefab != null)
            {
                TimedRemoval.After(Instantiate(prefab, hull, Quaternion.identity, ship), seconds / speed);
                return wait;
            }

            // Cosmetic scatter only, never a game value.
            float scale = ship.lossyScale.x;
            int count = Mathf.RoundToInt(Mathf.Lerp(8f, 26f, Mathf.Clamp01((float)gameEvent.Amount / bigRepair)));
            PlaceholderEffect effect = PlaceholderEffect.Create("Réparation", hull, seconds / speed, stage.Theme != null ? stage.Theme.GlowMaterial : null);
            for (int i = 0; i < count; i++)
            {
                Vector3 start = Vector3.Scale(Random.insideUnitSphere, new Vector3(0.9f, 0.3f, 1.1f)) * scale;
                effect.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, nanites, start, Quaternion.identity, Vector3.one * Random.Range(0.04f, 0.09f) * scale)
                { Velocity = Vector3.up * Random.Range(0.6f, 1.4f) * scale * speed, Delay = Random.Range(0f, 0.4f) / speed, Duration = 0.7f / speed, Rise = 0.3f, Flicker = 0.5f, Glow = true });
            }

            return wait;
        }
    }
}
