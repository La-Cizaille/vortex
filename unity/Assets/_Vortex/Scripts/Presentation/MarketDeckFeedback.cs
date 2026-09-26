using UnityEngine;
using Vortex.Core.Content;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The black markets' decks at work (ANIMATIONS.md §2): a card revealed (<see cref="GameEventType.MarketCardRevealed"/>)
    /// leaves its market's deck face down, flips over on the way and lands on its place, which stays empty until it
    /// arrives; a market recycled (<see cref="GameEventType.MarketRecycled"/>) sends its cards back to the deck, turning
    /// face down. The cards that travel are the 3D card model; the events say which card and which place.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Paquets du marché", fileName = "MarketDeckFeedback")]
    public sealed class MarketDeckFeedback : FeedbackAsset
    {
        [Tooltip("Durée du trajet d'une carte entre le paquet et sa place, en secondes à vitesse normale.")]
        [SerializeField, Min(0.05f)] private float seconds = 0.4f;
        [Tooltip("Hauteur de la courbe du trajet, en hauteurs de carte.")]
        [SerializeField, Min(0f)] private float arc = 0.3f;
        [Tooltip("Attente après une carte révélée : les suivantes partent pendant que celle-ci vole.")]
        [SerializeField, Min(0f)] private float revealWait = 0.14f;
        [Tooltip("Attente après un recyclage, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float recycleWait = 0.35f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            MarketDisplay? markets = stage.Markets;
            var slot = (CardSlot)gameEvent.Value;
            RectTransform? deck = markets != null ? markets.DeckPlace(slot) : null;
            if (markets == null || deck == null || stage.View == null)
            {
                return 0f;
            }

            float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
            (Vector3 pile, float height) = markets.OnCardLayer(deck);
            Quaternion facing = stage.View.transform.rotation;
            if (gameEvent.Type == GameEventType.MarketRecycled)
            {
                foreach ((var card, RectTransform place) in markets.Shown(slot))
                {
                    CardDisplay? copy = stage.NewCard(card.CardId);
                    if (copy != null)
                    {
                        (Vector3 from, float _) = markets.OnCardLayer(place);
                        copy.gameObject.AddComponent<CardTrip>()
                            .Fly(from, pile, facing, height, height * arc, seconds / speed, false, Color.clear, null)
                            .Turning(0f, 180f);
                    }
                }

                return recycleWait;
            }

            int index = gameEvent.Amount;
            RectTransform? target = markets.PlaceOf(slot, index);
            CardDisplay? revealed = target != null && gameEvent.Id != null ? stage.NewCard(gameEvent.Id) : null;
            if (target == null || revealed == null)
            {
                return revealWait;
            }

            (Vector3 to, float _) = markets.OnCardLayer(target);
            markets.Conceal(slot, index, true);
            revealed.gameObject.AddComponent<CardTrip>()
                .Fly(pile, to, facing, height, height * arc, seconds / speed, false, Color.clear, null)
                .Turning(180f, 360f, () => markets.Conceal(slot, index, false));
            return revealWait;
        }
    }
}
