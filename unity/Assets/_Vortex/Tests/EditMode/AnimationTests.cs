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
            // A model with its mesh on its own root, like an imported single-mesh model (third playtest: nothing moved).
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            Transform body = motion.Body;
            Assert.That(body.GetComponent<MeshRenderer>(), Is.Not.Null);

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
        public void The_attacker_turns_to_its_target_then_fires_a_bolt_recoils_and_turns_back()
        {
            // The attacker faces the middle of the table (+z); its target is to its right.
            Transform attacker = Ship("Attaquant", new Vector3(0f, 0f, -5f), out ShipMotion aiming);
            Transform target = Ship("Cible", new Vector3(8f, 0f, -5f), out ShipMotion _);
            var stage = new Stage(attacker, target, aiming);

            ScriptableObject.CreateInstance<AimFeedback>().Play(new GameEvent { Type = GameEventType.AttackDeclared, Player = 0, Other = 1 }, stage);
            aiming.Tick(1f);
            Assert.That(aiming.Yaw, Is.EqualTo(90f).Within(2f), "Nose towards the target before the shot.");

            float wait = ScriptableObject.CreateInstance<LaserFeedback>().Play(new GameEvent { Type = GameEventType.AttackResolved, Player = 0, Other = 1, Value = 8 }, stage);
            PlaceholderEffect bolt = Object.FindObjectsByType<PlaceholderEffect>().Single();
            Assert.That((bolt.name, bolt.PieceCount), Is.EqualTo(("Tir laser", 3)), "Muzzle flash, bolt, impact.");
            Assert.That(wait, Is.GreaterThan(0.3f), "The damage waits for the impact.");

            aiming.Tick(0.2f);
            Assert.That(aiming.PushOffset.x, Is.LessThan(0f), "The attacker recoils, away from its target.");
            aiming.Tick(wait + 2f);
            Assert.That(aiming.Yaw, Is.EqualTo(0f).Within(0.5f), "Back to its place after the shot.");
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
            Assert.That(motion.PushSpin.magnitude, Is.GreaterThan(10f), "Spun around by a heavy hit.");
            PlaceholderEffect[] impacts = Object.FindObjectsByType<PlaceholderEffect>().Where(e => e.name == "Impact").OrderBy(e => e.PieceCount).ToArray();
            Assert.That(impacts, Has.Length.EqualTo(2));
            Assert.That(impacts[1].PieceCount, Is.GreaterThan(impacts[0].PieceCount * 2), "A heavier hit makes a bigger impact.");
            motion.Tick(4f);
            Assert.That(motion.PushOffset.magnitude, Is.LessThan(0.001f), "Back where it was: the elastic settled.");
            Assert.That(motion.PushSpin.magnitude, Is.LessThan(0.1f));

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
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Length, Is.EqualTo(3), "The pulse, and without markers two jets at the back of the model (playtest 4).");
            CleanUpEffects();

            Marker(ship, "Reacteur_Gauche");
            Marker(ship, "Reacteur_Droit");
            thrusters.Play(combo, new Stage(ship, null, motion));
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Length, Is.EqualTo(3), "The pulse and one jet per engine.");
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
        public void A_deflected_bolt_bounces_off_the_deflecting_shield_to_the_new_target()
        {
            Transform attacker = Ship("Attaquant", new Vector3(0f, 0f, -5f), out ShipMotion aiming);
            Transform target = Ship("Cible", new Vector3(8f, 0f, 3f), out ShipMotion _);
            Transform deflector = Ship("Dévieur", new Vector3(-6f, 0f, 3f), out ShipMotion deflecting);
            var stage = new Stage(attacker, target, aiming, null, (2, deflecting));

            ScriptableObject.CreateInstance<AimFeedback>().Play(new GameEvent { Type = GameEventType.AttackDeclared, Player = 0, Other = 2 }, stage);
            ScriptableObject.CreateInstance<DeflectFeedback>().Play(new GameEvent { Type = GameEventType.AttackRedirected, Player = 1, Other = 2 }, stage);
            Assert.That(stage.Attack.Deflector, Is.EqualTo(2));
            CleanUpEffects();

            ScriptableObject.CreateInstance<LaserFeedback>().Play(new GameEvent { Type = GameEventType.AttackResolved, Player = 0, Other = 1, Value = 8, Amount = 5, Values = new List<int> { 0, 8, 8 } }, stage);
            PlaceholderEffect shot = Object.FindObjectsByType<PlaceholderEffect>().Single(e => e.name == "Tir laser");
            Assert.That(shot.PieceCount, Is.EqualTo(5), "Muzzle flash, first leg, flash on the deflecting shield, second leg, impact.");
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Count(e => e.name == "Bouclier"), Is.EqualTo(1), "The deflecting ship's shield lights up.");
            Assert.That((stage.Attack.Deflector, stage.Attack.LandedDamage), Is.EqualTo((-1, 5)), "Drawn: the deflection is forgotten, the damage kept.");
        }

        [Test]
        public void A_shield_that_stops_part_of_a_bolt_lights_up_and_a_protection_that_spares_it_all_makes_the_ship_dodge()
        {
            Transform attacker = Ship("Attaquant", new Vector3(0f, 0f, -5f), out ShipMotion aiming);
            Transform target = Ship("Cible", new Vector3(0f, 0f, 5f), out ShipMotion dodging);
            var stage = new Stage(attacker, target, aiming, null, (1, dodging));

            ScriptableObject.CreateInstance<LaserFeedback>().Play(new GameEvent { Type = GameEventType.AttackResolved, Player = 0, Other = 1, Value = 8, Amount = 3, Values = new List<int> { 5, 3, 3 } }, stage);
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Count(e => e.name == "Bouclier"), Is.EqualTo(1), "The shield stopped 5 of 8: it lights up.");

            // Seen from the target's side: seat 0 of this stage is the target.
            var spared = new GameEvent { Type = GameEventType.HpLossPrevented, Player = 0, Other = 1, Amount = 3, Cause = HpLossCause.Attack };
            ScriptableObject.CreateInstance<DodgeFeedback>().Play(spared, new Stage(target, attacker, dodging) { Memory = stage.Attack });
            dodging.Tick(0.14f);
            Assert.That(Mathf.Abs(dodging.PushOffset.x), Is.GreaterThan(0.5f), "All of it spared: the ship swerves aside.");
            dodging.Tick(2f);
            Assert.That(dodging.PushOffset, Is.EqualTo(Vector3.zero), "And comes back.");
        }

        [Test]
        public void A_critical_hit_shakes_the_camera_which_comes_back_to_its_place()
        {
            var camera = new GameObject("Caméra").AddComponent<Camera>();
            _created.Add(camera.gameObject);
            camera.transform.localPosition = new Vector3(1f, 2f, 3f);
            CameraShake shake = camera.gameObject.AddComponent<CameraShake>();
            shake.Shake(0.2f, 0.5f);
            shake.Tick(0.1f);
            Assert.That(camera.transform.localPosition, Is.Not.EqualTo(new Vector3(1f, 2f, 3f)));
            shake.Tick(1f);
            Assert.That((shake.Shaking, camera.transform.localPosition), Is.EqualTo((false, new Vector3(1f, 2f, 3f))));
        }

        [Test]
        public void A_shield_change_lights_the_shield_and_a_damaged_ship_smokes()
        {
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            var stage = new Stage(ship, null, motion);
            ShieldPulseFeedback pulse = ScriptableObject.CreateInstance<ShieldPulseFeedback>();
            Assert.That(pulse.Play(new GameEvent { Type = GameEventType.ShieldChanged, Player = 0, Value = 5, Amount = 5 }, stage), Is.Zero, "No change, nothing to show.");
            pulse.Play(new GameEvent { Type = GameEventType.ShieldChanged, Player = 0, Value = 7, Amount = 2 }, stage);
            pulse.Play(new GameEvent { Type = GameEventType.ShieldChanged, Player = 0, Value = 1, Amount = 7 }, stage);
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Count(e => e.name == "Bouclier"), Is.EqualTo(2));

            HullDamage hull = motion.Body.gameObject.AddComponent<HullDamage>();
            hull.Show(0.3f, false, null);
            hull.Tick(3f);
            Assert.That(hull.Puffs, Is.Zero, "Above half its HP, no smoke.");
            hull.Show(0.9f, false, null);
            for (int i = 0; i < 20; i++)
            {
                hull.Tick(0.25f);
            }

            Assert.That(hull.Puffs, Is.GreaterThan(5), "Badly damaged: it smokes.");
            int puffs = hull.Puffs;
            hull.Show(1f, true, null);
            hull.Tick(3f);
            Assert.That(hull.Puffs, Is.EqualTo(puffs), "A wreck does not smoke.");
        }

        [Test]
        public void Torment_bursts_into_spores_and_cleansing_rises_off_the_ship()
        {
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            TormentFeedback torment = ScriptableObject.CreateInstance<TormentFeedback>();
            torment.Play(new GameEvent { Type = GameEventType.TormentPlaced, Player = 0, CardUid = 3, Value = 1 }, new Stage(ship, null, motion));
            torment.Play(new GameEvent { Type = GameEventType.TormentsRemoved, Player = 0, CardUid = 3, Amount = 1 }, new Stage(ship, null, motion));
            torment.Play(new GameEvent { Type = GameEventType.TormentPlaced, Player = -1, CardUid = 9, Value = 1 }, new Stage(ship, null, motion));
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Select(e => e.name), Is.EquivalentTo(new[] { "Spores", "Purification" }), "A token on a market card has no ship.");
            motion.Tick(0.05f);
            Assert.That(motion.PushOffset, Is.Not.EqualTo(Vector3.zero), "The ship shudders.");
        }

        [Test]
        public void A_stolen_card_flies_as_a_3D_card_and_a_card_placed_by_hand_does_not()
        {
            Transform thief = Ship("Voleur", new Vector3(-4f, 0f, 0f), out ShipMotion motion);
            Transform victim = Ship("Victime", new Vector3(4f, 0f, 0f), out ShipMotion _);
            CardDisplay prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ThemeAssets.CardPrefabPath).GetComponent<CardDisplay>();
            var stage = new Stage(thief, victim, motion) { Cards = () => Object.Instantiate(prefab) };
            CardFlightFeedback flight = ScriptableObject.CreateInstance<CardFlightFeedback>();

            flight.Play(new GameEvent { Type = GameEventType.CardStolen, Player = 0, Other = 1, CardUid = 5, Id = "A_001" }, stage);
            CardTrip trip = Object.FindObjectsByType<CardTrip>().Single();
            _created.Add(trip.gameObject);
            Assert.That(trip.transform.position.x, Is.EqualTo(4f).Within(0.5f), "It leaves the victim's ship.");
            trip.Tick(0.25f);
            Assert.That(trip.transform.position.y, Is.GreaterThan(1f), "It flies on a curve above the table.");
            Assert.That(trip.Tick(5f), Is.False, "Arrived: gone, the table shows it in its place.");

            stage.PlacedByHand.Add(6);
            flight.Play(new GameEvent { Type = GameEventType.CardActivated, Player = 0, CardUid = 6, Id = "A_001" }, stage);
            Assert.That(Object.FindObjectsByType<CardTrip>(), Is.Empty, "Dropped where it goes by hand: no trip.");
        }

        [Test]
        public void A_thrown_ship_flies_past_its_place_on_the_elastic_and_a_wreck_drifts_off()
        {
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            motion.Throw(new Vector3(0f, 0f, 6f), Vector3.zero);
            float farthest = 0f;
            float past = 0f;
            for (int i = 0; i < 200; i++)
            {
                motion.Tick(0.01f);
                farthest = Mathf.Max(farthest, motion.PushOffset.z);
                past = Mathf.Min(past, motion.PushOffset.z);
            }

            Assert.That(farthest, Is.GreaterThan(0.5f), "Thrown off freely.");
            Assert.That(past, Is.LessThan(0f), "The elastic brings it back a little past its place.");

            motion.Wreck();
            motion.Throw(new Vector3(0f, 0f, 6f), Vector3.zero);
            motion.Tick(3f);
            Assert.That(motion.PushOffset.z, Is.GreaterThan(1f), "A wreck has no elastic: it drifts off.");
        }

        [Test]
        public void A_critical_die_leaps_towards_the_camera_and_lands_with_a_burst()
        {
            GameObject die = PlaceholderDie.Build(null, Color.white, Color.black);
            _created.Add(die);
            var camera = new GameObject("Caméra").AddComponent<Camera>();
            _created.Add(camera.gameObject);
            var canvas = new GameObject("Toile", typeof(RectTransform)).AddComponent<Canvas>();
            _created.Add(canvas.gameObject);
            var place = new GameObject("Place", typeof(RectTransform)).GetComponent<RectTransform>();
            place.SetParent(canvas.transform, false);
            DieSpinner spinner = die.AddComponent<DieSpinner>();
            spinner.Follow(place, camera, 3f, 720f);
            spinner.Show(8, 0f);
            float rest = Vector3.Distance(die.transform.position, camera.transform.position);

            Vector3? landed = null;
            spinner.Leap(0.6f, at => landed = at);
            spinner.Tick(0.3f);
            Assert.That(Vector3.Distance(die.transform.position, camera.transform.position), Is.LessThan(rest * 0.7f), "Towards the camera.");
            spinner.Tick(0.4f);
            Assert.That((spinner.Leaping, landed.HasValue), Is.EqualTo((false, true)), "Landed, with its burst.");
            Assert.That(Vector3.Distance(die.transform.position, camera.transform.position), Is.EqualTo(rest).Within(0.01f), "Back in its place.");
        }

        [Test]
        public void A_round_event_sends_a_wave_and_damage_sent_back_flies_back_first()
        {
            Transform ship = Ship("Vaisseau", new Vector3(0f, 0f, -4f), out ShipMotion motion);
            Transform reflector = Ship("Talion", new Vector3(0f, 0f, 4f), out ShipMotion _);
            var centre = new GameObject("Centre").transform;
            _created.Add(centre.gameObject);

            ScriptableObject.CreateInstance<EventFeedback>().Play(new GameEvent { Type = GameEventType.EventRevealed, Id = "EVT_TEST" }, new Stage(ship, null, motion, centre));
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Single().name, Is.EqualTo("Vague d'événement"), "No effect in the theme: the generic wave.");
            CleanUpEffects();

            KnockbackFeedback knockback = ScriptableObject.CreateInstance<KnockbackFeedback>();
            float direct = knockback.Play(new GameEvent { Type = GameEventType.HpLost, Player = 0, Amount = 4, Cause = HpLossCause.Attack }, new Stage(ship, reflector, motion, centre));
            float sentBack = knockback.Play(new GameEvent { Type = GameEventType.HpLost, Player = 0, Other = 1, Amount = 4, Cause = HpLossCause.Reflect }, new Stage(ship, reflector, motion, centre));
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Count(e => e.name == "Renvoi"), Is.EqualTo(1));
            Assert.That(sentBack, Is.GreaterThan(direct), "The hit waits for the bolt sent back.");
        }

        [Test]
        public void The_sky_is_one_mesh_of_stars_and_an_overcharged_ship_crackles()
        {
            Starfield sky = Starfield.Create(null, Vector3.zero, 300, 60f, null);
            _created.Add(sky.gameObject);
            Assert.That((sky.Stars, sky.GetComponent<MeshFilter>().sharedMesh.vertexCount), Is.EqualTo((300, 1200)));

            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            OverchargeArcs arcs = motion.Body.gameObject.AddComponent<OverchargeArcs>();
            arcs.Show(false, false, Color.cyan, null);
            arcs.Tick(2f);
            Assert.That(arcs.Sparks, Is.Zero, "No token, no sparks.");
            arcs.Show(true, true, Color.cyan, null);
            for (int i = 0; i < 20; i++)
            {
                arcs.Tick(0.1f);
            }

            Assert.That(arcs.Sparks, Is.GreaterThan(5));
        }

        [Test]
        public void A_ship_shows_its_contamination_its_effects_and_its_turn_and_repairs_sparkle()
        {
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            ShipAura aura = ship.gameObject.AddComponent<ShipAura>();
            aura.Show(0.8f, Color.green, new[] { Color.red, Color.blue }, true, Color.yellow, null);
            Assert.That((aura.Rings, aura.TurnShown), Is.EqualTo((2, true)), "A ring per effect in play, and the halo of its turn.");
            for (int i = 0; i < 10; i++)
            {
                aura.Tick(0.3f);
            }

            Assert.That(aura.Spores, Is.GreaterThan(3), "Contaminated: spores drift off.");
            int spores = aura.Spores;
            aura.Show(0f, Color.green, new Color[0], false, Color.yellow, null);
            aura.Tick(3f);
            Assert.That((aura.Rings, aura.TurnShown, aura.Spores), Is.EqualTo((0, false, spores)), "Healthy, no effect, not its turn.");

            RepairFeedback repair = ScriptableObject.CreateInstance<RepairFeedback>();
            repair.Play(new GameEvent { Type = GameEventType.HpGained, Player = 0, Amount = 1 }, new Stage(ship, null, motion));
            repair.Play(new GameEvent { Type = GameEventType.HpGained, Player = 0, Amount = 8 }, new Stage(ship, null, motion));
            int[] sparkles = Object.FindObjectsByType<PlaceholderEffect>().Where(e => e.name == "Réparation").Select(e => e.PieceCount).OrderBy(n => n).ToArray();
            Assert.That(sparkles, Has.Length.EqualTo(2));
            Assert.That(sparkles[1], Is.GreaterThan(sparkles[0]), "More sparkles for a bigger repair.");
        }

        [Test]
        public void The_camera_turns_around_the_winner_and_comes_back_for_the_next_game()
        {
            Transform ship = Ship("Vainqueur", new Vector3(3f, 0f, 2f), out ShipMotion motion);
            var camera = new GameObject("Caméra").AddComponent<Camera>();
            _created.Add(camera.gameObject);
            camera.transform.SetPositionAndRotation(new Vector3(0f, 7f, -10f), Quaternion.Euler(33f, 0f, 0f));
            var stage = new Stage(ship, null, motion) { ViewCamera = camera };

            ScriptableObject.CreateInstance<VictoryFeedback>().Play(new GameEvent { Type = GameEventType.GameOver, Player = 0, Value = (int)Vortex.Core.State.WinCondition.GalacticElection }, stage);
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Any(e => e.name == "Pluie d'or"), Is.True, "The Election rains gold.");
            CameraOrbit orbit = camera.GetComponent<CameraOrbit>();
            orbit.Tick(5f);
            Assert.That(Vector3.Dot(camera.transform.forward, (ship.position - camera.transform.position).normalized), Is.GreaterThan(0.99f), "Looking at the winner.");
            Assert.That(camera.transform.position, Is.Not.EqualTo(new Vector3(0f, 7f, -10f)));
            orbit.Restore();
            Assert.That((orbit.Orbiting, camera.transform.position), Is.EqualTo((false, new Vector3(0f, 7f, -10f))), "Back for the next game.");
        }

        [Test]
        public void Each_ship_rolls_its_initiative_die_above_itself_and_the_winner_flares()
        {
            Transform ship = Ship("Vaisseau", Vector3.zero, out ShipMotion motion);
            var camera = new GameObject("Caméra").AddComponent<Camera>();
            _created.Add(camera.gameObject);
            var stage = new Stage(ship, null, motion) { ViewCamera = camera };
            InitiativeFeedback initiative = ScriptableObject.CreateInstance<InitiativeFeedback>();

            initiative.Play(new GameEvent { Type = GameEventType.InitiativeRolled, Player = 0, Value = 3 }, stage);
            initiative.Play(new GameEvent { Type = GameEventType.InitiativeRolled, Player = 0, Value = 6 }, stage);
            InitiativeRoll[] rolls = Object.FindObjectsByType<InitiativeRoll>();
            Assert.That(rolls, Has.Length.EqualTo(1), "A roll again for a tie replaces the ship's die.");
            _created.Add(rolls[0].gameObject);
            Assert.That(rolls[0].transform.position.y, Is.GreaterThan(ship.position.y + 1f), "Above the ship.");
            rolls[0].Tick(0.7f);
            Assert.That(rolls[0].GetComponent<DieSpinner>().Value, Is.EqualTo(6), "It shows the engine's value.");

            initiative.Play(new GameEvent { Type = GameEventType.InitiativeWon, Player = 0 }, stage);
            Assert.That(Object.FindObjectsByType<PlaceholderEffect>().Any(e => e.name == "Initiative gagnée"), Is.True);
        }

        [Test]
        public void The_profile_plays_the_first_animations()
        {
            var profile = AssetDatabase.LoadAssetAtPath<FeedbackProfile>(ProjectAssets.ProfilePath);
            Assert.That(profile.For(GameEventType.AttackDeclared), Is.InstanceOf<AimFeedback>());
            Assert.That(profile.For(GameEventType.AttackResolved), Is.InstanceOf<LaserFeedback>());
            Assert.That(profile.For(GameEventType.HpLost), Is.InstanceOf<KnockbackFeedback>());
            Assert.That(profile.For(GameEventType.TechnologyActivated), Is.InstanceOf<ThrusterFeedback>());
            Assert.That(profile.For(GameEventType.PlayerEliminated), Is.InstanceOf<ExplosionFeedback>());
            Assert.That(profile.For(GameEventType.AttackRedirected), Is.InstanceOf<DeflectFeedback>());
            Assert.That(profile.For(GameEventType.CriticalHit), Is.InstanceOf<CriticalFeedback>());
            Assert.That(profile.For(GameEventType.HpLossPrevented), Is.InstanceOf<DodgeFeedback>());
            Assert.That(profile.For(GameEventType.ShieldChanged), Is.InstanceOf<ShieldPulseFeedback>());
            Assert.That(profile.For(GameEventType.TormentPlaced), Is.InstanceOf<TormentFeedback>());
            Assert.That(profile.For(GameEventType.TormentsRemoved), Is.InstanceOf<TormentFeedback>());
            Assert.That(profile.For(GameEventType.MarketCardTaken), Is.InstanceOf<CardFlightFeedback>());
            Assert.That(profile.For(GameEventType.CardStolen), Is.InstanceOf<CardFlightFeedback>());
            Assert.That(profile.For(GameEventType.CardActivated), Is.InstanceOf<CardFlightFeedback>());
            Assert.That(profile.For(GameEventType.EventRevealed), Is.InstanceOf<EventFeedback>());
            Assert.That(profile.For(GameEventType.HpGained), Is.InstanceOf<RepairFeedback>());
            Assert.That(profile.For(GameEventType.GameOver), Is.InstanceOf<VictoryFeedback>());
            Assert.That(profile.For(GameEventType.InitiativeRolled), Is.InstanceOf<InitiativeFeedback>());
            Assert.That(profile.For(GameEventType.InitiativeWon), Is.InstanceOf<InitiativeFeedback>());
        }

        private static void CleanUpEffects()
        {
            foreach (PlaceholderEffect effect in Object.FindObjectsByType<PlaceholderEffect>())
            {
                Object.DestroyImmediate(effect.gameObject);
            }
        }

        private static void Marker(Transform ship, string name) =>
            new GameObject(name).transform.SetParent(ship.GetComponent<ShipMotion>().Body, false);

        private Transform Ship(string name, Vector3 position, out ShipMotion motion)
        {
            var root = new GameObject(name);
            _created.Add(root);
            root.transform.position = position;
            root.transform.rotation = Quaternion.LookRotation(position.sqrMagnitude > 0f ? -position.normalized : Vector3.forward);
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            model.transform.SetParent(root.transform, false);
            motion = ShipMotion.Attach(root.transform, model.transform, 0.05f, 2f, 1f, 3f, 0f);
            return root.transform;
        }

        private sealed class Stage : IFeedbackStage
        {
            private readonly Transform _player;
            private readonly Transform? _other;
            private readonly ShipMotion _motion;
            private readonly Transform? _table;

            private readonly (int Seat, ShipMotion Motion) _extra;

            public Stage(Transform player, Transform? other, ShipMotion motion, Transform? table = null, (int Seat, ShipMotion Motion) extra = default)
            {
                _player = player;
                _other = other;
                _motion = motion;
                _table = table;
                _extra = extra;
            }

            public AttackMemory Memory { get; set; } = new AttackMemory();

            public float PlaybackSpeed => 1f;

            public ThemeSettings? Theme => null;

            public Camera? ViewCamera { get; set; }

            public Camera? View => ViewCamera;

            public MarketDisplay? Markets => null;

            public System.Func<CardDisplay>? Cards { get; set; }

            public HashSet<int> PlacedByHand { get; } = new HashSet<int>();

            public CardDisplay? NewCard(string cardId) => Cards?.Invoke();

            public bool TakePlacedByHand(int uid) => PlacedByHand.Remove(uid);

            public AttackMemory Attack => Memory;

            public Transform? AnchorFor(FeedbackAnchor anchor, GameEvent gameEvent) => anchor switch
            {
                FeedbackAnchor.Player => _player,
                FeedbackAnchor.Other => _other,
                FeedbackAnchor.Table => _table,
                _ => null,
            };

            public ShipMotion? MotionOf(int seat) => seat == 0 ? _motion : (_extra.Motion != null && seat == _extra.Seat ? _extra.Motion : null);
        }
    }
}
