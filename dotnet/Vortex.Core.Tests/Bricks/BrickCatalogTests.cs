using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Vortex.Core.Content;
using Vortex.Core.Effects;
using Vortex.Core.Effects.Bricks;

namespace Vortex.Core.Tests.Bricks
{
    /// <summary>The brick catalog is closed, documented and strict about parameters (ADR-0007, SECURITY.md).</summary>
    [TestFixture]
    public class BrickCatalogTests
    {
        private static readonly Dictionary<string, BrickInfo> Catalog = BrickCatalog.Entries().ToDictionary(b => b.Name, StringComparer.Ordinal);

        private static Effect Compile(string json)
        {
            EffectSpec spec = Newtonsoft.Json.JsonConvert.DeserializeObject<EffectSpec>(json)!;
            return BrickCatalog.Compile(Catalog, spec);
        }

        [Test]
        public void Every_brick_is_uniquely_named_and_documented()
        {
            IReadOnlyList<BrickInfo> entries = BrickCatalog.Entries();
            Assert.That(entries.Select(b => b.Name), Is.Unique);
            Assert.That(entries, Has.All.Matches<BrickInfo>(b => b.Description.Length > 20));
            Assert.That(entries.SelectMany(b => b.Parameters), Has.All.Matches<BrickParamInfo>(p => p.Description.Length > 0));
        }

        [Test]
        public void Every_brick_can_be_built_with_its_documented_defaults()
        {
            foreach (BrickInfo b in BrickCatalog.Entries())
            {
                var parameters = new JObject();
                foreach (BrickParamInfo p in b.Parameters.Where(p => p.DefaultValue == null))
                {
                    parameters[p.Name] = p.Range != null ? JToken.FromObject(int.Parse(p.Range.Split("..")[1], System.Globalization.CultureInfo.InvariantCulture)) : JToken.FromObject(p.Type.Split(" / ")[0]);
                }

                parameters["brick"] = b.Name;
                Assert.DoesNotThrow(() => Compile(parameters.ToString()), b.Name);
            }
        }

        [Test]
        public void Editor_schema_lists_exactly_the_catalog_bricks()
        {
            string path = Path.Combine(TestPaths.RepoRoot, "core", "Runtime", "Data", "schema", "cards.schema.json");
            JObject schema = JObject.Parse(File.ReadAllText(path));
            var listed = schema.SelectToken("$defs.card.properties.effects.items.properties.brick.enum")!.Select(t => (string)t!).ToList();
            Assert.That(listed, Is.EquivalentTo(Catalog.Keys));
        }

        [TestCase("{ \"brick\": \"NoSuchBrick\" }", "unknown brick")]
        [TestCase("{ \"brick\": \"AttackValueBonus\" }", "missing required parameter 'amount'")]
        [TestCase("{ \"brick\": \"AttackValueBonus\", \"amount\": 4, \"amout\": 1 }", "unknown parameter 'amout'")]
        [TestCase("{ \"brick\": \"AttackValueBonus\", \"amount\": \"4\" }", "must be an integer")]
        [TestCase("{ \"brick\": \"AttackValueBonus\", \"amount\": 99 }", "must be in -20..20")]
        [TestCase("{ \"brick\": \"AttackValueBonus\", \"amount\": 4, \"when\": \"Sometimes\" }", "must be one of")]
        [TestCase("{ \"brick\": \"CapIncomingAttackLoss\", \"max\": 1, \"unlessOvercharged\": 1 }", "must be true or false")]
        public void Invalid_parameters_are_rejected_with_a_clear_message(string json, string expected)
        {
            var ex = Assert.Throws<BrickParamException>(() => Compile(json));
            Assert.That(ex!.Message, Does.Contain(expected));
        }

        [TestCase("{ \"amount\": 4 }")]
        [TestCase("{ \"brick\": 42 }")]
        [TestCase("{ \"brick\": \"Heal\", \"amount\": { \"nested\": 1 } }")]
        [TestCase("{ \"brick\": \"Heal\", \"amount\": [1, 2] }")]
        [TestCase("[ \"Heal\" ]")]
        public void Malformed_effect_objects_are_rejected_by_the_converter(string json)
        {
            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<EffectSpec>(json));
        }

        [Test]
        public void Card_usage_must_match_brick_kinds()
        {
            GameData data = TestPaths.LoadRealContent();
            CardDefinition single = data.Modifiers.First(c => c.Usage == CardUsage.SingleUse);
            CardDefinition durable = data.Modifiers.First(c => c.Usage == CardUsage.Durable);
            var passive = new EffectSpec("AttackAdvantage");
            var activation = new EffectSpec("GainOvercharge", new[] { new KeyValuePair<string, JToken>("amount", 1) });

            var broken = new GameData(
                data.Modifiers.Select(c =>
                    c.Id == single.Id ? With(c, passive) :
                    c.Id == durable.Id ? With(c, activation) : c).ToList(),
                data.Events,
                data.Technologies);

            var ex = Assert.Throws<GameDataException>(() => EffectCatalog.Build(broken));
            Assert.That(ex!.Message, Does.Contain(single.Id + ": a single-use card needs activation bricks only"));
            Assert.That(ex.Message, Does.Contain(durable.Id + ": a durable card cannot have activation bricks"));
        }

        [Test]
        public void Every_real_card_event_and_technology_compiles()
        {
            Assert.DoesNotThrow(() => EffectCatalog.Build(TestPaths.LoadRealContent()));
            Assert.That(TestPaths.LoadRealContent().Modifiers, Has.All.Matches<CardDefinition>(c => c.Effects.Count > 0), "Every modifier has at least one effect.");
        }

        private static CardDefinition With(CardDefinition c, EffectSpec effect)
        {
            return new CardDefinition(c.Id, c.Name, c.Slot, c.Color, c.Usage, c.Copies, c.NeedsReview, c.Text, c.Ruling, new[] { effect });
        }
    }
}
