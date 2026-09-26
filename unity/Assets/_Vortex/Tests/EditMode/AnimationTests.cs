using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The first animations of the table (ANIMATIONS.md, lot 1), with their placeholder effects.</summary>
    public class AnimationTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void CleanUp()
        {
            foreach (GameObject created in _created.Where(o => o != null))
            {
                Object.DestroyImmediate(created);
            }

            foreach (PlaceholderEffect effect in Object.FindObjectsByType<PlaceholderEffect>())
            {
                Object.DestroyImmediate(effect.gameObject);
            }

            foreach (DieSpinner die in Object.FindObjectsByType<DieSpinner>())
            {
                Object.DestroyImmediate(die.gameObject);
            }

            _created.Clear();
        }

        [Test]
        public void A_ship_sways_under_its_root_and_a_push_goes_out_then_back()
        {
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            Transform body = ship.Find(ShipMotion.BodyName);
            Assert.That(body, Is.Not.Null);
            Assert.That(ship.GetComponentsInChildren<MeshRenderer>().All(r => r.transform.IsChildOf(body)), Is.True, "The parts move under the body.");

            motion.Tick(0.8f);
            Assert.That(body.localPosition.y, Is.Not.EqualTo(0f), "It sways.");
            Assert.That(ship.position, Is.EqualTo(Vector3.zero), "Its root, which the panel follows, stays still.");

            motion.Push(new Vector3(0f, 0f, -1f), 0.1f, 0.5f);
            motion.Tick(0.1f);
            Assert.That(motion.PushOffset.z, Is.EqualTo(-1f).Within(0.01f), "Out, all the way.");
            motion.Tick(0.6f);
            Assert.That(motion.PushOffset, Is.EqualTo(Vector3.zero), "And back.");

            motion.Push(Vector3.forward, 1f, 1f);
            motion.Wreck();
            motion.Tick(0.5f);
            Assert.That((motion.Wrecked, motion.PushOffset), Is.EqualTo((true, Vector3.zero)), "A wreck is not pushed any more.");
        }

        [Test]
        public void An_attack_draws_a_beam_to_the_target_and_the_attacker_recoils()
        {
            Transform attacker = Ship("Attaquant", new Vector3(0f, 0f, -5f), out ShipMotion recoiling);
            Transform target = Ship("Cible", new Vector3(0f, 0f, 5f), out ShipMotion _);
            var stage = new Stage(attacker, target, recoiling);
            float wait = ScriptableObject.CreateInstance<BeamFeedback>().Play(new GameEvent { Type = GameEventType.AttackResolved, Player = 0, Other = 1, Value = 8 }, stage);

            PlaceholderEffect beam = Object.FindObjectsByType<PlaceholderEffect>().Single();
            Assert.That((beam.name, beam.PieceCount), Is.EqualTo(("Rayon", 1)));
            Assert.That(beam.transform.position.z, Is.EqualTo(0f).Within(1f), "Between the two ships.");
            recoiling.Tick(0.06f);
            Assert.That(recoiling.PushOffset.z, Is.LessThan(0f), "The attacker recoils, away from its target.");
            Assert.That(wait, Is.GreaterThan(0f));
        }

        [Test]
        public void Damage_throws_the_ship_back_the_further_the_more_it_loses_and_other_losses_do_not()
        {
            Transform ship = Ship("Cible", new Vector3(0f, 0f, 4f), out ShipMotion motion);
            var centre = new GameObject("Centre").transform;
            _created.Add(centre.gameObject);
            var stage = new Stage(ship, null, motion, centre);
            KnockbackFeedback knockback = ScriptableObject.CreateInstance<KnockbackFeedback>();

            knockback.Play(new GameEvent { Type = GameEventType.HpLost, Player = 0, Amount = 4, Cause = HpLossCause.Attack }, stage);
            motion.Tick(0.1f);
            float small = motion.PushOffset.z;
            motion.Tick(2f);
            knockback.Play(new GameEvent { Type = GameEventType.HpLost, Player = 0, Amount = 10, Cause = HpLossCause.Attack }, stage);
            motion.Tick(0.1f);
            Assert.That(small, Is.GreaterThan(0f), "Away from the middle of the table.");
            Assert.That(motion.PushOffset.z, Is.GreaterThan(small), "Further for more damage.");
            motion.Tick(2f);

            knockback.Play(new GameEvent { Type = GameEventType.HpLost, Player = 0, Amount = 3, Cause = HpLossCause.Torment }, stage);
            motion.Tick(0.1f);
            Assert.That(motion.PushOffset, Is.EqualTo(Vector3.zero), "A Torment loss has its own animation later.");
        }

        [Test]
        public void A_combo_lights_a_flame_on_each_engine_marker()
        {
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            ThrusterFeedback thrusters = ScriptableObject.CreateInstance<ThrusterFeedback>();
            var combo = new GameEvent { Type = GameEventType.TechnologyActivated, Player = 0, Value = (int)TechColor.Red };

            thrusters.Play(combo, new Stage(ship, null, motion));
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Length, Is.EqualTo(1), "Without markers, one flame behind the ship.");
            CleanUpEffects();

            Marker(ship, "Reacteur_Gauche");
            Marker(ship, "Reacteur_Droit");
            thrusters.Play(combo, new Stage(ship, null, motion));
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Length, Is.EqualTo(2), "One flame per engine.");
        }

        [Test]
        public void An_elimination_bursts_into_a_flash_and_debris()
        {
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            ScriptableObject.CreateInstance<ExplosionFeedback>().Play(new GameEvent { Type = GameEventType.PlayerEliminated, Player = 0 }, new Stage(ship, null, motion));
            PlaceholderEffect explosion = Object.FindObjectsByType<PlaceholderEffect>().Single();
            Assert.That(explosion.PieceCount, Is.GreaterThan(10));
            Assert.That(explosion.Tick(5f), Is.False, "It ends by itself.");
        }

        [Test]
        public void The_generated_d8_numbers_its_faces_like_a_real_one_and_turns_the_rolled_face_to_the_camera()
        {
            GameObject die = PlaceholderDie.Build(null, Color.white, Color.black);
            _created.Add(die);
            Transform[] faces = die.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith(PlaceholderDie.FacePrefix)).ToArray();
            Assert.That(faces.Select(f => int.Parse(f.name.Substring(PlaceholderDie.FacePrefix.Length))), Is.EquivalentTo(Enumerable.Range(1, 8)));
            foreach (Transform face in faces)
            {
                int number = int.Parse(face.name.Substring(PlaceholderDie.FacePrefix.Length));
                Transform opposite = faces.Single(f => Vector3.Dot(f.forward, face.forward) < -0.99f);
                Assert.That(number + int.Parse(opposite.name.Substring(PlaceholderDie.FacePrefix.Length)), Is.EqualTo(9), "Opposite faces add up to 9.");
            }

            var camera = new GameObject("Caméra").AddComponent<Camera>();
            _created.Add(camera.gameObject);
            var canvas = new GameObject("Toile", typeof(RectTransform)).AddComponent<Canvas>();
            _created.Add(canvas.gameObject);
            var place = new GameObject("Place", typeof(RectTransform)).GetComponent<RectTransform>();
            place.SetParent(canvas.transform, false);

            DieSpinner spinner = die.AddComponent<DieSpinner>();
            spinner.Follow(place, camera, 3f, 720f);
            spinner.Tick(0.3f);
            Assert.That(spinner.Show(6, 0f), Is.True);
            Transform six = faces.Single(f => f.name == PlaceholderDie.FacePrefix + 6);
            Assert.That(Vector3.Dot(six.forward, -camera.transform.forward), Is.GreaterThan(0.999f), "The rolled face looks at the camera.");
            Assert.That(Vector3.Dot(six.up, camera.transform.up), Is.GreaterThan(0.999f), "Upright.");
        }

        [Test]
        public void The_profile_plays_the_first_animations()
        {
            var profile = AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProjectAssets.ProfilePath);
            Assert.That(profile.For(GameEventType.AttackResolved), Is.InstanceOf<BeamFeedback>());
            Assert.That(profile.For(GameEventType.HpLost), Is.InstanceOf<KnockbackFeedback>());
            Assert.That(profile.For(GameEventType.TechnologyActivated), Is.InstanceOf<ThrusterFeedback>());
            Assert.That(profile.For(GameEventType.PlayerEliminated), Is.InstanceOf<ExplosionFeedback>());
        }

        private static void CleanUpEffects()
        {
            foreach (PlaceholderEffect effect in Object.FindObjectsByType<PlaceholderEffect>())
            {
                Object.DestroyImmediate(effect.gameObject);
            }
        }

        private static void Marker(Transform ship, string name) =>
            new GameObject(name).transform.SetParent(ship.Find(ShipMotion.BodyName), false);

        private Transform Ship(string name, Vector3 position, out ShipMotion motion)
        {
            var root = new GameObject(name);
            _created.Add(root);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(position.sqrMagnitude > 0f ? -position.normalized : Vector3.forward);
            PlaceholderShip.Build(root.transform, Color.cyan);
            motion = ShipMotion.Attach(root.transform, 0.05f, 2f, 1f, 3f, 0f);
            return root.transform;
        }

        private sealed class Stage : IFeedbackStage
        {
            private readonly Transform _player;
            private readonly Transform? _other;
            private readonly ShipMotion _motion;
            private readonly Transform? _table;

            public Stage(Transform player, Transform? other, ShipMotion motion, Transform? table = null)
            {
                _player = player;
                _other = other;
                _motion = motion;
                _table = table;
            }

            public float PlaybackSpeed => 1f;

            public ThemeSettings? Theme => null;

            public Camera? View => null;

            public Transform? AnchorFor(FeedbackAnchor anchor, GameEvent gameEvent) => anchor switch
            {
                FeedbackAnchor.Player => _player,
                FeedbackAnchor.Other => _other,
                FeedbackAnchor.Table => _table,
                _ => null,
            };

            public ShipMotion? MotionOf(int seat) => seat == 0 ? _motion : null;
        }
    }
}
