using UnityEngine;
using Vortex.Core.Events;
using Vortex.Core.State;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The end of the game (<see cref="GameEventType.GameOver"/>, ANIMATIONS.md §3): the camera turns slowly around the
    /// winner, coming closer (<see cref="CameraOrbit"/>). A Galactic Election buries it under falling propaganda leaflets (DIRECTION_ARTISTIQUE §6.7: grave, not festive); a Domination bursts in its
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
        [Tooltip("Couleur des tracts qui tombent sur l'élu de l'Élection galactique (papier jauni).")]
        [SerializeField] private Color tract = new Color32(207, 196, 168, 255);
        [Tooltip("Couleur des tracts imprimés en rouge sang, mêlés aux autres.")]
        [SerializeField] private Color bloodTract = new Color32(142, 27, 27, 255);
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
                // Leaflets drifting down around the elected, slowly, turning; paper, not light. Cosmetic scatter only.
                PlaceholderEffect rain = PlaceholderEffect.Create("Tracts", ship.position + (Vector3.up * 4f * scale), seconds / speed, null);
                for (int i = 0; i < 50; i++)
                {
                    Vector3 start = Vector3.Scale(Random.insideUnitSphere, new Vector3(2.5f, 0.5f, 2.5f)) * scale;
                    Color paper = i % 3 == 0 ? bloodTract : tract;
                    rain.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Cube, paper, start, Random.rotation, new Vector3(0.16f, 0.008f, 0.11f) * scale)
                    { Velocity = (Vector3.down * Random.Range(0.7f, 1.2f) + (Random.insideUnitSphere * 0.3f)) * scale * speed, Delay = Random.Range(0f, 1.5f) / speed, Duration = 2.4f / speed, Rise = 0f, Flicker = 0f, Glow = false });
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
