using System.Collections.Generic;
using UnityEngine;
using Vortex.Core.Content;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A combo (<see cref="GameEventType.TechnologyActivated"/>, ANIMATIONS.md §2): the ship's engines flare up in the
    /// technology's colour and the ship surges forward a little. The flame starts from each <c>Reacteur…</c> marker of the
    /// model (behind the ship without one); with a prefab, the prefab is the flame.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Réacteurs", fileName = "ThrusterFeedback")]
    public sealed class ThrusterFeedback : FeedbackAsset
    {
        [Tooltip("Flamme posée sur chaque réacteur, orientée vers l'arrière. Vide : une flamme provisoire.")]
        [SerializeField] private GameObject? prefab;
        [Tooltip("Durée de la flambée, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 1.1f;
        [Tooltip("Longueur de la flamme provisoire, en unités de la scène.")]
        [SerializeField, Min(0.1f)] private float length = 1.4f;
        [Tooltip("Élan du vaisseau vers l'avant, en unités de la scène.")]
        [SerializeField, Min(0f)] private float surge = 0.2f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.8f;

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
            IReadOnlyList<Transform> engines = ShipParts.EnginesOf(ship, out Vector3 behind);
            Quaternion backwards = Quaternion.LookRotation(-ship.forward);
            float scale = ship.lossyScale.x;
            if (engines.Count == 0)
            {
                Flame(behind, backwards, color, scale, speed);
            }

            foreach (Transform engine in engines)
            {
                Flame(engine.position, backwards, color, scale, speed);
            }

            stage.MotionOf(gameEvent.Player)?.Push(ship.forward * surge, 0.15f / speed, 0.7f / speed);
            return wait;
        }

        private void Flame(Vector3 at, Quaternion backwards, Color color, float scale, float speed)
        {
            if (prefab != null)
            {
                Destroy(Instantiate(prefab, at, backwards), seconds / speed);
                return;
            }

            // A long bright core and a wider glow around it, pushed backwards.
            Vector3 back = backwards * Vector3.forward;
            PlaceholderEffect.Create("Flamme", at + (back * length * 0.5f * scale), seconds / speed)
                .Add(PrimitiveType.Sphere, Color.Lerp(color, Color.white, 0.6f), Vector3.zero, backwards, new Vector3(0.18f, 0.18f, length) * scale, Vector3.zero)
                .Add(PrimitiveType.Sphere, color, Vector3.zero, backwards, new Vector3(0.38f, 0.38f, length * 0.8f) * scale, back * 0.3f * speed);
        }
    }
}
