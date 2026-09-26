using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Where a card goes (ANIMATIONS.md §3): bought (<see cref="GameEventType.MarketCardTaken"/>), it flies from the
    /// market to the buyer's ship; stolen (<see cref="GameEventType.CardStolen"/>), from the victim's ship to the thief's;
    /// used (<see cref="GameEventType.CardActivated"/>), it leaves the ship for the middle of the table and burns up there.
    /// The card itself makes the trip: a 3D card showing it (playtest 4). A card the person dropped where it goes by hand
    /// makes no trip: the table puts it in its place. The table's own cards change once the events are played.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Trajet de carte", fileName = "CardFlightFeedback")]
    public sealed class CardFlightFeedback : FeedbackAsset
    {
        [Tooltip("Durée du trajet, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 0.55f;
        [Tooltip("Hauteur de la courbe du trajet au-dessus de la table, en unités de la scène.")]
        [SerializeField, Min(0f)] private float arc = 1.5f;
        [Tooltip("Hauteur de la carte qui vole, en unités de la scène.")]
        [SerializeField, Min(0.1f)] private float height = 1.3f;
        [Tooltip("Éclat de la flamme d'une carte utilisée : au-dessus du seuil du Bloom (1,5), elle rayonne.")]
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

            if (stage.TakePlacedByHand(gameEvent.CardUid))
            {
                return 0.1f;
            }

            CardDisplay? card = from is null || to is null || gameEvent.Id is null ? null : stage.NewCard(gameEvent.Id);
            if (card == null)
            {
                return wait * 0.5f;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            Color flame = (stage.Theme != null ? stage.Theme.Seat(gameEvent.Player) : Color.white) * brightness;
            Quaternion facing = stage.View != null ? stage.View.transform.rotation : Quaternion.identity;
            card.gameObject.AddComponent<CardTrip>().Fly(
                from!.Value, to!.Value, facing, height, arc, seconds / speed, gameEvent.Type == GameEventType.CardActivated, flame, stage.Theme != null ? stage.Theme.GlowMaterial : null);
            return wait;
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
