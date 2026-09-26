using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Vortex.Client.Theme;

namespace Vortex.Editor
{
    /// <summary>
    /// Builds the table's visual effects from the Blender workshop's material (lot B4, tools/blender/build_effects.py:
    /// meshes and flipbooks in <c>Art/Effects/</c>) and gives them to the game (docs/ANIMATIONS.md section 6): particle
    /// prefabs for the shot, the impact, the explosion, the afterburner, the spores, the smoke and the overcharge arcs, and
    /// the shield's plasma material. What an effect looks like lives in its prefab and materials, never in the game's
    /// code (ADR-0014): each is made once, then belongs to the designer, and a feedback or theme field already set is
    /// left alone. Colours follow the art direction (docs/DIRECTION_ARTISTIQUE.md §6.1, §6.7): weight, grit, the dark
    /// palette, light only where something burns. Runs when the effects folder changes and with
    /// <see cref="ProjectAssets.EnsureAll"/>, once the feedbacks exist.
    /// </summary>
    public static class EffectSync
    {
        /// <summary>Folder of the effects' meshes and images.</summary>
        public const string EffectsFolder = "Assets/_Vortex/Art/Effects";

        /// <summary>The effects' meshes, exported from Blender, with their images next to them.</summary>
        public const string ModelPath = EffectsFolder + "/Effets.fbx";

        /// <summary>Folder of the effect prefabs.</summary>
        public const string PrefabFolder = "Assets/_Vortex/Prefabs/Effects";

        /// <summary>The shield's plasma material.</summary>
        public const string PlasmaMaterialPath = "Assets/_Vortex/Theme/Materials/Plasma.mat";

        /// <summary>Every effect prefab, by name.</summary>
        public static readonly string[] Prefabs = { "TirLaser", "Impact", "Explosion", "Postcombustion", "Spores", "Fumee", "Arcs" };

        private const string FeedbackFolder = "Assets/_Vortex/Presentation/Feedback/";
        private const string MaterialFolder = "Assets/_Vortex/Theme/Materials/";

        // Where each prefab goes: the asset and its field.
        private static readonly (string Prefab, string Asset, string Field)[] Uses =
        {
            ("TirLaser", FeedbackFolder + "Laser.asset", "prefab"),
            ("Impact", FeedbackFolder + "Knockback.asset", "sparks"),
            ("Explosion", FeedbackFolder + "Explosion.asset", "prefab"),
            ("Postcombustion", FeedbackFolder + "Thrusters.asset", "prefab"),
            ("Spores", FeedbackFolder + "Torment.asset", "prefab"),
            ("Fumee", ThemeAssets.ThemePath, "smokePrefab"),
            ("Arcs", ThemeAssets.ThemePath, "arcPrefab"),
        };

        private enum Blend
        {
            Alpha,
            Additive,
        }

        /// <summary>Builds what is missing and gives it to the empty fields.</summary>
        public static void Sync()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
            {
                return;
            }

            var kit = new Kit();
            if (!kit.Complete)
            {
                return;
            }

            EnsureFolder(PrefabFolder);
            var built = new Dictionary<string, GameObject>
            {
                ["TirLaser"] = Ensure("TirLaser", kit.Laser),
                ["Impact"] = Ensure("Impact", kit.Impact),
                ["Explosion"] = Ensure("Explosion", kit.Explosion),
                ["Postcombustion"] = Ensure("Postcombustion", kit.Afterburner),
                ["Spores"] = Ensure("Spores", kit.Spores),
                ["Fumee"] = Ensure("Fumee", kit.Smoke),
                ["Arcs"] = Ensure("Arcs", kit.Arcs),
            };

            foreach ((string prefab, string asset, string field) in Uses)
            {
                AssignIfEmpty(asset, field, built[prefab]);
            }

            AssignIfEmpty(ThemeAssets.ThemePath, "shieldMaterial", kit.Plasma);
        }

        private static void EnsureFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder)!.Replace('\\', '/'), System.IO.Path.GetFileName(folder));
            }
        }

        /// <summary>Path of an effect prefab.</summary>
        public static string PrefabPath(string name) => PrefabFolder + "/" + name + ".prefab";

        private static GameObject Ensure(string name, System.Action<Transform> build)
        {
            string path = PrefabPath(name);
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(name);
            try
            {
                build(root.transform);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void AssignIfEmpty(string assetPath, string field, Object value)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
            {
                return;
            }

            var serialized = new SerializedObject(asset);
            SerializedProperty? property = serialized.FindProperty(field);
            if (property == null || property.objectReferenceValue != null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        // ---------------------------------------------------------------------------------------------- materials

        // A particle's own colour is stored in 8 bits and stops at 1: what makes an effect shine past the Bloom's
        // threshold (1.5) is its material's colour, `glow` times white; the particles give the hue and fade.
        private static Material ParticleMaterial(string name, string texture, Blend blend, float glow = 1f)
        {
            string path = MaterialFolder + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap", Texture(texture));
            material.SetColor("_BaseColor", new Color(glow, glow, glow, 1f));
            SeeThrough(material, blend);
            ProjectAssets.Create(material, path);
            return material;
        }

        // Unlit meshes that shine (the bolt): additive, tinted by their own colour, bright enough for the Bloom.
        private static Material GlowMaterial(string name, Color color)
        {
            string path = MaterialFolder + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            SeeThrough(material, Blend.Additive);
            ProjectAssets.Create(material, path);
            return material;
        }

        // Debris: dark torn plating that catches the table's lights.
        private static Material DebrisMaterial()
        {
            string path = MaterialFolder + "FxEclat.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Simple Lit"));
            material.SetColor("_BaseColor", new Color(0.09f, 0.085f, 0.08f));
            ProjectAssets.Create(material, path);
            return material;
        }

        private static Material PlasmaMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(PlasmaMaterialPath);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Vortex/Plasma"));
            material.SetTexture("_BaseMap", Texture("Plasma"));
            material.SetTextureScale("_BaseMap", new Vector2(3f, 2f));
            material.renderQueue = (int)RenderQueue.Transparent;
            ProjectAssets.Create(material, PlasmaMaterialPath);
            return material;
        }

        private static void SeeThrough(Material material, Blend blend)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", blend == Blend.Additive ? 2f : 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)(blend == Blend.Additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(EffectsFolder + "/Effets.fbm/" + name + ".png");

        // ------------------------------------------------------------------------------------------------ effects

        // The materials and meshes every effect draws from. Particle colours are picked in sRGB and multiply the
        // textures in linear light: a smoke must be mid grey to read as dark smoke on the dark sky (ARB-98).
        private sealed class Kit
        {
            private readonly Material _fire;
            private readonly Material _flame;
            private readonly Material _smoke;
            private readonly Material _spark;
            private readonly Material _arc;
            private readonly Material _spore;
            private readonly Material _ring;
            private readonly Material _debris;
            private readonly Material _core;
            private readonly Material _sheath;
            private readonly Dictionary<string, Mesh> _meshes;

            public Kit()
            {
                _meshes = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Mesh>().GroupBy(m => m.name).ToDictionary(g => g.Key, g => g.First());
                Complete = new[] { "Feu", "Fumee", "Etincelle", "Arc", "Spore", "Onde", "Plasma" }.All(name => Texture(name) != null)
                    && new[] { "Trait", "Gaine", "Onde", "Eclat_1" }.All(_meshes.ContainsKey);
                if (!Complete)
                {
                    _fire = _flame = _smoke = _spark = _arc = _spore = _ring = _debris = _core = _sheath = null!;
                    Plasma = null!;
                    return;
                }

                _fire = ParticleMaterial("FxFeu", "Feu", Blend.Alpha, 2.2f);
                _flame = ParticleMaterial("FxFlamme", "Feu", Blend.Additive, 3.5f);
                _smoke = ParticleMaterial("FxFumee", "Fumee", Blend.Alpha);
                _spark = ParticleMaterial("FxEtincelle", "Etincelle", Blend.Additive, 4f);
                _arc = ParticleMaterial("FxArc", "Arc", Blend.Additive, 3.5f);
                _spore = ParticleMaterial("FxSpore", "Spore", Blend.Alpha, 1.6f);
                _ring = ParticleMaterial("FxOnde", "Onde", Blend.Additive, 2.5f);
                _debris = DebrisMaterial();
                _core = GlowMaterial("FxTrait", new Color(4f, 1.9f, 0.5f));
                _sheath = GlowMaterial("FxGaine", new Color(0.5f, 0.17f, 0.03f));
                Plasma = PlasmaMaterial();
            }

            public bool Complete { get; }

            public Material Plasma { get; }

            private Mesh[] Shards => _meshes.Where(pair => pair.Key.StartsWith("Eclat_", System.StringComparison.Ordinal)).OrderBy(pair => pair.Key, System.StringComparer.Ordinal).Select(pair => pair.Value).ToArray();

            // The shot: a hot bolt in its sheath, leaving a dirty trail of smoke and embers (§6.7: brief, dense, amber).
            public void Laser(Transform root)
            {
                MeshPart(root, "Coeur", _meshes["Trait"], _core);
                MeshPart(root, "Gaine", _meshes["Gaine"], _sheath);

                ParticleSystem trail = Particles(root, "Trainee", _smoke, 0.5f, loop: true, world: true);
                Life(trail, 0.25f, 0.4f, 0f, 0f, 0.12f, 0.2f, new Color(0.55f, 0.52f, 0.5f, 0.5f));
                Rate(trail, 0f, 60f);
                Grow(trail, 1f, 2.2f);
                Fade(trail);
                Frames(trail, randomFrame: true);

                ParticleSystem embers = Particles(root, "Escarbilles", _spark, 0.5f, loop: true, world: true);
                Life(embers, 0.15f, 0.3f, 0.3f, 1f, 0.03f, 0.05f, new Color(1f, 0.45f, 0.1f));
                Rate(embers, 0f, 25f);
                Sphere(embers, 0.03f);
                Heat(embers);
            }

            // A hit on the hull (+Z out of the hull; the game scales it with the blow): a flash, a spray of sparks, torn
            // plating, a jet of escaping air, then black smoke.
            public void Impact(Transform root)
            {
                ParticleSystem flash = Particles(root, "Eclair", _spark, 0.2f);
                Life(flash, 0.1f, 0.1f, 0f, 0f, 0.9f, 0.9f, new Color(1f, 0.62f, 0.3f));
                Burst(flash, 0f, 1);
                Fade(flash);

                ParticleSystem sparks = Particles(root, "Etincelles", _spark, 0.3f);
                Life(sparks, 0.25f, 0.6f, 4f, 9f, 0.03f, 0.06f, Color.white);
                Burst(sparks, 0f, 30);
                Cone(sparks, 50f, 0.05f);
                Stretch(sparks, 0.06f);
                Drag(sparks, 0.25f);
                Heat(sparks);

                ParticleSystem shards = Particles(root, "Eclats", _debris, 0.3f);
                Life(shards, 0.8f, 1.3f, 1.5f, 4f, 0.05f, 0.12f, Color.white);
                Burst(shards, 0f, 7);
                Cone(shards, 60f, 0.05f);
                Tumble(shards);
                Meshes(shards, Shards);

                ParticleSystem vapour = Particles(root, "Vapeur", _smoke, 0.3f);
                Life(vapour, 0.3f, 0.5f, 3f, 5f, 0.15f, 0.25f, new Color(0.85f, 0.87f, 0.9f, 0.4f));
                Burst(vapour, 0f, 6);
                Cone(vapour, 15f, 0.02f);
                Grow(vapour, 1f, 2.5f);
                Fade(vapour);
                Frames(vapour, randomFrame: true);

                ParticleSystem smoke = Particles(root, "Fumee", _smoke, 0.3f);
                Life(smoke, 0.9f, 1.3f, 0.3f, 0.8f, 0.3f, 0.45f, new Color(0.5f, 0.48f, 0.46f, 0.8f));
                Burst(smoke, 0.05f, 4);
                Cone(smoke, 40f, 0.1f);
                Grow(smoke, 1f, 2f);
                Fade(smoke);
                Frames(smoke, randomFrame: false);
            }

            // A ship blown apart (1.2 s): a flash, a fireball that burns out into soot, a shockwave on the table's plane,
            // torn plating and embers flung out, black smoke.
            public void Explosion(Transform root)
            {
                ParticleSystem flash = Particles(root, "Eclair", _spark, 0.3f);
                Life(flash, 0.18f, 0.18f, 0f, 0f, 3.2f, 3.2f, new Color(1f, 0.7f, 0.4f));
                Burst(flash, 0f, 1);
                Fade(flash);

                ParticleSystem fire = Particles(root, "Boule de feu", _fire, 0.3f);
                Life(fire, 0.7f, 1f, 0.5f, 1.8f, 1f, 1.8f, Color.white);
                Burst(fire, 0f, 8);
                Sphere(fire, 0.35f);
                Grow(fire, 0.5f, 1.3f);
                Spin(fire);
                Frames(fire, randomFrame: false);

                ParticleSystem wave = Particles(root, "Onde", _ring, 0.3f);
                Life(wave, 0.55f, 0.55f, 0f, 0f, 0.6f, 0.6f, new Color(1f, 0.48f, 0.12f));
                Burst(wave, 0f, 1);
                Grow(wave, 1f, 5f);
                Fade(wave);
                Meshes(wave, new[] { _meshes["Onde"] });
                wave.GetComponent<ParticleSystemRenderer>().alignment = ParticleSystemRenderSpace.Local;

                ParticleSystem debris = Particles(root, "Debris", _debris, 0.3f);
                Life(debris, 0.9f, 1.1f, 3f, 7f, 0.08f, 0.22f, Color.white);
                Burst(debris, 0f, 14);
                Sphere(debris, 0.3f);
                Tumble(debris);
                Meshes(debris, Shards);

                ParticleSystem embers = Particles(root, "Braises", _spark, 0.3f);
                Life(embers, 0.4f, 1f, 3f, 8f, 0.03f, 0.06f, Color.white);
                Burst(embers, 0f, 30);
                Sphere(embers, 0.3f);
                Stretch(embers, 0.05f);
                Drag(embers, 0.2f);
                Heat(embers);

                ParticleSystem smoke = Particles(root, "Fumee noire", _smoke, 0.3f);
                Life(smoke, 0.8f, 1f, 0.3f, 1f, 1.2f, 1.8f, new Color(0.42f, 0.4f, 0.38f, 0.85f));
                Burst(smoke, 0.1f, 7);
                Sphere(smoke, 0.5f);
                Grow(smoke, 0.7f, 1.5f);
                Fade(smoke);
                Frames(smoke, randomFrame: false);
            }

            // An engine at full power, on the ship (+Z backwards): a white-hot core, a flame that cools to red, a little
            // smoke left behind.
            public void Afterburner(Transform root)
            {
                ParticleSystem core = Particles(root, "Coeur", _spark, 1f, loop: true);
                Life(core, 0.08f, 0.12f, 2f, 3f, 0.22f, 0.22f, new Color(1f, 0.8f, 0.6f));
                Rate(core, 30f, 0f);
                Cone(core, 4f, 0.03f);
                Fade(core);

                ParticleSystem flame = Particles(root, "Flamme", _flame, 1f, loop: true);
                Life(flame, 0.2f, 0.28f, 4.5f, 6.5f, 0.36f, 0.36f, Color.white);
                Rate(flame, 55f, 0f);
                Cone(flame, 6f, 0.06f);
                Grow(flame, 1f, 0.15f);
                Spin(flame);
                Frames(flame, randomFrame: true, frames: 6);
                ColorOverLife(flame, new[] { (new Color(1f, 0.8f, 0.45f), 0f), (new Color(1f, 0.38f, 0.07f), 0.4f), (new Color(0.2f, 0.03f, 0.01f), 1f) }, new[] { (1f, 0f), (0.8f, 0.5f), (0f, 1f) });

                ParticleSystem smoke = Particles(root, "Fumee", _smoke, 1f, loop: true, world: true);
                Life(smoke, 0.5f, 0.7f, 1f, 2f, 0.2f, 0.3f, new Color(0.5f, 0.48f, 0.46f, 0.3f));
                Rate(smoke, 10f, 0f);
                Cone(smoke, 10f, 0.05f);
                Grow(smoke, 1f, 3f);
                Fade(smoke);
                Frames(smoke, randomFrame: true);
            }

            // The Torment takes hold (1 s, on the hull): sickly spores drift out of a greenish haze (§6.7: horror
            // suggested, not shown).
            public void Spores(Transform root)
            {
                ParticleSystem haze = Particles(root, "Brume", _smoke, 0.3f);
                Life(haze, 0.8f, 1f, 0.05f, 0.15f, 0.6f, 0.8f, new Color(0.5f, 0.8f, 0.25f, 0.35f));
                Burst(haze, 0f, 4);
                Sphere(haze, 0.3f);
                Grow(haze, 1f, 1.6f);
                Fade(haze);
                Frames(haze, randomFrame: true);

                ParticleSystem spores = Particles(root, "Spores", _spore, 0.3f);
                Life(spores, 0.7f, 1f, 0.2f, 0.8f, 0.1f, 0.22f, new Color(0.56f, 1f, 0.25f));
                Burst(spores, 0f, 24);
                Sphere(spores, 0.35f);
                Spin(spores);
                Fade(spores);
                Frames(spores, randomFrame: true, columns: 2, rows: 2);
                ParticleSystem.NoiseModule noise = spores.noise;
                noise.enabled = true;
                noise.strength = 0.4f;
                noise.frequency = 1.5f;
            }

            // A damaged hull smokes (3 s, one puff after another): thick black smoke rising, now and then an ember.
            public void Smoke(Transform root)
            {
                ParticleSystem plume = Particles(root, "Panache", _smoke, 0.5f, world: true);
                Life(plume, 1.6f, 2.4f, 0.3f, 0.6f, 0.45f, 0.6f, new Color(0.52f, 0.5f, 0.48f, 0.75f));
                Burst(plume, 0f, 2);
                Burst(plume, 0.15f, 1);
                Burst(plume, 0.3f, 1);
                Sphere(plume, 0.15f);
                Grow(plume, 1f, 2.6f);
                Spin(plume);
                Fade(plume);
                Frames(plume, randomFrame: false);
                ParticleSystem.VelocityOverLifetimeModule rise = plume.velocityOverLifetime;
                rise.enabled = true;
                rise.space = ParticleSystemSimulationSpace.World;
                rise.x = new ParticleSystem.MinMaxCurve(0f);
                rise.y = new ParticleSystem.MinMaxCurve(0.9f);
                rise.z = new ParticleSystem.MinMaxCurve(0f);

                ParticleSystem embers = Particles(root, "Braise", _spark, 0.5f, world: true);
                Life(embers, 0.4f, 0.8f, 0.5f, 1.2f, 0.03f, 0.03f, Color.white);
                Burst(embers, 0f, 2);
                Sphere(embers, 0.1f);
                Heat(embers);
            }

            // Overcharge (1 s, on the hull): short, violent arcs, a few at a time, and their sparks (§6.7, review: rarer
            // and more violent than the placeholder).
            public void Arcs(Transform root)
            {
                ParticleSystem arcs = Particles(root, "Arcs", _arc, 1f);
                Life(arcs, 0.07f, 0.14f, 0f, 0f, 0.5f, 1f, new Color(1f, 0.6f, 0.2f));
                Burst(arcs, 0f, 2);
                Burst(arcs, 0.25f, 1, 0.7f);
                Burst(arcs, 0.5f, 2, 0.8f);
                Burst(arcs, 0.75f, 1, 0.6f);
                Sphere(arcs, 0.25f);
                Frames(arcs, randomFrame: true);
                ParticleSystem.MainModule main = arcs.main;
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);

                ParticleSystem sparks = Particles(root, "Etincelles", _spark, 1f);
                Life(sparks, 0.15f, 0.35f, 2f, 5f, 0.02f, 0.04f, Color.white);
                Burst(sparks, 0f, 8);
                Burst(sparks, 0.5f, 6, 0.8f);
                Sphere(sparks, 0.25f);
                Stretch(sparks, 0.05f);
                Heat(sparks);
            }

            private static void MeshPart(Transform root, string name, Mesh mesh, Material material)
            {
                var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
                part.transform.SetParent(root, false);
                part.GetComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = part.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        // -------------------------------------------------------------------------------------- particle modules

        // A particle system that plays once (or loops) on its own, scaled with its parents (the game scales an impact with
        // its blow, and an effect on a ship with the ship).
        private static ParticleSystem Particles(Transform parent, string name, Material material, float duration, bool loop = false, bool world = false)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            ParticleSystem system = holder.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = system.main;
            main.duration = duration;
            main.loop = loop;
            main.playOnAwake = true;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.gravityModifier = 0f;
            main.maxParticles = 120;
            main.stopAction = ParticleSystemStopAction.None;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;
            ParticleSystemRenderer renderer = holder.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        private static void Life(ParticleSystem system, float shortest, float longest, float slowest, float fastest, float smallest, float largest, Color color)
        {
            ParticleSystem.MainModule main = system.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(shortest, longest);
            main.startSpeed = new ParticleSystem.MinMaxCurve(slowest, fastest);
            main.startSize = new ParticleSystem.MinMaxCurve(smallest, largest);
            main.startColor = color;
        }

        private static void Burst(ParticleSystem system, float time, int count, float probability = 1f)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            var burst = new ParticleSystem.Burst(time, (short)count) { probability = probability };
            emission.SetBursts(Enumerable.Range(0, emission.burstCount).Select(emission.GetBurst).Append(burst).ToArray());
        }

        private static void Rate(ParticleSystem system, float perSecond, float perUnit)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = perSecond;
            emission.rateOverDistance = perUnit;
        }

        private static void Cone(ParticleSystem system, float angle, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = angle;
            shape.radius = radius;
        }

        private static void Sphere(ParticleSystem system, float radius)
        {
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
        }

        private static void Grow(ParticleSystem system, float from, float to)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, from, 1f, to));
        }

        // Fades out over the particle's life; its start colour keeps its hue and opacity.
        private static void Fade(ParticleSystem system) =>
            ColorOverLife(system, new[] { (Color.white, 0f), (Color.white, 1f) }, new[] { (1f, 0f), (0.7f, 0.5f), (0f, 1f) });

        // Hot to cold: white-hot, amber, dark red, gone (as a share of the material's glow).
        private static void Heat(ParticleSystem system) =>
            ColorOverLife(system, new[] { (new Color(1f, 0.75f, 0.45f), 0f), (new Color(0.8f, 0.3f, 0.05f), 0.4f), (new Color(0.15f, 0.02f, 0.005f), 1f) }, new[] { (1f, 0f), (1f, 0.6f), (0f, 1f) });

        // Multiplies the start colour over the particle's life.
        private static void ColorOverLife(ParticleSystem system, (Color Color, float Time)[] colors, (float Alpha, float Time)[] alphas)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                colors.Select(c => new GradientColorKey(c.Color, c.Time)).ToArray(),
                alphas.Select(a => new GradientAlphaKey(a.Alpha, a.Time)).ToArray());
            ParticleSystem.ColorOverLifetimeModule over = system.colorOverLifetime;
            over.enabled = true;
            over.color = gradient;
        }

        private static void Stretch(ParticleSystem system, float velocityScale)
        {
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = velocityScale;
            renderer.lengthScale = 1f;
        }

        // Slows the particles down (there is no gravity in space, but debris and sparks lose their speed).
        private static void Drag(ParticleSystem system, float share)
        {
            ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100f;
            limit.drag = share * 10f;
        }

        private static void Spin(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);
        }

        private static void Tumble(ParticleSystem system)
        {
            ParticleSystem.MainModule main = system.main;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            ParticleSystem.RotationOverLifetimeModule rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = true;
            rotation.x = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rotation.y = new ParticleSystem.MinMaxCurve(-6f, 6f);
            rotation.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            Drag(system, 0.05f);
        }

        private static void Meshes(ParticleSystem system, Mesh[] meshes)
        {
            ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.SetMeshes(meshes);
            renderer.alignment = ParticleSystemRenderSpace.Local;
        }

        // A flipbook of 4 x 4 frames (or columns x rows): played over the particle's life, or one frame drawn at random
        // among the first `frames` and kept.
        private static void Frames(ParticleSystem system, bool randomFrame, int frames = 0, int columns = 4, int rows = 4)
        {
            ParticleSystem.TextureSheetAnimationModule sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = columns;
            sheet.numTilesY = rows;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            if (randomFrame)
            {
                int count = frames > 0 ? frames : columns * rows;
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
                sheet.startFrame = new ParticleSystem.MinMaxCurve(0f, count - 0.01f);
            }
            else
            {
                sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
            }
        }
    }
}
