using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Torment (ANIMATIONS.md §2): a token placed (<see cref="GameEventType.TormentPlaced"/>) makes a cloud of spores
    /// burst around the ship that owns the card, which shudders; tokens removed (<see cref="GameEventType.TormentsRemoved"/>)
    /// rise off it as a clear sparkle. A token on a market card has no ship: nothing plays. With a prefab, the prefab is the
    /// cloud.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Tourment", fileName = "TormentFeedback")]
    public sealed class TormentFeedback : FeedbackAsset
    {
        [Tooltip("Nuage de spores posé sur le vaisseau. Vide : un nuage provisoire.")]
        [SerializeField] private GameObject? prefab;
        [Tooltip("Couleur des spores ; au-dessus de 1,5, elles rayonnent.")]
        [SerializeField, ColorUsage(false, true)] private Color spores = new Color(1.2f, 2.2f, 0.6f);
        [Tooltip("Couleur de l'éclat quand les jetons s'en vont.")]
        [SerializeField, ColorUsage(false, true)] private Color cleansed = new Color(1.6f, 2f, 2.4f);
        [Tooltip("Durée de l'effet, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 1f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.4f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? ship = gameEvent.Player >= 0 ? stage.AnchorFor(FeedbackAnchor.Player, gameEvent) : null;
            if (ship == null)
            {
                return wait * 0.5f;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            float scale = ship.lossyScale.x;
            Vector3 hull = ShipParts.HullOf(ship);
            bool placed = gameEvent.Type == GameEventType.TormentPlaced;
            if (placed && prefab != null)
            {
                TimedRemoval.After(Instantiate(prefab, hull, Quaternion.identity, ship), seconds / speed);
            }
            else
            {
                // Cosmetic scatter only, never a game value.
                PlaceholderEffect cloud = PlaceholderEffect.Create(placed ? "Spores" : "Purification", hull, seconds / speed, stage.Theme != null ? stage.Theme.GlowMaterial : null);
                for (int i = 0; i < (placed ? 18 : 12); i++)
                {
                    Vector3 drift = placed ? Random.insideUnitSphere * 0.9f : (Vector3.up * 1.4f) + (Random.insideUnitSphere * 0.4f);
                    Vector3 start = Random.insideUnitSphere * 0.8f * scale;
                    cloud.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, placed ? spores : cleansed, start, Quaternion.identity, Vector3.one * Random.Range(0.06f, 0.16f) * scale)
                    { Velocity = drift * scale * speed, Delay = Random.Range(0f, 0.25f) / speed, Rise = 0.3f, Flicker = 0.3f, Glow = true });
                }
            }

            if (placed)
            {
                // The ship shudders, sickened.
                ShipMotion? motion = stage.MotionOf(gameEvent.Player);
                for (int i = 0; i < 3; i++)
                {
                    float side = i % 2 == 0 ? 1f : -1f;
                    motion?.Push(ship.right * 0.06f * side * scale, new Vector3(0f, 0f, 6f * side), 0.05f / speed, 0.12f / speed, i * 0.12f / speed);
                }
            }

            return wait;
        }
    }
}
