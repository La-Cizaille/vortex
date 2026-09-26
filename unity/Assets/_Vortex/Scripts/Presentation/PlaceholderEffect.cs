using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A stand-in visual effect made of Unity primitives (ANIMATIONS.md): a laser bolt, an engine jet, a ring, debris.
    /// Each piece has its own start and duration within the effect; it grows quickly, may fly off and flicker, then
    /// shrinks away. With the theme's glow material, pieces are luminous (bright enough for the Bloom); without it, they
    /// use the pipeline's default material. The effect destroys itself at the end and gives way to the designer's effect
    /// as soon as a feedback has one.
    /// </summary>
    public sealed class PlaceholderEffect : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly List<Piece> _pieces = new List<Piece>();
        private MaterialPropertyBlock? _block;
        private Material? _glow;
        private float _seconds = 1f;
        private float _time;

        /// <summary>Pieces of the effect (tests).</summary>
        public int PieceCount => _pieces.Count;

        /// <summary>
        /// Creates an empty effect at a world position, lasting at least <paramref name="seconds"/> (longer if a piece
        /// ends later); <paramref name="glow"/> is the material of its luminous pieces, or null.
        /// </summary>
        public static PlaceholderEffect Create(string name, Vector3 position, float seconds, Material? glow = null)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            PlaceholderEffect effect = root.AddComponent<PlaceholderEffect>();
            effect._seconds = Mathf.Max(0.01f, seconds);
            effect._glow = glow;
            return effect;
        }

        /// <summary>
        /// Adds a piece that lasts the whole effect: a primitive in a colour, at a place relative to the effect, that grows
        /// to <paramref name="size"/> and moves at <paramref name="velocity"/> (units per second). The axes set in
        /// <paramref name="fixedAxes"/> keep their size.
        /// </summary>
        public PlaceholderEffect Add(PrimitiveType shape, Color color, Vector3 position, Quaternion rotation, Vector3 size, Vector3 velocity, Vector3 fixedAxes = default) =>
            Add(new PieceSpec(shape, color, position, rotation, size) { Velocity = velocity, FixedAxes = fixedAxes });

        /// <summary>
        /// Adds a ring of <paramref name="count"/> small luminous sparks that spread flat from the effect's centre to
        /// <paramref name="radius"/> while they fade: a shockwave.
        /// </summary>
        public PlaceholderEffect AddRing(Color color, int count, float radius, float sparkSize, float seconds, float delay = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Add(new PieceSpec(PrimitiveType.Sphere, color, Vector3.zero, Quaternion.LookRotation(outward), new Vector3(sparkSize, sparkSize, sparkSize * 3f))
                { Velocity = outward * radius / seconds, Delay = delay, Duration = seconds, Rise = 0.1f, Glow = true });
            }

            return this;
        }

        /// <summary>Adds a piece described in full (start, duration, glow, flicker).</summary>
        public PlaceholderEffect Add(PieceSpec spec)
        {
            GameObject part = GameObject.CreatePrimitive(spec.Shape);
            part.name = spec.Shape.ToString();
            Discard(part.GetComponent<Collider>());
            part.transform.SetParent(transform, false);
            part.transform.localPosition = spec.Position;
            part.transform.localRotation = spec.Rotation;
            part.transform.localScale = Vector3.zero;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (spec.Glow && _glow != null)
            {
                renderer.sharedMaterial = _glow;
            }

            float duration = spec.Duration > 0f ? spec.Duration : _seconds - spec.Delay;
            _seconds = Mathf.Max(_seconds, spec.Delay + duration);
            _pieces.Add(new Piece(part.transform, renderer, spec, Mathf.Max(0.01f, duration), Random.value * 10f));
            return this;
        }

        /// <summary>Moves the effect on (every frame; tests call it directly). Returns false once it is over.</summary>
        public bool Tick(float deltaTime)
        {
            _time += deltaTime;
            if (_time >= _seconds)
            {
                Discard(gameObject);
                return false;
            }

            _block ??= new MaterialPropertyBlock();
            foreach (Piece piece in _pieces)
            {
                float local = _time - piece.Spec.Delay;
                bool shown = local >= 0f && local < piece.Duration;
                piece.Renderer.enabled = shown;
                if (!shown)
                {
                    continue;
                }

                // Quick growth, then a fade to nothing; a flickering piece also pulses along its length.
                float t = local / piece.Duration;
                float rise = Mathf.Clamp(piece.Spec.Rise, 0.01f, 0.99f);
                float grow = t < rise ? t / rise : 1f - ((t - rise) / (1f - rise));
                float glow = Mathf.Clamp01(grow * 1.5f);
                if (piece.Spec.Expand)
                {
                    // A shockwave: it keeps growing while it fades.
                    grow = 1f - ((1f - t) * (1f - t));
                    glow = 1f - t;
                }
                float pulse = 1f + (piece.Spec.Flicker * (Mathf.PerlinNoise(piece.Seed, local * 18f) - 0.5f) * 2f);
                Vector3 size = piece.Spec.Size;
                Vector3 fixedAxes = piece.Spec.FixedAxes;
                piece.Part.localPosition = piece.Spec.Position + (piece.Spec.Velocity * local);
                piece.Part.localScale = new Vector3(
                    Scale(size.x, fixedAxes.x, grow),
                    Scale(size.y, fixedAxes.y, grow) * pulse,
                    Scale(size.z, fixedAxes.z, grow));

                // Luminous pieces fade in brightness too.
                piece.Renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColor, piece.Spec.Glow ? piece.Spec.Color * glow : piece.Spec.Color);
                piece.Renderer.SetPropertyBlock(_block);
            }

            return true;
        }

        private static float Scale(float size, float fixedAxis, float grow) => fixedAxis > 0f ? size : size * grow;

        private static void Discard(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>One piece of a placeholder effect.</summary>
        public sealed class PieceSpec
        {
            /// <summary>Creates a piece: a primitive in a colour, at a place relative to the effect, growing to a size.</summary>
            public PieceSpec(PrimitiveType shape, Color color, Vector3 position, Quaternion rotation, Vector3 size)
            {
                Shape = shape;
                Color = color;
                Position = position;
                Rotation = rotation;
                Size = size;
            }

            /// <summary>Primitive shape.</summary>
            public PrimitiveType Shape { get; }

            /// <summary>Colour; above 1 (HDR) a luminous piece blooms.</summary>
            public Color Color { get; }

            /// <summary>Start place, relative to the effect.</summary>
            public Vector3 Position { get; }

            /// <summary>Orientation, relative to the effect.</summary>
            public Quaternion Rotation { get; }

            /// <summary>Full size.</summary>
            public Vector3 Size { get; }

            /// <summary>Movement, in units per second.</summary>
            public Vector3 Velocity { get; set; }

            /// <summary>Axes (set to 1) that keep their full size all along.</summary>
            public Vector3 FixedAxes { get; set; }

            /// <summary>Seconds after the effect starts before the piece shows.</summary>
            public float Delay { get; set; }

            /// <summary>Seconds the piece lasts (0: to the end of the effect).</summary>
            public float Duration { get; set; }

            /// <summary>Share of its life the piece spends growing (0 to 1).</summary>
            public float Rise { get; set; } = 0.2f;

            /// <summary>Pulse of its length (its Y axis, the long axis of a capsule), from 0 (steady) to 1.</summary>
            public float Flicker { get; set; }

            /// <summary>Whether the piece keeps growing to its size while it fades (a shockwave), instead of shrinking back.</summary>
            public bool Expand { get; set; }

            /// <summary>Whether the piece uses the glow material.</summary>
            public bool Glow { get; set; }
        }

        private readonly struct Piece
        {
            public Piece(Transform part, Renderer renderer, PieceSpec spec, float duration, float seed)
            {
                Part = part;
                Renderer = renderer;
                Spec = spec;
                Duration = duration;
                Seed = seed;
            }

            public Transform Part { get; }

            public Renderer Renderer { get; }

            public PieceSpec Spec { get; }

            public float Duration { get; }

            public float Seed { get; }
        }
    }
}
