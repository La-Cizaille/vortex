using UnityEngine;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>Spawns a prefab (particles, animated object, Timeline...) at an anchor, destroys it after its lifetime.</summary>
    [CreateAssetMenu(menuName = "Vortex/Retours visuels/Prefab", fileName = "PrefabFeedback")]
    public sealed class PrefabFeedback : FeedbackAsset
    {
        [SerializeField] private GameObject? prefab;
        [SerializeField] private FeedbackAnchor anchor = FeedbackAnchor.Player;
        [SerializeField, Min(0f)] private float lifetime = 1f;
        [SerializeField, Min(0f)] private float wait = 0.5f;

        /// <inheritdoc/>
        public override float Play(GameEvent gameEvent, IFeedbackStage stage)
        {
            Transform? at = stage.AnchorFor(anchor, gameEvent);
            if (prefab != null && at != null)
            {
                GameObject instance = Instantiate(prefab, at.position, at.rotation, at);
                Destroy(instance, lifetime);
            }

            return wait;
        }
    }
}
