using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Presentation;
using Vortex.Core.Events;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Events play one at a time, each waiting for its feedback (ADR-0014).</summary>
    public class EventPlayerTests
    {
        private sealed class NoStage : IFeedbackStage
        {
            public Transform? AnchorFor(FeedbackAnchor anchor, GameEvent gameEvent) => null;

            public float PlaybackSpeed => 1f;
        }

        private sealed class FixedFeedback : IFeedback
        {
            private readonly float _seconds;

            public FixedFeedback(float seconds) => _seconds = seconds;

            public List<GameEventType> Played { get; } = new List<GameEventType>();

            public float Play(GameEvent gameEvent, IFeedbackStage stage)
            {
                Played.Add(gameEvent.Type);
                return _seconds;
            }
        }

        private static GameEvent E(GameEventType type) => new GameEvent { Type = type };

        [Test]
        public void Events_play_in_order_each_after_the_previous_feedback()
        {
            var feedback = new FixedFeedback(1f);
            var player = new EventPlayer(_ => feedback, new NoStage());
            var started = new List<GameEventType>();
            int idle = 0;
            player.EventStarted += e => started.Add(e.Type);
            player.Idle += () => idle++;

            player.Enqueue(new[] { E(GameEventType.RoundStarted), E(GameEventType.TurnStarted), E(GameEventType.TurnEnded) });
            Assert.That(player.IsPlaying, Is.True);
            player.Tick(0f);
            Assert.That(started, Is.EqualTo(new[] { GameEventType.RoundStarted }));
            player.Tick(0.5f);
            Assert.That(started, Has.Count.EqualTo(1), "The first feedback lasts 1 s.");
            player.Tick(0.5f);
            Assert.That(started, Has.Count.EqualTo(2));
            player.Tick(1f);
            Assert.That(started, Has.Count.EqualTo(3));
            Assert.That(idle, Is.Zero, "The last feedback is still running.");
            player.Tick(1f);
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(idle, Is.EqualTo(1));
        }

        [Test]
        public void Speed_scales_time_and_must_be_positive()
        {
            var player = new EventPlayer(_ => new FixedFeedback(1f), new NoStage()) { Speed = 4f };
            player.Enqueue(new[] { E(GameEventType.RoundStarted), E(GameEventType.TurnStarted) });
            player.Tick(0f);
            player.Tick(0.25f);
            Assert.That(player.IsPlaying, Is.True, "Second event started, its feedback runs.");
            player.Tick(0.25f);
            Assert.That(player.IsPlaying, Is.False);
            Assert.That(() => player.Speed = 0f, Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Instant_events_all_start_in_the_same_tick()
        {
            var player = new EventPlayer(_ => null, new NoStage());
            var started = new List<GameEventType>();
            player.EventStarted += e => started.Add(e.Type);
            player.Enqueue(new[] { E(GameEventType.DiceRolled), E(GameEventType.ShieldChanged), E(GameEventType.HpLost) });
            player.Tick(0.01f);
            Assert.That(started, Has.Count.EqualTo(3));
            Assert.That(player.IsPlaying, Is.False);
        }

        [Test]
        public void Skipping_starts_every_event_without_feedback()
        {
            var feedback = new FixedFeedback(5f);
            var player = new EventPlayer(_ => feedback, new NoStage());
            var started = new List<GameEventType>();
            player.EventStarted += e => started.Add(e.Type);
            player.Enqueue(new[] { E(GameEventType.RoundStarted), E(GameEventType.TurnStarted) });
            player.Tick(0f);
            player.SkipAll();
            Assert.That(started, Has.Count.EqualTo(2));
            Assert.That(feedback.Played, Has.Count.EqualTo(1), "Only the event already started played its feedback.");
            Assert.That(player.IsPlaying, Is.False);
        }

        [Test]
        public void A_profile_maps_event_types_and_falls_back()
        {
            var pause = ScriptableObject.CreateInstance<PauseFeedback>();
            pause.Configure(0.3f);
            var longPause = ScriptableObject.CreateInstance<PauseFeedback>();
            longPause.Configure(2f);
            var profile = ScriptableObject.CreateInstance<FeedbackProfile>();
            profile.Configure(pause, (GameEventType.AttackResolved, longPause));

            Assert.That(profile.For(GameEventType.AttackResolved)!.Play(E(GameEventType.AttackResolved), new NoStage()), Is.EqualTo(2f));
            Assert.That(profile.For(GameEventType.HpLost)!.Play(E(GameEventType.HpLost), new NoStage()), Is.EqualTo(0.3f).Within(1e-6));
            Object.DestroyImmediate(pause);
            Object.DestroyImmediate(longPause);
            Object.DestroyImmediate(profile);
        }

        [Test]
        public void The_project_has_a_default_feedback_profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProjectAssets.ProfilePath);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.For(GameEventType.GameStarted), Is.Not.Null, "Every event type has at least the fallback.");
        }
    }
}
