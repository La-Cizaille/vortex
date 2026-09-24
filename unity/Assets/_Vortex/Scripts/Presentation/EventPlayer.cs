using System;
using System.Collections.Generic;
using Vortex.Core.Events;

namespace Vortex.Client.Presentation
{
    /// <summary>
    /// Plays game events one at a time (ADR-0014): each event starts its feedback, then the player waits the time the
    /// feedback asks for before the next one. Plain C#, advanced by <see cref="Tick"/>, so it is deterministic and
    /// testable without a scene; a MonoBehaviour drives it in the game.
    /// </summary>
    public sealed class EventPlayer
    {
        private readonly Queue<GameEvent> _queue = new Queue<GameEvent>();
        private readonly Func<GameEventType, IFeedback?> _feedbackFor;
        private readonly IFeedbackStage _stage;
        private float _wait;
        private float _speed = 1f;

        /// <summary>Creates a player.</summary>
        /// <param name="feedbackFor">Feedback of each event type (usually <see cref="FeedbackProfile.For"/>).</param>
        /// <param name="stage">Anchors of the scene.</param>
        public EventPlayer(Func<GameEventType, IFeedback?> feedbackFor, IFeedbackStage stage)
        {
            _feedbackFor = feedbackFor ?? throw new ArgumentNullException(nameof(feedbackFor));
            _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        }

        /// <summary>Raised when an event starts playing: views update what it changes.</summary>
        public event Action<GameEvent>? EventStarted;

        /// <summary>Raised when the queue is empty and the last feedback is over: the presentation is idle again.</summary>
        public event Action? Idle;

        /// <summary>Playback speed multiplier (1 = normal). Must be positive.</summary>
        public float Speed
        {
            get => _speed;
            set => _speed = value > 0f ? value : throw new ArgumentOutOfRangeException(nameof(value), "Speed must be positive.");
        }

        /// <summary>True while events remain or a feedback is still running. Player input waits for it.</summary>
        public bool IsPlaying => _wait > 0f || _queue.Count > 0;

        /// <summary>Adds events at the end of the queue.</summary>
        public void Enqueue(IEnumerable<GameEvent> events)
        {
            if (events is null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            foreach (GameEvent e in events)
            {
                _queue.Enqueue(e);
            }
        }

        /// <summary>Advances time by <paramref name="deltaTime"/> seconds (scaled by <see cref="Speed"/>), starting every event that is due.</summary>
        public void Tick(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            _wait -= Math.Max(0f, deltaTime) * _speed;
            while (_wait <= 0f && _queue.Count > 0)
            {
                GameEvent e = _queue.Dequeue();
                float duration = _feedbackFor(e.Type)?.Play(e, _stage) ?? 0f;
                EventStarted?.Invoke(e);
                _wait = Math.Max(_wait, 0f) + Math.Max(0f, duration);
            }

            if (_wait <= 0f && _queue.Count == 0)
            {
                _wait = 0f;
                Idle?.Invoke();
            }
        }

        /// <summary>Starts every remaining event without waiting (skip button): views catch up, feedbacks are not played.</summary>
        public void SkipAll()
        {
            while (_queue.Count > 0)
            {
                EventStarted?.Invoke(_queue.Dequeue());
            }

            _wait = 0f;
            Idle?.Invoke();
        }
    }
}
