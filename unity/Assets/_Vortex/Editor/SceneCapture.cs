using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Vortex.Client.Gallery;
using Vortex.Client.Menus;
using Vortex.Client.Presentation;
using Vortex.Core.Commands;
using Vortex.Core.Content;
using Vortex.Core.Events;
using Vortex.Core.Projection;
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
        /// VORTEX_PHASE=market, during it. VORTEX_PHASE=aim shows an attack being aimed at the first opponent it may target,
        /// with the engine's preview next to it (ADR-0018). VORTEX_PHASE=pause shows the pause menu, VORTEX_PHASE=end
        /// plays the game to its end and shows the end of game panel, and VORTEX_PHASE=log opens the game log. With a person,
        /// VORTEX_PHASE=decision renders the first decision they answer on the table (ARB-82): the person buys and uses
        /// cards, so that something asks them.
        /// </summary>
        public static void Game()
        {
            string output = Output();
            int round = int.TryParse(Environment.GetEnvironmentVariable("VORTEX_ROUND"), out int value) ? Math.Clamp(value, 1, 30) : 4;
            bool human = Environment.GetEnvironmentVariable("VORTEX_HUMAN") == "1";
            string? phase = Environment.GetEnvironmentVariable("VORTEX_PHASE");
            bool atMarket = phase == "market";
            bool toTheEnd = phase == "end";
            bool atDecision = phase == "decision";
            EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
            (Camera camera, RenderTexture target) = Prepare();
            GameDirector director = Object.FindAnyObjectByType<GameDirector>();
            director.HumanFirstSeat = human;
            director.TurnSeconds = int.TryParse(Environment.GetEnvironmentVariable("VORTEX_TURN_SECONDS"), out int turn) ? turn : 0;
            director.Begin();
            for (int frame = 0; frame < 50000 && (toTheEnd ? !director.GameOver.Shown : director.Model!.Outcome == null); frame++)
            {
                director.Advance(0.2f);
                bool reached = !toTheEnd && (atDecision || director.Model!.Round >= round);
                if (!human && reached && director.IsPlaying)
                {
                    break;
                }

                if (human && director.Controls.Offered)
                {
                    // The person's turn: rendered at the market or after it, once the round is reached; or their first
                    // decision on the table.
                    PlayerControls controls = director.Controls;
                    bool asked = controls.Choices != null || controls.Decision.gameObject.activeSelf;
                    if (atDecision ? controls.Choices != null : reached && !asked && controls.CanEndMarket == atMarket)
                    {
                        break;
                    }

                    if (TableAnswers.Answer(controls))
                    {
                        // A decision, answered on the table.
                    }
                    else if (controls.CanEndMarket)
                    {
                        if (!(atDecision && Buy(director)))
                        {
                            controls.EndMarket();
                        }
                    }
                    else if (!(atDecision && UseACard(director)))
                    {
                        controls.EndTurn();
                    }
                }
            }

            // Dice trays are animated by the frame loop, which does not run here: show their result.
            foreach (DiceTray tray in Object.FindObjectsByType<DiceTray>())
            {
                tray.Settle();
            }

            if (phase == "pause")
            {
                director.Pause.Open();
            }

            if (phase == "log")
            {
                Object.FindAnyObjectByType<GameLogDisplay>().Toggle();
            }

            if (phase == "effects")
            {
                PlayEffects(director, float.TryParse(Environment.GetEnvironmentVariable("VORTEX_EFFECT_TIME"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float at) ? at : 0.35f);
            }

            if (human && phase == "aim")
            {
                // The layers must stand where they are drawn before the pointer positions are read.
                Settle(camera);
                AimFirstAttack(director);
            }

            Render(camera, target, output);
        }

        // Buys the first card of a market onto the person's side (attack in even rounds, defense in odd ones).
        private static bool Buy(GameDirector director)
        {
            Rect side = CardAnchor.ScreenRectOf((RectTransform)director.Seats[director.Viewer].transform);
            CardSlot slot = director.Model!.Round % 2 == 0 ? CardSlot.Attack : CardSlot.Defense;
            return director.Controls.CanBuy(slot, 0) && director.Controls.Buy(slot, 0, side.center);
        }

        // Drags one of the person's usable cards into the middle; false when none can be used.
        private static bool UseACard(GameDirector director)
        {
            PlayerView me = director.Session!.View.Players[director.Viewer];
            PlayerControls controls = director.Controls;
            Vector2 middle = CardAnchor.ScreenRectOf(controls.ActivationZone).center;
            return new[] { me.AttackSlot, me.DefenseSlot }.Any(card => card != null && controls.CanUse(card.Uid) && controls.UseCard(card.Uid, middle));
        }

        // The animations of the table frozen mid-way (ANIMATIONS.md §5), to judge them without playing: seat 2 aims at
        // seat 0 and fires into its shield, seat 3 powers up, seat 4 explodes, seat 5 takes a heavy hit, seat 1 gets a
        // Torment token and the round's event sends its wave; seat 4 smokes and seat 2's overcharge crackles, with the
        // theme's effects. The feedbacks are the project's own (their effect prefabs, lot B4), or fresh ones without the
        // project's. The frame loop does not run here: effects, particles, bolts and ships are moved on by hand, by the
        // same amount of time.
        private static void PlayEffects(GameDirector director, float seconds)
        {
            IFeedbackStage stage = director;
            int seats = director.Ships.Count;

            // The game played to reach the round left effects behind, never moved on here: they go first.
            foreach (PlaceholderEffect left in Object.FindObjectsByType<PlaceholderEffect>())
            {
                Object.DestroyImmediate(left.gameObject);
            }

            foreach (TimedRemoval left in Object.FindObjectsByType<TimedRemoval>())
            {
                Object.DestroyImmediate(left.gameObject);
            }

            foreach (ProjectileFlight left in Object.FindObjectsByType<ProjectileFlight>())
            {
                Object.DestroyImmediate(left.gameObject);
            }

            var shot = new GameEvent { Type = GameEventType.AttackResolved, Player = 1 % seats, Other = 0, Value = 10, Amount = 5, Values = new System.Collections.Generic.List<int> { 5, 5, 5 } };
            Feedback<AimFeedback>("Aim").Play(new GameEvent { Type = GameEventType.AttackDeclared, Player = shot.Player, Other = 0 }, stage);
            Advance(0.6f);
            Feedback<LaserFeedback>("Laser").Play(shot, stage);
            if (seats > 2)
            {
                Feedback<ThrusterFeedback>("Thrusters").Play(new GameEvent { Type = GameEventType.TechnologyActivated, Player = 2, Value = (int)TechColor.Blue }, stage);
            }

            Feedback<TormentFeedback>("Torment").Play(new GameEvent { Type = GameEventType.TormentPlaced, Player = 0, Value = 1 }, stage);
            Feedback<EventFeedback>("RoundEvent").Play(new GameEvent { Type = GameEventType.EventRevealed, Id = "EVT_CAPTURE" }, stage);
            if (seats > 4)
            {
                Feedback<KnockbackFeedback>("Knockback").Play(new GameEvent { Type = GameEventType.HpLost, Player = 4, Amount = 12, Cause = HpLossCause.Attack }, stage);
            }

            if (seats > 3)
            {
                Feedback<ExplosionFeedback>("Explosion").Play(new GameEvent { Type = GameEventType.PlayerEliminated, Player = 3 }, stage);
            }

            ThemeEffect(stage.Theme != null ? stage.Theme.SmokePrefab : null, stage, 4 % seats, false);
            ThemeEffect(stage.Theme != null ? stage.Theme.ArcPrefab : null, stage, 2 % seats, true);
            Advance(seconds);
        }

        // The project's feedback asset, or a fresh one.
        private static T Feedback<T>(string name)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(ProjectAssets.ProfilePath.Substring(0, ProjectAssets.ProfilePath.LastIndexOf('/') + 1) + name + ".asset");
            return asset != null ? asset : ScriptableObject.CreateInstance<T>();
        }

        // A theme effect on a ship's hull, as the table places it (HullDamage, OverchargeArcs).
        private static void ThemeEffect(GameObject? prefab, IFeedbackStage stage, int seat, bool carried)
        {
            Transform? ship = stage.MotionOf(seat)?.transform;
            if (prefab != null && ship != null)
            {
                Object.Instantiate(prefab, ShipParts.HullOf(ship), carried ? ship.rotation : Quaternion.identity, carried ? ship : null);
            }
        }

        private static void Advance(float seconds)
        {
            const float step = 1f / 60f;
            for (float time = 0f; time < seconds; time += step)
            {
                foreach (ShipMotion motion in Object.FindObjectsByType<ShipMotion>())
                {
                    motion.Tick(step);
                }

                foreach (PlaceholderEffect effect in Object.FindObjectsByType<PlaceholderEffect>())
                {
                    if (effect != null)
                    {
                        effect.Tick(step);
                    }
                }

                foreach (TimedRemoval timed in Object.FindObjectsByType<TimedRemoval>())
                {
                    if (timed != null)
                    {
                        timed.Tick(step);
                    }
                }

                foreach (ProjectileFlight bolt in Object.FindObjectsByType<ProjectileFlight>())
                {
                    if (bolt != null)
                    {
                        bolt.Tick(step);
                    }
                }

                // Particle systems: each outermost one, with the systems inside it.
                foreach (ParticleSystem particles in Object.FindObjectsByType<ParticleSystem>())
                {
                    if (particles != null && (particles.transform.parent == null || particles.transform.parent.GetComponentInParent<ParticleSystem>() == null))
                    {
                        particles.Simulate(step, true, false, false);
                    }
                }
            }
        }

        /// <summary>
        /// Renders the menu scene: the home screen, or with VORTEX_PHASE the local game menu (local), its development menu
        /// (dev) or the options (options).
        /// </summary>
        public static void Menu()
        {
            string output = Output();
            string? phase = Environment.GetEnvironmentVariable("VORTEX_PHASE");
            EditorSceneManager.OpenScene(MenuScene.ScenePath, OpenSceneMode.Single);
            (Camera camera, RenderTexture target) = Prepare();
            MainMenu menu = Object.FindAnyObjectByType<MainMenu>();
            menu.Setup(_ => { });
            if (phase == "local" || phase == "dev")
            {
                menu.ShowLocalGame();
            }

            if (phase == "dev")
            {
                menu.LocalGame.Development!.gameObject.SetActive(true);
            }
            else if (phase == "options")
            {
                menu.ShowOptions();
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

        // Drags the attack action over the first opponent the engine lets the person attack.
        private static void AimFirstAttack(GameDirector director)
        {
            ActionButton? attack = director.Controls.Actions.FirstOrDefault(a => a.Action == CrewAction.Attack && a.Available);
            int target = director.Session!.LegalCommands(0).Where(c => c.Type == CommandType.Attack).Select(c => c.Target).DefaultIfEmpty(-1).First();
            if (attack == null || target < 0)
            {
                Debug.LogWarning("Capture: no attack to aim this turn; the table is rendered without it.");
                return;
            }

            var from = (RectTransform)attack.transform;
            director.Controls.BeginAim(CrewAction.Attack, from, CardAnchor.ScreenRectOf(from).center);
            director.Controls.Aim(CardAnchor.ScreenRectOf((RectTransform)director.Seats[target].transform).center);
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
        // is drawn by the camera too, at 1 unit: in front of the 3D cards and the zoom, as on screen. The image is HDR,
        // as the screen's intermediate image is: URP renders in the format of a target texture, and an 8-bit one would
        // clip the glowing parts to 1 before Bloom sees them.
        private static (Camera Camera, RenderTexture Target) Prepare()
        {
            Camera camera = Object.FindAnyObjectByType<Camera>();
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.DefaultHDR);
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
            Settle(camera);

            // To an 8-bit sRGB image, as the screen shows it.
            RenderTexture shown = RenderTexture.GetTemporary(Width, Height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(target, shown);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = shown;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(shown);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllBytes(output, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            camera.targetTexture = null;
            Object.DestroyImmediate(target);
            Debug.Log("Captured " + output);
        }

        // Layouts and anchors are normally settled over frames; settle them now, a few times for nested layouts.
        private static void Settle(Camera camera)
        {
            // Cards on their way, initiative dice and leaping dice were never moved on here (no frame loop): they finish
            // first, as they would have long before this round.
            foreach (CardTrip trip in Object.FindObjectsByType<CardTrip>())
            {
                trip.Tick(60f);
            }

            foreach (InitiativeRoll roll in Object.FindObjectsByType<InitiativeRoll>())
            {
                roll.Tick(60f);
            }

            foreach (DieSpinner die in Object.FindObjectsByType<DieSpinner>())
            {
                if (die.Leaping)
                {
                    die.Tick(60f);
                }
            }

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

                foreach (DieSpinner die in Object.FindObjectsByType<DieSpinner>())
                {
                    die.Place();
                }

                foreach (DeckDisplay deck in Object.FindObjectsByType<DeckDisplay>())
                {
                    deck.Place();
                }

                foreach (CockpitDisplay cockpit in Object.FindObjectsByType<CockpitDisplay>())
                {
                    cockpit.Place();
                    cockpit.Settle();
                }

                camera.Render();
            }
        }
    }
}
