using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Vortex.Core.Content;

namespace Vortex.Core.Tests.Content
{
    /// <summary>Hardening of the JSON loader (docs/SECURITY.md, surface S2).</summary>
    [TestFixture]
    public class GameDataSerializerTests
    {
        private static string ValidJson => File.ReadAllText(TestPaths.GameDataJson);

        [Test]
        public void Round_trip_is_byte_identical()
        {
            string json = ValidJson;
            Assert.That(GameDataSerializer.Serialize(GameDataSerializer.Deserialize(json)), Is.EqualTo(json));
        }

        [Test]
        public void Output_uses_lf_line_endings_only()
        {
            Assert.That(GameDataSerializer.Serialize(GameDataSerializer.Deserialize(ValidJson)), Does.Not.Contain("\r"));
        }

        [Test]
        public void Type_metadata_is_rejected()
        {
            // "$type" must never select a CLR type (deserialization gadget → RCE).
            string json = ValidJson.Replace("\"schemaVersion\": 1,", "\"$type\": \"System.IO.FileInfo, System.IO.FileSystem\", \"schemaVersion\": 1,", System.StringComparison.Ordinal);
            Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(json));
        }

        [Test]
        public void Unknown_members_are_rejected()
        {
            string json = ValidJson.Replace("\"schemaVersion\": 1,", "\"schemaVersion\": 1, \"extra\": true,", System.StringComparison.Ordinal);
            Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(json));
        }

        [Test]
        public void Integer_encoded_enums_are_rejected()
        {
            string json = ValidJson.Replace("\"color\": \"Blue\"", "\"color\": 99", System.StringComparison.Ordinal);
            Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(json));
        }

        [Test]
        public void Oversized_input_is_rejected_before_parsing()
        {
            string json = new string(' ', GameDataSerializer.MaxInputChars + 1);
            Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(json));
        }

        [Test]
        public void Excessive_nesting_is_rejected()
        {
            var sb = new StringBuilder();
            sb.Append('{').Append("\"schemaVersion\": 1, \"modifiers\": ");
            sb.Append(string.Concat(Enumerable.Repeat("[", 100))).Append(string.Concat(Enumerable.Repeat("]", 100)));
            sb.Append('}');
            Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(sb.ToString()));
        }

        [Test]
        public void Bidi_override_in_text_is_rejected()
        {
            // Trojan Source (CVE-2021-42574): displayed text would differ from stored text.
            string rlo = ((char)0x202E).ToString();
            string json = ValidJson.Replace("\"name\": \"Canon à particules\"", "\"name\": \"Canon" + rlo + "\"", System.StringComparison.Ordinal);
            var ex = Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(json));
            Assert.That(ex!.Message, Does.Contain("U+202E"));
        }

        [Test]
        public void Duplicate_ids_are_rejected()
        {
            string json = ValidJson.Replace("\"id\": \"A_002\"", "\"id\": \"A_001\"", System.StringComparison.Ordinal);
            var ex = Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(json));
            Assert.That(ex!.Message, Does.Contain("duplicate id 'A_001'"));
        }

        [Test]
        public void Id_not_matching_its_slot_is_rejected()
        {
            string json = ValidJson.Replace("\"id\": \"A_001\"", "\"id\": \"D_901\"", System.StringComparison.Ordinal);
            Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(json));
        }

        [Test]
        public void Wrong_schema_version_is_rejected()
        {
            string json = ValidJson.Replace("\"schemaVersion\": 1,", "\"schemaVersion\": 2,", System.StringComparison.Ordinal);
            Assert.Throws<GameDataException>(() => GameDataSerializer.Deserialize(json));
        }
    }
}
