using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A ship that holds an overcharge token crackles (ANIMATIONS.md §3): small electric sparks run over its hull, more
    /// often and brighter when the token is armed for the next attack. The table sets the state; the sparks are
    /// placeholder effects.
    /// </summary>
    public sealed class OverchargeArcs : MonoBehaviour
    {
        private Material? _glow;
        private GameObject? _arc;
        private Color _color = Color.cyan;
        private bool _charged;
        private bool _armed;
        private float _next;
        private float _time;

        /// <summary>Sparks given off so far (tests).</summary>
        public int Sparks { get; private set; }

        /// <summary>
        /// Whether the ship holds a token, whether it is armed, the sparks' colour and material, and the designer's arc
        /// (null: placeholder sparks).
        /// </summary>
        public void Show(bool charged, bool armed, Color color, Material? glow, GameObject? arc = null)
        {
            _arc = arc;
            _charged = charged;
            _armed = armed && charged;
            _color = color;
            _glow = glow;
        }

        /// <summary>Moves the sparks on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            if (!_charged || _time < _next)
            {
                return;
            }

            // Cosmetic crackle only, never a game value.
            _next = _time + Random.Range(_armed ? 0.05f : 0.15f, _armed ? 0.15f : 0.4f);
            float scale = transform.lossyScale.x;
            Vector3 at = transform.TransformPoint(new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0f, 0.3f), Random.Range(-0.9f, 0.9f)));
            Vector3 along = Random.onUnitSphere;
            Sparks++;
            if (_arc != null)
            {
                Destroy(Instantiate(_arc, at, Quaternion.LookRotation(along), transform), 1f);
                return;
            }

            PlaceholderEffect.Create("Arc électrique", at, 0.12f, _glow)
                .Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Capsule, _color * (_armed ? 4f : 2.5f), Vector3.zero, Quaternion.FromToRotation(Vector3.up, along), new Vector3(0.03f, 0.18f, 0.03f) * scale)
                { Rise = 0.1f, Flicker = 0.8f, Glow = true });
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
