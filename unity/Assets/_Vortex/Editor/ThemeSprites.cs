using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vortex.Editor
{
    /// <summary>
    /// The interface's generated surfaces (docs/DIRECTION_ARTISTIQUE.md 6.4, lot 2): a light riveted frame over a
    /// translucent dark fill for what is on board, a terminal screen for the figures, hazard stripes for alerts. They are placeholders in the
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

        /// <summary>Hazard stripes, tiled.</summary>
        public const string HazardPath = Folder + "/Danger.png";

        private const int PlateSize = 128;
        private const int PlateBorder = 26;
        private const int ScreenSize = 64;
        private const int ScreenBorder = 10;
        private const int HazardSize = 64;

        /// <summary>The riveted plate.</summary>
        public static Sprite Plate => AssetDatabase.LoadAssetAtPath<Sprite>(PlatePath);

        /// <summary>The terminal screen.</summary>
        public static Sprite Screen => AssetDatabase.LoadAssetAtPath<Sprite>(ScreenPath);

        /// <summary>The hazard stripes.</summary>
        public static Sprite Hazard => AssetDatabase.LoadAssetAtPath<Sprite>(HazardPath);

        /// <summary>Draws the missing sprites.</summary>
        public static void Ensure()
        {
            Draw(PlatePath, DrawPlate(), PlateBorder, tiled: false);
            Draw(ScreenPath, DrawScreen(), ScreenBorder, tiled: false);
            Draw(HazardPath, DrawHazard(), 0, tiled: true);
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

        // A light frame (playtest of lot 2: the brushed, opaque plates were heavy): a translucent dark fill that lets the
        // table show through, a thin steel rim, and a small rivet in each corner. No texture: it tints cleanly.
        private static Texture2D DrawPlate()
        {
            var texture = new Texture2D(PlateSize, PlateSize, TextureFormat.RGBA32, false);
            var fill = new Color(0.055f, 0.06f, 0.068f, 0.62f);
            var rim = new Color(0.42f, 0.44f, 0.47f, 0.95f);
            var shade = new Color(0.02f, 0.02f, 0.025f, 0.8f);
            for (int y = 0; y < PlateSize; y++)
            {
                for (int x = 0; x < PlateSize; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(PlateSize - 1 - x, PlateSize - 1 - y));
                    texture.SetPixel(x, y, edge < 2 ? rim : edge < 3 ? shade : fill);
                }
            }

            foreach (Vector2Int corner in new[] { new Vector2Int(9, 9), new Vector2Int(PlateSize - 10, 9), new Vector2Int(9, PlateSize - 10), new Vector2Int(PlateSize - 10, PlateSize - 10) })
            {
                Rivet(texture, corner);
            }

            texture.Apply();
            return texture;
        }

        private static void Rivet(Texture2D texture, Vector2Int centre)
        {
            for (int y = -5; y <= 5; y++)
            {
                for (int x = -5; x <= 5; x++)
                {
                    float d = Mathf.Sqrt((x * x) + (y * y));
                    if (d > 2.8f)
                    {
                        continue;
                    }

                    // Small domed head: lit at the top left, a dark ring around it.
                    float v = d > 2f ? 0.08f : 0.5f + (0.04f * (y - x));
                    texture.SetPixel(centre.x + x, centre.y + y, new Color(v, v, v * 1.03f, 1f));
                }
            }
        }

        // A dark green screen with a thin frame and scan lines.
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
                    int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(ScreenSize - 1 - x, ScreenSize - 1 - y));
                    texture.SetPixel(x, y, edge < 2 ? frame : y % 3 == 0 ? line : glass);
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
