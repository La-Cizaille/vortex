using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vortex.Client.Menus
{
    /// <summary>
    /// The person's options on this device (INTERFACE.md 5): the animation speed, and under Windows the full screen and
    /// the resolution. They are kept with <see cref="PlayerPrefs"/>, which anyone can edit outside the game: every value
    /// read back is checked, and an unexpected one falls back to the default (docs/SECURITY.md).
    /// </summary>
    public sealed class UserOptions
    {
        private const string SpeedKey = "vortex.options.speed";
        private const string FullScreenKey = "vortex.options.full-screen";
        private const string WidthKey = "vortex.options.width";
        private const string HeightKey = "vortex.options.height";
        private const int MinSide = 640;
        private const int MaxSide = 16384;

        private static readonly float[] AllowedSpeeds = { 1f, 2f, 4f };

        private UserOptions(float speed, bool fullScreen, int width, int height)
        {
            Speed = speed;
            FullScreen = fullScreen;
            Width = width;
            Height = height;
        }

        /// <summary>Animation speeds offered, slowest first.</summary>
        public static IReadOnlyList<float> Speeds => AllowedSpeeds;

        /// <summary>True where the screen can be set from the options (Windows, and the editor to see the menu).</summary>
        public static bool ScreenOptionsAvailable =>
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;

        /// <summary>Default animation speed.</summary>
        public float Speed { get; set; }

        /// <summary>Full screen (Windows).</summary>
        public bool FullScreen { get; set; }

        /// <summary>Screen width in pixels (Windows); 0: the current one.</summary>
        public int Width { get; set; }

        /// <summary>Screen height in pixels (Windows); 0: the current one.</summary>
        public int Height { get; set; }

        /// <summary>Reads the options kept on this device, with <paramref name="defaultSpeed"/> when none is kept or it is not offered.</summary>
        public static UserOptions Load(float defaultSpeed)
        {
            float fallback = Array.IndexOf(AllowedSpeeds, defaultSpeed) >= 0 ? defaultSpeed : AllowedSpeeds[0];
            float speed = PlayerPrefs.GetFloat(SpeedKey, fallback);
            int width = PlayerPrefs.GetInt(WidthKey, 0);
            int height = PlayerPrefs.GetInt(HeightKey, 0);
            bool validSize = width >= MinSide && width <= MaxSide && height >= MinSide && height <= MaxSide;
            return new UserOptions(
                Array.IndexOf(AllowedSpeeds, speed) >= 0 ? speed : fallback,
                PlayerPrefs.GetInt(FullScreenKey, 1) != 0,
                validSize ? width : 0,
                validSize ? height : 0);
        }

        /// <summary>The next speed offered after <paramref name="speed"/> (back to the first after the last).</summary>
        public static float NextSpeed(float speed) => AllowedSpeeds[(Array.IndexOf(AllowedSpeeds, speed) + 1) % AllowedSpeeds.Length];

        /// <summary>Keeps the options on this device.</summary>
        public void Save()
        {
            PlayerPrefs.SetFloat(SpeedKey, Speed);
            PlayerPrefs.SetInt(FullScreenKey, FullScreen ? 1 : 0);
            PlayerPrefs.SetInt(WidthKey, Width);
            PlayerPrefs.SetInt(HeightKey, Height);
            PlayerPrefs.Save();
        }

        /// <summary>Applies the screen options (Windows player only; elsewhere the screen is left as it is).</summary>
        public void ApplyScreen()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer)
            {
                return;
            }

            int width = Width > 0 ? Width : Screen.currentResolution.width;
            int height = Height > 0 ? Height : Screen.currentResolution.height;
            Screen.SetResolution(width, height, FullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
    }
}
