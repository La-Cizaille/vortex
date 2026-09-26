using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The initiative (ANIMATIONS.md §3): each ship rolls its d8 above itself (<see cref="GameEventType.InitiativeRolled"/>),
    /// which spins and settles on the engine's value; the winner (<see cref="GameEventType.InitiativeWon"/>) flares up.
    /// A roll again for a tie replaces the ship's die. The die is the theme's model, or the generated one.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Initiative", fileName = "InitiativeFeedback")]
    public sealed class InitiativeFeedback : FeedbackAsset
    {
        /// <summary>Name prefix of an initiative die, followed by its seat.</summary>
        public const string DieName = "Initiative ";

        [Tooltip("Taille du dé au-dessus du vaisseau, en unités de la scène.")]
        [SerializeField, Min(0.1f)] private float size = 0.9f;
        [Tooltip("Hauteur du dé au-dessus du vaisseau, en unités de la scène.")]
        [SerializeField, Min(0f)] private float height = 1.8f;
        [Tooltip("Temps avant que le dé montre sa valeur, puis durée pendant laquelle il reste, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float rollSeconds = 0.6f;
        [SerializeField, Min(0f)] private float holdSeconds = 2.2f;
        [Tooltip("Couleur de l'éclat du vainqueur ; au-dessus de 1,5, il rayonne.")]
        [SerializeField, ColorUsage(false, true)] private Color winner = new Color(3f, 2.4f, 0.8f);

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? ship = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            if (ship == null || stage.View == null)
            {
                return 0.2f;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            if (gameEvent.Type == GameEventType.InitiativeWon)
            {
                PlaceholderEffect.Create("Initiative gagnée", ship.position + (Vector3.up * height * ship.lossyScale.x), 0.8f / speed, stage.Theme != null ? stage.Theme.GlowMaterial : null)
                    .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, winner, Vector3.zero, Quaternion.identity, Vector3.one * size * 1.6f)
                    { Duration = 0.3f / speed, Rise = 0.2f, Glow = true })
                    .AddRing(winner, 18, size * 2.5f, 0.06f, 0.7f / speed);
                return 0.6f;
            }

            // A roll again for a tie replaces the ship's die.
            string name = DieName + gameEvent.Player.ToString(System.Globalization.CultureInfo.InvariantCulture);
            GameObject? previous = GameObject.Find(name);
            if (previous != null)
            {
                Discard(previous);
            }

            GameObject? model = stage.Theme != null ? stage.Theme.DieModel : null;
            GameObject die = model != null ? Instantiate(model) : Vortex.Client.Theme.PlaceholderDie.Build(null, new Color32(236, 238, 245, 255), new Color32(26, 28, 40, 255));
            die.name = name;
            DieSpinner spinner = die.AddComponent<DieSpinner>();
            spinner.Hover(ship.position + (Vector3.up * height * ship.lossyScale.x), stage.View, size, 900f);
            die.AddComponent<InitiativeRoll>().Play(spinner, gameEvent.Value, rollSeconds / speed, holdSeconds / speed);
            return rollSeconds * 0.6f;
        }

        private static void Discard(GameObject target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
