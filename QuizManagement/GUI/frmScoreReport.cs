using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using QuizManagement.BUS;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public sealed class frmScoreReport : Form
    {
        private readonly Database db = new Database();
        private readonly int ownerUserId;

        private readonly Color pageBg = UiTheme.PageBg;
        private readonly Color cardBg = UiTheme.SurfaceRaised;
        private readonly Color cardBorder = UiTheme.Border;
        private readonly Color textMain = UiTheme.TextPrimary;
        private readonly Color textSub = UiTheme.TextSecondary;
        private readonly Color blue = UiTheme.Primary;
        private readonly Color green = UiTheme.Success;
        private readonly Color orange = UiTheme.Warning;

        private ComboBox cboExam;
        private ComboBox cboClass;
        private TextBox txtKeyword;
        private DateTimePicker dtFrom;
        private DateTimePicker dtTo;
        private CheckBox chkUseDate;
        private DataGridView dgvScores;
        private Panel pnlGridHost;
        private Label lblSummary;
        private Label lblStatAttempts;
        private Label lblStatClasses;
        private Label lblStatAverage;
        private Label lblEmptyState;

        private DataTable currentTable = new DataTable();

        public frmScoreReport(int ownerUserId)
        {
            this.ownerUserId = ownerUserId;

            Text = "Bảng điểm học sinh";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1180, 720);
            MinimumSize = new Size(1040, 640);
            BackColor = pageBg;
            Font = new Font("Segoe UI", 9.5F);

            InitUi();
            Load += FrmScoreReport_Load;
        }

        private void FrmScoreReport_Load(object sender, EventArgs e)
        {
            Database.EnsureStudentAssessmentColumns();
            LoadExamOptions();
            LoadClassOptions();
            ResetDates();
            LoadScores();
        }

        private void InitUi()
        {
            SuspendLayout();

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                BackColor = cardBg,
                Padding = new Padding(28, 18, 28, 16)
            };
            Controls.Add(header);

            var lblTitle = new Label
            {
                Text = "BẢNG ĐIỂM HỌC SINH",
                AutoSize = true,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = textMain,
                Location = new Point(0, 0)
            };
            header.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Theo dõi kết quả theo lớp, bài thi và xuất nhanh danh sách điểm để thông báo cho cả lớp.",
                AutoSize = true,
                Font = new Font("Segoe UI", 10F),
                ForeColor = textSub,
                Location = new Point(2, 42)
            };
            header.Controls.Add(lblSubtitle);

            var body = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = pageBg,
                Padding = new Padding(20, 16, 20, 18)
            };
            Controls.Add(body);

            var statsWrap = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 92,
                ColumnCount = 3,
                BackColor = pageBg
            };
            statsWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            statsWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            statsWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));

            statsWrap.Controls.Add(CreateStatCard("Lượt làm bài", "0", blue, out lblStatAttempts), 0, 0);
            statsWrap.Controls.Add(CreateStatCard("Số lớp xuất hiện", "0", green, out lblStatClasses), 1, 0);
            statsWrap.Controls.Add(CreateStatCard("Điểm trung bình", "0.00", orange, out lblStatAverage), 2, 0);

            var filterCard = CreateCard();
            filterCard.Dock = DockStyle.Top;
            filterCard.Height = 186;
            filterCard.Padding = new Padding(22, 18, 22, 18);
            filterCard.Margin = new Padding(0, 14, 0, 0);

            filterCard.Controls.Add(new Label
            {
                Text = "BỘ LỌC BẢNG ĐIỂM",
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = blue,
                Location = new Point(0, 0)
            });

            filterCard.Controls.Add(new Label
            {
                Text = "Chọn nhanh phạm vi cần xem, sau đó có thể xuất ra file Excel ngay ở cùng màn hình.",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.2F),
                ForeColor = textSub,
                Location = new Point(2, 28)
            });

            var lblKeyword = CreateFieldLabel("Từ khóa", new Point(0, 62));
            filterCard.Controls.Add(lblKeyword);
            txtKeyword = CreateTextBox(new Point(0, 86), 290, "Nhập họ tên, lớp hoặc tên bài thi...");
            filterCard.Controls.Add(txtKeyword);

            var lblExam = CreateFieldLabel("Bài thi", new Point(308, 62));
            filterCard.Controls.Add(lblExam);
            cboExam = CreateCombo(new Point(308, 86), 250);
            filterCard.Controls.Add(cboExam);

            var lblClass = CreateFieldLabel("Lớp", new Point(576, 62));
            filterCard.Controls.Add(lblClass);
            cboClass = CreateCombo(new Point(576, 86), 180);
            filterCard.Controls.Add(cboClass);

            chkUseDate = new CheckBox
            {
                Text = "Lọc theo ngày nộp",
                AutoSize = true,
                ForeColor = textMain,
                Location = new Point(0, 132)
            };
            chkUseDate.CheckedChanged += (s, e) => ToggleDateInputs();
            filterCard.Controls.Add(chkUseDate);

            dtFrom = CreateDatePicker(new Point(150, 128));
            dtTo = CreateDatePicker(new Point(316, 128));
            filterCard.Controls.Add(dtFrom);
            filterCard.Controls.Add(dtTo);

            var btnReset = CreateButton("Đặt lại", new Point(760, 82), 120, Color.White, textMain);
            btnReset.Click += (s, e) => LoadScores();
            filterCard.Controls.Add(btnReset);

            var btnFilter = CreateButton("Lọc dữ liệu", new Point(892, 82), 126, blue, Color.White);
            btnFilter.Click += (s, e) => LoadScores();
            filterCard.Controls.Add(btnFilter);

            var btnExport = CreateButton("Xuất Excel", new Point(1030, 82), 120, green, Color.White);
            btnExport.Click += BtnExport_Click;
            filterCard.Controls.Add(btnExport);

            var tableCard = CreateCard();
            tableCard.Dock = DockStyle.Fill;
            tableCard.Padding = new Padding(18, 16, 18, 18);
            tableCard.Margin = new Padding(0, 14, 0, 0);
            body.Controls.Add(tableCard);
            body.Controls.Add(filterCard);
            body.Controls.Add(statsWrap);

            var lblTableTitle = new Label
            {
                Text = "DANH SÁCH KẾT QUẢ",
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = blue,
                Location = new Point(0, 0)
            };
            tableCard.Controls.Add(lblTableTitle);

            lblSummary = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.2F),
                ForeColor = textSub,
                Location = new Point(2, 28)
            };
            tableCard.Controls.Add(lblSummary);

            pnlGridHost = new Panel
            {
                Location = new Point(0, 58),
                Size = new Size(0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Padding = new Padding(0),
                BorderStyle = BorderStyle.FixedSingle
            };
            tableCard.Controls.Add(pnlGridHost);

            dgvScores = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 42,
                RowTemplate = { Height = 38 },
                GridColor = cardBorder,
                ScrollBars = ScrollBars.Vertical
            };
            UiTheme.ApplyGrid(dgvScores);
            dgvScores.CellFormatting += DgvScores_CellFormatting;
            dgvScores.CellDoubleClick += DgvScores_CellDoubleClick;
            pnlGridHost.Controls.Add(dgvScores);

            lblEmptyState = new Label
            {
                Text = "Chua co du lieu phu hop voi bo loc hien tai.\r\nHay doi bai thi, lop hoac khoang ngay de xem lai ket qua.",
                Size = new Size(520, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = textSub,
                Visible = false
            };
            tableCard.Controls.Add(lblEmptyState);

            tableCard.Resize += (s, e) =>
            {
                int contentWidth = Math.Max(0, tableCard.ClientSize.Width - 36);
                int contentHeight = Math.Max(260, tableCard.ClientSize.Height - 76);

                if (pnlGridHost != null)
                {
                    pnlGridHost.Location = new Point(0, 58);
                    pnlGridHost.Size = new Size(contentWidth, contentHeight);
                }

                if (lblEmptyState != null)
                {
                    lblEmptyState.Location = new Point(
                        Math.Max(0, (tableCard.ClientSize.Width - lblEmptyState.Width) / 2),
                        Math.Max(84, (tableCard.ClientSize.Height - lblEmptyState.Height) / 2));
                }
            };

            tableCard.PerformLayout();

            ResumeLayout();
        }

        private Panel CreateStatCard(string title, string value, Color accent, out Label valueLabel)
        {
            var card = CreateCard();
            card.Margin = new Padding(0, 0, 12, 0);
            card.Padding = new Padding(18, 14, 18, 14);

            var accentBar = new Panel
            {
                BackColor = accent,
                Size = new Size(5, 44),
                Location = new Point(0, 18)
            };
            card.Controls.Add(accentBar);

            var lblTitle = new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.3F),
                ForeColor = textSub,
                Location = new Point(18, 14)
            };
            card.Controls.Add(lblTitle);

            valueLabel = new Label
            {
                Text = value,
                AutoSize = true,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = textMain,
                Location = new Point(18, 34)
            };
            card.Controls.Add(valueLabel);

            return card;
        }

        private Panel CreateCard()
        {
            var card = new Panel
            {
                BackColor = cardBg
            };
            card.Paint += (s, e) =>
            {
                UiTheme.PaintSoftCard(card, e, 20);
            };
            UiTheme.AttachRoundedRegion(card, 20);
            return card;
        }

        private Label CreateFieldLabel(string text, Point location)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = textMain,
                Location = location
            };
        }

        private TextBox CreateTextBox(Point location, int width, string placeholder)
        {
            var txt = new TextBox
            {
                Location = location,
                Size = new Size(width, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.ApplyTextBox(txt);
            txt.Tag = placeholder;
            txt.ForeColor = Color.FromArgb(148, 163, 184);
            txt.Text = placeholder;
            txt.Enter += FilterText_Enter;
            txt.Leave += FilterText_Leave;
            return txt;
        }

        private void FilterText_Enter(object sender, EventArgs e)
        {
            var txt = sender as TextBox;
            if (txt == null) return;
            string hint = txt.Tag as string ?? "";
            if (txt.Text == hint)
            {
                txt.Text = "";
                txt.ForeColor = textMain;
            }
        }

        private void FilterText_Leave(object sender, EventArgs e)
        {
            var txt = sender as TextBox;
            if (txt == null) return;
            string hint = txt.Tag as string ?? "";
            if (string.IsNullOrWhiteSpace(txt.Text))
            {
                txt.Text = hint;
                txt.ForeColor = Color.FromArgb(148, 163, 184);
            }
        }

        private ComboBox CreateCombo(Point location, int width)
        {
            var cbo = new ComboBox
            {
                Location = location,
                Size = new Size(width, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            UiTheme.ApplyComboBox(cbo);
            return cbo;
        }

        private DateTimePicker CreateDatePicker(Point location)
        {
            return new DateTimePicker
            {
                Location = location,
                Size = new Size(150, 30),
                Format = DateTimePickerFormat.Short
            };
        }

        private Button CreateButton(string text, Point location, int width, Color backColor, Color foreColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = location,
                Size = new Size(width, 36),
                BackColor = backColor,
                ForeColor = foreColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiTheme.ApplyButton(btn, backColor == Color.White ? UiButtonVariant.Secondary : (backColor == green ? UiButtonVariant.Success : UiButtonVariant.Primary));
            btn.ForeColor = foreColor;
            return btn;
        }

        private void LoadExamOptions()
        {
            string ownerFilter = BuildOwnedExamPredicate("");
            string sql =
                "SELECT 0 AS ExamID, N'Tất cả bài thi' AS ExamName " +
                "UNION ALL " +
                "SELECT ExamID, ExamName FROM Exams " +
                (!string.IsNullOrWhiteSpace(ownerFilter) ? "WHERE " + ownerFilter + " " : "") +
                "ORDER BY ExamID";

            DataTable dt = db.ExecuteQuery(sql);
            cboExam.DataSource = dt;
            cboExam.DisplayMember = "ExamName";
            cboExam.ValueMember = "ExamID";
            cboExam.SelectedValue = 0;
        }

        private void LoadClassOptions()
        {
            string ownerFilter = BuildOwnedExamPredicate("e");
            string sql =
                "SELECT N'' AS ClassName, N'Tất cả lớp' AS DisplayName " +
                "UNION ALL " +
                "SELECT DISTINCT ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(r.StudentClass, u.ClassName))), N''), N'(Chưa nhập lớp)') AS ClassName, " +
                "ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(r.StudentClass, u.ClassName))), N''), N'(Chưa nhập lớp)') AS DisplayName " +
                "FROM Results r " +
                "INNER JOIN Exams e ON e.ExamID = r.ExamID " +
                "LEFT JOIN Users u ON u.UserID = r.UserID " +
                (!string.IsNullOrWhiteSpace(ownerFilter) ? "WHERE " + ownerFilter + " " : "") +
                "ORDER BY DisplayName";

            DataTable dt = db.ExecuteQuery(sql);
            cboClass.DataSource = dt;
            cboClass.DisplayMember = "DisplayName";
            cboClass.ValueMember = "ClassName";
            cboClass.SelectedValue = "";
        }

        private void LoadScores()
        {
            int examId = cboExam.SelectedValue == null ? 0 : Convert.ToInt32(cboExam.SelectedValue);
            string classFilter = cboClass.SelectedValue == null ? "" : cboClass.SelectedValue.ToString();
            string keyword = EscapeSql(GetKeywordText());

            var where = new List<string>();
            string ownerFilter = BuildOwnedExamPredicate("e");
            if (!string.IsNullOrWhiteSpace(ownerFilter))
                where.Add(ownerFilter);
            if (examId > 0)
                where.Add("r.ExamID=" + examId);

            if (!string.IsNullOrWhiteSpace(classFilter))
            {
                if (classFilter == "(Chưa nhập lớp)")
                    where.Add("ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(r.StudentClass, u.ClassName))), N''), N'(Chưa nhập lớp)') = N'(Chưa nhập lớp)'");
                else
                    where.Add("ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(r.StudentClass, u.ClassName))), N''), N'(Chưa nhập lớp)') = N'" + EscapeSql(classFilter) + "'");
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                where.Add("(" +
                    "ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(r.StudentFullName, u.FullName))), N''), u.Username) LIKE N'%" + keyword + "%' OR " +
                    "ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(r.StudentClass, u.ClassName))), N''), N'') LIKE N'%" + keyword + "%' OR " +
                    "e.ExamName LIKE N'%" + keyword + "%')");
            }

            if (chkUseDate.Checked)
            {
                string fromText = dtFrom.Value.Date.ToString("yyyy-MM-dd");
                string toText = dtTo.Value.Date.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-dd HH:mm:ss");
                where.Add("r.SubmitDate >= '" + fromText + "' AND r.SubmitDate <= '" + toText + "'");
            }

            string sql =
                "SELECT r.ResultID AS [Mã], " +
                "ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(r.StudentFullName, u.FullName))), N''), u.Username) AS [Họ tên], " +
                "ISNULL(NULLIF(LTRIM(RTRIM(ISNULL(r.StudentClass, u.ClassName))), N''), N'(Chưa nhập lớp)') AS [Lớp], " +
                "e.ExamName AS [Tên bài thi], " +
                "CAST(ROUND(r.Score, 2) AS DECIMAL(10,2)) AS [Điểm số], " +
                "CONVERT(NVARCHAR(19), r.SubmitDate, 120) AS [Nộp lúc] " +
                "FROM Results r " +
                "INNER JOIN Exams e ON e.ExamID = r.ExamID " +
                "LEFT JOIN Users u ON u.UserID = r.UserID " +
                (where.Count > 0 ? "WHERE " + string.Join(" AND ", where) + " " : "") +
                "ORDER BY [Lớp], [Họ tên], r.SubmitDate DESC";

            currentTable = db.ExecuteQuery(sql);
            dgvScores.DataSource = currentTable;

            if (dgvScores.Columns.Count > 0)
            {
                dgvScores.Columns["Mã"].FillWeight = 55;
                dgvScores.Columns["Họ tên"].FillWeight = 145;
                dgvScores.Columns["Lớp"].FillWeight = 85;
                dgvScores.Columns["Tên bài thi"].FillWeight = 145;
                dgvScores.Columns["Điểm số"].FillWeight = 70;
                dgvScores.Columns["Nộp lúc"].FillWeight = 110;
            }

            if (dgvScores.Columns.Count > 0)
            {
                DataGridViewColumn scoreColumn = dgvScores.Columns.Count > 4 ? dgvScores.Columns[4] : null;
                DataGridViewColumn examColumn = dgvScores.Columns.Count > 3 ? dgvScores.Columns[3] : null;
                DataGridViewColumn submitColumn = dgvScores.Columns.Count > 5 ? dgvScores.Columns[5] : null;

                if (scoreColumn != null)
                {
                    scoreColumn.DisplayIndex = 3;
                    scoreColumn.FillWeight = 70;
                    scoreColumn.MinimumWidth = 90;
                }

                if (examColumn != null)
                {
                    examColumn.DisplayIndex = 4;
                    examColumn.FillWeight = 120;
                    examColumn.MinimumWidth = 140;
                }

                if (submitColumn != null)
                {
                    submitColumn.DisplayIndex = 5;
                    submitColumn.FillWeight = 95;
                    submitColumn.MinimumWidth = 120;
                }
            }

            ConfigureScoreGridColumns();

            bool hasRows = currentTable != null && currentTable.Rows.Count > 0;
            dgvScores.Visible = hasRows;
            if (lblEmptyState != null)
            {
                lblEmptyState.Visible = !hasRows;
                if (lblEmptyState.Parent != null)
                {
                    lblEmptyState.Location = new Point(
                        Math.Max(0, (lblEmptyState.Parent.ClientSize.Width - lblEmptyState.Width) / 2),
                        Math.Max(84, (lblEmptyState.Parent.ClientSize.Height - lblEmptyState.Height) / 2));
                }
            }

            UpdateSummary();
            RefreshSummaryHint();
        }

        private void UpdateSummary()
        {
            int attempts = currentTable?.Rows.Count ?? 0;
            int classCount = 0;
            decimal avg = 0M;

            if (currentTable != null && currentTable.Rows.Count > 0)
            {
                classCount = currentTable.Rows.Cast<DataRow>()
                    .Select(r => (r["Lớp"] ?? "").ToString())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                avg = currentTable.Rows.Cast<DataRow>()
                    .Average(r => Convert.ToDecimal(r["Điểm số"]));
            }

            lblStatAttempts.Text = attempts.ToString();
            lblStatClasses.Text = classCount.ToString();
            lblStatAverage.Text = avg.ToString("0.00", CultureInfo.InvariantCulture);
            lblSummary.Text = "Hiển thị " + attempts + " lượt làm bài trong phạm vi đang chọn.";
        }

        private string BuildOwnedExamPredicate(string examAlias)
        {
            if (ownerUserId <= 0)
                return "";

            string examRef = string.IsNullOrWhiteSpace(examAlias) ? "Exams" : examAlias;
            return "(" + examRef + ".CreatedByUserID = " + ownerUserId +
                   " OR EXISTS (" +
                   "SELECT 1 FROM ExamQuestions eq " +
                   "INNER JOIN Questions q ON q.QuestionID = eq.QuestionID " +
                   "WHERE eq.ExamID = " + examRef + ".ExamID AND q.CreatedByUserID = " + ownerUserId +
                   "))";
        }

        private void ConfigureScoreGridColumns()
        {
            if (dgvScores == null || dgvScores.Columns.Count == 0)
                return;

            ConfigureGridColumn("M????", 0, 8, 90);
            ConfigureGridColumn("H?????? t????n", 1, 18, 200);
            ConfigureGridColumn("L???????p", 2, 12, 130);
            ConfigureGridColumn("????i??????m s???????", 3, 10, 110);
            ConfigureGridColumn("T????n b????i thi", 4, 18, 180);
            ConfigureGridColumn("N???????p l????c", 5, 16, 180);
        }

        private void ConfigureGridColumn(string columnName, int displayIndex, int fillWeight, int minimumWidth)
        {
            DataGridViewColumn column = dgvScores.Columns[columnName];
            if (column == null)
                return;

            column.DisplayIndex = displayIndex;
            column.FillWeight = fillWeight;
            column.MinimumWidth = minimumWidth;
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private void ResetFilters()
        {
            if (cboExam.Items.Count > 0) cboExam.SelectedIndex = 0;
            if (cboClass.Items.Count > 0) cboClass.SelectedIndex = 0;
            txtKeyword.Text = txtKeyword.Tag as string ?? "";
            txtKeyword.ForeColor = Color.FromArgb(148, 163, 184);
            chkUseDate.Checked = false;
            ResetDates();
            ToggleDateInputs();
            LoadScores();
        }

        private void RefreshSummaryHint()
        {
            int attempts = currentTable?.Rows.Count ?? 0;
            if (lblSummary == null) return;

            if (attempts == 0)
            {
                lblSummary.Text = "Khong co luot lam bai nao khop voi bo loc hien tai.";
                return;
            }

            string examName = cboExam.SelectedValue != null && Convert.ToInt32(cboExam.SelectedValue) > 0
                ? cboExam.Text
                : "tat ca bai thi";
            string className = !string.IsNullOrWhiteSpace(cboClass.SelectedValue == null ? "" : cboClass.SelectedValue.ToString())
                ? cboClass.Text
                : "tat ca lop";

            lblSummary.Text = "Hien thi " + attempts + " luot lam bai cua " + className + " trong pham vi " + examName + ". Nhap dup mot dong de mo chi tiet bai lam.";
        }

        private void ResetDates()
        {
            dtFrom.Value = DateTime.Today.AddDays(-30);
            dtTo.Value = DateTime.Today;
        }

        private void ToggleDateInputs()
        {
            dtFrom.Enabled = chkUseDate.Checked;
            dtTo.Enabled = chkUseDate.Checked;
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (currentTable == null || currentTable.Rows.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu điểm để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Excel Workbook (*.xlsx)|*.xlsx";
                dialog.FileName = "bang-diem-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".xlsx";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                var rows = currentTable.Rows.Cast<DataRow>().Select(r => new ScoreExportService.ScoreExportRow
                {
                    FullName = r["Họ tên"].ToString(),
                    ClassName = r["Lớp"].ToString(),
                    ExamName = r["Tên bài thi"].ToString(),
                    SubjectName = r["Tên bài thi"].ToString(),
                    ScoreText = Convert.ToDecimal(r["Điểm số"]).ToString("0.##", CultureInfo.InvariantCulture),
                    SubmitDateText = r["Nộp lúc"].ToString()
                }).ToList();

                ScoreExportService.ExportClassScoresXlsx(dialog.FileName, rows);
                MessageBox.Show("Đã xuất bảng điểm ra file Excel thành công.", "Xuất Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DgvScores_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvScores.Columns[e.ColumnIndex].Name == "Điểm số" && e.Value != null)
            {
                decimal score;
                if (decimal.TryParse(e.Value.ToString(), out score))
                {
                    if (score >= 8)
                        e.CellStyle.ForeColor = Color.FromArgb(22, 163, 74);
                    else if (score >= 5)
                        e.CellStyle.ForeColor = Color.FromArgb(37, 99, 235);
                    else
                        e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38);

                    e.CellStyle.Font = new Font("Segoe UI", 9.3F, FontStyle.Bold);
                }
            }
        }

        private void DgvScores_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvScores.Rows.Count) return;

            int resultId;
            if (!int.TryParse((dgvScores.Rows[e.RowIndex].Cells["Mã"].Value ?? "").ToString(), out resultId) || resultId <= 0)
                return;

            var review = ExamAttemptReviewService.LoadByResultId(resultId);
            if (review == null || review.TotalQuestions <= 0 || review.ReviewRows == null || review.ReviewRows.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu chi tiết cho bài làm này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool hasFillBlank = review.ReviewRows.Any(r => r.IsFillBlank) && !review.IsPracticeMode;
            if (hasFillBlank)
            {
                using (var reviewDlg = new FillBlankReviewDialog(review.ReviewRows))
                {
                    if (reviewDlg.ShowDialog(this) == DialogResult.OK)
                    {
                        ExamAttemptReviewService.ApplyTeacherFillBlankReview(resultId, reviewDlg.UpdatedRows);
                        review = ExamAttemptReviewService.LoadByResultId(resultId);
                    }
                }
            }

            using (var frm = new frmResult(
                review.CorrectCount,
                review.TotalQuestions,
                review.Score,
                review.EarnedPoints,
                review.TotalPoints,
                review.ReviewRows))
            {
                frm.ShowDialog(this);
            }
        }

        private static string EscapeSql(string value)
        {
            return (value ?? "").Replace("'", "''");
        }

        private string GetKeywordText()
        {
            string hint = txtKeyword.Tag as string ?? "";
            string text = (txtKeyword.Text ?? "").Trim();
            return string.Equals(text, hint, StringComparison.Ordinal) ? "" : text;
        }
    }
}
