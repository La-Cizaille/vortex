using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Damage (<see cref="GameEventType.HpLost"/> from an attack or sent back, ANIMATIONS.md §2), graded by how much the
    /// ship loses (its severity, from 0 to 1 at <c>heavyDamage</c> HP):
    /// <list type="bullet">
    /// <item>the impact grows with it: a bigger flash, more and faster sparks, hull debris from a medium hit, a
    /// shockwave ring for a heavy one;</item>
    /// <item>the ship is thrown back away from the middle of the table, then shaken by a few smaller jolts in random
    /// directions and spun around its axes, the harder the more it loses.</item>
    /// </list>
    /// Every jolt comes back: the ship ends where it was, unless it is destroyed (<see cref="ShipMotion.Wreck"/>). Other
    /// losses (Torment, events, costs) only wait: they have their own animations later.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Recul aux dégâts", fileName = "KnockbackFeedback")]
    public sealed class KnockbackFeedback : FeedbackAsset
    {
        [Tooltip("Perte de PV qui donne l'impact et la projection les plus forts.")]
        [SerializeField, Min(1)] private int heavyDamage = 10;
        [Tooltip("Projection la plus faible puis la plus forte, en unités de la scène.")]
        [SerializeField, Min(0f)] private float lightThrow = 0.2f;
        [SerializeField, Min(0f)] private float heavyThrow = 1.1f;
        [Tooltip("Rotation la plus forte du vaisseau projeté, en degrés (tangage, lacet, roulis).")]
        [SerializeField] private Vector3 heavySpin = new Vector3(18f, 28f, 45f);
        [Tooltip("Nombre de secousses qui suivent la projection, pour l'impact le plus fort.")]
        [SerializeField, Range(0, 8)] private int heavyJolts = 4;
        [Tooltip("Durée de la projection, puis du retour, en secondes à vitesse normale.")]
        [SerializeField, Min(0.01f)] private float outSeconds = 0.09f;
        [SerializeField, Min(0.01f)] private float backSeconds = 0.9f;
        [Tooltip("Impact posé sur la coque (effet provisoire sinon). Son échelle suit la gravité.")]
        [SerializeField] private GameObject? sparks;
        [Tooltip("Éclat des effets lumineux : au-dessus du seuil du Bloom (1,5), ils rayonnent.")]
        [SerializeField, Min(0f)] private float brightness = 3f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.45f;
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
            float severity = Mathf.Clamp01((float)gameEvent.Amount / heavyDamage);
            Transform? centre = stage.AnchorFor(FeedbackAnchor.Table, gameEvent);
            Vector3 away = centre != null ? ship.position - centre.position : -ship.forward;
            away.y = 0f;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : -ship.forward;
            float scale = ship.lossyScale.x;

            Throw(stage.MotionOf(gameEvent.Player), away, severity, scale, speed);
            Impact(ShipParts.HullOf(ship), away, severity, scale, speed, stage);
            return wait;
        }

        private static Vector3 RandomSigns() =>
            new Vector3(Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 1f);

        // The throw, then the jolts: each one comes back by itself, so the ship ends where it was.
        private void Throw(ShipMotion? motion, Vector3 away, float severity, float scale, float speed)
        {
            if (motion == null)
            {
                return;
            }

            // Cosmetic chaos only, never a game value.
            float distance = Mathf.Lerp(lightThrow, heavyThrow, severity) * scale;
            Vector3 spin = Vector3.Scale(heavySpin * Mathf.Lerp(0.2f, 1f, severity), RandomSigns());
            motion.Push((away + (Vector3.up * 0.25f * severity)) * distance, spin, outSeconds / speed, backSeconds / speed);

            int jolts = Mathf.RoundToInt(heavyJolts * severity);
            for (int i = 0; i < jolts; i++)
            {
                float fade = 1f - ((float)i / Mathf.Max(1, jolts));
                Vector3 direction = Random.onUnitSphere;
                direction.y *= 0.4f;
                Vector3 shake = Vector3.Scale(heavySpin * 0.5f * severity * fade, RandomSigns());
                float delay = (outSeconds + (0.08f * (i + 1))) / speed;
                motion.Push(direction * distance * 0.35f * fade, shake, 0.05f / speed, 0.25f / speed, delay);
            }
        }

        private void Impact(Vector3 hull, Vector3 away, float severity, float scale, float speed, IFeedbackStage stage)
        {
            if (sparks != null)
            {
                GameObject shown = Instantiate(sparks, hull, Quaternion.LookRotation(away));
                shown.transform.localScale *= Mathf.Lerp(0.6f, 1.6f, severity);
                Destroy(shown, 1.5f / speed);
                return;
            }

            float seconds = Mathf.Lerp(0.35f, 0.8f, severity) / speed;
            PlaceholderEffect effect = PlaceholderEffect.Create("Impact", hull, seconds, stage.Theme != null ? stage.Theme.GlowMaterial : null);

            // The flash, bigger for a heavier hit.
            effect.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, new Color(1.6f, 1.1f, 0.6f) * brightness, Vector3.zero, Quaternion.identity, Vector3.one * Mathf.Lerp(0.25f, 0.9f, severity) * scale)
            { Duration = Mathf.Lerp(0.12f, 0.25f, severity) / speed, Rise = 0.2f, Glow = true });

            // Sparks, more and faster; cosmetic scatter only.
            int count = Mathf.RoundToInt(Mathf.Lerp(6f, 28f, severity));
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = (away + (Random.insideUnitSphere * 1.1f)).normalized;
                effect.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, new Color(1.4f, 0.8f, 0.25f) * brightness, Vector3.zero, Quaternion.LookRotation(direction), new Vector3(0.05f, 0.05f, 0.16f) * scale)
                { Velocity = direction * Random.Range(2f, 3f + (4f * severity)) * scale * speed, Duration = Random.Range(0.3f, 1f) * seconds, Rise = 0.1f, Glow = true });
            }

            // Pieces of the hull from a medium hit on: dark, flying away.
            int debris = Mathf.RoundToInt(Mathf.Max(0f, severity - 0.3f) * 12f);
            for (int i = 0; i < debris; i++)
            {
                Vector3 direction = (away + (Random.insideUnitSphere * 0.9f)).normalized;
                effect.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Cube, new Color(0.22f, 0.22f, 0.25f), Vector3.zero, Random.rotation, Vector3.one * Random.Range(0.05f, 0.14f) * scale)
                { Velocity = direction * Random.Range(1.5f, 3.5f) * scale * speed, Rise = 0.05f });
            }

            // A shockwave for a heavy hit.
            if (severity >= 0.6f)
            {
                effect.AddRing(new Color(1.4f, 0.9f, 0.4f) * brightness * 0.6f, 18, Mathf.Lerp(1.2f, 2.4f, severity) * scale, 0.05f * scale, 0.45f / speed);
            }
        }
    }
}
