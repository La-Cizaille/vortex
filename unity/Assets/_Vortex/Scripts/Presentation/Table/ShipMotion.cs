using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// How a ship moves by itself (ANIMATIONS.md §2): a slow sway, rolling and bobbing as if it hovered in place, the
    /// short pushes the feedbacks give it (recoil of a shot, knockback of a hit), and a turn towards the target it aims
    /// at. Only the ship's body (the model) moves, under a still root that the table places: the panel that follows the
    /// ship does not shake, and a model whose mesh is on its own root moves too. A wreck drifts slowly instead.
    /// </summary>
    public sealed class ShipMotion : MonoBehaviour
    {

        private readonly List<Move> _pushes = new List<Move>();
        private Transform _body = null!;
        private float _height;
        private float _roll;
        private float _pitch;
        private float _period = 3f;
        private float _phase;
        private float _time;
        private float _yaw;
        private float _yawFrom;
        private float _yawTo;
        private float _turn;
        private float _turnTime;
        private float _releaseIn;
        private float _releaseSeconds;

        /// <summary>True once the ship is a wreck.</summary>
        public bool Wrecked { get; private set; }

        /// <summary>How far the pushes move the body now (tests).</summary>
        public Vector3 PushOffset { get; private set; }

        /// <summary>The ship's model, which moves (tests).</summary>
        public Transform Body => _body;

        /// <summary>The turn of the body towards its aim, in degrees (tests).</summary>
        public float Yaw => _yaw;

        /// <summary>
        /// Adds the motion to a ship: <paramref name="ship"/> is the still root the table places, <paramref name="body"/>
        /// the model under it, which moves. <paramref name="phase"/> (0 to 1) shifts the sway, so that the ships of a table
        /// do not move together.
        /// </summary>
        public static ShipMotion Attach(Transform ship, Transform body, float height, float rollDegrees, float pitchDegrees, float periodSeconds, float phase)
        {
            if (body.parent != ship)
            {
                throw new System.ArgumentException("The body must be a child of the ship's root.", nameof(body));
            }

            ShipMotion motion = ship.gameObject.AddComponent<ShipMotion>();
            motion._body = body;
            motion._height = Mathf.Max(0f, height);
            motion._roll = Mathf.Max(0f, rollDegrees);
            motion._pitch = Mathf.Max(0f, pitchDegrees);
            motion._period = Mathf.Max(0.1f, periodSeconds);
            motion._phase = phase;
            return motion;
        }

        /// <summary>
        /// Moves the body by <paramref name="offset"/> (world space) in <paramref name="outSeconds"/>, then back in
        /// <paramref name="backSeconds"/>, starting after <paramref name="delay"/>. Pushes add up.
        /// </summary>
        public void Push(Vector3 offset, float outSeconds, float backSeconds, float delay = 0f) =>
            Push(offset, Vector3.zero, outSeconds, backSeconds, delay);

        /// <summary>
        /// Moves the body by <paramref name="offset"/> (world space) and tilts it by <paramref name="spin"/> (degrees around
        /// its own axes: pitch, yaw, roll), then brings it back, like <see cref="Push(Vector3, float, float, float)"/>.
        /// </summary>
        public void Push(Vector3 offset, Vector3 spin, float outSeconds, float backSeconds, float delay = 0f)
        {
            _pushes.Add(new Move(offset, spin, Mathf.Max(0.01f, outSeconds), Mathf.Max(0.01f, backSeconds), Mathf.Max(0f, delay)));
        }

        /// <summary>How far the pushes tilt the body now, in degrees (tests).</summary>
        public Vector3 PushSpin { get; private set; }

        /// <summary>Turns the ship's nose towards a point of the table in <paramref name="seconds"/>; it stays aimed until released.</summary>
        public void Aim(Vector3 target, float seconds)
        {
            _releaseIn = 0f;
            Vector3 local = transform.InverseTransformPoint(target);
            TurnTo(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, seconds);
        }

        /// <summary>Turns the ship back to its place in the table, in <paramref name="seconds"/>, after <paramref name="delay"/>.</summary>
        public void Release(float seconds, float delay = 0f)
        {
            if (delay <= 0f)
            {
                TurnTo(0f, seconds);
                return;
            }

            _releaseIn = delay;
            _releaseSeconds = seconds;
        }

        /// <summary>The ship is destroyed: no more sway, a slow drift, and no pushes left.</summary>
        public void Wreck()
        {
            Wrecked = true;
            _pushes.Clear();
            TurnTo(0f, 0f);
        }

        /// <summary>Moves the body on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            if (_releaseIn > 0f)
            {
                _releaseIn -= deltaTime;
                if (_releaseIn <= 0f)
                {
                    TurnTo(0f, _releaseSeconds);
                }
            }

            if (_turnTime < _turn)
            {
                _turnTime += deltaTime;
                _yaw = Mathf.LerpAngle(_yawFrom, _yawTo, EaseInOut(Mathf.Clamp01(_turnTime / _turn)));
            }

            float angle = 2f * Mathf.PI * (_time / _period + _phase);
            Vector3 position;
            Quaternion rotation;
            if (Wrecked)
            {
                // A wreck turns slowly on itself and sinks a little.
                position = new Vector3(0f, -_height * 2f, 0f);
                rotation = Quaternion.Euler(0f, _time * 6f, 0f);
            }
            else
            {
                position = new Vector3(0f, Mathf.Sin(angle) * _height, 0f);
                rotation = Quaternion.Euler(Mathf.Sin(angle * 0.5f + 1f) * _pitch, 0f, Mathf.Sin(angle * 0.75f) * _roll);
            }

            Vector3 pushed = Vector3.zero;
            Vector3 spun = Vector3.zero;
            for (int i = _pushes.Count - 1; i >= 0; i--)
            {
                Move push = _pushes[i];
                push.Time += deltaTime;
                if (push.Time < 0f)
                {
                    _pushes[i] = push;
                    continue;
                }

                if (push.Time >= push.Out + push.Back)
                {
                    _pushes.RemoveAt(i);
                    continue;
                }

                _pushes[i] = push;
                float reach = push.Time < push.Out
                    ? EaseOut(push.Time / push.Out)
                    : 1f - EaseInOut((push.Time - push.Out) / push.Back);
                pushed += push.Offset * reach;
                spun += push.Spin * reach;
            }

            PushOffset = pushed;
            PushSpin = spun;
            _body.localPosition = position + transform.InverseTransformVector(pushed);
            _body.localRotation = Quaternion.Euler(0f, _yaw, 0f) * rotation * Quaternion.Euler(spun);
        }

        private void TurnTo(float yaw, float seconds)
        {
            _yawFrom = _yaw;
            _yawTo = yaw;
            _turn = Mathf.Max(0f, seconds);
            _turnTime = 0f;
            if (_turn <= 0f)
            {
                _yaw = yaw;
            }
        }

        private static float EaseOut(float t) => 1f - ((1f - t) * (1f - t));

        private static float EaseInOut(float t) => t * t * (3f - (2f * t));

        private void Update() => Tick(Time.deltaTime);

        private struct Move
        {
            public Move(Vector3 offset, Vector3 spin, float outSeconds, float backSeconds, float delay)
            {
                Offset = offset;
                Spin = spin;
                Out = outSeconds;
                Back = backSeconds;
                Time = -delay;
            }

            public Vector3 Offset;
            public Vector3 Spin;
            public float Out;
            public float Back;
            public float Time;
        }
    }
}
