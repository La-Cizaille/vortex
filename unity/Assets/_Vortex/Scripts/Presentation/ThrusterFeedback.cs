using System.Collections.Generic;
using UnityEngine;
using Vortex.Core.Content;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A combo (<see cref="GameEventType.TechnologyActivated"/>, ANIMATIONS.md §2): the ship powers up. A pulse in the
    /// technology's colour spreads around it, then each engine lights up with an afterburner jet (a white-hot core in a
    /// flickering sheath of the technology's colour) while the ship surges forward. The jets start from each
    /// <c>Reacteur…</c> marker of the model (behind the ship without one); with a prefab, the prefab is the jet.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Réacteurs", fileName = "ThrusterFeedback")]
    public sealed class ThrusterFeedback : FeedbackAsset
    {
        [Tooltip("Jet posé sur chaque réacteur, orienté vers l'arrière. Vide : un jet provisoire.")]
        [SerializeField] private GameObject? prefab;
        [Tooltip("Durée de la poussée, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 1.4f;
        [Tooltip("Longueur du jet provisoire, en unités de la scène (à l'échelle du vaisseau).")]
        [SerializeField, Min(0.1f)] private float length = 1.8f;
        [Tooltip("Rayon de l'onde autour du vaisseau, en unités de la scène (à l'échelle du vaisseau).")]
        [SerializeField, Min(0.1f)] private float pulseRadius = 2.2f;
        [Tooltip("Éclat des effets lumineux : au-dessus du seuil du Bloom (1,5), ils rayonnent.")]
        [SerializeField, Min(0f)] private float brightness = 2.5f;
        [Tooltip("Élan du vaisseau vers l'avant, en unités de la scène.")]
        [SerializeField, Min(0f)] private float surge = 0.35f;
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
            Color color = stage.Theme != null ? stage.Theme.Technology((TechColor)gameEvent.Value) : Color.cyan;
            Material? glow = stage.Theme != null ? stage.Theme.GlowMaterial : null;
            float scale = ship.lossyScale.x;

            // The power-up: a ring of sparks in the technology's colour spreads around the ship and fades.
            PlaceholderEffect.Create("Onde de combo", ship.position + (Vector3.up * 0.1f * scale), 0.8f / speed, glow)
                .AddRing(color * brightness, 20, pulseRadius * scale, 0.07f * scale, 0.8f / speed);

            IReadOnlyList<Transform> engines = ShipParts.EnginesOf(ship, out Vector3 behind);
            ShipMotion? motion = stage.MotionOf(gameEvent.Player);
            Transform carrier = motion != null ? motion.Body : ship;
            if (engines.Count == 0)
            {
                Jet(behind, ship, carrier, color, glow, scale, speed);
            }

            foreach (Transform engine in engines)
            {
                Jet(engine.position, ship, carrier, color, glow, scale, speed);
            }

            motion?.Push(ship.forward * surge * scale, 0.25f / speed, 0.9f / speed, 0.15f / speed);
            return wait;
        }

        // One afterburner: an ignition flash, then a long flickering jet pointing backwards. It rides on the moving body,
        // so it stays on its engine while the ship surges and sways.
        private void Jet(Vector3 at, Transform ship, Transform carrier, Color color, Material? glow, float scale, float speed)
        {
            Quaternion backwards = Quaternion.LookRotation(-ship.forward);
            if (prefab != null)
            {
                GameObject jet = Instantiate(prefab, at, backwards, carrier);
                Destroy(jet, seconds / speed);
                return;
            }

            Vector3 back = -ship.forward;
            Quaternion lengthwise = Quaternion.FromToRotation(Vector3.up, back);
            float delay = 0.15f / speed;
            float burn = seconds / speed;
            Color core = Color.Lerp(color, Color.white, 0.6f) * brightness;
            PlaceholderEffect effect = PlaceholderEffect.Create("Postcombustion", at, delay + burn, glow);
            effect.transform.SetParent(carrier, true);
            effect.transform.rotation = Quaternion.identity;
            effect
                .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, core, Vector3.zero, Quaternion.identity, Vector3.one * 0.25f * scale)
                { Duration = 0.2f / speed, Rise = 0.3f, Glow = true })
                .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Capsule, color * brightness * 0.8f, back * length * 0.55f * scale, lengthwise, new Vector3(0.26f, length * 0.55f, 0.26f) * scale)
                { Delay = delay, Duration = burn, Rise = 0.15f, Flicker = 0.45f, Glow = true })
                .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Capsule, core, back * length * 0.3f * scale, lengthwise, new Vector3(0.1f, length * 0.32f, 0.1f) * scale)
                { Delay = delay, Duration = burn, Rise = 0.1f, Flicker = 0.25f, Glow = true });
        }
    }
}
