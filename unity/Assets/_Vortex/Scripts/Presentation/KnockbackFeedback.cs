using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Damage (<see cref="GameEventType.HpLost"/> from an attack or sent back, ANIMATIONS.md §2): the ship is thrown back,
    /// away from the middle of the table, the further the more it loses, then comes back; a few sparks fly off the hull.
    /// Other losses (Torment, events, costs) only wait: they have their own animations later.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Recul aux dégâts", fileName = "KnockbackFeedback")]
    public sealed class KnockbackFeedback : FeedbackAsset
    {
        [Tooltip("Recul par point de vie perdu, en unités de la scène.")]
        [SerializeField, Min(0f)] private float perPoint = 0.05f;
        [Tooltip("Recul maximal, en unités de la scène.")]
        [SerializeField, Min(0f)] private float maximum = 0.7f;
        [Tooltip("Durée du recul puis du retour, en secondes à vitesse normale.")]
        [SerializeField, Min(0.01f)] private float outSeconds = 0.1f;
        [SerializeField, Min(0.01f)] private float backSeconds = 0.6f;
        [Tooltip("Étincelles à l'impact (effet provisoire). Vide : des éclats générés.")]
        [SerializeField] private GameObject? sparks;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.35f;
        [Tooltip("Attente pour une perte qui ne vient pas d'une attaque.")]
        [SerializeField, Min(0f)] private float otherWait = 0.15f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            if (gameEvent.Cause != HpLossCause.Attack && gameEvent.Cause != HpLossCause.Reflect)
            {
                return otherWait;
            }

            Transform? ship = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            if (ship == null || gameEvent.Amount <= 0)
            {
                return wait;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            Transform? centre = stage.AnchorFor(FeedbackAnchor.Table, gameEvent);
            Vector3 away = centre != null ? ship.position - centre.position : -ship.forward;
            away.y = 0f;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : -ship.forward;
            float distance = Mathf.Min(maximum, perPoint * gameEvent.Amount);
            stage.MotionOf(gameEvent.Player)?.Push(away * distance, outSeconds / speed, backSeconds / speed);

            Vector3 hull = ShipParts.HullOf(ship);
            if (sparks != null)
            {
                Destroy(Instantiate(sparks, hull, Quaternion.LookRotation(away)), 1f / speed);
            }
            else
            {
                PlaceholderEffect effect = PlaceholderEffect.Create("Étincelles", hull, 0.45f / speed, stage.Theme != null ? stage.Theme.GlowMaterial : null);
                for (int i = 0; i < 8; i++)
                {
                    // Cosmetic scatter only, never a game value.
                    Vector3 direction = (away + (Random.insideUnitSphere * 0.9f)).normalized;
                    effect.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, new Color(4f, 2.4f, 0.8f), Vector3.zero, Quaternion.identity, Vector3.one * 0.08f)
                    { Velocity = direction * 3f * speed, Rise = 0.1f, Glow = true });
                }
            }

            return wait;
        }
    }
}
