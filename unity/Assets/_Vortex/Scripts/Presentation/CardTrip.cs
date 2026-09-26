using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A 3D card travelling across the table (ANIMATIONS.md §3, playtest 4): it rises from where it starts, flies along a
    /// curve, turning once on itself, and lands where it goes; then it is gone (the table shows the card in its new place),
    /// or it burns up in the middle of the table for a card that is used.
    /// </summary>
    [RequireComponent(typeof(CardDisplay))]
    public sealed class CardTrip : MonoBehaviour
    {
        private Vector3 _from;
        private Vector3 _to;
        private Quaternion _facing;
        private float _arc;
        private float _seconds = 1f;
        private float _time;
        private float _height;
        private bool _burnsUp;
        private Material? _glow;
        private Color _flame;

        /// <summary>How far along its trip the card is, from 0 to 1 (tests).</summary>
        public float Progress => Mathf.Clamp01(_time / _seconds);

        /// <summary>
        /// Sends the card from <paramref name="from"/> to <paramref name="to"/> in <paramref name="seconds"/>, facing the
        /// camera's way (<paramref name="facing"/>), <paramref name="height"/> units tall, along a curve
        /// <paramref name="arc"/> units high; <paramref name="burnsUp"/> ends it in a flash of <paramref name="flame"/>.
        /// </summary>
        public void Fly(Vector3 from, Vector3 to, Quaternion facing, float height, float arc, float seconds, bool burnsUp, Color flame, Material? glow)
        {
            _from = from;
            _to = to;
            _facing = facing;
            _arc = arc;
            _seconds = Mathf.Max(0.05f, seconds);
            _height = height;
            _burnsUp = burnsUp;
            _flame = flame;
            _glow = glow;
            Tick(0f);
        }

        /// <summary>Moves the card on (every frame; tests call it directly). Returns false once it has arrived.</summary>
        public bool Tick(float deltaTime)
        {
            _time += deltaTime;
            float t = Mathf.Clamp01(_time / _seconds);
            float eased = t * t * (3f - (2f * t));
            transform.position = Vector3.Lerp(_from, _to, eased) + (Vector3.up * _arc * 4f * eased * (1f - eased));

            // One turn on itself during the trip; a card that burns up shrinks as it arrives.
            transform.rotation = _facing * Quaternion.Euler(0f, 360f * eased, 0f);
            float size = _height / Mathf.Max(0.01f, GetComponent<CardDisplay>().Size.y);
            float shrink = _burnsUp ? Mathf.Clamp01((1f - t) / 0.25f) : 1f;
            transform.localScale = Vector3.one * size * shrink;
            if (t < 1f)
            {
                return true;
            }

            if (_burnsUp)
            {
                PlaceholderEffect.Create("Carte consumée", _to, 0.45f, _glow)
                    .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, _flame, Vector3.zero, Quaternion.identity, Vector3.one * _height * 0.8f)
                    { Duration = 0.3f, Rise = 0.2f, Glow = true })
                    .AddRing(_flame, 14, _height * 1.3f, 0.05f, 0.4f);
            }

            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }

            return false;
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
