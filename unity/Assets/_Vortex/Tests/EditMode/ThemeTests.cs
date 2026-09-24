using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Vortex.Client.Content;
using Vortex.Client.Theme;
using Vortex.Core.Content;
using Vortex.Editor;

namespace Vortex.Tests.EditMode
{
    /// <summary>The theme asset and the conversion of card texts into rich text.</summary>
    public class ThemeTests
    {
        private static readonly Regex Tag = new Regex("<[^<>]*>");
        private static readonly string[] Bold = { "<b>", "</b>" };

        [Test]
        public void The_project_theme_gives_each_technology_its_own_colour()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeSettings>(ThemeAssets.ThemePath);
            Assert.That(theme, Is.Not.Null);
            var colours = new[] { TechColor.Neutral, TechColor.Blue, TechColor.Red, TechColor.Green, TechColor.Yellow }
                .Select(theme.Technology)
                .ToList();
            Assert.That(colours.Distinct().Count(), Is.EqualTo(colours.Count));
            Assert.That(theme.Seat(0), Is.Not.EqualTo(theme.Seat(1)));
        }

        [Test]
        public void Card_text_turns_bold_into_rich_text_and_leaves_missing_icons_out()
        {
            string text = CardText.ToRichText("Vos <ATQ>**attaques** volent 2 points de <BOU> **bouclier**.", _ => false);
            Assert.That(text, Is.EqualTo("Vos <b>attaques</b> volent 2 points de <b>bouclier</b>."));
        }

        [Test]
        public void Card_text_uses_the_icon_sprites_the_theme_has()
        {
            string text = CardText.ToRichText("<ATQ>**attaques** et <TOR>tourment", icon => icon == "ATQ");
            Assert.That(text, Is.EqualTo("<sprite name=\"ATQ\"><b>attaques</b> et tourment"));
        }

        [Test]
        public void Card_text_cannot_restyle_the_screen()
        {
            string text = CardText.ToRichText("<size=300>x</size> a < b <ABCDEF> **fin", _ => true);
            Assert.That(Tags(text).Except(new[] { "<noparse>", "</noparse>", "<b>", "</b>" }), Is.Empty);
            Assert.That(text, Does.EndWith("<b>fin</b>"), "An unclosed bold is closed.");
        }

        [Test]
        public void Every_content_text_converts_to_known_markup()
        {
            GameData data = AssetDatabase.LoadAssetAtPath<GameContent>(ProjectAssets.ContentPath).LoadData();
            IEnumerable<string> texts = data.Modifiers.Select(c => c.Text)
                .Concat(data.Events.Select(e => e.Text))
                .Concat(data.Technologies.Select(t => t.Text));
            foreach (string source in texts)
            {
                string withIcons = CardText.ToRichText(source, _ => true);
                string withoutIcons = CardText.ToRichText(source, _ => false);
                Assert.That(Tags(withoutIcons).Except(Bold), Is.Empty, source);
                Assert.That(Tags(withIcons).Where(t => !t.StartsWith("<sprite name=\"", System.StringComparison.Ordinal)).Except(Bold), Is.Empty, source);
                Assert.That(withoutIcons, Does.Not.Contain("**"), source);
            }
        }

        [Test]
        public void Interface_texts_come_from_the_table_with_a_visible_marker_for_gaps()
        {
            var table = ScriptableObject.CreateInstance<TextTable>();
            Assert.That(table.Get("missing.key"), Is.EqualTo("#missing.key"));

            Assert.That(table.AddMissing(new[] { new KeyValuePair<string, string>("a", "A") }), Is.True);
            Assert.That(table.AddMissing(new[] { new KeyValuePair<string, string>("a", "autre") }), Is.False, "An existing text is never replaced.");
            Assert.That(table.Get("a"), Is.EqualTo("A"));
            Object.DestroyImmediate(table);
        }

        [Test]
        public void The_project_text_table_has_every_key_the_code_uses()
        {
            var table = AssetDatabase.LoadAssetAtPath<TextTable>(ThemeAssets.TextsPath);
            Assert.That(table, Is.Not.Null);
            List<string> constants = typeof(TextKeys).GetFields()
                .Where(f => f.IsLiteral)
                .Select(f => (string)f.GetRawConstantValue())
                .ToList();
            Assert.That(TextKeys.Defaults.Select(d => d.Key), Is.EquivalentTo(constants), "Every key has a default text.");
            foreach (string key in constants)
            {
                Assert.That(table.Contains(key), key);
            }
        }

        private static IEnumerable<string> Tags(string richText) => Tag.Matches(richText).Cast<Match>().Select(m => m.Value);
    }
}
