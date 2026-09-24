using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Vortex.Core.Config;
using Vortex.Core.Content;

namespace Vortex.Core.Tests.Content
{
    /// <summary>
    /// Asserts the committed content matches the design as agreed (docs/RULES.md A2, docs/ARBITRAGES.md).
    /// These are the only tests tied to the real card list: when the designer adds or removes
    /// cards on purpose, update the expected counts here in the same PR. Engine tests use test cards.
    /// </summary>
    [TestFixture]
    public class GameDataContentTests
    {
        private GameData _data = null!;

        [OneTimeSetUp]
        public void Load()
        {
            _data = TestPaths.LoadRealContent();
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
                _data.Technologies.Select(t => t.Id),
                Is.EquivalentTo(new[] { "TECH_BLUE", "TECH_RED", "TECH_GREEN", "TECH_YELLOW" }));
        }

        [Test]
        public void Rule_options_match_the_designer_rulings()
        {
            // docs/ARBITRAGES.md ARB-50 (clockwise rotation of the first player), ARB-51 (election with 3 technologies)
            // and ARB-54 (rhythm of the standard 5-player table).
            GameConfig config = TestPaths.LoadRealConfig();
            Assert.That(config.RoundStartRotation, Is.EqualTo(RoundStartRotation.Clockwise));
            Assert.That(config.TechnologiesToWin, Is.EqualTo(3));
            Assert.That(config.StartingHp, Is.EqualTo(30));
            Assert.That(config.ForPlayers(5)!.StartShield, Is.EqualTo(5));
            Assert.That(config.ForPlayers(5)!.DoomRound, Is.EqualTo(16));
        }

        [Test]
        public void Cards_flagged_by_the_designer_are_marked_for_review()
        {
            Assert.That(
                _data.Modifiers.Where(m => m.NeedsReview).Select(m => m.Id),
                Is.EquivalentTo(new[] { "A_003", "A_010", "A_015", "A_021", "A_022" }));
        }

        [Test]
        public void Rulings_never_reference_another_card()
        {
            // RULES.md B1: a ruling is written against the effect model only, so cards can be
            // added or removed without editing the others.
            var anyId = new Regex(@"\b(A_[0-9]{3}|D_[0-9]{3}|EVT_[A-Z_]+|TECH_[A-Z]+)\b");
            var names = _data.Modifiers.Select(m => m.Name.Trim()).Where(n => n.Length > 6).ToList();

            foreach (CardDefinition card in _data.Modifiers)
            {
                Assert.That(anyId.IsMatch(card.Ruling), Is.False, card.Id + " ruling names a card id.");
                string? other = names.FirstOrDefault(n => n != card.Name.Trim() && card.Ruling.Contains(n, System.StringComparison.OrdinalIgnoreCase));
                Assert.That(other, Is.Null, card.Id + " ruling names another card: " + other);
            }
        }
    }
}
