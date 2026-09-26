using UnityEngine;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The end of the game (<see cref="GameEventType.GameOver"/>, ANIMATIONS.md §3): the camera turns slowly around the
    /// winner, coming closer (<see cref="CameraOrbit"/>). A Galactic Election rains gold on it; a Domination bursts in its
    /// colour. A draw shows nothing more. The next game puts the camera back.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Victoire", fileName = "VictoryFeedback")]
    public sealed class VictoryFeedback : FeedbackAsset
    {
        [Tooltip("Rotation de la caméra autour du vainqueur, en degrés.")]
        [SerializeField] private float orbit = 35f;
        [Tooltip("Rapprochement de la caméra, en part de sa distance (0 à 1).")]
        [SerializeField, Range(0f, 0.9f)] private float closer = 0.35f;
        [Tooltip("Durée du mouvement de caméra, en secondes à vitesse normale.")]
        [SerializeField, Min(0.1f)] private float seconds = 3f;
        [Tooltip("Couleur de la pluie d'or de l'Élection galactique ; au-dessus de 1,5, elle rayonne.")]
        [SerializeField, ColorUsage(false, true)] private Color gold = new Color(3f, 2.3f, 0.6f);
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 1.5f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? ship = gameEvent.Player >= 0 ? stage.AnchorFor(FeedbackAnchor.Player, gameEvent) : null;
            if (ship == null || stage.View == null)
            {
                return wait * 0.5f;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            CameraOrbit camera = stage.View.TryGetComponent(out CameraOrbit existing) ? existing : stage.View.gameObject.AddComponent<CameraOrbit>();
            camera.Orbit(ship.position, orbit, closer, seconds / speed);

            Material? glow = stage.Theme != null ? stage.Theme.GlowMaterial : null;
            float scale = ship.lossyScale.x;
            if ((WinCondition)gameEvent.Value == WinCondition.GalacticElection)
            {
                // Gold falling from above, all around the winner; cosmetic scatter only.
                PlaceholderEffect rain = PlaceholderEffect.Create("Pluie d'or", ship.position + (Vector3.up * 4f * scale), seconds / speed, glow);
                for (int i = 0; i < 60; i++)
                {
                    Vector3 start = Vector3.Scale(Random.insideUnitSphere, new Vector3(2.5f, 0.5f, 2.5f)) * scale;
                    rain.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, gold, start, Quaternion.identity, Vector3.one * Random.Range(0.05f, 0.12f) * scale)
                    { Velocity = Vector3.down * Random.Range(1.5f, 2.5f) * scale * speed, Delay = Random.Range(0f, 1.5f) / speed, Duration = 1.6f / speed, Rise = 0.1f, Flicker = 0.4f, Glow = true });
                }
            }
            else
            {
                // Bursts of light in the winner's colour, one after the other.
                Color burst = (stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.white) * 3f;
                PlaceholderEffect bursts = PlaceholderEffect.Create("Salves de victoire", ship.position + (Vector3.up * 1.2f * scale), seconds / speed, glow);
                for (int i = 0; i < 4; i++)
                {
                    bursts.AddRing(burst, 20, (1.8f + i) * scale, 0.07f * scale, 0.7f / speed, i * 0.45f / speed);
                }
            }

            return wait;
        }
    }
}
