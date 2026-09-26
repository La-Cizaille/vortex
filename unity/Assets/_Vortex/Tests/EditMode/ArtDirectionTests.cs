using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Vortex.Client.Menus;
using Vortex.Client.Content;
using Vortex.Client.Presentation;
using Vortex.Client.Theme;
using Vortex.Core.Content;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The foundations of the art direction (docs/DIRECTION_ARTISTIQUE.md, ARB-93 to ARB-98).</summary>
    public class ArtDirectionTests
    {
        [Test]
        public void The_theme_has_the_fonts_of_the_art_direction_with_static_atlases_that_write_french()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            TMP_FontAsset?[] fonts = { theme.TitleFont, theme.BodyFont, theme.LabelFont, theme.StencilFont, theme.EngravedFont, theme.TerminalFont };
            Assert.That(fonts, Has.All.Not.Null, "Six fonts: title, body, label, stencil, engraved, terminal.");
            Assert.That(fonts.Distinct().Count(), Is.EqualTo(6), "Six different faces.");
            foreach (TMP_FontAsset? font in fonts)
            {
                Assert.That(font!.atlasPopulationMode, Is.EqualTo(AtlasPopulationMode.Static), font.name + ": the build carries the glyphs, not the font file.");
                Assert.That(font.HasCharacters("àâçéèêëîïôùûüÿœ«»’…—€ÀÉÈÇŒ", out _), Is.True, font.name + " writes French.");
            }

            Assert.That(TMP_Settings.defaultFontAsset, Is.EqualTo(theme.LabelFont), "Interface texts without a font of their own take the label font.");
        }

        [Test]
        public void A_cards_flavour_text_reaches_its_face_and_an_absent_one_stays_empty()
        {
            var texts = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
            var with = new TechnologyDefinition("TECH_BLUE", TechColor.Blue, "Légion de l'Ordre", "Règle.", "Arbitrage.", null, "L'Ordre ne rit pas.");
            var without = new TechnologyDefinition("TECH_BLUE", TechColor.Blue, "Légion de l'Ordre", "Règle.", "Arbitrage.", null);
            Assert.That(CardFace.Of(with, texts).Flavor, Is.EqualTo("L'Ordre ne rit pas."));
            Assert.That(CardFace.Of(without, texts).Flavor, Is.Empty);
        }

        [Test]
        public void An_opponents_figures_sit_on_a_radar_that_sweeps_and_glitches_but_always_settles_back()
        {
            var panel = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GameScene.SeatPanelPath));
            try
            {
                RadarScreen radar = panel.GetComponentInChildren<RadarScreen>();
                Assert.That(radar, Is.Not.Null, "The panel's screen is a radar.");
                TMP_Text hp = panel.transform.Find("PV").GetComponent<TMP_Text>();
                Vector2 rest = hp.rectTransform.anchoredPosition;
                float alpha = hp.color.a;

                radar.Tick(RadarScreen.SweepSeconds / 4f);
                Assert.That(radar.Sweep, Is.EqualTo(0.25f).Within(0.01f), "The bar sweeps down.");

                bool glitched = false;
                for (int frame = 0; frame < 1000; frame++)
                {
                    radar.Tick(1f / 30f);
                    glitched |= radar.Glitching;
                }

                Assert.That(glitched, Is.True, "Now and then the picture jumps.");
                while (radar.Glitching)
                {
                    radar.Tick(1f / 30f);
                }

                Assert.That((hp.rectTransform.anchoredPosition, hp.color.a), Is.EqualTo((rest, alpha)), "And the figures settle back where they were.");
            }
            finally
            {
                Object.DestroyImmediate(panel);
            }
        }

        [Test]
        public void The_end_of_a_game_is_a_poster_with_its_stamp_and_the_epitaphs_of_the_fallen()
        {
            try
            {
                EditorSceneManager.OpenScene(GameScene.ScenePath, OpenSceneMode.Single);
                GameOverPanel panel = Object.FindAnyObjectByType<GameOverPanel>(FindObjectsInactive.Include);
                panel.Show("Victoire de Vous : Domination", "RECHERCHÉ", "Ci-gît Bot.");
                Assert.That(panel.Poster, Is.EqualTo(("RECHERCHÉ", "Ci-gît Bot.")));
                Assert.That(panel.Text, Is.EqualTo("Victoire de Vous : Domination"));
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [Test]
        public void The_vortex_grows_as_the_end_of_times_nears_and_swells_when_an_event_is_revealed()
        {
            var sky = new GameObject("Fond");
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            disc.name = VortexDisplay.PartName;
            disc.transform.SetParent(sky.transform, false);
            disc.transform.localScale = new Vector3(2f, 2f, 0.01f);
            try
            {
                VortexDisplay vortex = VortexDisplay.Attach(sky);
                Assert.That(vortex.HasVortex, Is.True);
                vortex.ShowProgress(0.5f);
                for (int i = 0; i < 100; i++)
                {
                    vortex.Tick(0.1f);
                }

                Assert.That(vortex.Progress, Is.EqualTo(0.5f).Within(0.01f));
                float grown = disc.transform.localScale.x;
                Assert.That(grown, Is.EqualTo(2f * (1f + (VortexDisplay.Growth * 0.5f))).Within(0.01f), "Halfway to the end: part of its growth.");

                vortex.Pulse();
                vortex.Tick(0.6f);
                Assert.That(disc.transform.localScale.x, Is.GreaterThan(grown), "An event: it swells.");
                vortex.Tick(1f);
                Assert.That(disc.transform.localScale.x, Is.EqualTo(grown).Within(0.01f), "Then settles back.");
            }
            finally
            {
                Object.DestroyImmediate(sky);
            }
        }

        [Test]
        public void The_casino_die_lights_up_on_its_highest_face_only()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            Assume.That(theme.DieModel, Is.Not.Null, "The theme has the casino die.");
            var eye = new GameObject("Caméra").AddComponent<Camera>();
            var place = new GameObject("Place", typeof(RectTransform)).GetComponent<RectTransform>();
            GameObject die = Object.Instantiate(theme.DieModel!);
            try
            {
                DieSpinner spinner = die.AddComponent<DieSpinner>();
                spinner.Follow(place, eye, 5f, 0f);
                Assert.That(spinner.Show(8, 0f), Is.True);
                Assert.That(spinner.Lit, Is.True, "An 8: the light is on.");
                spinner.Show(3, 0f);
                Assert.That(spinner.Lit, Is.False, "Any other face: off.");
            }
            finally
            {
                Object.DestroyImmediate(die);
                Object.DestroyImmediate(eye.gameObject);
                Object.DestroyImmediate(place.gameObject);
            }
        }
    }
}
