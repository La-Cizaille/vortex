using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vortex.Editor
{
    /// <summary>
    /// The interface's generated surfaces (docs/DIRECTION_ARTISTIQUE.md 6.4, lot 2): a soft rounded panel for what
    /// is on board, a rounded outline for the seat whose turn it is, a terminal screen for the figures, hazard stripes for alerts. They are placeholders in the
    /// sense of the project: a designer's image with the same name replaces them. Each is drawn once, never overwritten:
    /// delete one to redraw it.
    /// </summary>
    public static class ThemeSprites
    {
        /// <summary>Folder of the generated interface sprites.</summary>
        public const string Folder = "Assets/_Vortex/Theme/Sprites";

        /// <summary>Riveted plate, nine-sliced.</summary>
        public const string PlatePath = Folder + "/Plaque.png";

        /// <summary>Terminal screen, nine-sliced.</summary>
        public const string ScreenPath = Folder + "/Ecran.png";

        /// <summary>Rounded outline, nine-sliced: the frame of the seat whose turn it is.</summary>
        public const string OutlinePath = Folder + "/Contour.png";

        /// <summary>Yellowed poster paper with torn edges, nine-sliced: the menus (lot 4).</summary>
        public const string PaperPath = Folder + "/Affiche.png";

        /// <summary>Hazard stripes, tiled.</summary>
        public const string HazardPath = Folder + "/Danger.png";

        private const int PlateSize = 128;
        private const int PlateBorder = 24;
        private const float Radius = 16f;
        private const int ScreenSize = 64;
        private const int ScreenBorder = 10;
        private const int HazardSize = 64;
        private const int PaperSize = 128;
        private const int PaperBorder = 24;

        /// <summary>The riveted plate.</summary>
        public static Sprite Plate => AssetDatabase.LoadAssetAtPath<Sprite>(PlatePath);

        /// <summary>The rounded outline.</summary>
        public static Sprite Outline => AssetDatabase.LoadAssetAtPath<Sprite>(OutlinePath);

        /// <summary>The poster paper.</summary>
        public static Sprite Paper => AssetDatabase.LoadAssetAtPath<Sprite>(PaperPath);

        /// <summary>The terminal screen.</summary>
        public static Sprite Screen => AssetDatabase.LoadAssetAtPath<Sprite>(ScreenPath);

        /// <summary>The hazard stripes.</summary>
        public static Sprite Hazard => AssetDatabase.LoadAssetAtPath<Sprite>(HazardPath);

        /// <summary>Draws the missing sprites.</summary>
        public static void Ensure()
        {
            Draw(PlatePath, DrawPlate(), PlateBorder, tiled: false);
            Draw(OutlinePath, DrawOutline(), PlateBorder, tiled: false);
            Draw(ScreenPath, DrawScreen(), ScreenBorder, tiled: false);
            Draw(HazardPath, DrawHazard(), 0, tiled: true);
            Draw(PaperPath, DrawPaper(), PaperBorder, tiled: false);
        }

        private static void Draw(string path, Texture2D texture, int border, bool tiled)
        {
            if (File.Exists(path))
            {
                Object.DestroyImmediate(texture);
                return;
            }

            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            Debug.Log("Created " + path);
        }

        // A soft panel (ARB-100): rounded corners, a faint dark fill, a hairline edge barely there. It frames without
        // weighing: the table shows through, and what matters is the text and the figures on it.
        private static Texture2D DrawPlate()
        {
            var texture = new Texture2D(PlateSize, PlateSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < PlateSize; y++)
            {
                for (int x = 0; x < PlateSize; x++)
                {
                    float d = RoundedDistance(x, y, PlateSize, Radius);
                    float inside = Mathf.Clamp01(0.5f - d);
                    float edge = Mathf.Clamp01(1f - Mathf.Abs(d + 0.5f));
                    var fill = new Color(0.055f, 0.06f, 0.07f, 0.5f * inside);
                    texture.SetPixel(x, y, Color.Lerp(fill, new Color(0.7f, 0.72f, 0.76f, 0.14f), edge * 0.8f));
                }
            }

            texture.Apply();
            return texture;
        }

        // A rounded outline, 3 px, white: the frame of the seat whose turn it is, tinted by the game.
        private static Texture2D DrawOutline()
        {
            var texture = new Texture2D(PlateSize, PlateSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < PlateSize; y++)
            {
                for (int x = 0; x < PlateSize; x++)
                {
                    float d = RoundedDistance(x, y, PlateSize, Radius);
                    float alpha = Mathf.Clamp01(1.5f - Mathf.Abs(d + 1.5f));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        // Signed distance, in pixels, from the pixel centre to the edge of a rounded square (negative inside).
        private static float RoundedDistance(int x, int y, int size, float radius)
        {
            float half = size / 2f;
            float px = Mathf.Abs(x + 0.5f - half) - (half - radius);
            float py = Mathf.Abs(y + 0.5f - half) - (half - radius);
            float outside = new Vector2(Mathf.Max(px, 0f), Mathf.Max(py, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(px, py), 0f) - radius;
        }

        // A dark green screen with rounded corners, a thin frame and scan lines.
        private static Texture2D DrawScreen()
        {
            var texture = new Texture2D(ScreenSize, ScreenSize, TextureFormat.RGBA32, false);
            var glass = new Color(0.02f, 0.035f, 0.025f, 1f);
            var line = new Color(0.012f, 0.022f, 0.016f, 1f);
            var frame = new Color(0.086f, 0.19f, 0.11f, 1f);
            for (int y = 0; y < ScreenSize; y++)
            {
                for (int x = 0; x < ScreenSize; x++)
                {
                    // Rounded like the panels (ARB-100), a thin frame, scan lines.
                    float d = RoundedDistance(x, y, ScreenSize, 7f);
                    Color pixel = d > -1.5f ? frame : y % 3 == 0 ? line : glass;
                    pixel.a *= Mathf.Clamp01(0.5f - d);
                    texture.SetPixel(x, y, pixel);
                }
            }

            texture.Apply();
            return texture;
        }

        // Yellowed poster paper (docs/DIRECTION_ARTISTIQUE.md 6.4): bone with a fine grain (no specks: the sheet stretches),
        // darker towards the edges,
        // the edges torn unevenly. White enough to be tinted; the grain stays.
        private static Texture2D DrawPaper()
        {
            var random = new System.Random(11);
            var texture = new Texture2D(PaperSize, PaperSize, TextureFormat.RGBA32, false);
            int[] tear = new int[PaperSize];
            for (int i = 0; i < PaperSize; i++)
            {
                tear[i] = random.Next(0, 5);
            }

            for (int y = 0; y < PaperSize; y++)
            {
                for (int x = 0; x < PaperSize; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x - tear[y], y - tear[x]), Mathf.Min(PaperSize - 1 - x - tear[(y * 7) % PaperSize], PaperSize - 1 - y - tear[(x * 5) % PaperSize]));
                    if (edge < 0)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float grain = (float)(random.NextDouble() - 0.5) * 0.06f;
                    float age = edge < 10 ? (10 - edge) * 0.012f : 0f;
                    float v = 0.97f + grain - age;
                    texture.SetPixel(x, y, new Color(v, v * 0.95f, v * 0.82f, 1f));
                }
            }

            texture.Apply();
            return texture;
        }

        // Yellow and black at 45 degrees, repeating on both axes.
        private static Texture2D DrawHazard()
        {
            var texture = new Texture2D(HazardSize, HazardSize, TextureFormat.RGBA32, false);
            var yellow = new Color32(217, 165, 20, 255);
            var black = new Color32(17, 17, 17, 255);
            for (int y = 0; y < HazardSize; y++)
            {
                for (int x = 0; x < HazardSize; x++)
                {
                    texture.SetPixel(x, y, ((x + y) / (HazardSize / 4)) % 2 == 0 ? yellow : black);
                }
            }

            texture.Apply();
            return texture;
        }
    }
}
