using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
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
    }
}
