using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// How a ship moves by itself (ANIMATIONS.md §2): a slow sway, rolling and bobbing as if it hovered in place, and the
    /// short pushes the feedbacks give it (recoil of a shot, knockback of a hit). Only the ship's body moves: its root
    /// stays where the table put it, so the panel that follows the ship does not shake. A wreck drifts slowly instead.
    /// </summary>
    public sealed class ShipMotion : MonoBehaviour
    {
        /// <summary>Name of the child that carries the ship's parts and moves.</summary>
        public const string BodyName = "Mouvement";

        private readonly List<Move> _pushes = new List<Move>();
        private Transform _body = null!;
        private float _height;
        private float _roll;
        private float _pitch;
        private float _period = 3f;
        private float _phase;
        private float _time;

        /// <summary>True once the ship is a wreck.</summary>
        public bool Wrecked { get; private set; }

        /// <summary>How far the pushes move the body now (tests).</summary>
        public Vector3 PushOffset { get; private set; }

        /// <summary>
        /// Adds the motion to a ship: its parts move under a new body child. <paramref name="phase"/> (0 to 1) shifts the sway,
        /// so that the ships of a table do not move together.
        /// </summary>
        public static ShipMotion Attach(Transform ship, float height, float rollDegrees, float pitchDegrees, float periodSeconds, float phase)
        {
            var body = new GameObject(BodyName).transform;
            body.SetParent(ship, false);
            var parts = new List<Transform>();
            foreach (Transform part in ship)
            {
                if (part != body)
                {
                    parts.Add(part);
                }
            }

            foreach (Transform part in parts)
            {
                part.SetParent(body, false);
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
        /// <paramref name="backSeconds"/>. Pushes add up.
        /// </summary>
        public void Push(Vector3 offset, float outSeconds, float backSeconds)
        {
            _pushes.Add(new Move(offset, Mathf.Max(0.01f, outSeconds), Mathf.Max(0.01f, backSeconds)));
        }

        /// <summary>The ship is destroyed: no more sway, a slow drift, and no pushes left.</summary>
        public void Wreck()
        {
            Wrecked = true;
            _pushes.Clear();
        }

        /// <summary>Moves the body on (every frame; tests call it directly).</summary>
        public void Tick(float deltaTime)
        {
            _time += deltaTime;
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
            for (int i = _pushes.Count - 1; i >= 0; i--)
            {
                Move push = _pushes[i];
                push.Time += deltaTime;
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
            }

            PushOffset = pushed;
            _body.localPosition = position + transform.InverseTransformVector(pushed);
            _body.localRotation = rotation;
        }

        private static float EaseOut(float t) => 1f - ((1f - t) * (1f - t));

        private static float EaseInOut(float t) => t * t * (3f - (2f * t));

        private void Update() => Tick(Time.deltaTime);

        private struct Move
        {
            public Move(Vector3 offset, float outSeconds, float backSeconds)
            {
                Offset = offset;
                Out = outSeconds;
                Back = backSeconds;
                Time = 0f;
            }

            public Vector3 Offset;
            public float Out;
            public float Back;
            public float Time;
        }
    }
}
