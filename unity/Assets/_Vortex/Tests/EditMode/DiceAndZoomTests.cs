using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Presentation;
using Vortex.Core.Events;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>Readability after the first playtest: animated dice and enlarged cards (M4.5).</summary>
    public class DiceAndZoomTests
    {
        private sealed class Stage : IFeedbackStage
        {
            private readonly Transform _anchor;

            public Stage(Transform anchor, float speed)
            {
                _anchor = anchor;
                PlaybackSpeed = speed;
            }

            public float PlaybackSpeed { get; }

            public Transform? AnchorFor(FeedbackAnchor anchor, GameEvent gameEvent) => _anchor;

            public Vortex.Client.Theme.ThemeSettings? Theme => null;

            public Camera? View => null;

            public ShipMotion? MotionOf(int seat) => null;
        }

        [Test]
        public void Dice_roll_then_settle_on_the_values_of_the_engine()
        {
            DiceTray tray = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssets.DiceTrayPath)).GetComponent<DiceTray>();
            tray.Roll(new[] { 3, 8 }, 11, 0.5f, 0.5f);
            Assert.That(tray.Settled, Is.False);
            Assert.That(tray.Faces.Count(), Is.EqualTo(2));

            tray.Advance(0.6f);
            Assert.That(tray.Settled, Is.True);
            Assert.That(tray.Faces, Is.EqualTo(new[] { "3", "8" }));
            Assert.That(tray.Total, Is.EqualTo("= 11"));

            tray.Advance(1f);
            Assert.That(tray == null, Is.True, "The tray goes away after showing the result.");
        }

        [Test]
        public void The_dice_feedback_follows_the_playback_speed()
        {
            var parent = new GameObject("Ancre", typeof(RectTransform));
            try
            {
                var feedback = AssetDatabase.LoadAssetAtPath<DiceFeedback>(ProjectAssets.DicePath);
                var roll = new GameEvent { Type = GameEventType.DiceRolled, Player = 0, Values = new System.Collections.Generic.List<int> { 2, 5 }, Amount = 7 };
                float wait = feedback.Play(roll, new Stage(parent.transform, 4f));
                Assert.That(wait, Is.EqualTo(1.4f).Within(1e-4), "The wait is in playback seconds; the event player applies the speed.");

                DiceTray tray = parent.GetComponentInChildren<DiceTray>();
                Assert.That(tray, Is.Not.Null);
                tray.Advance(0.2f);
                Assert.That(tray.Settled, Is.True, "At speed 4, a 0.6 s roll takes 0.15 s.");

                feedback.Play(new GameEvent { Type = GameEventType.DieRolled, Value = 4 }, new Stage(parent.transform, 1f));
                Assert.That(parent.GetComponentsInChildren<DiceTray>(), Has.Length.EqualTo(1), "A new roll replaces the previous tray.");

                Assert.That(feedback.Play(new GameEvent { Type = GameEventType.DiceRolled }, new Stage(parent.transform, 1f)), Is.Zero, "No dice, nothing to show.");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void The_default_profile_shows_dice_for_every_roll()
        {
            var profile = AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProjectAssets.ProfilePath);
            Assert.That(profile.For(GameEventType.DiceRolled), Is.InstanceOf<DiceFeedback>());
            Assert.That(profile.For(GameEventType.DieRolled), Is.InstanceOf<DiceFeedback>());
        }

        [Test]
        public void Pointing_at_a_table_card_shows_it_enlarged()
        {
            Scene scene = EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            try
            {
                GameDirector director = scene.GetRootGameObjects().Select(o => o.GetComponent<GameDirector>()).Single(d => d != null);
                director.HumanFirstSeat = false;
                director.Begin();
                director.Advance(0f);

                CardHover[] cards = Object.FindObjectsByType<CardHover>();
                Assert.That(cards, Is.Not.Empty, "Every card of the table can be pointed at.");
                CardZoom zoom = Object.FindAnyObjectByType<CardZoom>();
                CardDisplay pointed = cards[0].GetComponent<CardDisplay>();

                cards[0].OnPointerEnter(null!);
                Assert.That(zoom.Shown, Is.Not.Null);
                Assert.That(zoom.Shown!.Face.Id, Is.EqualTo(pointed.Face.Id));
                Assert.That(zoom.Shown.Torments, Is.EqualTo(pointed.Torments));

                cards[0].OnPointerExit(null!);
                Assert.That(zoom.Shown, Is.Null);
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }
    }
}
