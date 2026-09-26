using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Shakes the table's camera for a heavy moment (a critical hit, ANIMATIONS.md §3), then puts it back exactly where it
    /// was. The interface is drawn by the same camera, so it stays still on screen: only the 3D table shakes.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraShake : MonoBehaviour
    {
        private Vector3 _rest;
        private float _amplitude;
        private float _seconds;
        private float _time;
        private bool _shaking;

        /// <summary>True while the camera shakes (tests).</summary>
        public bool Shaking => _shaking;

        /// <summary>Shakes the camera by up to <paramref name="amplitude"/> units, fading out over <paramref name="seconds"/>.</summary>
        public void Shake(float amplitude, float seconds)
        {
            if (!_shaking)
            {
                _rest = transform.localPosition;
            }

            _amplitude = Mathf.Max(_shaking ? _amplitude * (1f - (_time / _seconds)) : 0f, amplitude);
            _seconds = Mathf.Max(0.01f, seconds);
            _time = 0f;
            _shaking = true;
        }

        /// <summary>Moves the shake on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            if (!_shaking)
            {
                return;
            }

            _time += deltaTime;
            if (_time >= _seconds)
            {
                transform.localPosition = _rest;
                _shaking = false;
                return;
            }

            // Smooth noise rather than random jumps, fading out.
            float fade = 1f - (_time / _seconds);
            float t = _time * 32f;
            var offset = new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f, 0f) * 2f * _amplitude * fade;
            transform.localPosition = _rest + (transform.localRotation * offset);
        }

        private void LateUpdate() => Tick(Time.deltaTime);
    }
}
