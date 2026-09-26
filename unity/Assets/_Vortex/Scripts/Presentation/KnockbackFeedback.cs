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
    /// <item>the ship is thrown like a ball on an elastic (playtest 4), in the direction of the shot, tumbling, the harder
    /// the more it loses; the elastic brings it back, a little past its place, and it settles.</item>
    /// </list>
    /// It always ends where it was, unless it is destroyed (<see cref="ShipMotion.Wreck"/>): then it drifts away. Damage
    /// sent back (cause Reflect) first flies back as a bolt from the ship that returns it. Other losses (Torment, events,
    /// costs) only wait: they have their own animations.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Recul aux dégâts", fileName = "KnockbackFeedback")]
    public sealed class KnockbackFeedback : FeedbackAsset
    {
        [Tooltip("Perte de PV qui donne l'impact et la projection les plus forts.")]
        [SerializeField, Min(1)] private int heavyDamage = 10;
        [Tooltip("Projection la plus faible puis la plus forte, en unités de la scène.")]
        [SerializeField, Min(0f)] private float lightThrow = 0.2f;
        [SerializeField, Min(0f)] private float heavyThrow = 1.1f;
        [Tooltip("Rotation la plus forte du vaisseau projeté, en degrés (tangage, lacet, roulis) ; l'élastique le ramène.")]
        [SerializeField] private Vector3 heavySpin = new Vector3(18f, 28f, 45f);
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
            // Away from the ship that caused the loss when there is one (the shot's direction), else from the middle.
            Transform? attacker = gameEvent.Other >= 0 ? stage.AnchorFor(FeedbackAnchor.Other, gameEvent) : null;
            Transform? centre = stage.AnchorFor(FeedbackAnchor.Table, gameEvent);
            Vector3 away = attacker != null && attacker != ship ? ship.position - attacker.position : centre != null ? ship.position - centre.position : -ship.forward;
            away.y = 0f;
            away = away.sqrMagnitude > 0.0001f ? away.normalized : -ship.forward;
            float scale = ship.lossyScale.x;

            // Sent back: the bolt returns from the ship that reflects it, and the hit waits for it.
            float delay = 0f;
            Transform? source = gameEvent.Cause == HpLossCause.Reflect && gameEvent.Other >= 0 ? stage.AnchorFor(FeedbackAnchor.Other, gameEvent) : null;
            if (source != null && source != ship)
            {
                delay = ReturnBolt(source, ship, stage, speed);
            }

            Throw(stage.MotionOf(gameEvent.Player), away, severity, scale, speed, delay);
            Impact(ShipParts.HullOf(ship), away, severity, scale, speed, stage, delay);
            return wait + delay;
        }

        private static Vector3 RandomSigns() =>
            new Vector3(Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 1f, Random.value < 0.5f ? -1f : 1f);

        private static float ReturnBolt(Transform source, Transform ship, IFeedbackStage stage, float speed)
        {
            Vector3 from = ShipParts.HullOf(source);
            Vector3 along = ShipParts.HullOf(ship) - from;
            float travel = along.magnitude / 20f;

            // A white-hot bolt: the damage is the attacker's own, sent back.
            var color = new Color(3f, 3f, 3.6f);
            PlaceholderEffect.Create("Renvoi", from, travel / speed, stage.Theme != null ? stage.Theme.GlowMaterial : null)
                .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Capsule, color, Vector3.zero, Quaternion.FromToRotation(Vector3.up, along), new Vector3(0.12f, 0.45f, 0.12f))
                { Velocity = along / travel * speed, Duration = travel / speed, Rise = 0.05f, FixedAxes = Vector3.up, Glow = true });
            return travel;
        }

        // Thrown like a ball on an elastic (playtest 4): away from the shot, tumbling, harder the heavier the hit; the
        // elastic brings it back. Cosmetic chaos only, never a game value.
        private void Throw(ShipMotion? motion, Vector3 away, float severity, float scale, float speed, float delay)
        {
            if (motion == null)
            {
                return;
            }

            // The distance the elastic lets it reach, turned into a starting speed; a little random slant.
            float reach = Mathf.Lerp(lightThrow, heavyThrow, severity) * scale;
            Vector3 direction = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f) * away;
            Vector3 velocity = ((direction * 1f) + (Vector3.up * 0.35f * severity)) * reach * Mathf.Sqrt(ShipMotion.Stiffness) * 1.4f * speed;
            Vector3 spin = Vector3.Scale(heavySpin * Mathf.Lerp(0.3f, 1f, severity), RandomSigns()) * 6f * speed;
            if (delay > 0f)
            {
                motion.ThrowAfter(delay / speed, velocity, spin);
                return;
            }

            motion.Throw(velocity, spin);
        }

        private void Impact(Vector3 hull, Vector3 away, float severity, float scale, float speed, IFeedbackStage stage, float delay)
        {
            if (sparks != null && delay <= 0f)
            {
                GameObject shown = Instantiate(sparks, hull, Quaternion.LookRotation(away));
                shown.transform.localScale *= Mathf.Lerp(0.6f, 1.6f, severity);
                Destroy(shown, 1.5f / speed);
                return;
            }

            float seconds = Mathf.Lerp(0.35f, 0.8f, severity) / speed;
            float start = delay / speed;
            PlaceholderEffect effect = PlaceholderEffect.Create("Impact", hull, start + seconds, stage.Theme != null ? stage.Theme.GlowMaterial : null);

            // The flash, bigger for a heavier hit.
            effect.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, new Color(1.6f, 1.1f, 0.6f) * brightness, Vector3.zero, Quaternion.identity, Vector3.one * Mathf.Lerp(0.25f, 0.9f, severity) * scale)
            { Delay = start, Duration = Mathf.Lerp(0.12f, 0.25f, severity) / speed, Rise = 0.2f, Glow = true });

            // Sparks, more and faster; cosmetic scatter only.
            int count = Mathf.RoundToInt(Mathf.Lerp(6f, 28f, severity));
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = (away + (Random.insideUnitSphere * 1.1f)).normalized;
                effect.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, new Color(1.4f, 0.8f, 0.25f) * brightness, Vector3.zero, Quaternion.LookRotation(direction), new Vector3(0.05f, 0.05f, 0.16f) * scale)
                { Velocity = direction * Random.Range(2f, 3f + (4f * severity)) * scale * speed, Delay = start, Duration = Random.Range(0.3f, 1f) * seconds, Rise = 0.1f, Glow = true });
            }

            // Pieces of the hull from a medium hit on: dark, flying away.
            int debris = Mathf.RoundToInt(Mathf.Max(0f, severity - 0.3f) * 12f);
            for (int i = 0; i < debris; i++)
            {
                Vector3 direction = (away + (Random.insideUnitSphere * 0.9f)).normalized;
                effect.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Cube, new Color(0.22f, 0.22f, 0.25f), Vector3.zero, Random.rotation, Vector3.one * Random.Range(0.05f, 0.14f) * scale)
                { Velocity = direction * Random.Range(1.5f, 3.5f) * scale * speed, Delay = start, Duration = seconds, Rise = 0.05f });
            }

            // A shockwave for a heavy hit.
            if (severity >= 0.6f)
            {
                effect.AddRing(new Color(1.4f, 0.9f, 0.4f) * brightness * 0.6f, 18, Mathf.Lerp(1.2f, 2.4f, severity) * scale, 0.05f * scale, 0.45f / speed, start);
            }
        }
    }
}
