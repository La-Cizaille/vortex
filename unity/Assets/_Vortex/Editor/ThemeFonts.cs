using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Vortex.Client.Theme;

namespace Vortex.Editor
{
    /// <summary>
    /// The fonts of the art direction (docs/DIRECTION_ARTISTIQUE.md 6.2, ARB-95): TextMeshPro font assets built from the
    /// OFL fonts in <c>Art/Fonts/</c> (sources and licences in <c>art-src/LICENCES.md</c>), given to the theme, and the
    /// interface font made TextMeshPro's default. Each asset is built once, never overwritten: delete it to rebuild it.
    /// The atlases are static and hold the characters of French text: the build carries pictures of the glyphs, not the
    /// font files, so no font is parsed on the player's device (docs/SECURITY.md). A missing glyph falls back to the
    /// default font of TextMeshPro.
    /// </summary>
    public static class ThemeFonts
    {
        /// <summary>Folder of the font assets.</summary>
        public const string Folder = "Assets/_Vortex/Theme/Fonts";

        /// <summary>
        /// Every character the atlases hold: printable ASCII, Latin-1 (French accents, « », non-breaking space), and the
        /// typography of the texts (œ, Œ, Ÿ, curly quotes, dashes, ellipsis, bullet, ×, arrows, narrow no-break space).
        /// </summary>
        public static readonly string Characters = BuildCharacters();

        private const string FontsFolder = "Assets/_Vortex/Art/Fonts";
        private const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        /// <summary>The font assets: name, source file, and the size the glyphs are sampled at.</summary>
        private static readonly (string Name, string Source, int Size)[] Fonts =
        {
            ("Anton", "Anton/Anton-Regular.ttf", 72),
            ("Pochoir", "BigShouldersStencil/BigShouldersStencilDisplay-Black.ttf", 72),
            ("Grave", "IMFellEnglish/IMFeENrm28P.ttf", 56),
            ("Grave Italique", "IMFellEnglish/IMFeENit28P.ttf", 56),
            ("Barlow", "Barlow/Barlow-Regular.ttf", 56),
            ("Barlow Gras", "Barlow/Barlow-SemiBold.ttf", 56),
            ("Barlow Italique", "Barlow/Barlow-Italic.ttf", 56),
            ("Barlow Condensed", "BarlowCondensed/BarlowCondensed-SemiBold.ttf", 56),
            ("Barlow Condensed Gras", "BarlowCondensed/BarlowCondensed-Bold.ttf", 56),
            ("Terminal", "ShareTechMono/ShareTechMono-Regular.ttf", 56),
        };

        /// <summary>
        /// The terminal font with a soft green halo (a blurred underlay, which the mobile text shader supports): the glow
        /// of the radar screens (ARB-102). Built once from the terminal font's material; delete it to rebuild it.
        /// </summary>
        public const string TerminalGlowPath = Folder + "/Terminal Lueur.mat";

        /// <summary>Path of the font asset named <paramref name="name"/>.</summary>
        public static string PathOf(string name) => Folder + "/" + name + " SDF.asset";

        /// <summary>Builds the missing font assets, links bold and italic faces, and fills the theme where it has none.</summary>
        public static void Ensure()
        {
            var made = new Dictionary<string, TMP_FontAsset>();
            foreach ((string name, string source, int size) in Fonts)
            {
                TMP_FontAsset? asset = EnsureFont(name, FontsFolder + "/" + source, size);
                if (asset != null)
                {
                    made[name] = asset;
                }
            }

            if (made.Count != Fonts.Length)
            {
                return;
            }

            // Bold and italic take the real faces instead of TextMeshPro's slanted or thickened regular.
            Pair(made["Barlow"], bold: made["Barlow Gras"], italic: made["Barlow Italique"]);
            Pair(made["Barlow Condensed"], bold: made["Barlow Condensed Gras"], italic: null);
            Pair(made["Grave"], bold: null, italic: made["Grave Italique"]);

            ThemeSettings theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            if (theme != null && theme.AssignFontsIfMissing(made["Anton"], made["Barlow"], made["Barlow Condensed"], made["Pochoir"], made["Grave"], made["Terminal"]))
            {
                EditorUtility.SetDirty(theme);
                AssetDatabase.SaveAssetIfDirty(theme);
            }

            MakeDefault(made["Barlow Condensed"]);
            EnsureTerminalGlow(made["Terminal"]);
        }

        private static void EnsureTerminalGlow(TMP_FontAsset terminal)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(TerminalGlowPath) != null)
            {
                return;
            }

            var glow = new Material(terminal.material) { name = "Terminal Lueur" };
            glow.EnableKeyword("UNDERLAY_ON");
            glow.SetColor("_UnderlayColor", new Color(0.3f, 1f, 0.48f, 0.55f));
            glow.SetFloat("_UnderlayOffsetX", 0f);
            glow.SetFloat("_UnderlayOffsetY", 0f);
            glow.SetFloat("_UnderlayDilate", 0.25f);
            glow.SetFloat("_UnderlaySoftness", 0.75f);
            ProjectAssets.Create(glow, TerminalGlowPath);
        }

        private static TMP_FontAsset? EnsureFont(string name, string sourcePath, int size)
        {
            string path = PathOf(name);
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null)
            {
                return existing;
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                Debug.LogWarning("Font missing, its asset waits: " + sourcePath);
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, size, size / 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = name + " SDF";
            if (!asset.TryAddCharacters(Characters, out string missing) && missing.Trim().Length > 0)
            {
                Debug.Log(name + ": " + missing.Length + " characters not in the font, left to the fallback: " + missing);
            }

            // Frozen: what the atlas holds is all the build carries.
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
            if (fallback != null)
            {
                asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            }

            ProjectAssets.Create(asset, path);
            for (int i = 0; i < asset.atlasTextures.Length; i++)
            {
                Texture2D atlas = asset.atlasTextures[i];
                atlas.name = name + " Atlas " + i;
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }

            asset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        // Weight 700 is TextMeshPro's bold; the italic of the regular weight (400) is its italic. The table is the asset's
        // own array: its entries are changed in place.
        private static void Pair(TMP_FontAsset regular, TMP_FontAsset? bold, TMP_FontAsset? italic)
        {
            TMP_FontWeightPair[] table = regular.fontWeightTable;
            if (bold != null)
            {
                table[7].regularTypeface = bold;
            }

            if (italic != null)
            {
                table[4].italicTypeface = italic;
            }

            EditorUtility.SetDirty(regular);
            AssetDatabase.SaveAssetIfDirty(regular);
        }

        // Every interface text without a font of its own takes the interface font.
        private static void MakeDefault(TMP_FontAsset font)
        {
            TMP_Settings settings = TMP_Settings.instance;
            if (settings == null)
            {
                return;
            }

            var serialized = new SerializedObject(settings);
            SerializedProperty property = serialized.FindProperty("m_defaultFontAsset");
            if (property == null || property.objectReferenceValue == font)
            {
                return;
            }

            property.objectReferenceValue = font;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(settings);
        }

        private static string BuildCharacters()
        {
            IEnumerable<int> ascii = Enumerable.Range(0x20, 0x7F - 0x20);
            IEnumerable<int> latin1 = Enumerable.Range(0xA0, 0x100 - 0xA0);
            int[] typography = { 0x0152, 0x0153, 0x0178, 0x2018, 0x2019, 0x201C, 0x201D, 0x2013, 0x2014, 0x2026, 0x2022, 0x2192, 0x2190, 0x2191, 0x2193, 0x202F, 0x20AC, 0x2264, 0x2265 };
            return new string(ascii.Concat(latin1).Concat(typography).Select(c => (char)c).ToArray());
        }
    }
}
