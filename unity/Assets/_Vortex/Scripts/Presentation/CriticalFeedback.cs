using System.Linq;
using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A critical hit (<see cref="GameEventType.CriticalHit"/>, ANIMATIONS.md §3). The die that made it (an 8, still on
    /// screen) leaps towards the camera and falls back with a burst of energy (playtest 4). The engine says it before the
    /// shot is drawn: the attack also remembers it, so that the bolt (<see cref="LaserFeedback"/>) strikes with a bigger
    /// flash and shakes the camera at the impact.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Coup critique", fileName = "CriticalFeedback")]
    public sealed class CriticalFeedback : FeedbackAsset
    {
        [Tooltip("Durée du saut du dé, en secondes à vitesse normale.")]
        [SerializeField, Min(0.1f)] private float leapSeconds = 0.7f;
        [Tooltip("Couleur de l'éclat d'énergie à la retombée ; au-dessus de 1,5, il rayonne.")]
        [SerializeField, ColorUsage(false, true)] private Color energy = new Color(3f, 2.2f, 0.8f);
        [Tooltip("Face du dé qui fait le critique.")]
        [SerializeField, Min(1)] private int criticalFace = 8;
        [Tooltip("Attente quand aucun dé n'est à l'écran, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.1f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            stage.Attack.MarkCritical();
            Transform? foreground = stage.AnchorFor(FeedbackAnchor.Foreground, gameEvent);
            DiceTray? tray = foreground != null ? foreground.GetComponentsInChildren<DiceTray>().LastOrDefault() : null;
            DieSpinner? die = tray != null ? tray.Dice3D.FirstOrDefault(d => d != null && d.Value == criticalFace) ?? tray.Dice3D.FirstOrDefault() : null;
            if (tray == null || die == null)
            {
                return wait;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            float seconds = leapSeconds / speed;
            tray.Extend(seconds + 0.3f);
            Material? glow = stage.Theme != null ? stage.Theme.GlowMaterial : null;
            Camera? view = stage.View;
            die.Leap(seconds, landed =>
            {
                float size = die.transform.lossyScale.x;
                PlaceholderEffect.Create("Énergie du critique", landed, 0.5f / speed, glow)
                    .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, energy, Vector3.zero, Quaternion.identity, Vector3.one * size * 1.4f)
                    { Duration = 0.2f / speed, Rise = 0.2f, Glow = true })
                    .AddRing(energy, 16, size * 2.2f, size * 0.08f, 0.45f / speed);
                if (view != null)
                {
                    CameraShake shake = view.TryGetComponent(out CameraShake existing) ? existing : view.gameObject.AddComponent<CameraShake>();
                    shake.Shake(0.08f, 0.3f / speed);
                }
            });
            return leapSeconds;
        }
    }
}
