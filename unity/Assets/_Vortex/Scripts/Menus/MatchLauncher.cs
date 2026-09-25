using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vortex.Client.Session;

namespace Vortex.Client.Menus
{
    /// <summary>
    /// Carries the game chosen in the menu to the game scene (ADR-0019): an object kept across the scene change, found by
    /// the game scene when it starts, and dropped when going back to the menu. Opening the game scene directly (editor)
    /// finds none, and the scene plays its own test game.
    /// </summary>
    public sealed class MatchLauncher : MonoBehaviour
    {
        /// <summary>The game to play.</summary>
        public MatchSetup? Setup { get; private set; }

        /// <summary>Keeps the setup across the scene change, then opens the game scene.</summary>
        public static void Launch(MatchSetup setup)
        {
            if (setup is null)
            {
                throw new ArgumentNullException(nameof(setup));
            }

            MatchLauncher? launcher = Find();
            if (launcher == null)
            {
                launcher = new GameObject("Lancement de partie").AddComponent<MatchLauncher>();
                DontDestroyOnLoad(launcher.gameObject);
            }

            launcher.Setup = setup;
            SceneManager.LoadScene(SceneNames.Game);
        }

        /// <summary>The launcher of the game being played, or null when the game scene was opened directly.</summary>
        public static MatchLauncher? Find() => FindAnyObjectByType<MatchLauncher>();

        /// <summary>Leaves the game for the home menu.</summary>
        public static void BackToMenu()
        {
            MatchLauncher? launcher = Find();
            if (launcher != null)
            {
                Destroy(launcher.gameObject);
            }

            SceneManager.LoadScene(SceneNames.Menu);
        }
    }
}
