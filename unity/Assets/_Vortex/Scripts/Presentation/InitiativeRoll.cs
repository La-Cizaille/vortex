using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// The timing of an initiative die (<see cref="InitiativeFeedback"/>): it spins, shows the engine's value, stays a
    /// moment, then goes.
    /// </summary>
    [RequireComponent(typeof(DieSpinner))]
    public sealed class InitiativeRoll : MonoBehaviour
    {
        private DieSpinner? _die;
        private int _value;
        private float _roll;
        private float _hold;
        private float _time;
        private bool _shown;

        /// <summary>Spins for <paramref name="rollSeconds"/>, shows <paramref name="value"/>, stays <paramref name="holdSeconds"/>.</summary>
        public void Play(DieSpinner die, int value, float rollSeconds, float holdSeconds)
        {
            _die = die;
            _value = value;
            _roll = rollSeconds;
            _hold = holdSeconds;
        }

        /// <summary>Moves the roll on (every frame; tests call it directly). Returns false once the die is gone.</summary>
        public bool Tick(float deltaTime)
        {
            _time += deltaTime;
            if (!_shown && _time >= _roll && _die != null)
            {
                _die.Show(_value, 0.2f);
                _shown = true;
            }

            if (_time < _roll + _hold)
            {
                return true;
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
