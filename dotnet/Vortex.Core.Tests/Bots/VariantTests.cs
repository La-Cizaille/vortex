using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Vortex.Core.Config;
using Vortex.Core.Content;
using Vortex.Simulator;

namespace Vortex.Core.Tests.Bots
{
    /// <summary>Variant files: small patches over the reference content, validated by the game's loader.</summary>
    [TestFixture]
    public class VariantTests
    {
        private string _dir = string.Empty;

        [SetUp]
        public void CreateDirectory()
        {
            _dir = Path.Combine(Path.GetTempPath(), "vortex-variants-" + TestContext.CurrentContext.Test.ID);
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void DeleteDirectory()
        {
            Directory.Delete(_dir, recursive: true);
        }

        [Test]
        public void The_reference_is_the_repository_content()
        {
            ContentSet reference = Variants.LoadReference(TestPaths.DataDir);
            Assert.That(reference.Data.Modifiers.Select(c => c.Id), Is.EqualTo(TestPaths.LoadRealContent().Modifiers.Select(c => c.Id)));
            Assert.That(reference.Config.RoundStartRotation, Is.EqualTo(TestPaths.LoadRealConfig().RoundStartRotation));
            Assert.That(reference.ContentSha256, Has.Length.EqualTo(64));
        }

        [Test]
        public void A_config_patch_changes_only_what_it_names()
        {
            ContentSet reference = Variants.LoadReference(TestPaths.DataDir);
            ContentSet v = Load("{ \"name\": \"Rotation\", \"description\": \"d\", \"config\": { \"roundStartRotation\": \"CounterClockwise\", \"technologiesToWin\": 2 } }");

            Assert.That(v.Name, Is.EqualTo("Rotation"));
            Assert.That(v.Description, Is.EqualTo("d"));
            Assert.That(v.Config.RoundStartRotation, Is.EqualTo(RoundStartRotation.CounterClockwise));
            Assert.That(v.Config.TechnologiesToWin, Is.EqualTo(2));
            Assert.That(v.Config.StartingHp, Is.EqualTo(reference.Config.StartingHp));
            Assert.That(v.ContentSha256, Is.Not.EqualTo(reference.ContentSha256));
        }

        [Test]
        public void Player_count_settings_are_patched_by_player_count()
        {
            ContentSet reference = Variants.LoadReference(TestPaths.DataDir);
            ContentSet v = Load("{ \"name\": \"Bouclier\", \"config\": { \"playerCounts\": { \"5\": { \"startShield\": 6 } } } }");

            Assert.That(v.Config.ForPlayers(5)!.StartShield, Is.EqualTo(6));
            Assert.That(v.Config.ForPlayers(5)!.DoomRound, Is.EqualTo(reference.Config.ForPlayers(5)!.DoomRound));
            Assert.That(v.Config.ForPlayers(4)!.StartShield, Is.EqualTo(reference.Config.ForPlayers(4)!.StartShield));
        }

        [Test]
        public void A_patch_that_restates_the_reference_keeps_its_fingerprint()
        {
            ContentSet reference = Variants.LoadReference(TestPaths.DataDir);
            ContentSet v = Load("{ \"name\": \"Same\", \"config\": { \"startingHp\": " + reference.Config.StartingHp + " } }");
            Assert.That(v.ContentSha256, Is.EqualTo(reference.ContentSha256));
        }

        [Test]
        public void A_card_patch_merges_objects_and_replaces_arrays()
        {
            ContentSet v = Load("{ \"name\": \"Canon\", \"cards\": { \"A_001\": { \"copies\": 3, \"effects\": [ { \"brick\": \"StealShieldBeforeAttack\", \"amount\": 3 } ] } } }");
            CardDefinition card = v.Data.Modifiers.Single(c => c.Id == "A_001");
            CardDefinition original = TestPaths.LoadRealContent().Modifiers.Single(c => c.Id == "A_001");

            Assert.That(card.Copies, Is.EqualTo(3));
            Assert.That(card.Name, Is.EqualTo(original.Name));
            Assert.That(card.Effects, Has.Count.EqualTo(1));
            Assert.That((int)card.Effects[0].Parameters["amount"], Is.EqualTo(3));
        }

        [TestCase("{ \"description\": \"no name\" }", "'name' is required")]
        [TestCase("{ \"name\": \"x\", \"rules\": {} }", "unknown key 'rules'")]
        [TestCase("{ \"name\": \"x\", \"cards\": { \"A_999\": { \"copies\": 1 } } }", "unknown id 'A_999'")]
        [TestCase("{ \"name\": \"x\", \"cards\": { \"A_001\": { \"id\": \"A_002\" } } }", "an id cannot be changed")]
        [TestCase("{ \"name\": \"x\", \"cards\": { \"A_001\": 3 } }", "must be an object")]
        [TestCase("{ \"name\": \"x\", \"cards\": [] }", "must be an object keyed by id")]
        [TestCase("{ \"name\": \"x\", \"config\": { \"startingHp\": null } }", "null values are not allowed")]
        [TestCase("{ \"name\": \"x\", \"config\": { \"schemaVersion\": 3 } }", "cannot be changed by a variant")]
        [TestCase("{ \"name\": \"x\", \"config\": { \"technologiesToWin\": 9 } }", "technologiesToWin")]
        [TestCase("{ \"name\": \"x\", \"config\": { \"startingHp\": \"many\" } }", "startingHp")]
        [TestCase("{ \"name\": \"x\", \"events\": { \"EVT_FIN_DES_TEMPS\": { \"effects\": [ { \"brick\": \"NoSuchBrick\" } ] } } }", "NoSuchBrick")]
        [TestCase("{ \"name\": \"x\", \"config\": { \"startingHp\": 20, \"startingHp\": 30 } }", "startingHp")]
        [TestCase("{ \"name\": \"x\", \"config\": 3 }", "'config' must be an object")]
        [TestCase("{ \"name\": 3 }", "'name' must be a string")]
        [TestCase("{ \"name\": \"a|b\" }", "forbidden character U+007C")]
        [TestCase("{ \"name\": \"x\", \"description\": \"line\\nbreak\" }", "forbidden character U+000A")]
        [TestCase("{ \"name\": \"x\", \"config\": { \"playerCounts\": { \"five\": { \"startShield\": 3 } } } }", "unknown player count 'five'")]
        [TestCase("{ \"name\": ", "")]
        public void Invalid_variants_are_refused_with_a_message(string json, string expected)
        {
            Assert.That(() => Load(json), Throws.TypeOf<GameDataException>().With.Message.Contains(expected));
        }

        [Test]
        public void Oversized_or_missing_variant_files_are_refused()
        {
            string big = Path.Combine(_dir, "big.json");
            File.WriteAllText(big, "{ \"name\": \"" + new string('x', 300 * 1024) + "\" }");
            Assert.That(() => Variants.LoadVariant(TestPaths.DataDir, big), Throws.TypeOf<GameDataException>());
            Assert.That(() => Variants.LoadVariant(TestPaths.DataDir, Path.Combine(_dir, "missing.json")), Throws.TypeOf<GameDataException>());
        }

        [Test]
        public void Deeply_nested_variants_are_refused()
        {
            string nested = string.Concat(Enumerable.Repeat("{\"a\":", 40)) + "1" + new string('}', 40);
            Assert.That(() => Load("{ \"name\": \"x\", \"config\": " + nested + " }"), Throws.TypeOf<GameDataException>());
        }

        [Test]
        public void Every_variant_shipped_in_the_repository_is_valid()
        {
            string dir = Path.Combine(TestPaths.RepoRoot, "docs", "balance", "variants");
            string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.json") : Array.Empty<string>();
            foreach (string file in files)
            {
                Assert.That(() => Variants.LoadVariant(TestPaths.DataDir, file), Throws.Nothing, file);
            }
        }

        private ContentSet Load(string json)
        {
            string path = Path.Combine(_dir, "variant.json");
            File.WriteAllText(path, json);
            return Variants.LoadVariant(TestPaths.DataDir, path);
        }
    }
}
