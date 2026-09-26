using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// An attack that lands (<see cref="GameEventType.AttackResolved"/>, after its dice, ANIMATIONS.md §2): the attacker,
    /// already turned towards its target (<see cref="AimFeedback"/>), fires a laser bolt in its colour, with a flash at the
    /// muzzle; the bolt flies to the target and flashes on impact; the attacker recoils, then turns back. The next event
    /// (the damage) waits for the impact. With a prefab, the prefab is the bolt, flying along its forward axis.
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
        [Tooltip("Temps pour revenir à sa place après le tir, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float returnSeconds = 0.5f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? attacker = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            Transform? target = stage.AnchorFor(FeedbackAnchor.Other, gameEvent);
            if (attacker == null || target == null || attacker == target)
            {
                return 0.2f;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            Vector3 to = ShipParts.HullOf(target);
            ShipMotion? motion = stage.MotionOf(gameEvent.Player);

            // Aimed at the final target: a deflected attack turns to its new target first.
            motion?.Aim(to, aimSeconds / speed);

            // The bolt leaves the nose once the ship faces the target.
            Vector3 flat = to - attacker.position;
            flat.y = 0f;
            Vector3 heading = flat.sqrMagnitude > 0.0001f ? flat.normalized : attacker.forward;
            float scale = attacker.lossyScale.x;
            Vector3 from = attacker.position + (heading * 0.9f * scale) + (Vector3.up * 0.12f * scale);
            Vector3 along = to - from;
            float travel = along.magnitude / boltSpeed;
            float delay = aimSeconds;
            Color color = (stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.white) * brightness;
            Color flash = color * 0.5f;

            if (prefab != null)
            {
                GameObject bolt = Instantiate(prefab, from, Quaternion.LookRotation(along));
                bolt.AddComponent<ProjectileFlight>().Fly(from, to, delay / speed, travel / speed);
            }
            else
            {
                Material? glow = stage.Theme != null ? stage.Theme.GlowMaterial : null;
                Quaternion lengthwise = Quaternion.FromToRotation(Vector3.up, along);
                PlaceholderEffect.Create("Tir laser", from, (delay + travel + 0.25f) / speed, glow)
                    .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, flash, Vector3.zero, Quaternion.identity, Vector3.one * 0.22f * scale)
                    { Delay = delay / speed, Duration = 0.12f / speed, Rise = 0.3f, Glow = true })
                    .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Capsule, color, Vector3.zero, lengthwise, new Vector3(boltWidth, boltLength * 0.5f, boltWidth))
                    { Delay = delay / speed, Duration = travel / speed, Velocity = along / travel * speed, Rise = 0.05f, FixedAxes = Vector3.up, Glow = true })
                    .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, flash, along, Quaternion.identity, Vector3.one * 0.3f * target.lossyScale.x)
                    { Delay = (delay + travel) / speed, Duration = 0.25f / speed, Rise = 0.25f, Glow = true });
            }

            motion?.Push(-heading * recoil * scale, 0.06f / speed, 0.35f / speed, delay / speed);
            motion?.Release(returnSeconds / speed, (delay + travel + 0.1f) / speed);

            // The damage plays on impact.
            return delay + travel;
        }
    }
}
