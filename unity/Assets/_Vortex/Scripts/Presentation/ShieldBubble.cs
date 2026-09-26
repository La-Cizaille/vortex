using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The ship's shield made visible (ANIMATIONS.md §2): a translucent sphere of plasma around the ship, in its seat's
    /// colour. Additive and faint, so the ship shows through it. Used when the shield stops a shot and when its value
    /// changes. A placeholder: a real plasma look (brighter at the edges, rippling where it is hit) needs its own shader,
    /// to come with the definitive effects.
    /// </summary>
    public static class ShieldBubble
    {
        /// <summary>Radius of the sphere around a ship of scale 1, in scene units.</summary>
        public const float Radius = 1.5f;

        /// <summary>
        /// Shows the shield of <paramref name="ship"/> for <paramref name="seconds"/>, after <paramref name="delay"/>, as
        /// bright as <paramref name="strength"/> (0 to 1); a <paramref name="flicker"/> makes it crackle.
        /// </summary>
        public static PlaceholderEffect Show(Transform ship, Color color, float strength, float seconds, Material? glow, float delay = 0f, float flicker = 0f)
        {
            float scale = ship.lossyScale.x;
            Vector3 centre = ShipParts.HullOf(ship);
            PlaceholderEffect bubble = PlaceholderEffect.Create("Bouclier", centre, delay + seconds, glow);
            bubble.transform.rotation = ship.rotation;
            bubble.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, color * Mathf.Lerp(0.05f, 0.16f, strength), Vector3.zero, Quaternion.identity, new Vector3(2.2f, 1.3f, 2.6f) * Radius * scale)
            { Delay = delay, Duration = seconds, Rise = 0.15f, Flicker = flicker, Glow = true });
            return bubble;
        }

        /// <summary>Where a shot coming from <paramref name="from"/> meets the shield of <paramref name="ship"/>.</summary>
        public static Vector3 SurfaceTowards(Transform ship, Vector3 from)
        {
            Vector3 centre = ShipParts.HullOf(ship);
            Vector3 towards = from - centre;
            return centre + (towards.normalized * Radius * 1.1f * ship.lossyScale.x);
        }
    }
}
