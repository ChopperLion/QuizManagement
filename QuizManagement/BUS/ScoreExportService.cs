using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace QuizManagement.BUS
{
    public static class ScoreExportService
    {
        public sealed class ScoreExportRow
        {
            public string FullName { get; set; }
            public string ClassName { get; set; }
            public string ExamName { get; set; }
            public string SubjectName { get; set; }
            public string ScoreText { get; set; }
            public string SubmitDateText { get; set; }
        }

        public static void ExportClassScoresXlsx(string fileName, IList<ScoreExportRow> rows)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Create, false))
            {
                WriteZipEntry(zip, "[Content_Types].xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                    "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                    "<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>" +
                    "<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>" +
                    "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
                    "</Types>");

                WriteZipEntry(zip, "_rels/.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                    "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
                    "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/>" +
                    "</Relationships>");

                WriteZipEntry(zip, "docProps/core.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">" +
                    "<dc:title>Bảng điểm lớp</dc:title>" +
                    "<dc:creator>QuizManagement</dc:creator>" +
                    "<cp:lastModifiedBy>QuizManagement</cp:lastModifiedBy>" +
                    "</cp:coreProperties>");

                WriteZipEntry(zip, "docProps/app.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\">" +
                    "<Application>QuizManagement</Application>" +
                    "</Properties>");

                WriteZipEntry(zip, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    "<sheets><sheet name=\"BangDiem\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                    "</workbook>");

                WriteZipEntry(zip, "xl/_rels/workbook.xml.rels",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                    "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                    "</Relationships>");

                WriteZipEntry(zip, "xl/styles.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                    "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Segoe UI\"/></font></fonts>" +
                    "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                    "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                    "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                    "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
                    "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                    "</styleSheet>");

                WriteZipEntry(zip, "xl/worksheets/sheet1.xml", BuildSheetXml(rows));
            }
        }

        private static string BuildSheetXml(IList<ScoreExportRow> rows)
        {
            string[][] data = BuildRows(rows);
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>");
            sb.Append("<sheetFormatPr defaultRowHeight=\"18\"/>");
            sb.Append("<cols>");
            int[] widths = { 28, 18, 28, 24, 12, 22 };
            for (int i = 0; i < widths.Length; i++)
            {
                sb.Append("<col min=\"").Append(i + 1).Append("\" max=\"").Append(i + 1)
                    .Append("\" width=\"").Append(widths[i].ToString(CultureInfo.InvariantCulture))
                    .Append("\" customWidth=\"1\"/>");
            }
            sb.Append("</cols>");
            sb.Append("<sheetData>");

            for (int r = 0; r < data.Length; r++)
            {
                sb.Append("<row r=\"").Append(r + 1).Append("\">");
                for (int c = 0; c < data[r].Length; c++)
                {
                    sb.Append("<c r=\"").Append(ColumnName(c)).Append(r + 1).Append("\" t=\"inlineStr\"><is><t>")
                        .Append(EscapeXml(data[r][c]))
                        .Append("</t></is></c>");
                }
                sb.Append("</row>");
            }

            sb.Append("</sheetData>");
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private static string[][] BuildRows(IList<ScoreExportRow> rows)
        {
            var allRows = new List<string[]>
            {
                new[] { "Họ tên", "Lớp", "Tên bài thi", "Môn / nhóm đề", "Điểm số", "Thời gian nộp" }
            };

            if (rows != null)
            {
                foreach (ScoreExportRow row in rows)
                {
                    allRows.Add(new[]
                    {
                        row.FullName ?? "",
                        row.ClassName ?? "",
                        row.ExamName ?? "",
                        row.SubjectName ?? "",
                        row.ScoreText ?? "",
                        row.SubmitDateText ?? ""
                    });
                }
            }

            return allRows.ToArray();
        }

        private static string ColumnName(int index)
        {
            int value = index + 1;
            var sb = new StringBuilder();
            while (value > 0)
            {
                int mod = (value - 1) % 26;
                sb.Insert(0, (char)('A' + mod));
                value = (value - 1) / 26;
            }
            return sb.ToString();
        }

        private static string EscapeXml(string text)
        {
            return SecurityElement.Escape(text ?? "") ?? "";
        }

        private static void WriteZipEntry(ZipArchive zip, string entryName, string content)
        {
            ZipArchiveEntry entry = zip.CreateEntry(entryName);
            using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }
    }
}
