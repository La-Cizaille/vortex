using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Vortex.Core.Config;
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

        private static string ReadDataFile(string fileName)
        {
            return System.IO.File.ReadAllText(System.IO.Path.Combine(TestPaths.DataDir, fileName));
        }

        /// <summary>Loads the three content files and the configuration, with <paramref name="fileName"/> replaced.</summary>
        private static void LoadAllWith(string fileName, string json)
        {
            string Pick(string name) => name == fileName ? json : ReadDataFile(name);
            GameData data = GameDataLoader.Load(Pick(CardsFile.FileName), Pick(EventsFile.FileName), Pick(TechnologiesFile.FileName));
            GameDataLoader.LoadConfig(Pick(GameConfig.FileName), data);
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

        // A repeated key makes a reviewer read one value while Newtonsoft silently keeps the last one:
        // same "misleading text" family as Trojan Source. Every file and every nesting level is covered.
        [TestCase(CardsFile.FileName, "\"schemaVersion\": 4,", "\"schemaVersion\": 4, \"schemaVersion\": 4,", "schemaVersion")]
        [TestCase(CardsFile.FileName, "\"id\": \"A_001\",", "\"id\": \"A_001\", \"copies\": 8,", "copies")]
        [TestCase(CardsFile.FileName, "\"brick\": \"StealShieldBeforeAttack\",", "\"brick\": \"StealShieldBeforeAttack\", \"amount\": 8,", "amount")]
        [TestCase(EventsFile.FileName, "\"id\": \"EVT_TROU_NOIR\",", "\"id\": \"EVT_TROU_NOIR\", \"copies\": 9,", "copies")]
        [TestCase(TechnologiesFile.FileName, "\"id\": \"TECH_BLUE\",", "\"id\": \"TECH_BLUE\", \"name\": \"Chaos\",", "name")]
        [TestCase(GameConfig.FileName, "\"startingHp\": 30,", "\"startingHp\": 30, \"startingHp\": 1,", "startingHp")]
        [TestCase(GameConfig.FileName, "\"players\": 2,", "\"players\": 2, \"doomRound\": 99,", "doomRound")]
        public void Repeated_keys_are_rejected(string fileName, string fragment, string replacement, string key)
        {
            string original = ReadDataFile(fileName);
            Assert.That(original, Does.Contain(fragment), "Test precondition: fragment not found in " + fileName + ".");
            string json = original.Replace(fragment, replacement, StringComparison.Ordinal);

            var ex = Assert.Throws<GameDataException>(() => LoadAllWith(fileName, json));
            Assert.That(ex!.Message, Does.StartWith(fileName + ": ").And.Contain("duplicate key '" + key + "'").And.Contain("line "));
        }

        [Test]
        public void Keys_differing_only_by_case_are_rejected()
        {
            // Newtonsoft falls back to a case-insensitive member match: "Copies" would silently set "copies".
            string json = Replace(TestPaths.CardsJson, "\"id\": \"A_001\",", "\"id\": \"A_001\", \"Copies\": 8,");
            var ex = Assert.Throws<GameDataException>(() => LoadWithCards(json));
            Assert.That(ex!.Message, Does.Contain("duplicate key 'copies'").And.Contain("as 'Copies'"));
        }

        [Test]
        public void Keys_are_compared_after_unescaping()
        {
            // "cop\u0069es" does not read like "copies", but it is the same key once decoded.
            string json = Replace(TestPaths.CardsJson, "\"id\": \"A_001\",", "\"id\": \"A_001\", \"cop\\u0069es\": 8,");
            var ex = Assert.Throws<GameDataException>(() => LoadWithCards(json));
            Assert.That(ex!.Message, Does.Contain("duplicate key 'copies'"));
        }

        [Test]
        public void Content_after_the_root_value_is_rejected()
        {
            // A second document after the first one would be read by a reviewer but ignored by the game.
            Assert.Throws<GameDataException>(() => LoadWithCards(TestPaths.CardsJson + "{ \"schemaVersion\": 4, \"cards\": [] }\n"));
        }

        [Test]
        public void Effect_converter_rejects_repeated_parameters_on_its_own()
        {
            // Defence in depth: holds for any future entry point that embeds effects (saves, scenarios).
            const string json = "{ \"brick\": \"GainOvercharge\", \"amount\": 1, \"amount\": 9 }";
            var ex = Assert.Catch<Newtonsoft.Json.JsonException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<EffectSpec>(json));
            Assert.That(ex!.Message, Does.Contain("'amount'"));
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
