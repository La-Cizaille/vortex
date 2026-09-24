using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// Generated stand-in illustrations. Each id gets its own pattern (stripes, rings or dots, with a frequency and an
    /// angle derived from the id), drawn in white and grey so that a view can tint it with the card's technology
    /// colour. Cards stay recognisable at a glance until their real art is dropped in.
    /// </summary>
    public static class PlaceholderArt
    {
        /// <summary>Width of a placeholder, in pixels.</summary>
        public const int Width = 128;

        /// <summary>Height of a placeholder, in pixels.</summary>
        public const int Height = 96;

        /// <summary>The pixels of an id's placeholder, row by row from the bottom: white and grey only.</summary>
        public static Color32[] Pattern(string id)
        {
            uint hash = Fnv1a(id);
            int pattern = (int)(hash % 3);
            float frequency = 3 + ((hash >> 2) % 6);
            float angle = ((hash >> 5) % 180) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float centreX = Width * (0.25f + (((hash >> 13) % 50) / 100f));
            float centreY = Height * (0.25f + (((hash >> 19) % 50) / 100f));

            var pixels = new Color32[Width * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float u = (float)x / Width;
                    float v = (float)y / Width;
                    float wave = pattern switch
                    {
                        0 => Mathf.Sin(((u * cos) + (v * sin)) * frequency * 2f * Mathf.PI),
                        1 => Mathf.Sin(Vector2.Distance(new Vector2(x, y), new Vector2(centreX, centreY)) / Width * frequency * 2f * Mathf.PI),
                        _ => Mathf.Sin(u * frequency * 2f * Mathf.PI) * Mathf.Sin(v * frequency * 2f * Mathf.PI),
                    };
                    byte level = wave > 0f ? (byte)255 : (byte)165;
                    pixels[(y * Width) + x] = new Color32(level, level, level, 255);
                }
            }

            return pixels;
        }

        /// <summary>Creates the placeholder of an id. The caller owns it and releases it with <see cref="Release"/>.</summary>
        public static Sprite Create(string id)
        {
            var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
            {
                name = "Placeholder " + id,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(Pattern(id));
            texture.Apply(false, true);

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        /// <summary>Destroys a placeholder and its texture.</summary>
        public static void Release(Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture2D texture = sprite.texture;
            Discard(sprite);
            Discard(texture);
        }

        private static void Discard(Object target)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        // 32-bit FNV-1a: a stable hash, unlike string.GetHashCode which may change between runs.
        private static uint Fnv1a(string id)
        {
            uint hash = 2166136261;
            foreach (char c in id)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash;
        }
    }
}
