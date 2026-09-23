using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Vortex.CardImporter;
using Vortex.Core.Content;

namespace Vortex.Core.Tests.Importer
{
    [TestFixture]
    public class CardImporterTests
    {
        [Test]
        public void Committed_gamedata_matches_the_design_workbook()
        {
            // Guards against hand-edits of generated JSON and forgotten re-imports.
            (_, string json) = Program.Import(TestPaths.DesignWorkbook);
            Assert.That(json, Is.EqualTo(File.ReadAllText(TestPaths.GameDataJson)),
                "gamedata.json is stale: run the CardImporter and commit the result.");
        }

        [TestCase("tempête électro-magnetique", "TEMPETE_ELECTRO_MAGNETIQUE")]
        [TestCase("Le calme avant la tempête ", "LE_CALME_AVANT_LA_TEMPETE")]
        [TestCase("Nuée parasitaire", "NUEE_PARASITAIRE")]
        [TestCase("Cœur", "COEUR")]
        public void Slug_folds_french_diacritics(string input, string expected)
        {
            Assert.That(WorkbookParser.Slug(input), Is.EqualTo(expected));
        }

        [Test]
        public void Clean_normalises_line_endings_and_strips_invisible_characters()
        {
            string input = "  Ligne 1  \r\nLigne" + (char)0xFE0F + " 2" + (char)0x200B + "\r";
            Assert.That(WorkbookParser.Clean(input), Is.EqualTo("Ligne 1\nLigne 2"));
        }

        [TestCase("2.0", 2)]
        [TestCase("1", 1)]
        public void ParseCopies_accepts_whole_numbers(string input, int expected)
        {
            Assert.That(WorkbookParser.ParseCopies(input, "t"), Is.EqualTo(expected));
        }

        [TestCase("1.5")]
        [TestCase("0")]
        [TestCase("-1")]
        [TestCase("99")]
        [TestCase("deux")]
        public void ParseCopies_rejects_invalid_values(string input)
        {
            Assert.Throws<InvalidDataException>(() => WorkbookParser.ParseCopies(input, "t"));
        }

        [Test]
        public void Unknown_colour_is_rejected()
        {
            Assert.Throws<InvalidDataException>(() => WorkbookParser.ParseColor("Violet", "t"));
        }

        [TestCase("worksheets/sheet1.xml", "xl/worksheets/sheet1.xml")]
        [TestCase("/xl/worksheets/sheet2.xml", "xl/worksheets/sheet2.xml")]
        [TestCase("./worksheets/../worksheets/sheet3.xml", "xl/worksheets/sheet3.xml")]
        public void Relationship_targets_are_normalised(string target, string expected)
        {
            Assert.That(XlsxReader.NormalisePartName(target), Is.EqualTo(expected));
        }

        [TestCase("../../etc/passwd")]
        [TestCase("/docProps/core.xml")]
        [TestCase("../[Content_Types].xml")]
        public void Relationship_targets_outside_xl_are_rejected(string target)
        {
            Assert.Throws<InvalidDataException>(() => XlsxReader.NormalisePartName(target));
        }

        [Test]
        public void Xml_with_a_dtd_is_rejected()
        {
            // XXE / billion laughs: any DOCTYPE must be refused.
            const string evil = "<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e SYSTEM \"file:///c:/windows/win.ini\">]><x>&e;</x>";
            using MemoryStream xlsx = BuildZip(("xl/workbook.xml", Encoding.UTF8.GetBytes(evil)), ("xl/_rels/workbook.xml.rels", Encoding.UTF8.GetBytes(EmptyRels)));
            Assert.Throws<XmlException>(() => XlsxReader.Load(xlsx));
        }

        [Test]
        public void Zip_bomb_like_entries_are_rejected()
        {
            // 5 MB of zeros compresses by far more than the allowed ratio.
            byte[] zeros = new byte[5 * 1024 * 1024];
            using MemoryStream xlsx = BuildZip(("xl/_rels/workbook.xml.rels", zeros));
            Assert.Throws<InvalidDataException>(() => XlsxReader.Load(xlsx));
        }

        private const string EmptyRels = "<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"/>";

        private static MemoryStream BuildZip(params (string Name, byte[] Content)[] entries)
        {
            var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach ((string name, byte[] content) in entries)
                {
                    using Stream s = zip.CreateEntry(name, CompressionLevel.Optimal).Open();
                    s.Write(content, 0, content.Length);
                }
            }

            ms.Position = 0;
            return ms;
        }
    }
}
