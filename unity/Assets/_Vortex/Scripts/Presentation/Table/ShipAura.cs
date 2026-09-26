using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// What lasts around a ship (ANIMATIONS.md §3, lot 3), set by the table from what it shows:
    /// <list type="bullet">
    /// <item>contaminated by Torment: spores drift off it, more often the more tokens it carries;</item>
    /// <item>its effects in play: a slowly turning ring of light per effect, in the effect's colour;</item>
    /// <item>its turn: a soft halo of light under it, which pulses.</item>
    /// </list>
    /// All placeholder visuals, made of primitives, in the glow material.
    /// </summary>
    public sealed class ShipAura : MonoBehaviour
    {
        private const int RingDots = 18;

        private readonly List<Transform> _rings = new List<Transform>();
        private readonly List<Color> _ringColors = new List<Color>();
        private Material? _glow;
        private Transform? _halo;
        private Color _haloColor = Color.white;
        private Color _spores = Color.green;
        private float _contamination;
        private float _nextSpore;
        private float _time;

        /// <summary>Rings of effects shown (tests).</summary>
        public int Rings => _rings.Count;

        /// <summary>Whether the turn halo shows (tests).</summary>
        public bool TurnShown => _halo != null && _halo.gameObject.activeSelf;

        /// <summary>Spores given off so far (tests).</summary>
        public int Spores { get; private set; }

        /// <summary>
        /// Shows how contaminated the ship is (0 to 1) and in what colour its spores drift, one ring per effect in play (in
        /// its colour), and whether it is the ship's turn (a halo in <paramref name="turnColor"/>).
        /// </summary>
        public void Show(float contamination, Color spores, IReadOnlyList<Color> effects, bool turn, Color turnColor, Material? glow)
        {
            _contamination = Mathf.Clamp01(contamination);
            _spores = spores;
            _glow = glow;
            _haloColor = turnColor;
            SetRings(effects);
            SetHalo(turn);
        }

        /// <summary>Moves the aura on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            for (int i = 0; i < _rings.Count; i++)
            {
                _rings[i].localRotation = Quaternion.Euler(0f, _time * (25f + (i * 12f)) * (i % 2 == 0 ? 1f : -1f), 0f);
            }

            if (_halo != null && _halo.gameObject.activeSelf)
            {
                float pulse = 0.75f + (0.25f * Mathf.Sin(_time * 3f));
                Paint(_halo.GetComponent<Renderer>(), _haloColor * pulse);
            }

            if (_contamination > 0f && _time >= _nextSpore)
            {
                // From a spore every second at one token to five a second when heavily contaminated; cosmetic only.
                _nextSpore = _time + Mathf.Lerp(1f, 0.2f, _contamination);
                float scale = transform.lossyScale.x;
                Vector3 at = transform.TransformPoint(Random.insideUnitSphere * 0.7f);
                PlaceholderEffect.Create("Spore", at, 1.4f, _glow)
                    .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, _spores, Vector3.zero, Quaternion.identity, Vector3.one * Random.Range(0.05f, 0.12f) * scale)
                    { Velocity = (Vector3.up * 0.4f + (Random.insideUnitSphere * 0.2f)) * scale, Rise = 0.3f, Flicker = 0.4f, Glow = true });
                Spores++;
            }
        }

        private static void Paint(Renderer renderer, Color color)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(Shader.PropertyToID("_BaseColor"), color);
            renderer.SetPropertyBlock(block);
        }

        private static GameObject Piece(PrimitiveType shape, Transform parent, Material? glow)
        {
            GameObject piece = GameObject.CreatePrimitive(shape);
            Discard(piece.GetComponent<Collider>());
            piece.transform.SetParent(parent, false);
            Renderer renderer = piece.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (glow != null)
            {
                renderer.sharedMaterial = glow;
            }

            return piece;
        }

        private static void Discard(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        // One ring per effect, rebuilt only when the effects change.
        private void SetRings(IReadOnlyList<Color> effects)
        {
            bool same = effects.Count == _ringColors.Count;
            for (int i = 0; same && i < effects.Count; i++)
            {
                same = effects[i] == _ringColors[i];
            }

            if (same)
            {
                return;
            }

            foreach (Transform ring in _rings)
            {
                Discard(ring.gameObject);
            }

            _rings.Clear();
            _ringColors.Clear();
            for (int r = 0; r < effects.Count; r++)
            {
                var ring = new GameObject("Anneau d'effet").transform;
                ring.SetParent(transform, false);
                ring.localPosition = new Vector3(0f, 0.05f + (r * 0.12f), 0f);
                float radius = 1.2f + (r * 0.15f);
                for (int d = 0; d < RingDots; d++)
                {
                    float angle = 2f * Mathf.PI * d / RingDots;
                    GameObject dot = Piece(PrimitiveType.Sphere, ring, _glow);
                    dot.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    dot.transform.localScale = Vector3.one * 0.07f;
                    Paint(dot.GetComponent<Renderer>(), effects[r]);
                }

                _rings.Add(ring);
                _ringColors.Add(effects[r]);
            }
        }

        private void SetHalo(bool turn)
        {
            if (_halo == null)
            {
                _halo = Piece(PrimitiveType.Cylinder, transform, _glow).transform;
                _halo.name = "Halo du tour";
                _halo.localPosition = new Vector3(0f, -0.25f, 0f);
                _halo.localScale = new Vector3(1.8f, 0.01f, 2.3f);
            }

            _halo.gameObject.SetActive(turn);
            Paint(_halo.GetComponent<Renderer>(), _haloColor);
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
