using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// An attack that lands (<see cref="GameEventType.AttackResolved"/>, after its dice, ANIMATIONS.md §2): a beam in the
    /// attacker's colour goes from its ship to the target's, thicker for a stronger attack, and the attacker recoils a
    /// little. The target is the final one: a deflected attack goes to the new target. With a prefab, the prefab is
    /// stretched along the beam (its forward axis, one unit long); without one, a placeholder beam is drawn.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Rayon d'attaque", fileName = "BeamFeedback")]
    public sealed class BeamFeedback : FeedbackAsset
    {
        [Tooltip("Rayon à étirer entre les deux vaisseaux (axe avant, une unité de long). Vide : un rayon provisoire.")]
        [SerializeField] private GameObject? prefab;
        [Tooltip("Durée du rayon, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 0.5f;
        [Tooltip("Épaisseur du rayon provisoire pour une attaque faible.")]
        [SerializeField, Min(0.01f)] private float width = 0.06f;
        [Tooltip("Épaisseur du rayon provisoire pour une attaque forte.")]
        [SerializeField, Min(0.01f)] private float strongWidth = 0.18f;
        [Tooltip("Valeur d'attaque qui donne l'épaisseur la plus forte.")]
        [SerializeField, Min(1)] private int strongValue = 16;
        [Tooltip("Recul du vaisseau qui tire, en unités de la scène.")]
        [SerializeField, Min(0f)] private float recoil = 0.25f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.45f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? attacker = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            Transform? target = stage.AnchorFor(FeedbackAnchor.Other, gameEvent);
            if (attacker == null || target == null || attacker == target)
            {
                return wait;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            Vector3 from = ShipParts.MuzzleOf(attacker);
            Vector3 to = ShipParts.HullOf(target);
            Vector3 along = to - from;
            float strength = Mathf.Clamp01((float)gameEvent.Value / strongValue);
            Color color = Color.Lerp(stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.white, Color.white, strength * 0.35f);

            if (prefab != null)
            {
                GameObject beam = Instantiate(prefab, from, Quaternion.LookRotation(along));
                beam.transform.localScale = new Vector3(1f, 1f, along.magnitude);
                Destroy(beam, seconds / speed);
            }
            else
            {
                float thickness = Mathf.Lerp(width, strongWidth, strength);
                PlaceholderEffect.Create("Rayon", from + (along * 0.5f), seconds / speed)
                    .Add(PrimitiveType.Cylinder, color, Vector3.zero, Quaternion.FromToRotation(Vector3.up, along), new Vector3(thickness, along.magnitude * 0.5f, thickness), Vector3.zero, Vector3.up);
            }

            stage.MotionOf(gameEvent.Player)?.Push(-along.normalized * recoil, 0.06f / speed, 0.35f / speed);
            return wait;
        }
    }
}
