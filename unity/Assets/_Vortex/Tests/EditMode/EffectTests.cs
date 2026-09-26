using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The effects of the Blender lot B4 (EffectSync): built, given to the game, and over before it removes them.</summary>
    public class EffectTests
    {
        // How long the game keeps each effect before destroying it: the feedback's `seconds` where it has one, else the
        // time written in the code that places it (KnockbackFeedback 1.5 s, HullDamage 3 s, OverchargeArcs 1 s).
        private static readonly (string Prefab, string? Feedback, float Seconds)[] Lifetimes =
        {
            ("Impact", null, 1.5f),
            ("Explosion", "Explosion", 0f),
            ("Spores", "Torment", 0f),
            ("Fumee", null, 3f),
            ("Arcs", null, 1f),
        };

        [Test]
        public void Every_effect_is_built_and_given_to_the_game()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(EffectSync.ModelPath) == null)
            {
                Assert.Ignore("No effects model yet.");
                return;
            }

            foreach (string name in EffectSync.Prefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EffectSync.PrefabPath(name));
                Assert.That(prefab, Is.Not.Null, name);
                foreach (ParticleSystem system in prefab.GetComponentsInChildren<ParticleSystem>())
                {
                    Assert.That(system.main.scalingMode, Is.EqualTo(ParticleSystemScalingMode.Hierarchy), name + "/" + system.name + ": scales with its ship or its blow");
                    Assert.That(system.GetComponent<ParticleSystemRenderer>().sharedMaterial, Is.Not.Null, name + "/" + system.name);
                }
            }

            string feedback = "Assets/_Vortex/Presentation/Feedback/";
            Assert.That(Field(feedback + "Laser.asset", "prefab"), Is.SameAs(Prefab("TirLaser")));
            Assert.That(Field(feedback + "Knockback.asset", "sparks"), Is.SameAs(Prefab("Impact")));
            Assert.That(Field(feedback + "Explosion.asset", "prefab"), Is.SameAs(Prefab("Explosion")));
            Assert.That(Field(feedback + "Thrusters.asset", "prefab"), Is.SameAs(Prefab("Postcombustion")));
            Assert.That(Field(feedback + "Torment.asset", "prefab"), Is.SameAs(Prefab("Spores")));
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            Assert.That(theme.SmokePrefab, Is.SameAs(Prefab("Fumee")));
            Assert.That(theme.ArcPrefab, Is.SameAs(Prefab("Arcs")));
            Assert.That(theme.ShieldLook, Is.SameAs(AssetDatabase.LoadAssetAtPath<Material>(EffectSync.PlasmaMaterialPath)));
            Assert.That(theme.ShieldLook!.shader.isSupported, Is.True, "the plasma shader compiles");
        }

        [Test]
        public void Every_effect_that_plays_once_is_over_before_the_game_removes_it()
        {
            foreach ((string name, string? feedback, float seconds) in Lifetimes)
            {
                GameObject prefab = Prefab(name);
                if (prefab == null)
                {
                    Assert.Ignore("No effects yet.");
                    return;
                }

                float kept = feedback != null ? Seconds("Assets/_Vortex/Presentation/Feedback/" + feedback + ".asset") : seconds;
                foreach (ParticleSystem system in prefab.GetComponentsInChildren<ParticleSystem>())
                {
                    ParticleSystem.MainModule main = system.main;
                    Assert.That(main.loop, Is.False, name + "/" + system.name + " plays once");
                    ParticleSystem.EmissionModule emission = system.emission;
                    float lastBurst = Enumerable.Range(0, emission.burstCount).Select(i => emission.GetBurst(i).time).DefaultIfEmpty(0f).Max();
                    float end = main.startDelay.constantMax + lastBurst + main.startLifetime.constantMax;
                    Assert.That(end, Is.LessThanOrEqualTo(kept + 0.001f), name + "/" + system.name + ": over in " + end + " s, removed after " + kept + " s");
                }
            }
        }

        [Test]
        public void In_the_editor_an_effect_is_removed_after_its_time_without_errors()
        {
            // Unity refuses Destroy outside play; the game's scenes run in the editor in tests and captures.
            var effect = new GameObject("Effet");
            Assert.That(TimedRemoval.After(effect, 1f), Is.SameAs(effect));
            TimedRemoval timer = effect.GetComponent<TimedRemoval>();
            Assert.That(timer.Tick(0.6f), Is.True);
            Assert.That(effect == null, Is.False, "still there");
            Assert.That(timer.Tick(0.6f), Is.False);
            Assert.That(effect == null, Is.True, "gone");
        }

        private static GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(EffectSync.PrefabPath(name));

        private static Object? Field(string asset, string field) =>
            new SerializedObject(AssetDatabase.LoadMainAssetAtPath(asset)).FindProperty(field).objectReferenceValue;

        private static float Seconds(string asset) =>
            new SerializedObject(AssetDatabase.LoadMainAssetAtPath(asset)).FindProperty("seconds").floatValue;
    }
}
