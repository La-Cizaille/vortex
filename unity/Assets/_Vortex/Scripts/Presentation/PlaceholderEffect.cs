using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A stand-in visual effect made of Unity primitives (ANIMATIONS.md): a beam, a flare, a burst of debris. Each piece
    /// grows quickly, may fly off, then shrinks away; the effect destroys itself at the end. It needs no material or
    /// particle asset, like the placeholder ship, and gives way to the designer's effect as soon as a feedback has one.
    /// </summary>
    public sealed class PlaceholderEffect : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        private readonly List<Piece> _pieces = new List<Piece>();
        private float _seconds = 1f;
        private float _time;

        /// <summary>Pieces of the effect (tests).</summary>
        public int PieceCount => _pieces.Count;

        /// <summary>Creates an empty effect at a world position, lasting <paramref name="seconds"/>.</summary>
        public static PlaceholderEffect Create(string name, Vector3 position, float seconds)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            PlaceholderEffect effect = root.AddComponent<PlaceholderEffect>();
            effect._seconds = Mathf.Max(0.01f, seconds);
            return effect;
        }

        /// <summary>
        /// Adds a piece: a primitive in a colour, at a place relative to the effect, that grows to
        /// <paramref name="size"/> and moves at <paramref name="velocity"/> (units per second). The axes set in
        /// <paramref name="fixedAxes"/> keep their size (a beam keeps its length while it thins out).
        /// </summary>
        public PlaceholderEffect Add(PrimitiveType shape, Color color, Vector3 position, Quaternion rotation, Vector3 size, Vector3 velocity, Vector3 fixedAxes = default)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = shape.ToString();
            Discard(part.GetComponent<Collider>());
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localRotation = rotation;
            part.transform.localScale = Vector3.zero;
            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.SetPropertyBlock(block);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pieces.Add(new Piece(part.transform, position, size, velocity, fixedAxes));
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

            // Quick growth over the first fifth, then a slow fade to nothing.
            float t = _time / _seconds;
            float grow = t < 0.2f ? t / 0.2f : 1f - ((t - 0.2f) / 0.8f);
            foreach (Piece piece in _pieces)
            {
                piece.Part.localPosition = piece.Start + (piece.Velocity * _time);
                piece.Part.localScale = new Vector3(
                    Scale(piece.Size.x, piece.Fixed.x, grow),
                    Scale(piece.Size.y, piece.Fixed.y, grow),
                    Scale(piece.Size.z, piece.Fixed.z, grow));
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

        private readonly struct Piece
        {
            public Piece(Transform part, Vector3 start, Vector3 size, Vector3 velocity, Vector3 fixedAxes)
            {
                Part = part;
                Start = start;
                Size = size;
                Velocity = velocity;
                Fixed = fixedAxes;
            }

            public Transform Part { get; }

            public Vector3 Start { get; }

            public Vector3 Size { get; }

            public Vector3 Velocity { get; }

            public Vector3 Fixed { get; }
        }
    }
}
