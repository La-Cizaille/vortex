using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A 3D d8 over a place of the dice tray (ANIMATIONS.md §2): it spins on itself where it is, never thrown, then turns
    /// the face of the engine's value towards the camera. The value always comes from the engine; the spin is cosmetic.
    /// The die follows its place at a fixed distance from the camera, like the cards (ADR-0017), and takes its height.
    /// </summary>
    public sealed class DieSpinner : MonoBehaviour
    {
        private RectTransform? _place;
        private Camera? _view;
        private float _depth;
        private Vector3 _axis = Vector3.up;
        private float _degreesPerSecond = 720f;
        private Quaternion _from;
        private Quaternion _to;
        private float _settle;
        private float _settleTime;

        /// <summary>The value shown once settled, or 0 while spinning.</summary>
        public int Value { get; private set; }

        /// <summary>Follows <paramref name="place"/> at <paramref name="depth"/> units in front of <paramref name="view"/>.</summary>
        public void Follow(RectTransform place, Camera view, float depth, float degreesPerSecond)
        {
            _place = place;
            _view = view;
            _depth = depth;
            _degreesPerSecond = degreesPerSecond;

            // Cosmetic spin axis, tilted so that the die tumbles rather than turns flat.
            _axis = (Vector3.up + (Random.insideUnitSphere * 0.8f)).normalized;
            transform.rotation = Random.rotation;
            Place();
        }

        /// <summary>
        /// Turns face <paramref name="value"/> towards the camera, upright, in <paramref name="seconds"/> (0: at once).
        /// Returns false when the die has no marker for that face.
        /// </summary>
        public bool Show(int value, float seconds)
        {
            Transform? face = FaceMarker(Vortex.Client.Theme.PlaceholderDie.FacePrefix + value);
            if (face == null || _view == null)
            {
                return false;
            }

            Place();

            // The die's rotation that brings the face's forward axis to the camera and its up axis to the camera's up.
            Quaternion faceInDie = Quaternion.Inverse(transform.rotation) * face.rotation;
            Quaternion facing = Quaternion.LookRotation(-_view.transform.forward, _view.transform.up);
            _from = transform.rotation;
            _to = facing * Quaternion.Inverse(faceInDie);
            _settle = Mathf.Max(0f, seconds);
            _settleTime = 0f;
            Value = value;
            if (_settle <= 0f)
            {
                transform.rotation = _to;
            }

            return true;
        }

        /// <summary>Moves the die on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            Place();
            if (Value == 0)
            {
                transform.Rotate(_axis, _degreesPerSecond * deltaTime, Space.World);
            }
            else if (_settleTime < _settle)
            {
                _settleTime += deltaTime;
                float t = Mathf.Clamp01(_settleTime / _settle);
                transform.rotation = Quaternion.Slerp(_from, _to, 1f - ((1f - t) * (1f - t)));
            }
        }

        // The marker of a face, anywhere under the die (a model may nest its markers).
        private Transform? FaceMarker(string name)
        {
            foreach (Transform part in GetComponentsInChildren<Transform>())
            {
                if (part.name == name)
                {
                    return part;
                }
            }

            return null;
        }

        /// <summary>Moves the die over its place now (captures lay the interface out before drawing).</summary>
        public void Place()
        {
            if (_place == null || _view == null)
            {
                return;
            }

            Rect rect = CardAnchor.ScreenRectOf(_place);
            transform.position = _view.ViewportToWorldPoint(new Vector3(rect.center.x / _view.pixelWidth, rect.center.y / _view.pixelHeight, _depth));
            transform.localScale = Vector3.one * CardAnchor.WorldHeightAt(_view, _depth, rect.height);
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
