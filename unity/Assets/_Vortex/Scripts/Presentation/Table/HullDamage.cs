using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// A damaged ship smokes (ANIMATIONS.md §3): below half its HP, dark puffs rise from its hull, more and more often as
    /// it loses HP; below a quarter, sparks crackle too. It shows who is in danger without reading the figures. A wreck
    /// stops smoking. The table sets how damaged the ship is; the puffs are placeholder effects.
    /// </summary>
    public sealed class HullDamage : MonoBehaviour
    {
        private Material? _glow;
        private GameObject? _smoke;
        private float _damage;
        private float _next;
        private float _time;
        private bool _wrecked;

        /// <summary>How damaged the ship is: 0 intact, 1 at no HP.</summary>
        public float Damage => _damage;

        /// <summary>Puffs given off so far (tests).</summary>
        public int Puffs { get; private set; }

        /// <summary>
        /// Sets how damaged the ship is (0 intact, 1 at no HP), the material of the sparks, and the designer's smoke
        /// (null: placeholder puffs).
        /// </summary>
        public void Show(float damage, bool wrecked, Material? glow, GameObject? smoke = null)
        {
            _damage = Mathf.Clamp01(damage);
            _wrecked = wrecked;
            _glow = glow;
            _smoke = smoke;
        }

        /// <summary>Moves the smoke on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            if (_wrecked || _damage < 0.5f || _time < _next)
            {
                return;
            }

            // From one puff a second at half HP to five a second at the end; cosmetic scatter only.
            float severity = (_damage - 0.5f) / 0.5f;
            _next = _time + Mathf.Lerp(1f, 0.2f, severity);
            float scale = transform.lossyScale.x;
            Vector3 at = transform.TransformPoint(new Vector3(Random.Range(-0.4f, 0.4f), 0.25f, Random.Range(-0.6f, 0.4f)));
            Puffs++;
            if (_smoke != null)
            {
                TimedRemoval.After(Instantiate(_smoke, at, Quaternion.identity), 3f);
                return;
            }

            PlaceholderEffect puff = PlaceholderEffect.Create("Fumée", at, 1.6f, _glow);
            Color smoke = Color.Lerp(new Color(0.35f, 0.35f, 0.38f), new Color(0.12f, 0.12f, 0.13f), severity);
            puff.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, smoke, Vector3.zero, Quaternion.identity, Vector3.one * Random.Range(0.25f, 0.45f) * scale)
            { Velocity = (Vector3.up + (Random.insideUnitSphere * 0.3f)) * 0.6f * scale, Rise = 0.3f });
            if (_damage >= 0.75f && Random.value < 0.5f)
            {
                puff.Add(new PlaceholderEffect.PieceSpec(PrimitiveType.Sphere, new Color(4f, 2.2f, 0.6f), Vector3.zero, Quaternion.identity, Vector3.one * 0.06f * scale)
                { Velocity = Random.onUnitSphere * 1.5f * scale, Duration = 0.25f, Rise = 0.1f, Glow = true });
            }
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
