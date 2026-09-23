using System.IO;
using NUnit.Framework;
using Vortex.ContentTool;
using Vortex.Core.Content;

namespace Vortex.Core.Tests.ContentToolTests
{
    [TestFixture]
    public class ContentToolTests
    {
        [Test]
        public void Generated_catalogue_is_up_to_date()
        {
            string docPath = Path.Combine(TestPaths.RepoRoot, "docs", "CARDS.md");
            int code = Program.Run(new[] { "docs", TestPaths.DataDir, docPath, "--check" }, TextWriter.Null, TextWriter.Null);
            Assert.That(code, Is.Zero, "docs/CARDS.md is stale: run the ContentTool docs command.");
        }

        [Test]
        public void Catalogue_lists_every_card_and_renders_icon_tags()
        {
            GameData data = TestPaths.LoadRealContent();
            string md = CardsDocGenerator.Generate(data);
            foreach (CardDefinition c in data.Modifiers)
            {
                Assert.That(md, Does.Contain("### " + c.Id));
            }

            // Raw <ATQ> would be swallowed as an HTML tag by Markdown renderers.
            Assert.That(md, Does.Not.Contain("<ATQ>"));
            Assert.That(md, Does.Contain("[ATQ]"));
        }

        [Test]
        public void Format_check_passes_on_committed_content()
        {
            Assert.That(Program.Run(new[] { "format", TestPaths.DataDir, "--check" }, TextWriter.Null, TextWriter.Null), Is.Zero);
        }

        [Test]
        public void Validate_succeeds_on_committed_content()
        {
            Assert.That(Program.Run(new[] { "validate", TestPaths.DataDir }, TextWriter.Null, TextWriter.Null), Is.Zero);
        }

        [TestCase]
        [TestCase("unknown", "x")]
        [TestCase("docs", "only-one-arg")]
        public void Invalid_usage_returns_2(params string[] args)
        {
            Assert.That(Program.Run(args, TextWriter.Null, TextWriter.Null), Is.EqualTo(2));
        }
    }
}
