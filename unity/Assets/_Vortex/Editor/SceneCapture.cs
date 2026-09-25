using System;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Gallery;
using Vortex.Client.Presentation;
using Object = UnityEngine.Object;

namespace Vortex.Editor
{
    /// <summary>
    /// Renders a scene to a PNG without the editor window (tools/Capture-Unity.ps1), to check a layout, an illustration
    /// or a model in context from batch mode. The output path comes from VORTEX_CAPTURE; for the game, the round to
    /// reach comes from VORTEX_ROUND. Development tool: editor assembly only.
    /// </summary>
    public static class SceneCapture
    {
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>
        /// Bots play the game scene until the requested round, then the table is rendered. With VORTEX_HUMAN=1, the first
        /// seat is a person's: the table is rendered on their turn, with the controls offered, after the market or, with
        /// VORTEX_PHASE=market, during it.
        /// </summary>
        public static void Game()
        {
            string output = Output();
            int round = int.TryParse(Environment.GetEnvironmentVariable("VORTEX_ROUND"), out int value) ? Math.Clamp(value, 1, 30) : 4;
            bool human = Environment.GetEnvironmentVariable("VORTEX_HUMAN") == "1";
            bool atMarket = Environment.GetEnvironmentVariable("VORTEX_PHASE") == "market";
            EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            (Camera camera, RenderTexture target) = Prepare();
            GameDirector director = Object.FindAnyObjectByType<GameDirector>();
            director.HumanFirstSeat = human;
            director.Begin();
            for (int frame = 0; frame < 50000 && director.Model!.Outcome == null; frame++)
            {
                director.Advance(0.2f);
                bool reached = director.Model.Round >= round;
                if (!human && reached && director.IsPlaying)
                {
                    break;
                }

                if (human && director.Controls.Offered)
                {
                    // The person's turn: rendered at the market or after it, once the round is reached.
                    if (reached && director.Controls.CanEndMarket == atMarket && !director.Controls.Decision.gameObject.activeSelf)
                    {
                        break;
                    }

                    if (director.Controls.Decision.gameObject.activeSelf)
                    {
                        director.Controls.Decision.Choose(0);
                    }
                    else if (director.Controls.CanEndMarket)
                    {
                        director.Controls.EndMarket();
                    }
                    else
                    {
                        director.Controls.EndTurn();
                    }
                }
            }

            // Dice trays are animated by the frame loop, which does not run here: show their result.
            foreach (DiceTray tray in Object.FindObjectsByType<DiceTray>())
            {
                tray.Settle();
            }

            Render(camera, target, output);
        }

        /// <summary>Renders the gallery: every card, event, technology and seat ship.</summary>
        public static void Gallery()
        {
            string output = Output();
            EditorSceneManager.OpenScene(GalleryScene.ScenePath, OpenSceneMode.Single);
            (Camera camera, RenderTexture target) = Prepare();
            Object.FindAnyObjectByType<GalleryController>().Build();
            Render(camera, target, output);
        }

        private static string Output()
        {
            string? path = Environment.GetEnvironmentVariable("VORTEX_CAPTURE");
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Set VORTEX_CAPTURE to the .png file to write.");
            }

            return path;
        }

        // The camera draws into an image of the reference size. An overlay canvas never reaches a render texture, so it
        // is drawn by the camera too, at 1 unit: in front of the 3D cards and the zoom, as on screen.
        private static (Camera Camera, RenderTexture Target) Prepare()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            var target = new RenderTexture(Width, Height, 24);
            camera.targetTexture = target;
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>())
            {
                if (!canvas.isRootCanvas)
                {
                    continue;
                }

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.planeDistance = 1f;
                }

                canvas.worldCamera = camera;
            }

            return (camera, target);
        }

        private static void Render(Camera camera, RenderTexture target, string output)
        {
            // Layouts and anchors are normally settled over frames; settle them now, a few times for nested layouts.
            for (int pass = 0; pass < 3; pass++)
            {
                Canvas.ForceUpdateCanvases();
                foreach (LayoutGroup group in Object.FindObjectsByType<LayoutGroup>())
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
                }

                foreach (ContentSizeFitter fitter in Object.FindObjectsByType<ContentSizeFitter>())
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)fitter.transform);
                }

                foreach (ScreenAnchor anchor in Object.FindObjectsByType<ScreenAnchor>())
                {
                    anchor.Place();
                }

                foreach (CardAnchor card in Object.FindObjectsByType<CardAnchor>())
                {
                    card.Place();
                }

                camera.Render();
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllBytes(output, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Debug.Log("Captured " + output);
        }
    }
}
