using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Vortex.Core.Content;

namespace Vortex.Core.Tests.Content
{
    /// <summary>Hardening and canonical form of the content loader (docs/SECURITY.md, surfaces S2/S3).</summary>
    [TestFixture]
    public class ContentJsonTests
    {
        private static GameData LoadWithCards(string cardsJson)
        {
            return GameDataLoader.Load(cardsJson, TestPaths.EventsJson, TestPaths.TechnologiesJson);
        }

        private static string Replace(string json, string oldValue, string newValue)
        {
            Assert.That(json, Does.Contain(oldValue), "Test precondition: fragment not found in cards.json.");
            return json.Replace(oldValue, newValue, StringComparison.Ordinal);
        }

        [Test]
        public void Committed_content_files_are_canonical()
        {
            Assert.That(ContentJson.Serialize(ContentJson.Parse<CardsFile>(TestPaths.CardsJson, "cards")), Is.EqualTo(TestPaths.CardsJson));
            Assert.That(ContentJson.Serialize(ContentJson.Parse<EventsFile>(TestPaths.EventsJson, "events")), Is.EqualTo(TestPaths.EventsJson));
            Assert.That(ContentJson.Serialize(ContentJson.Parse<TechnologiesFile>(TestPaths.TechnologiesJson, "techs")), Is.EqualTo(TestPaths.TechnologiesJson));
        }

        [Test]
        public void Schema_pointer_is_accepted_and_preserved()
        {
            CardsFile file = ContentJson.Parse<CardsFile>(TestPaths.CardsJson, "cards");
            Assert.That(file.Schema, Is.EqualTo("./schema/cards.schema.json"));
        }

        [Test]
        public void Type_metadata_is_rejected()
        {
            // "$type" must never select a CLR type (deserialization gadget -> RCE).
            string json = Replace(TestPaths.CardsJson, "\"schemaVersion\": 4,", "\"$type\": \"System.IO.FileInfo, System.IO.FileSystem\", \"schemaVersion\": 4,");
            Assert.Throws<GameDataException>(() => LoadWithCards(json));
        }

        [Test]
        public void Unknown_members_are_rejected()
        {
            string json = Replace(TestPaths.CardsJson, "\"schemaVersion\": 4,", "\"schemaVersion\": 4, \"extra\": true,");
            Assert.Throws<GameDataException>(() => LoadWithCards(json));
        }

        [Test]
        public void Integer_encoded_enums_are_rejected()
        {
            string json = Replace(TestPaths.CardsJson, "\"color\": \"Blue\"", "\"color\": 99");
            Assert.Throws<GameDataException>(() => LoadWithCards(json));
        }

        [Test]
        public void Oversized_input_is_rejected_before_parsing()
        {
            string json = new string(' ', ContentJson.MaxInputChars + 1);
            Assert.Throws<GameDataException>(() => LoadWithCards(json));
        }

        [Test]
        public void Excessive_nesting_is_rejected()
        {
            var sb = new StringBuilder("{\"schemaVersion\": 4, \"cards\": ");
            sb.Append(string.Concat(Enumerable.Repeat("[", 100))).Append(string.Concat(Enumerable.Repeat("]", 100))).Append('}');
            Assert.Throws<GameDataException>(() => LoadWithCards(sb.ToString()));
        }

        [Test]
        public void Wrong_schema_version_is_rejected()
        {
            string json = Replace(TestPaths.CardsJson, "\"schemaVersion\": 4,", "\"schemaVersion\": 1,");
            Assert.Throws<GameDataException>(() => LoadWithCards(json));
        }

        [Test]
        public void Bidi_override_in_text_is_rejected()
        {
            // Trojan Source (CVE-2021-42574): displayed text would differ from stored text.
            string rlo = ((char)0x202E).ToString();
            string json = Replace(TestPaths.CardsJson, "\"name\": \"Canon à particules\"", "\"name\": \"Canon" + rlo + "\"");
            var ex = Assert.Throws<GameDataException>(() => LoadWithCards(json));
            Assert.That(ex!.Message, Does.Contain("U+202E"));
        }

        [Test]
        public void Duplicate_ids_are_rejected()
        {
            string json = Replace(TestPaths.CardsJson, "\"id\": \"A_002\"", "\"id\": \"A_001\"");
            var ex = Assert.Throws<GameDataException>(() => LoadWithCards(json));
            Assert.That(ex!.Message, Does.Contain("duplicate id 'A_001'"));
        }

        [Test]
        public void Id_not_matching_its_slot_is_rejected()
        {
            string json = Replace(TestPaths.CardsJson, "\"id\": \"A_001\"", "\"id\": \"D_901\"");
            Assert.Throws<GameDataException>(() => LoadWithCards(json));
        }

        [Test]
        public void Empty_ruling_is_rejected()
        {
            CardsFile file = ContentJson.Parse<CardsFile>(TestPaths.CardsJson, "cards");
            string ruling = file.Cards[0].Ruling;
            string json = Replace(TestPaths.CardsJson, "\"ruling\": " + Newtonsoft.Json.JsonConvert.ToString(ruling), "\"ruling\": \"\"");
            Assert.Throws<GameDataException>(() => LoadWithCards(json));
        }

        [TestCase("slot", typeof(CardSlot))]
        [TestCase("color", typeof(TechColor))]
        [TestCase("usage", typeof(CardUsage))]
        public void Editor_schema_enums_match_the_code(string property, Type enumType)
        {
            // The JSON Schema only helps editing; the C# validator is authoritative. Keep them aligned.
            string path = System.IO.Path.Combine(TestPaths.RepoRoot, "core", "Runtime", "Data", "schema", "cards.schema.json");
            var schema = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(path));
            var values = schema.SelectToken("$defs.card.properties." + property + ".enum")!.Select(t => (string)t!).ToArray();
            Assert.That(values, Is.EquivalentTo(Enum.GetNames(enumType)));
        }

        [Test]
        public void Technology_id_must_match_its_colour()
        {
            string json = TestPaths.TechnologiesJson.Replace("\"id\": \"TECH_BLUE\"", "\"id\": \"TECH_RED\"", StringComparison.Ordinal);
            Assert.Throws<GameDataException>(() => GameDataLoader.Load(TestPaths.CardsJson, TestPaths.EventsJson, json));
        }
    }
}
