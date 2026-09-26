using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Removes an effect after a while (the effect prefabs, docs/ANIMATIONS.md section 6). In play it is Unity's timed
    /// Destroy. In the editor (tests, captures), where Unity refuses Destroy, the effect counts down when ticked and is
    /// removed with DestroyImmediate; left unticked, it goes with its scene.
    /// </summary>
    public sealed class TimedRemoval : MonoBehaviour
    {
        private float _left;

        /// <summary>Seconds before the effect goes (tests).</summary>
        public float Left => _left;

        /// <summary>Removes <paramref name="target"/> in <paramref name="seconds"/>; returns it.</summary>
        public static GameObject After(GameObject target, float seconds)
        {
            if (Application.isPlaying)
            {
                Destroy(target, seconds);
                return target;
            }

            target.AddComponent<TimedRemoval>()._left = Mathf.Max(0f, seconds);
            return target;
        }

        /// <summary>Counts down in the editor; returns false once the effect is gone.</summary>
        public bool Tick(float deltaTime)
        {
            _left -= deltaTime;
            if (_left > 0f)
            {
                return true;
            }

            DestroyImmediate(gameObject);
            return false;
        }
    }
}
