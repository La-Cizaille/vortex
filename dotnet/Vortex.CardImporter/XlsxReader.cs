using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Vortex.CardImporter
{
    /// <summary>
    /// Minimal, read-only xlsx reader: returns the text value of every cell, per sheet name.
    /// </summary>
    /// <remarks>
    /// Hardening (the spreadsheet is an untrusted file, docs/SECURITY.md S3):
    /// <list type="bullet">
    /// <item>DTD processing prohibited and no XML resolver: blocks XXE and entity expansion ("billion laughs").</item>
    /// <item>Bounded archive: max entries, max uncompressed size per entry, max compression ratio (zip bombs).</item>
    /// <item>Only fixed, known part names are opened; relationship targets are normalised and must stay under <c>xl/</c> (path traversal).</item>
    /// </list>
    /// </remarks>
    internal sealed class XlsxReader
    {
        internal const int MaxEntries = 512;
        internal const long MaxEntryBytes = 20L * 1024 * 1024;
        internal const double MaxCompressionRatio = 200.0;

        private const string MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        private const string PackageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        private readonly Dictionary<string, Dictionary<string, string>> _sheets;

        private XlsxReader(Dictionary<string, Dictionary<string, string>> sheets)
        {
            _sheets = sheets;
        }

        /// <summary>Sheet names in workbook order.</summary>
        public IReadOnlyCollection<string> SheetNames => _sheets.Keys;

        /// <summary>Opens and fully reads a workbook.</summary>
        public static XlsxReader Load(Stream xlsx)
        {
            using var zip = new ZipArchive(xlsx, ZipArchiveMode.Read, leaveOpen: true);
            if (zip.Entries.Count > MaxEntries)
            {
                throw new InvalidDataException("Workbook has too many archive entries.");
            }

            List<string> sharedStrings = ReadSharedStrings(zip);
            Dictionary<string, string> relTargets = ReadWorkbookRelationships(zip);

            var sheets = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            using (XmlReader wb = OpenXml(zip, "xl/workbook.xml"))
            {
                while (wb.Read())
                {
                    if (wb.NodeType == XmlNodeType.Element && wb.LocalName == "sheet" && wb.NamespaceURI == MainNs)
                    {
                        string name = wb.GetAttribute("name") ?? throw new InvalidDataException("Sheet without name.");
                        string relId = wb.GetAttribute("id", RelNs) ?? throw new InvalidDataException("Sheet without r:id.");
                        if (!relTargets.TryGetValue(relId, out string? part))
                        {
                            throw new InvalidDataException("Unknown sheet relationship " + relId + ".");
                        }

                        sheets[name] = ReadSheet(zip, part, sharedStrings);
                    }
                }
            }

            return new XlsxReader(sheets);
        }

        /// <summary>Returns the cells of a sheet, keyed by reference (e.g. <c>B12</c>).</summary>
        public IReadOnlyDictionary<string, string> GetSheet(string name)
        {
            if (!_sheets.TryGetValue(name, out Dictionary<string, string>? cells))
            {
                throw new InvalidDataException("Sheet '" + name + "' not found. Available: " + string.Join(", ", _sheets.Keys));
            }

            return cells;
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var result = new List<string>();
            if (zip.GetEntry("xl/sharedStrings.xml") is null)
            {
                return result;
            }

            using XmlReader r = OpenXml(zip, "xl/sharedStrings.xml");
            StringBuilder? current = null;

            // Manual cursor: ReadElementContentAsString() and Skip() already advance the reader,
            // so Read() is only called when nothing else moved it.
            r.Read();
            while (!r.EOF)
            {
                if (r.NodeType == XmlNodeType.Element && r.NamespaceURI == MainNs)
                {
                    if (r.LocalName == "si" && r.IsEmptyElement)
                    {
                        result.Add(string.Empty);
                    }
                    else if (r.LocalName == "si")
                    {
                        current = new StringBuilder();
                    }
                    else if (r.LocalName == "t" && current != null)
                    {
                        // Rich text runs (<r><t>) are concatenated in order.
                        current.Append(r.ReadElementContentAsString());
                        continue;
                    }
                    else if (r.LocalName == "rPh")
                    {
                        // Phonetic hints are not part of the displayed text.
                        r.Skip();
                        continue;
                    }
                }
                else if (r.NodeType == XmlNodeType.EndElement && r.LocalName == "si" && current != null)
                {
                    result.Add(current.ToString());
                    current = null;
                }

                r.Read();
            }

            return result;
        }

        private static Dictionary<string, string> ReadWorkbookRelationships(ZipArchive zip)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            using XmlReader r = OpenXml(zip, "xl/_rels/workbook.xml.rels");
            while (r.Read())
            {
                if (r.NodeType == XmlNodeType.Element && r.LocalName == "Relationship" && r.NamespaceURI == PackageRelNs)
                {
                    string? id = r.GetAttribute("Id");
                    string? target = r.GetAttribute("Target");
                    if (id != null && target != null)
                    {
                        map[id] = NormalisePartName(target);
                    }
                }
            }

            return map;
        }

        // Relationship targets are relative to xl/ (or absolute from the package root).
        // Anything resolving outside xl/ is rejected.
        internal static string NormalisePartName(string target)
        {
            string path = target.Replace('\\', '/');
            path = path.StartsWith('/') ? path.TrimStart('/') : "xl/" + path;

            var segments = new List<string>();
            foreach (string segment in path.Split('/'))
            {
                if (segment.Length == 0 || segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (segments.Count == 0)
                    {
                        throw new InvalidDataException("Relationship target escapes the package: " + target);
                    }

                    segments.RemoveAt(segments.Count - 1);
                    continue;
                }

                segments.Add(segment);
            }

            string normalised = string.Join('/', segments);
            if (!normalised.StartsWith("xl/", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Relationship target outside xl/: " + target);
            }

            return normalised;
        }

        private static Dictionary<string, string> ReadSheet(ZipArchive zip, string part, List<string> sharedStrings)
        {
            var cells = new Dictionary<string, string>(StringComparer.Ordinal);
            using XmlReader r = OpenXml(zip, part);
            while (r.Read())
            {
                if (r.NodeType != XmlNodeType.Element || r.LocalName != "c" || r.NamespaceURI != MainNs)
                {
                    continue;
                }

                string? reference = r.GetAttribute("r");
                string? type = r.GetAttribute("t");
                if (reference is null || r.IsEmptyElement)
                {
                    continue;
                }

                string? value = ReadCellValue(r.ReadSubtree(), type, sharedStrings);
                if (!string.IsNullOrEmpty(value))
                {
                    cells[reference] = value;
                }
            }

            return cells;
        }

        private static string? ReadCellValue(XmlReader cell, string? type, List<string> sharedStrings)
        {
            using (cell)
            {
                var inline = new StringBuilder();
                string? raw = null;
                cell.Read();
                while (!cell.EOF)
                {
                    if (cell.NodeType == XmlNodeType.Element && cell.NamespaceURI == MainNs)
                    {
                        if (cell.LocalName == "v")
                        {
                            raw = cell.ReadElementContentAsString();
                            continue;
                        }

                        if (cell.LocalName == "t" && type == "inlineStr")
                        {
                            inline.Append(cell.ReadElementContentAsString());
                            continue;
                        }
                    }

                    cell.Read();
                }

                if (type == "inlineStr")
                {
                    return inline.ToString();
                }

                if (type == "s")
                {
                    if (!int.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int index)
                        || index < 0 || index >= sharedStrings.Count)
                    {
                        throw new InvalidDataException("Invalid shared string index '" + raw + "'.");
                    }

                    return sharedStrings[index];
                }

                return raw;
            }
        }

        private static XmlReader OpenXml(ZipArchive zip, string partName)
        {
            ZipArchiveEntry entry = zip.GetEntry(partName) ?? throw new InvalidDataException("Missing part " + partName + ".");
            if (entry.Length > MaxEntryBytes)
            {
                throw new InvalidDataException("Part " + partName + " is too large.");
            }

            if (entry.CompressedLength > 0 && (double)entry.Length / entry.CompressedLength > MaxCompressionRatio)
            {
                throw new InvalidDataException("Part " + partName + " has a suspicious compression ratio.");
            }

            // Copy into a bounded buffer: entry.Length comes from the archive header and could lie.
            var buffer = new MemoryStream();
            using (Stream s = entry.Open())
            {
                CopyBounded(s, buffer, MaxEntryBytes);
            }

            buffer.Position = 0;
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersFromEntities = 0,
                CloseInput = true,
            };
            return XmlReader.Create(buffer, settings);
        }

        private static void CopyBounded(Stream source, Stream destination, long maxBytes)
        {
            byte[] chunk = new byte[81920];
            long total = 0;
            int read;
            while ((read = source.Read(chunk, 0, chunk.Length)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    throw new InvalidDataException("Archive entry exceeds the size limit.");
                }

                destination.Write(chunk, 0, read);
            }
        }
    }
}
