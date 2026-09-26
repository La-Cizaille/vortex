using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Turns the table's camera slowly around a point (the winner, ANIMATIONS.md §3), always looking at it, then holds;
    /// <see cref="Restore"/> puts the camera back where it was for the next game. The interface is drawn by the same
    /// camera, so it stays where it is on screen.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraOrbit : MonoBehaviour
    {
        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private bool _resting = true;
        private Vector3 _target;
        private float _degrees;
        private float _closer;
        private float _seconds = 1f;
        private float _time;

        /// <summary>True while the camera is away from its place (tests).</summary>
        public bool Orbiting => !_resting;

        /// <summary>
        /// Brings the camera <paramref name="closer"/> (share of its distance, 0 to 1) to <paramref name="target"/> and
        /// turns it by <paramref name="degrees"/> around it, in <paramref name="seconds"/>.
        /// </summary>
        public void Orbit(Vector3 target, float degrees, float closer, float seconds)
        {
            if (_resting)
            {
                _restPosition = transform.position;
                _restRotation = transform.rotation;
                _resting = false;
            }

            _target = target;
            _degrees = degrees;
            _closer = Mathf.Clamp01(closer);
            _seconds = Mathf.Max(0.1f, seconds);
            _time = 0f;
        }

        /// <summary>Puts the camera back where it was before the orbit.</summary>
        public void Restore()
        {
            if (_resting)
            {
                return;
            }

            transform.SetPositionAndRotation(_restPosition, _restRotation);
            _resting = true;
        }

        /// <summary>Moves the camera on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            if (_resting)
            {
                return;
            }

            _time += deltaTime;
            float t = Mathf.Clamp01(_time / _seconds);
            float eased = t * t * (3f - (2f * t));
            Vector3 fromTarget = _restPosition - _target;
            Vector3 around = Quaternion.AngleAxis(_degrees * eased, Vector3.up) * fromTarget * (1f - (_closer * eased));
            Vector3 position = _target + around;
            Quaternion looking = Quaternion.LookRotation(_target - position, Vector3.up);
            transform.SetPositionAndRotation(position, Quaternion.Slerp(_restRotation, looking, eased));
        }

        private void LateUpdate() => Tick(Time.deltaTime);
    }
}
