using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// An attack that lands (<see cref="GameEventType.AttackResolved"/>, after its dice, ANIMATIONS.md §2): the attacker,
    /// already turned towards its target (<see cref="AimFeedback"/>), fires a laser bolt in its colour, with a flash at the
    /// muzzle; the attacker recoils, then turns back. What the attack met decides the rest:
    /// <list type="bullet">
    /// <item>deflected (ARB-88, <see cref="AttackMemory"/>): the bolt hits the deflecting ship's shield, then bifurcates
    /// to the new target;</item>
    /// <item>the target's shield stopped some of it: its plasma sphere lights up where the bolt strikes, and the bolt
    /// stops there when the shield stopped it all;</item>
    /// <item>a critical hit: a bigger flash and the camera shakes.</item>
    /// </list>
    /// The next event (the damage) waits for the impact. With a prefab, the prefab is the bolt, flying along its forward
    /// axis.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Tir laser", fileName = "LaserFeedback")]
    public sealed class LaserFeedback : FeedbackAsset
    {
        [Tooltip("Projectile qui vole vers la cible, selon son axe avant. Vide : un trait lumineux provisoire.")]
        [SerializeField] private GameObject? prefab;
        [Tooltip("Temps pour finir de s'orienter vers la cible avant de tirer, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float aimSeconds = 0.15f;
        [Tooltip("Vitesse du projectile, en unités de la scène par seconde.")]
        [SerializeField, Min(1f)] private float boltSpeed = 22f;
        [Tooltip("Longueur du trait lumineux provisoire.")]
        [SerializeField, Min(0.05f)] private float boltLength = 0.9f;
        [Tooltip("Épaisseur du trait lumineux provisoire.")]
        [SerializeField, Min(0.01f)] private float boltWidth = 0.12f;
        [Tooltip("Éclat des effets lumineux : au-dessus du seuil du Bloom (1,5), ils rayonnent.")]
        [SerializeField, Min(0f)] private float brightness = 4f;
        [Tooltip("Recul du vaisseau qui tire, en unités de la scène.")]
        [SerializeField, Min(0f)] private float recoil = 0.3f;
        [Tooltip("Secousse de la caméra sur un coup critique, en unités de la scène.")]
        [SerializeField, Min(0f)] private float criticalShake = 0.18f;
        [Tooltip("Temps pour revenir à sa place après le tir, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float returnSeconds = 0.5f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            (int deflectorSeat, bool critical) = stage.Attack.Land(gameEvent.Amount);
            Transform? attacker = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            Transform? target = stage.AnchorFor(FeedbackAnchor.Other, gameEvent);
            if (attacker == null || target == null || attacker == target)
            {
                return 0.2f;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            Transform? deflector = deflectorSeat >= 0 ? stage.MotionOf(deflectorSeat)?.transform : null;
            Material? glow = stage.Theme != null ? stage.Theme.GlowMaterial : null;
            ShipMotion? motion = stage.MotionOf(gameEvent.Player);

            // Aimed at what the bolt meets first: the deflecting ship, or the target.
            Transform first = deflector != null ? deflector : target;
            Vector3 aim = ShipParts.HullOf(first);
            motion?.Aim(aim, aimSeconds / speed);

            // The bolt leaves the nose once the ship faces its aim.
            Vector3 flat = aim - attacker.position;
            flat.y = 0f;
            Vector3 heading = flat.sqrMagnitude > 0.0001f ? flat.normalized : attacker.forward;
            float scale = attacker.lossyScale.x;
            Vector3 from = attacker.position + (heading * 0.9f * scale) + (Vector3.up * 0.12f * scale);
            Color color = (stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.white) * brightness;

            // What the target's shield stopped (RULES A6.7), from the engine's figures.
            int shield = gameEvent.Values != null && gameEvent.Values.Count > 0 ? gameEvent.Values[0] : 0;
            float stopped = gameEvent.Value > 0 ? Mathf.Clamp01((float)Mathf.Min(shield, gameEvent.Value) / gameEvent.Value) : 0f;

            float time = aimSeconds;
            PlaceholderEffect? effect = prefab == null ? PlaceholderEffect.Create("Tir laser", from, 0.1f, glow) : null;
            effect?.Add(Flash(Vector3.zero, color * 0.5f, 0.22f * scale, time, 0.12f, speed));

            Vector3 start = from;
            if (deflector != null)
            {
                // The deflecting ship's shield takes the bolt and sends it on (ARB-88).
                Vector3 bounce = ShieldBubble.SurfaceTowards(deflector, start);
                time = Leg(effect, from, start, bounce, color, time, speed);
                ShieldBubble.Show(deflector, Seat(stage, deflectorSeat) * brightness, 1f, 0.45f / speed, glow, (time - 0.05f) / speed, 0.4f);
                effect?.Add(Flash(bounce - from, color * 0.6f, 0.3f * scale, time, 0.18f, speed));
                stage.MotionOf(deflectorSeat)?.Push((deflector.position - start).normalized * 0.15f * scale, 0.06f / speed, 0.3f / speed, time / speed);
                start = bounce;
            }

            Vector3 end = stopped > 0f ? ShieldBubble.SurfaceTowards(target, start) : ShipParts.HullOf(target);
            time = Leg(effect, from, start, end, color, time, speed);
            if (stopped > 0f)
            {
                ShieldBubble.Show(target, Seat(stage, gameEvent.Other) * brightness, stopped, 0.5f / speed, glow, (time - 0.05f) / speed);
            }

            float impact = critical ? 0.7f : 0.3f;
            effect?.Add(Flash(end - from, critical ? Color.white * brightness : color * 0.5f, impact * target.lossyScale.x, time, critical ? 0.35f : 0.25f, speed));
            if (critical && stage.View != null)
            {
                // Unity objects do not work with ??: a missing component is a fake null.
                CameraShake shake = stage.View.TryGetComponent(out CameraShake existing) ? existing : stage.View.gameObject.AddComponent<CameraShake>();
                shake.Shake(criticalShake, 0.45f / speed);
            }

            if (prefab != null)
            {
                GameObject bolt = Instantiate(prefab, from, Quaternion.LookRotation(end - from));
                bolt.AddComponent<ProjectileFlight>().Fly(from, end, aimSeconds / speed, (time - aimSeconds) / speed);
            }

            motion?.Push(-heading * recoil * scale, 0.06f / speed, 0.35f / speed, aimSeconds / speed);
            motion?.Release(returnSeconds / speed, (time + 0.1f) / speed);

            // The damage plays on impact.
            return time;
        }

        private static Color Seat(IFeedbackStage stage, int seat) => stage.Theme != null ? stage.Theme.Seat(seat) : Color.white;

        private static PlaceholderEffect.PieceSpec Flash(Vector3 at, Color color, float size, float time, float seconds, float speed) =>
            new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, color, at, Quaternion.identity, Vector3.one * size)
            { Delay = time / speed, Duration = seconds / speed, Rise = 0.25f, Glow = true };

        // One straight flight of the bolt, from start to end, leaving at <paramref name="time"/>; returns when it arrives.
        private float Leg(PlaceholderEffect? effect, Vector3 origin, Vector3 start, Vector3 end, Color color, float time, float speed)
        {
            Vector3 along = end - start;
            float travel = Mathf.Max(0.02f, along.magnitude / boltSpeed);
            effect?.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Capsule, color, start - origin, Quaternion.FromToRotation(Vector3.up, along), new Vector3(boltWidth, boltLength * 0.5f, boltWidth))
            { Delay = time / speed, Duration = travel / speed, Velocity = along / travel * speed, Rise = 0.05f, FixedAxes = Vector3.up, Glow = true });
            return time + travel;
        }
    }
}
