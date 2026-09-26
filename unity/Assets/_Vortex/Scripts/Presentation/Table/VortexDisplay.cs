using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The vortex in the sky of the table (docs/DIRECTION_ARTISTIQUE.md 2.4, ANIMATIONS §6): the part named
    /// <see cref="PartName"/> of the theme's background. It turns slowly, grows as the game nears the end of times, and
    /// pulses when a round's event is revealed. A background without that part has no vortex to move.
    /// </summary>
    public sealed class VortexDisplay : MonoBehaviour
    {
        /// <summary>Name of the vortex part in the background model.</summary>
        public const string PartName = "Vortex";

        /// <summary>How much larger the vortex is when the end of times comes, compared with the first round.</summary>
        public const float Growth = 0.35f;

        private const float DegreesPerSecond = 4f;
        private const float PulseSeconds = 1.2f;
        private const float PulseSize = 0.12f;

        private Transform? _vortex;
        private Vector3 _rest = Vector3.one;
        private Vector3 _axis = Vector3.forward;
        private float _progress;
        private float _shown;
        private float _pulse;

        /// <summary>How close the end of times is, from 0 (first round) to 1 (the end of times), as shown now (tests).</summary>
        public float Progress => _shown;

        /// <summary>True when the background has a vortex part.</summary>
        public bool HasVortex => _vortex != null;

        /// <summary>Finds the vortex part in the background.</summary>
        public static VortexDisplay Attach(GameObject background)
        {
            VortexDisplay display = background.AddComponent<VortexDisplay>();
            foreach (Transform part in background.GetComponentsInChildren<Transform>(true))
            {
                if (part.name == PartName)
                {
                    display._vortex = part;
                    display._rest = part.localScale;
                    display._axis = Normal(part);
                    break;
                }
            }

            return display;
        }

        /// <summary>Sets how close the end of times is, from 0 to 1; the vortex grows there over a few seconds.</summary>
        public void ShowProgress(float progress) => _progress = Mathf.Clamp01(progress);

        /// <summary>A round's event is revealed: the vortex swells for an instant.</summary>
        public void Pulse() => _pulse = PulseSeconds;

        /// <summary>Moves the vortex on by <paramref name="seconds"/>.</summary>
        public void Tick(float seconds)
        {
            if (_vortex == null)
            {
                return;
            }

            _shown = Mathf.MoveTowards(_shown, _progress, seconds * 0.2f);
            _pulse = Mathf.Max(0f, _pulse - seconds);
            float swell = Mathf.Sin((_pulse / PulseSeconds) * Mathf.PI) * PulseSize;
            _vortex.localScale = _rest * (1f + (Growth * _shown) + swell);
            _vortex.Rotate(_axis, DegreesPerSecond * seconds, Space.Self);
        }

        // The vortex is a flat disc: it turns around its thinnest axis, whatever way the model was exported.
        private static Vector3 Normal(Transform part)
        {
            if (!part.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                return Vector3.forward;
            }

            Vector3 size = filter.sharedMesh.bounds.size;
            return size.x <= size.y && size.x <= size.z ? Vector3.right : size.y <= size.z ? Vector3.up : Vector3.forward;
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
