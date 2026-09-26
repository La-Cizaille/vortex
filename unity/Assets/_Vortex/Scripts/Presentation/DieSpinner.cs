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
        private float _leap;
        private float _leapTime;
        private float _closer = 1f;
        private System.Action<Vector3>? _landed;
        private Vector3? _hover;
        private float _hoverSize = 1f;

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
        /// Floats over a point of the table instead of a place of the interface (a ship's initiative roll), facing
        /// <paramref name="view"/>, <paramref name="size"/> units high.
        /// </summary>
        public void Hover(Vector3 point, Camera view, float size, float degreesPerSecond)
        {
            _hover = point;
            _hoverSize = size;
            _view = view;
            _degreesPerSecond = degreesPerSecond;
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
            Light(value);
            if (_settle <= 0f)
            {
                transform.rotation = _to;
            }

            return true;
        }

        /// <summary>Name of the die's light (the casino die of the art direction): lit when the highest face comes up.</summary>
        public const string LightName = "Voyant";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>True while the die's light is on (tests).</summary>
        public bool Lit { get; private set; }

        // The light goes on for the highest face the die has (a critical), off for any other.
        private void Light(int value)
        {
            Transform? bulb = FaceMarker(LightName);
            if (bulb == null || !bulb.TryGetComponent(out Renderer renderer))
            {
                return;
            }

            int highest = 0;
            while (FaceMarker(Vortex.Client.Theme.PlaceholderDie.FacePrefix + (highest + 1)) != null)
            {
                highest++;
            }

            Lit = value == highest;
            var block = new MaterialPropertyBlock();
            if (Lit)
            {
                var gold = new Color(1f, 0.72f, 0.3f);
                block.SetColor(BaseColorId, gold);
                block.SetColor(EmissionColorId, gold * 4f);
            }

            renderer.SetPropertyBlock(block);
        }

        /// <summary>True while the die leaps (tests).</summary>
        public bool Leaping => _leapTime < _leap;

        /// <summary>
        /// A critical hit (playtest 4): the die leaps towards the camera, spinning, then falls back to its place in
        /// <paramref name="seconds"/>; <paramref name="landed"/> is told where it lands, for its burst of energy.
        /// </summary>
        public void Leap(float seconds, System.Action<Vector3>? landed)
        {
            _leap = Mathf.Max(0.1f, seconds);
            _leapTime = 0f;
            _landed = landed;
        }

        /// <summary>Moves the die on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            if (_leapTime < _leap)
            {
                _leapTime += deltaTime;
                float t = Mathf.Clamp01(_leapTime / _leap);

                // Up towards the camera for the first half, then down again, faster, like a fall.
                float height = t < 0.5f ? Mathf.Sin(t * Mathf.PI) : 1f - (((t - 0.5f) / 0.5f) * ((t - 0.5f) / 0.5f));
                _closer = 1f - (0.55f * height);
                transform.Rotate(_axis, _degreesPerSecond * 1.5f * deltaTime * (1f - t), Space.World);
                if (t >= 1f)
                {
                    _closer = 1f;
                    if (Value > 0)
                    {
                        Show(Value, 0.15f);
                    }

                    Place();
                    _landed?.Invoke(transform.position);
                    _landed = null;
                }
            }

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
            if (_hover is Vector3 point)
            {
                transform.position = point;
                transform.localScale = Vector3.one * _hoverSize;
                return;
            }

            if (_place == null || _view == null)
            {
                return;
            }

            Rect rect = CardAnchor.ScreenRectOf(_place);
            float depth = _depth * _closer;
            transform.position = _view.ViewportToWorldPoint(new Vector3(rect.center.x / _view.pixelWidth, rect.center.y / _view.pixelHeight, depth));
            transform.localScale = Vector3.one * CardAnchor.WorldHeightAt(_view, _depth, rect.height);
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
