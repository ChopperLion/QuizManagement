using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using QuizManagement.DAL;

namespace QuizManagement.BUS
{
    public class QuestionImportResult
    {
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int Imported { get; set; }
        public int Linked { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; } = new List<string>();
    }

    public class QuestionImportService
    {
        private readonly Database db = new Database();

        public QuestionImportResult PreviewImport(string fileName)
        {
            List<Dictionary<string, string>> rows = ReadRows(fileName);
            var result = new QuestionImportResult();
            AnalyzeRows(rows, result, null);
            return result;
        }

        public QuestionImportResult Import(string fileName, int ownerUserId, int? linkExamId)
        {
            Database.EnsureQuestionPointsColumn();
            Database.EnsureTeacherOwnershipColumns();
            Database.EnsureExamAccessColumns();
            Database.EnsureAdvancedExamColumns();
            Database.EnsureQuestionTextColumns();

            List<Dictionary<string, string>> rows = ReadRows(fileName);
            var result = new QuestionImportResult();
            var validItems = new List<QuestionImportItem>();
            AnalyzeRows(rows, result, validItems);

            ImportValidItems(validItems, ownerUserId, linkExamId, result);
            return result;
        }

        public static string BuildGuide()
        {
            return "Bạn có thể nhập câu hỏi từ file Excel (.xlsx) hoặc file văn bản (.csv, .txt, .tsv)." + Environment.NewLine +
                "Cách dễ nhất là bấm tải file mẫu rồi điền theo đúng các cột có sẵn." + Environment.NewLine +
                "Các cột nên có: Câu hỏi, A, B, C, D, Đáp án, Nội dung đáp án, Loại câu, Điểm." + Environment.NewLine +
                "Loại câu hỗ trợ: Trắc nghiệm hoặc Trả lời ngắn/Điền khuyết." + Environment.NewLine +
                "Với câu trả lời ngắn/điền khuyết, bạn có thể để trống A/B/C/D và nhập đáp án vào cột Nội dung đáp án." + Environment.NewLine +
                "Nếu dùng CSV/TXT, mỗi cột nên ngăn cách bằng dấu chấm phẩy (;).";
        }

        public static void CreateTemplateFile(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".xlsx")
            {
                CreateXlsxTemplate(fileName);
                return;
            }

            CreateDelimitedTemplate(fileName, ext == ".tsv" ? '\t' : ';');
        }

        private void ImportValidItems(List<QuestionImportItem> items, int ownerUserId, int? linkExamId, QuestionImportResult result)
        {
            if (items.Count == 0) return;

            using (SqlConnection conn = db.GetConnection())
            {
                conn.Open();
                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        foreach (QuestionImportItem item in items)
                        {
                            int questionId = FindExistingQuestionId(conn, tx, item, ownerUserId);
                            if (questionId <= 0)
                                questionId = InsertQuestion(conn, tx, item, ownerUserId);

                            result.Imported++;

                            if (linkExamId.HasValue && questionId > 0 && !ExamQuestionExists(conn, tx, linkExamId.Value, questionId))
                            {
                                InsertExamQuestion(conn, tx, linkExamId.Value, questionId, item.Points);
                                result.Linked++;
                            }
                        }

                        tx.Commit();
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        result.Skipped += items.Count;
                        result.Imported = 0;
                        result.Linked = 0;
                        result.Errors.Clear();
                        result.Errors.Add("Import da bi huy de tranh du lieu do dang. Loi: " + ex.Message);
                    }
                }
            }
        }

        private void AnalyzeRows(List<Dictionary<string, string>> rows, QuestionImportResult result, List<QuestionImportItem> validItems)
        {
            result.TotalRows = rows.Count;
            int line = 1;
            foreach (Dictionary<string, string> row in rows)
            {
                line++;
                QuestionImportItem item = MapRow(row);
                item.Line = line;

                if (string.IsNullOrWhiteSpace(item.Question) && row.Values.All(string.IsNullOrWhiteSpace))
                {
                    result.Skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Question) || string.IsNullOrWhiteSpace(item.CorrectAnswer))
                {
                    result.Skipped++;
                    AddError(result, "Dong " + line + ": thieu noi dung cau hoi hoac dap an dung.");
                    continue;
                }

                if (!item.IsFillBlank && (string.IsNullOrWhiteSpace(item.A) || string.IsNullOrWhiteSpace(item.B) ||
                    string.IsNullOrWhiteSpace(item.C) || string.IsNullOrWhiteSpace(item.D)))
                {
                    result.Skipped++;
                    AddError(result, "Dong " + line + ": cau trac nghiem thieu dap an A/B/C/D.");
                    continue;
                }

                result.ValidRows++;
                if (validItems != null)
                    validItems.Add(item);
            }
        }

        private int InsertQuestion(SqlConnection conn, SqlTransaction tx, QuestionImportItem item, int ownerUserId)
        {
            string ownerColumn = ownerUserId > 0 ? ",CreatedByUserID" : "";
            string ownerValue = ownerUserId > 0 ? ",@CreatedByUserID" : "";

            using (var cmd = new SqlCommand(
                "INSERT INTO Questions (QuestionContent,OptionA,OptionB,OptionC,OptionD,CorrectAnswer,Points,QuestionType" + ownerColumn + ") " +
                "OUTPUT INSERTED.QuestionID VALUES (@QuestionContent,@OptionA,@OptionB,@OptionC,@OptionD,@CorrectAnswer,@Points,@QuestionType" + ownerValue + ")", conn, tx))
            {
                cmd.Parameters.Add("@QuestionContent", SqlDbType.NVarChar, -1).Value = item.Question;
                cmd.Parameters.Add("@OptionA", SqlDbType.NVarChar, -1).Value = (object)item.A ?? DBNull.Value;
                cmd.Parameters.Add("@OptionB", SqlDbType.NVarChar, -1).Value = (object)item.B ?? DBNull.Value;
                cmd.Parameters.Add("@OptionC", SqlDbType.NVarChar, -1).Value = (object)item.C ?? DBNull.Value;
                cmd.Parameters.Add("@OptionD", SqlDbType.NVarChar, -1).Value = (object)item.D ?? DBNull.Value;
                cmd.Parameters.Add("@CorrectAnswer", SqlDbType.NVarChar, -1).Value = item.CorrectAnswer;
                SqlParameter points = cmd.Parameters.Add("@Points", SqlDbType.Decimal);
                points.Precision = 6;
                points.Scale = 2;
                points.Value = item.Points.HasValue ? (object)item.Points.Value : DBNull.Value;
                cmd.Parameters.Add("@QuestionType", SqlDbType.NVarChar, 20).Value = item.IsFillBlank ? "FillBlank" : "MultipleChoice";
                if (ownerUserId > 0)
                    cmd.Parameters.Add("@CreatedByUserID", SqlDbType.Int).Value = ownerUserId;

                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private int FindExistingQuestionId(SqlConnection conn, SqlTransaction tx, QuestionImportItem item, int ownerUserId)
        {
            string ownerFilter = ownerUserId > 0 ? " AND CreatedByUserID=@OwnerUserID" : "";
            using (var cmd = new SqlCommand(
                "SELECT QuestionID, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, " +
                "CASE WHEN COL_LENGTH('Questions','QuestionType') IS NULL THEN N'MultipleChoice' ELSE QuestionType END AS QuestionType " +
                "FROM Questions WHERE QuestionContent=@QuestionContent" + ownerFilter + " ORDER BY QuestionID DESC", conn, tx))
            {
                cmd.Parameters.Add("@QuestionContent", SqlDbType.NVarChar, -1).Value = item.Question;
                if (ownerUserId > 0)
                    cmd.Parameters.Add("@OwnerUserID", SqlDbType.Int).Value = ownerUserId;

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bool candidateFillBlank = string.Equals(reader["QuestionType"].ToString(), "FillBlank", StringComparison.OrdinalIgnoreCase);
                        if (candidateFillBlank != item.IsFillBlank)
                            continue;

                        string candidateCorrect = SafeDbText(reader["CorrectAnswer"]);
                        if (!SameValue(candidateCorrect, item.CorrectAnswer))
                            continue;

                        if (!item.IsFillBlank)
                        {
                            if (!SameValue(SafeDbText(reader["OptionA"]), item.A) ||
                                !SameValue(SafeDbText(reader["OptionB"]), item.B) ||
                                !SameValue(SafeDbText(reader["OptionC"]), item.C) ||
                                !SameValue(SafeDbText(reader["OptionD"]), item.D))
                                continue;
                        }

                        return Convert.ToInt32(reader["QuestionID"]);
                    }
                }

                return 0;
            }
        }

        private bool ExamQuestionExists(SqlConnection conn, SqlTransaction tx, int examId, int questionId)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM ExamQuestions WHERE ExamID=@ExamID AND QuestionID=@QuestionID", conn, tx))
            {
                cmd.Parameters.Add("@ExamID", SqlDbType.Int).Value = examId;
                cmd.Parameters.Add("@QuestionID", SqlDbType.Int).Value = questionId;
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private void InsertExamQuestion(SqlConnection conn, SqlTransaction tx, int examId, int questionId, decimal? points)
        {
            using (var cmd = new SqlCommand("INSERT INTO ExamQuestions (ExamID,QuestionID,Points) VALUES (@ExamID,@QuestionID,@Points)", conn, tx))
            {
                cmd.Parameters.Add("@ExamID", SqlDbType.Int).Value = examId;
                cmd.Parameters.Add("@QuestionID", SqlDbType.Int).Value = questionId;
                SqlParameter pointParam = cmd.Parameters.Add("@Points", SqlDbType.Decimal);
                pointParam.Precision = 6;
                pointParam.Scale = 2;
                pointParam.Value = points.HasValue ? (object)points.Value : 1M;
                cmd.ExecuteNonQuery();
            }
        }

        private QuestionImportItem MapRow(Dictionary<string, string> row)
        {
            string type = Get(row, "loai", "loai cau", "loai cau hoi", "questiontype", "type");
            bool fill = IsFillBlankType(type);
            string answerText = Get(row, "noi dung dap an", "dap an noi dung", "answer text");
            string correct = Get(row, "dap an", "dap an dung", "correct", "correctanswer");

            if (fill && !string.IsNullOrWhiteSpace(answerText))
                correct = answerText;

            decimal pointsValue;
            decimal? points = decimal.TryParse(Get(row, "diem", "so diem", "points").Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out pointsValue)
                ? (decimal?)pointsValue
                : null;

            var item = new QuestionImportItem
            {
                Question = Get(row, "cau hoi", "noi dung", "noi dung cau hoi", "question", "questioncontent"),
                A = fill ? "" : Get(row, "a", "optiona", "dap an a"),
                B = fill ? "" : Get(row, "b", "optionb", "dap an b"),
                C = fill ? "" : Get(row, "c", "optionc", "dap an c"),
                D = fill ? "" : Get(row, "d", "optiond", "dap an d"),
                CorrectAnswer = fill ? correct.Trim() : correct.Trim().ToUpperInvariant(),
                IsFillBlank = fill,
                Points = points
            };
            RecoverMergedOptions(item);
            return item;
        }

        private static void RecoverMergedOptions(QuestionImportItem item)
        {
            if (item.IsFillBlank) return;
            if (string.IsNullOrWhiteSpace(item.D))
                SplitOptionSuffix(ref item.C, ref item.D, "D");
            if (string.IsNullOrWhiteSpace(item.C))
                SplitOptionSuffix(ref item.B, ref item.C, "C");
            if (string.IsNullOrWhiteSpace(item.B))
                SplitOptionSuffix(ref item.A, ref item.B, "B");
        }

        private static void SplitOptionSuffix(ref string source, ref string target, string label)
        {
            if (string.IsNullOrWhiteSpace(source) || !string.IsNullOrWhiteSpace(target)) return;
            Match m = Regex.Match(source, @"\s+" + Regex.Escape(label) + @"[\.\)]\s+", RegexOptions.IgnoreCase);
            if (!m.Success) return;
            target = source.Substring(m.Index + m.Length).Trim();
            source = source.Substring(0, m.Index).Trim();
        }

        private List<Dictionary<string, string>> ReadRows(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (ext == ".xlsx") return ReadXlsxRows(fileName);
            if (ext == ".csv" || ext == ".txt" || ext == ".tsv") return ReadTextRows(fileName);
            throw new InvalidOperationException("Chi ho tro file .xlsx, .csv, .txt hoac .tsv.");
        }

        private static void CreateDelimitedTemplate(string fileName, char separator)
        {
            string sep = separator.ToString();
            string[] headers = new[]
            {
                "Cau hoi", "A", "B", "C", "D", "Dap an", "Noi dung dap an", "Loai cau", "Diem"
            };
            string[] row1 = new[]
            {
                "Thu do cua Viet Nam la gi?",
                "Ha Noi",
                "Da Nang",
                "Hue",
                "Can Tho",
                "A",
                "",
                "Trac nghiem",
                "1"
            };
            string[] row2 = new[]
            {
                "2 + 2 = ?",
                "",
                "",
                "",
                "",
                "",
                "4",
                "Tra loi ngan",
                "1"
            };

            var lines = new[]
            {
                string.Join(sep, headers),
                string.Join(sep, row1),
                string.Join(sep, row2)
            };
            File.WriteAllLines(fileName, lines, new UTF8Encoding(true));
        }

        private static void CreateXlsxTemplate(string fileName)
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
                    "<cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:dcmitype=\"http://purl.org/dc/dcmitype/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
                    "<dc:title>Question Import Template</dc:title>" +
                    "<dc:creator>QuizManagement</dc:creator>" +
                    "<cp:lastModifiedBy>QuizManagement</cp:lastModifiedBy>" +
                    "</cp:coreProperties>");

                WriteZipEntry(zip, "docProps/app.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\" xmlns:vt=\"http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes\">" +
                    "<Application>QuizManagement</Application>" +
                    "</Properties>");

                WriteZipEntry(zip, "xl/workbook.xml",
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                    "<sheets><sheet name=\"MauNhapCauHoi\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
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
                    "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                    "<fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills>" +
                    "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                    "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                    "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
                    "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                    "</styleSheet>");

                WriteZipEntry(zip, "xl/worksheets/sheet1.xml", BuildTemplateSheetXml());
            }
        }

        private static string BuildTemplateSheetXml()
        {
            string[][] rows = new[]
            {
                new[] { "Cau hoi", "A", "B", "C", "D", "Dap an", "Noi dung dap an", "Loai cau", "Diem" },
                new[] { "Thu do cua Viet Nam la gi?", "Ha Noi", "Da Nang", "Hue", "Can Tho", "A", "", "Trac nghiem", "1" },
                new[] { "2 + 2 = ?", "", "", "", "", "", "4", "Tra loi ngan", "1" }
            };

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"/></sheetViews>");
            sb.Append("<sheetFormatPr defaultRowHeight=\"15\"/>");
            sb.Append("<cols>");
            for (int i = 1; i <= 9; i++)
                sb.Append("<col min=\"").Append(i).Append("\" max=\"").Append(i).Append("\" width=\"20\" customWidth=\"1\"/>");
            sb.Append("</cols>");
            sb.Append("<sheetData>");

            for (int r = 0; r < rows.Length; r++)
            {
                sb.Append("<row r=\"").Append(r + 1).Append("\">");
                for (int c = 0; c < rows[r].Length; c++)
                {
                    sb.Append("<c r=\"").Append(ColumnName(c)).Append(r + 1).Append("\" t=\"inlineStr\"><is><t>")
                        .Append(EscapeXml(rows[r][c]))
                        .Append("</t></is></c>");
                }
                sb.Append("</row>");
            }

            sb.Append("</sheetData>");
            sb.Append("</worksheet>");
            return sb.ToString();
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
                writer.Write(content);
        }

        private List<Dictionary<string, string>> ReadTextRows(string fileName)
        {
            char sep = Path.GetExtension(fileName).Equals(".tsv", StringComparison.OrdinalIgnoreCase) ? '\t' : ';';
            var lines = File.ReadAllLines(fileName, Encoding.UTF8)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                .ToList();
            if (lines.Count == 0) return new List<Dictionary<string, string>>();

            string[] first = SplitLine(lines[0], sep);
            bool hasHeader = first.Any(c => Normalize(c).Contains("cauhoi") || Normalize(c).Contains("question"));
            string[] headers = hasHeader ? first : new[] { "Loai", "Cau hoi", "A", "B", "C", "D", "Dap an", "Diem" };
            int start = hasHeader ? 1 : 0;

            var rows = new List<Dictionary<string, string>>();
            for (int i = start; i < lines.Count; i++)
            {
                string[] values = SplitLine(lines[i], sep);
                var row = new Dictionary<string, string>();
                for (int c = 0; c < headers.Length && c < values.Length; c++)
                    row[Normalize(headers[c])] = values[c].Trim();
                rows.Add(row);
            }
            return rows;
        }

        private List<Dictionary<string, string>> ReadXlsxRows(string fileName)
        {
            using (ZipArchive zip = ZipFile.OpenRead(fileName))
            {
                List<string> shared = ReadSharedStrings(zip);
                ZipArchiveEntry sheet = zip.Entries.FirstOrDefault(e =>
                    e.FullName.StartsWith("xl/worksheets/sheet", StringComparison.OrdinalIgnoreCase) &&
                    e.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
                if (sheet == null) throw new InvalidOperationException("Khong tim thay sheet trong file Excel.");

                List<List<string>> rows = ReadSheetRows(sheet, shared);
                if (rows.Count == 0) return new List<Dictionary<string, string>>();

                string[] headers = rows[0].Select(Normalize).ToArray();
                var result = new List<Dictionary<string, string>>();
                for (int r = 1; r < rows.Count; r++)
                {
                    var dict = new Dictionary<string, string>();
                    for (int c = 0; c < headers.Length && c < rows[r].Count; c++)
                    {
                        if (!string.IsNullOrWhiteSpace(headers[c]))
                            dict[headers[c]] = rows[r][c];
                    }
                    result.Add(dict);
                }
                return result;
            }
        }

        private List<string> ReadSharedStrings(ZipArchive zip)
        {
            var values = new List<string>();
            ZipArchiveEntry entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null) return values;

            XmlDocument doc = LoadXml(entry);
            XmlNamespaceManager ns = CreateNs(doc);
            foreach (XmlNode si in doc.SelectNodes("//a:si", ns))
            {
                var sb = new StringBuilder();
                foreach (XmlNode t in si.SelectNodes(".//a:t", ns))
                    sb.Append(t.InnerText);
                values.Add(sb.ToString());
            }
            return values;
        }

        private List<List<string>> ReadSheetRows(ZipArchiveEntry sheet, List<string> shared)
        {
            XmlDocument doc = LoadXml(sheet);
            XmlNamespaceManager ns = CreateNs(doc);
            var result = new List<List<string>>();
            foreach (XmlNode row in doc.SelectNodes("//a:sheetData/a:row", ns))
            {
                var values = new SortedDictionary<int, string>();
                foreach (XmlNode cell in row.SelectNodes("a:c", ns))
                {
                    int col = ColumnIndex(cell.Attributes["r"]?.Value);
                    string value = "";
                    XmlNode inline = cell.SelectSingleNode("a:is/a:t", ns);
                    XmlNode v = cell.SelectSingleNode("a:v", ns);
                    if (inline != null) value = inline.InnerText;
                    else if (v != null) value = v.InnerText;
                    if (cell.Attributes["t"]?.Value == "s" && int.TryParse(value, out int idx) && idx >= 0 && idx < shared.Count)
                        value = shared[idx];
                    values[col] = value;
                }

                int max = values.Count == 0 ? -1 : values.Keys.Max();
                var list = new List<string>();
                for (int i = 0; i <= max; i++)
                    list.Add(values.ContainsKey(i) ? values[i] : "");
                result.Add(list);
            }
            return result;
        }

        private static string[] SplitLine(string line, char sep)
        {
            return line.Split(sep);
        }

        private static XmlDocument LoadXml(ZipArchiveEntry entry)
        {
            var doc = new XmlDocument();
            using (Stream s = entry.Open())
                doc.Load(s);
            return doc;
        }

        private static XmlNamespaceManager CreateNs(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("a", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            return ns;
        }

        private static int ColumnIndex(string cellRef)
        {
            if (string.IsNullOrWhiteSpace(cellRef)) return 0;
            Match m = Regex.Match(cellRef, "^[A-Z]+", RegexOptions.IgnoreCase);
            int sum = 0;
            foreach (char ch in m.Value.ToUpperInvariant())
                sum = sum * 26 + (ch - 'A' + 1);
            return Math.Max(0, sum - 1);
        }

        private static bool IsFillBlankType(string type)
        {
            string n = Normalize(type);
            return n.Contains("fill") || n.Contains("dienkhuyet") || n.Contains("traloingan") || n.Contains("tuluan") || n.Contains("shortanswer");
        }

        private static string Get(Dictionary<string, string> row, params string[] keys)
        {
            foreach (string key in keys)
            {
                string normalized = Normalize(key);
                if (row.TryGetValue(normalized, out string value))
                    return value ?? "";
            }
            return "";
        }

        private static string Normalize(string text)
        {
            string value = (text ?? "").Trim().ToLowerInvariant().Replace('\u0111', 'd').Replace('\u0110', 'D');
            string normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9]+", "");
        }

        public static string BuildReadFileErrorMessage(Exception ex, string fileName)
        {
            string safeName = Path.GetFileName(fileName ?? "");
            string raw = ex == null ? "" : ex.Message ?? "";
            bool locked = ex is IOException ||
                raw.IndexOf("another process", StringComparison.OrdinalIgnoreCase) >= 0 ||
                raw.IndexOf("being used", StringComparison.OrdinalIgnoreCase) >= 0;

            if (locked)
            {
                return "Không thể đọc file \"" + safeName + "\" vì file đang được mở ở Excel hoặc đang bị chương trình khác sử dụng." + Environment.NewLine +
                    "Vui lòng đóng file đó rồi thử nhập lại." + Environment.NewLine + Environment.NewLine +
                    "Nếu vẫn chưa được, hãy thử lưu file với tên khác rồi nhập lại.";
            }

            return "Không thể đọc file câu hỏi." + Environment.NewLine + raw;
        }

        private static string SafeDbText(object value)
        {
            return value == null || value == DBNull.Value ? "" : value.ToString().Trim();
        }

        private static bool SameValue(string left, string right)
        {
            return string.Equals((left ?? "").Trim(), (right ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static void AddError(QuestionImportResult result, string message)
        {
            if (result.Errors.Count < 5)
                result.Errors.Add(message);
        }

        private class QuestionImportItem
        {
            public int Line;
            public string Question;
            public string A;
            public string B;
            public string C;
            public string D;
            public string CorrectAnswer;
            public bool IsFillBlank;
            public decimal? Points;
        }
    }
}
