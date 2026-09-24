using System.Collections.Generic;
using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Shows a dice roll: the dice of an attack (<see cref="GameEventType.DiceRolled"/>: every die, then the total kept)
    /// or a single die rolled for an effect (<see cref="GameEventType.DieRolled"/>). The tray runs at the playback speed.
    /// </summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Dés", fileName = "DiceFeedback")]
    public sealed class DiceFeedback : FeedbackAsset
    {
        [SerializeField] private DiceTray? tray;
        [SerializeField] private FeedbackAnchor anchor = FeedbackAnchor.Market;
        [Tooltip("Durée du lancer, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float rollSeconds = 0.6f;
        [Tooltip("Temps pendant lequel le résultat reste affiché, en secondes à vitesse normale.")]
        [SerializeField, Min(0f)] private float holdSeconds = 0.8f;

        /// <summary>Sets the tray and where it appears (editor setup and tests).</summary>
        public void Configure(DiceTray trayPrefab, FeedbackAnchor where, float roll, float hold)
        {
            tray = trayPrefab;
            anchor = where;
            rollSeconds = Mathf.Max(0f, roll);
            holdSeconds = Mathf.Max(0f, hold);
        }

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            IReadOnlyList<int> values;
            int? sum = null;
            if (gameEvent.Type == GameEventType.DieRolled)
            {
                values = new[] { gameEvent.Value };
            }
            else if (gameEvent.Values != null && gameEvent.Values.Count > 0)
            {
                values = gameEvent.Values;
                sum = values.Count > 1 ? gameEvent.Amount : (int?)null;
            }
            else
            {
                return 0f;
            }

            Transform? at = stage.AnchorFor(anchor, gameEvent);
            if (tray != null && at != null)
            {
                float speed = Mathf.Max(0.01f, stage.PlaybackSpeed);
                DiceTray shown = Instantiate(tray, at, false);
                shown.Roll(values, sum, rollSeconds / speed, holdSeconds / speed);
            }

            // In playback seconds: the event player applies the speed itself.
            return rollSeconds + holdSeconds;
        }
    }
}
