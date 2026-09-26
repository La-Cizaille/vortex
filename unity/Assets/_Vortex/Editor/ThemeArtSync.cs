using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Vortex.Client.Theme;

namespace Vortex.Editor
{
    /// <summary>
    /// Brings the Blender models of the table into the theme once they are exported (lot B3 of the Blender workshop):
    /// the d8 (tools/blender/build_die.py) becomes the theme's die, and the sky behind the table
    /// (tools/blender/build_background.py) becomes a prefab with unlit materials, the theme's table background. A theme
    /// field already set is left alone: once assigned, it belongs to the designer. Runs when those art folders change
    /// and with <see cref="ThemeAssets.EnsureAll"/>.
    /// </summary>
    public static class ThemeArtSync
    {
        /// <summary>Folder of the dice models.</summary>
        public const string DiceFolder = "Assets/_Vortex/Art/Dice";

        /// <summary>Folder of the sky behind the table.</summary>
        public const string BackgroundsFolder = "Assets/_Vortex/Art/Backgrounds";

        /// <summary>The d8 exported from Blender.</summary>
        public const string DieModelPath = DiceFolder + "/D8.fbx";

        /// <summary>The sky behind the table exported from Blender: a backdrop (Fond) and the vortex (Vortex).</summary>
        public const string BackgroundModelPath = BackgroundsFolder + "/Fond.fbx";

        /// <summary>The prefab the theme places behind the table.</summary>
        public const string BackgroundPrefabPath = "Assets/_Vortex/Prefabs/TableBackground.prefab";

        /// <summary>Unlit material of the backdrop.</summary>
        public const string BackdropMaterialPath = "Assets/_Vortex/Theme/Materials/Fond.mat";

        /// <summary>Unlit, see-through material of the vortex.</summary>
        public const string VortexMaterialPath = "Assets/_Vortex/Theme/Materials/FondVortex.mat";

        /// <summary>Name of the vortex in the background model, which the game may turn and scale.</summary>
        public const string VortexPart = "Vortex";

        private const string BackdropPart = "Fond";

        /// <summary>Gives the theme the die and the table background when they exist and the theme has none.</summary>
        public static void Sync()
        {
            ThemeSettings theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            if (theme == null)
            {
                return;
            }

            GameObject die = AssetDatabase.LoadAssetAtPath<GameObject>(DieModelPath);
            bool changed = die != null && AssignIfEmpty(theme, "dieModel", die);
            GameObject? background = EnsureBackgroundPrefab();
            changed |= background != null && AssignIfEmpty(theme, "tableBackground", background);
            if (changed)
            {
                AssetDatabase.SaveAssetIfDirty(theme);
            }
        }

        private static bool AssignIfEmpty(ThemeSettings theme, string field, Object value)
        {
            var serialized = new SerializedObject(theme);
            SerializedProperty property = serialized.FindProperty(field);
            if (property.objectReferenceValue != null)
            {
                return false;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // The model with its unlit materials, saved once as a prefab; afterwards the prefab belongs to the designer.
        private static GameObject? EnsureBackgroundPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundModelPath);
            Texture2D backdropTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundsFolder + "/Fond.fbm/Fond_BaseColor.png");
            Texture2D vortexTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundsFolder + "/Fond.fbm/Vortex_BaseColor.png");
            if (model == null || backdropTexture == null || vortexTexture == null)
            {
                return null;
            }

            Material backdrop = EnsureMaterial(BackdropMaterialPath, backdropTexture, seeThrough: false);
            Material vortex = EnsureMaterial(VortexMaterialPath, vortexTexture, seeThrough: true);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    renderer.sharedMaterial = renderer.name == VortexPart ? vortex : backdrop;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    renderer.lightProbeUsage = LightProbeUsage.Off;
                    renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                }

                return PrefabUtility.SaveAsPrefabAsset(instance, BackgroundPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // Unlit: the sky keeps its colours whatever the lights of the table. The vortex is blended over the backdrop by
        // its alpha, so its black core hides the stars behind it.
        private static Material EnsureMaterial(string path, Texture2D texture, bool seeThrough)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            if (seeThrough)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            ProjectAssets.Create(material, path);
            return material;
        }
    }
}
