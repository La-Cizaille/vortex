using UnityEngine;

namespace Vortex.Client.Theme
{
    /// <summary>
    /// Sounds generated when the theme has none yet (like the placeholder art): a missing sound never blocks the game,
    /// and a sound file set in the theme replaces it without code. Audio proper comes with M5 (ARB-72).
    /// </summary>
    public static class PlaceholderSounds
    {
        private const int SampleRate = 44100;

        /// <summary>A short beep for the last seconds of a turn (a new clip each call: callers keep it).</summary>
        public static AudioClip Tick()
        {
            const float seconds = 0.08f;
            const float frequency = 880f;
            int length = (int)(SampleRate * seconds);
            var samples = new float[length];
            for (int i = 0; i < length; i++)
            {
                // A sine wave that fades out, so it ends without a click.
                float fade = 1f - ((float)i / length);
                samples[i] = 0.35f * fade * Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate);
            }

            AudioClip clip = AudioClip.Create("Tic (provisoire)", length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
