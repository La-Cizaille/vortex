using System.IO;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Content;

namespace Vortex.Core.Tests.Content
{
    /// <summary>
    /// Asserts the committed content matches the design as agreed (RULES.md §2).
    /// If the designer changes the spreadsheet on purpose, update these expectations in the same PR.
    /// </summary>
    [TestFixture]
    public class GameDataContentTests
    {
        private GameData _data = null!;

        [OneTimeSetUp]
        public void Load()
        {
            _data = GameDataSerializer.Deserialize(File.ReadAllText(TestPaths.GameDataJson));
        }

        [Test]
        public void Attack_deck_has_27_cards_and_54_copies()
        {
            var atk = _data.Modifiers.Where(m => m.Slot == CardSlot.Attack).ToList();
            Assert.That(atk, Has.Count.EqualTo(27));
            Assert.That(atk.Sum(m => m.Copies), Is.EqualTo(54));
        }

        [Test]
        public void Defense_deck_has_27_cards_and_50_copies()
        {
            var def = _data.Modifiers.Where(m => m.Slot == CardSlot.Defense).ToList();
            Assert.That(def, Has.Count.EqualTo(27));
            Assert.That(def.Sum(m => m.Copies), Is.EqualTo(50));
        }

        [Test]
        public void Event_deck_has_8_cards_and_15_copies_including_one_doom_event()
        {
            Assert.That(_data.Events, Has.Count.EqualTo(8));
            Assert.That(_data.Events.Sum(e => e.Copies), Is.EqualTo(15));
            Assert.That(_data.Events.Single(e => e.Id == "EVT_FIN_DES_TEMPS").Copies, Is.EqualTo(1));
        }

        [Test]
        public void One_technology_per_non_neutral_colour()
        {
            Assert.That(
                _data.Technologies.Select(t => t.Color),
                Is.EquivalentTo(new[] { TechColor.Blue, TechColor.Red, TechColor.Green, TechColor.Yellow }));
        }

        [Test]
        public void Cards_flagged_x_by_the_designer_are_marked_for_review()
        {
            Assert.That(
                _data.Modifiers.Where(m => m.NeedsReview).Select(m => m.Id),
                Is.EquivalentTo(new[] { "A_003", "A_010", "A_015", "A_021", "A_022" }));
        }

        [Test]
        public void Stray_invisible_characters_are_stripped_from_card_text()
        {
            // The spreadsheet has a U+FE0F variation selector in A_010's text.
            string text = _data.Modifiers.Single(m => m.Id == "A_010").Text;
            Assert.That(text.Any(c => c == (char)0xFE0F), Is.False);
        }
    }
}
