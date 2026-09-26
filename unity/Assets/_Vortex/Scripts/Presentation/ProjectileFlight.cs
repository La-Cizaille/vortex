using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Flies a projectile prefab from one point to another (<see cref="LaserFeedback"/>): hidden until it leaves, then
    /// straight to its target, then gone.
    /// </summary>
    public sealed class ProjectileFlight : MonoBehaviour
    {
        private Vector3 _from;
        private Vector3 _to;
        private float _delay;
        private float _travel = 1f;
        private float _time;

        /// <summary>Leaves <paramref name="from"/> after <paramref name="delay"/> and reaches <paramref name="to"/> <paramref name="travel"/> seconds later.</summary>
        public void Fly(Vector3 from, Vector3 to, float delay, float travel)
        {
            _from = from;
            _to = to;
            _delay = Mathf.Max(0f, delay);
            _travel = Mathf.Max(0.01f, travel);
            transform.position = from;
            SetVisible(_delay <= 0f);
        }

        /// <summary>Moves the projectile on (every frame; tests call it directly). Returns false once it has arrived.</summary>
        public bool Tick(float deltaTime)
        {
            _time += deltaTime;
            float flown = (_time - _delay) / _travel;
            if (flown >= 1f)
            {
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

            SetVisible(flown >= 0f);
            transform.position = Vector3.Lerp(_from, _to, Mathf.Max(0f, flown));
            return true;
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer part in GetComponentsInChildren<Renderer>())
            {
                part.enabled = visible;
            }
        }

        private void Update() => Tick(Time.deltaTime);
    }
}
