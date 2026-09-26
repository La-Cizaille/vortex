using UnityEngine;
using UnityEngine.UI;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The screen that shows an opponent's hit points and shield (docs/DIRECTION_ARTISTIQUE.md 6.4): a pirate radar, so
    /// the reading is not quite steady. A faint bar sweeps down the screen, and now and then the picture jumps and flickers
    /// for an instant. Cosmetic only: the figures themselves are never changed, and a glitch never lasts long enough to
    /// hide them.
    /// </summary>
    public sealed class RadarScreen : MonoBehaviour
    {
        /// <summary>Seconds a sweep takes from the top of the screen to the bottom.</summary>
        public const float SweepSeconds = 2.6f;

        /// <summary>Longest glitch, in seconds: short enough never to hide a figure.</summary>
        public const float MaxGlitchSeconds = 0.18f;

        [SerializeField] private RectTransform[] lines = System.Array.Empty<RectTransform>();
        [SerializeField] private Graphic[] flickering = System.Array.Empty<Graphic>();
        [SerializeField] private RectTransform? sweep;
        [SerializeField, Min(0.5f)] private float calmMin = 2.5f;
        [SerializeField, Min(0.5f)] private float calmMax = 7f;

        private Vector2[] _rest = System.Array.Empty<Vector2>();
        private float[] _alpha = System.Array.Empty<float>();
        private float _sweep;
        private float _calm;
        private float _glitch;

        /// <summary>True while the picture jumps (tests).</summary>
        public bool Glitching => _glitch > 0f;

        /// <summary>How far down the screen the sweep is, from 0 (top) to 1 (bottom) (tests).</summary>
        public float Sweep => _sweep;

        /// <summary>Wires the parts (editor setup): the lines that jump, the graphics that flicker, the sweeping bar.</summary>
        public void Assign(RectTransform[] jumping, Graphic[] flickers, RectTransform bar)
        {
            lines = jumping;
            flickering = flickers;
            sweep = bar;
        }

        /// <summary>Moves the screen on by <paramref name="seconds"/>.</summary>
        public void Tick(float seconds)
        {
            Remember();
            _sweep = (_sweep + (seconds / SweepSeconds)) % 1f;
            if (sweep != null)
            {
                sweep.anchorMin = new Vector2(0f, 1f - _sweep - 0.08f);
                sweep.anchorMax = new Vector2(1f, 1f - _sweep);
            }

            if (_glitch > 0f)
            {
                _glitch -= seconds;
                if (_glitch <= 0f)
                {
                    Settle();
                    return;
                }

                // A jump sideways and a dip in brightness, different each frame of the glitch.
                for (int i = 0; i < lines.Length; i++)
                {
                    lines[i].anchoredPosition = _rest[i] + new Vector2(Random.Range(-3f, 3f), 0f);
                }

                float dip = Random.Range(0.45f, 0.85f);
                for (int i = 0; i < flickering.Length; i++)
                {
                    Color c = flickering[i].color;
                    c.a = _alpha[i] * dip;
                    flickering[i].color = c;
                }

                return;
            }

            _calm -= seconds;
            if (_calm <= 0f)
            {
                _glitch = Random.Range(0.06f, MaxGlitchSeconds);
                _calm = Random.Range(calmMin, calmMax);
            }
        }

        private void Awake() => _calm = Random.Range(calmMin, calmMax);

        private void Update() => Tick(Time.unscaledDeltaTime);

        private void OnDisable() => Settle();

        // The resting place and brightness, read once.
        private void Remember()
        {
            if (_rest.Length == lines.Length && _alpha.Length == flickering.Length)
            {
                return;
            }

            _rest = new Vector2[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                _rest[i] = lines[i].anchoredPosition;
            }

            _alpha = new float[flickering.Length];
            for (int i = 0; i < flickering.Length; i++)
            {
                _alpha[i] = flickering[i].color.a;
            }
        }

        private void Settle()
        {
            _glitch = 0f;
            for (int i = 0; i < _rest.Length && i < lines.Length; i++)
            {
                lines[i].anchoredPosition = _rest[i];
            }

            for (int i = 0; i < _alpha.Length && i < flickering.Length; i++)
            {
                Color c = flickering[i].color;
                c.a = _alpha[i];
                flickering[i].color = c;
            }
        }
    }
}
