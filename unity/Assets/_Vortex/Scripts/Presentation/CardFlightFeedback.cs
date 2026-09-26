using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Where a card goes (ANIMATIONS.md §3): bought (<see cref="GameEventType.MarketCardTaken"/>), it flies from the
    /// market to the buyer's ship; stolen (<see cref="GameEventType.CardStolen"/>), from the victim's ship to the thief's;
    /// used (<see cref="GameEventType.CardActivated"/>), it leaves the ship for the middle of the table and burns up there.
    /// A glowing card-shaped placeholder makes the trip, in the colour of the seat that ends up with it; the table's own
    /// cards change once the events are played.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Trajet de carte", fileName = "CardFlightFeedback")]
    public sealed class CardFlightFeedback : FeedbackAsset
    {
        [Tooltip("Durée du trajet, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 0.55f;
        [Tooltip("Hauteur de la courbe du trajet au-dessus de la table, en unités de la scène.")]
        [SerializeField, Min(0f)] private float arc = 1.5f;
        [Tooltip("Éclat de la carte qui vole : au-dessus du seuil du Bloom (1,5), elle rayonne.")]
        [SerializeField, Min(0f)] private float brightness = 2f;
        [Tooltip("Attente avant l'événement suivant, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float wait = 0.45f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? player = stage.AnchorFor(FeedbackAnchor.Player, gameEvent);
            Vector3? from;
            Vector3? to;
            switch (gameEvent.Type)
            {
                case GameEventType.MarketCardTaken:
                    from = TablePoint(stage, stage.AnchorFor(FeedbackAnchor.Market, gameEvent));
                    to = player != null ? ShipParts.HullOf(player) : (Vector3?)null;
                    break;
                case GameEventType.CardStolen:
                    Transform? victim = stage.AnchorFor(FeedbackAnchor.Other, gameEvent);
                    from = victim != null ? ShipParts.HullOf(victim) : (Vector3?)null;
                    to = player != null ? ShipParts.HullOf(player) : (Vector3?)null;
                    break;
                default:
                    from = player != null ? ShipParts.HullOf(player) : (Vector3?)null;
                    to = TablePoint(stage, stage.AnchorFor(FeedbackAnchor.Table, gameEvent));
                    break;
            }

            if (from is null || to is null)
            {
                return wait * 0.5f;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            Color color = (stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.white) * brightness;
            Material? glow = stage.Theme != null ? stage.Theme.GlowMaterial : null;
            Fly(from.Value, to.Value, color, glow, speed, burnsUp: gameEvent.Type == GameEventType.CardActivated);
            return wait;
        }

        // The card's trip: a few steps along a curve above the table, each a short-lived glowing card.
        private void Fly(Vector3 from, Vector3 to, Color color, Material? glow, float speed, bool burnsUp)
        {
            const int steps = 6;
            float step = seconds / steps / speed;
            PlaceholderEffect flight = PlaceholderEffect.Create("Carte en vol", from, seconds / speed, glow);
            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / (steps - 1);
                Vector3 at = Vector3.Lerp(from, to, t) + (Vector3.up * arc * 4f * t * (1f - t));
                flight.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Cube, color * Mathf.Lerp(0.4f, 1f, t), at - from, Quaternion.Euler(70f, 0f, 20f * t), new Vector3(0.7f, 1f, 0.03f))
                { Delay = i * step, Duration = step * 2.5f, Rise = 0.2f, Glow = true });
            }

            if (burnsUp)
            {
                flight.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, color * 1.5f, to - from, Quaternion.identity, Vector3.one * 0.9f)
                { Delay = seconds / speed, Duration = 0.35f / speed, Rise = 0.2f, Glow = true });
                flight.AddRing(color, 14, 1.6f, 0.05f, 0.4f / speed, seconds / speed);
            }
        }

        // A point of the table under an anchor: a place of the interface is projected onto the table through the camera.
        private static Vector3? TablePoint(IFeedbackStage stage, Transform? anchor)
        {
            if (anchor == null)
            {
                return null;
            }

            if (!(anchor is RectTransform place) || stage.View == null)
            {
                return anchor.position;
            }

            Vector2 screen = CardAnchor.ScreenRectOf(place).center;
            Ray ray = stage.View.ScreenPointToRay(screen);
            return new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance) ? ray.GetPoint(distance) + (Vector3.up * 0.5f) : (Vector3?)null;
        }
    }
}
