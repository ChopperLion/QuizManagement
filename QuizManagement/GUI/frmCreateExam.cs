using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using QuizManagement.BUS;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class ucManageExam : UserControl
    {
        private readonly Database db = new Database();
        private readonly int ownerUserId;
        private bool studentMode;
        private int selectedExamID = -1;
        private bool isLoadingExamList;
        private int currentPage = 1;
        private int pageSize = 10;
        private int totalQuestionRows;
        private DataTable allQuestionsTable;

        private readonly Color BG = UiTheme.PageBg;
        private readonly Color CARD_BG = UiTheme.SurfaceRaised;
        private readonly Color TEXT_MAIN = UiTheme.TextPrimary;
        private readonly Color TEXT_SUB = UiTheme.TextSecondary;
        private readonly Color BORDER = UiTheme.Border;
        private readonly Color TABLE_HEADER_BG = UiTheme.Surface;
        private readonly Color TABLE_HEADER_FG = UiTheme.TextSecondary;

        private readonly Color BLUE = UiTheme.Primary;
        private readonly Color GREEN = UiTheme.Success;
        private readonly Color ORANGE = UiTheme.Warning;
        private readonly Color RED = UiTheme.Danger;
        private readonly Color PURPLE = UiTheme.Info;

        private Panel pnlHeader;
        private Label lblTitle;
        private Label lblSubtitle;
        private Button btnImportFile;
        private Button btnDownloadTemplate;
        private Button btnCreateSet;

        private Panel pnlBody;
        private TableLayoutPanel mainLayout;

        private RoundPanel pnlLeft;
        private TextBox txtSearchExam;
        private ListBox lstExam;
        private Button btnAddExamList;
        private Button btnTrash;

        private RoundPanel pnlRight;
        private Label lblDetailTitle;
        private Label lblDetailBadge;
        private Label lblDetailStatus;
        private Label lblDetailHint;
        private Button btnEditSet;
        private Button btnDuplicateSet;
        private Button btnDeleteSet;
        private Button btnShareExamLink;
        private TextBox txtSearchQuestion;
        private ComboBox cboFilterType;
        private ComboBox cboFilterPoints;
        private ComboBox cboFilterStatus;
        private Button btnApplyFilter;
        private Button btnCreateQuestion;
        private Button btnAddFromBank;
        private Button btnDistributeScore;
        private Button btnManualScore;
        private DataGridView dgvQuestions;
        private Panel pnlFooter;
        private Label lblPageInfo;
        private ComboBox cboPageSize;
        private Button btnPrevPage;
        private Button btnPage1;
        private Button btnPage2;
        private Button btnNextPage;
        private ContextMenuStrip examQuickMenu;

        public ucManageExam() : this(0)
        {
        }

        public ucManageExam(int ownerUserId)
        {
            this.ownerUserId = ownerUserId;
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = BG;
            Font = new Font("Segoe UI", 10F);
            InitUI();
            Load += ucManageExam_Load;
            Resize += (s, e) => LayoutFooter();
        }

        public void ConfigureForStudentPractice()
        {
            studentMode = true;
            if (lblTitle != null) lblTitle.Text = "ĐỀ ÔN CỦA TÔI";
            if (lblSubtitle != null) lblSubtitle.Text = "Tạo đề ôn, quản lý bộ đề và chia sẻ nhanh khi cần.";
            if (btnCreateSet != null) btnCreateSet.Text = "+  Tạo đề ôn";
        }

        private void ucManageExam_Load(object sender, EventArgs e)
        {
            Database.EnsureQuestionPointsColumn();
            Database.EnsureTeacherOwnershipColumns();
            Database.EnsureExamAccessColumns();
            Database.EnsureAdvancedExamColumns();
            EnsureQuickMenu();
            LoadExamList();
        }

        private void InitUI()
        {
            SuspendLayout();

            pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = CARD_BG,
                Padding = new Padding(24, 12, 24, 8)
            };

            lblTitle = new Label
            {
                Text = "QUẢN LÝ ĐỀ THI",
                AutoSize = true,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = TEXT_MAIN,
                Location = new Point(4, 4)
            };
            pnlHeader.Controls.Add(lblTitle);

            lblSubtitle = new Label
            {
                Text = "Quản lý bộ đề, câu hỏi và theo dõi mức độ sử dụng trong hệ thống",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = TEXT_SUB,
                Location = new Point(6, 44)
            };
            pnlHeader.Controls.Add(lblSubtitle);

            btnImportFile = CreateOutlineButton("Nhập từ file", BLUE);
            btnImportFile.Size = new Size(140, 36);
            btnImportFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnImportFile.Click += BtnImportFile_Click;
            pnlHeader.Controls.Add(btnImportFile);

            btnDownloadTemplate = CreateOutlineButton("Tải file mẫu", TEXT_SUB);
            btnDownloadTemplate.Size = new Size(140, 36);
            btnDownloadTemplate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDownloadTemplate.Click += BtnDownloadTemplate_Click;
            pnlHeader.Controls.Add(btnDownloadTemplate);

            btnCreateSet = CreateSolidButton("+  Tạo bộ đề mới", BLUE);
            btnCreateSet.Size = new Size(160, 36);
            btnCreateSet.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCreateSet.Click += BtnCreateSet_Click;
            pnlHeader.Controls.Add(btnCreateSet);

            pnlHeader.Resize += (s, e) =>
            {
                btnCreateSet.Location = new Point(pnlHeader.Width - btnCreateSet.Width - 4, 22);
                btnImportFile.Location = new Point(btnCreateSet.Left - btnImportFile.Width - 10, 22);
                btnDownloadTemplate.Location = new Point(btnImportFile.Left - btnDownloadTemplate.Width - 10, 22);
            };

            pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BG,
                Padding = new Padding(20, 12, 20, 16)
            };

            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            pnlBody.Controls.Add(mainLayout);

            BuildLeftPanel();
            BuildRightPanel();

            // Thêm Fill trước, Top sau để header không đè phần nội dung
            Controls.Add(pnlBody);
            Controls.Add(pnlHeader);

            ResumeLayout();
        }

        private void BuildLeftPanel()
        {
            pnlLeft = CreateRoundedPanel();
            pnlLeft.Dock = DockStyle.Fill;
            pnlLeft.Padding = new Padding(14);
            pnlLeft.Margin = new Padding(0, 0, 14, 0);
            mainLayout.Controls.Add(pnlLeft, 0, 0);

            var leftHeader = new Panel { Dock = DockStyle.Top, Height = 40 };
            var lblList = new Label
            {
                Text = "DANH SÁCH BỘ ĐỀ",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = TEXT_MAIN,
                AutoSize = true,
                Location = new Point(0, 10)
            };
            leftHeader.Controls.Add(lblList);

            btnAddExamList = CreateSolidButton("+", BLUE);
            btnAddExamList.Size = new Size(32, 32);
            btnAddExamList.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            btnAddExamList.Location = new Point(270, 4);
            btnAddExamList.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddExamList.Click += BtnCreateSet_Click;
            leftHeader.Controls.Add(btnAddExamList);

            txtSearchExam = CreateSearchBox("Tìm kiếm bộ đề...");
            txtSearchExam.Dock = DockStyle.Top;
            txtSearchExam.Margin = new Padding(0, 6, 0, 8);
            txtSearchExam.TextChanged += TxtSearchExam_TextChanged;

            lstExam = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Segoe UI", 10F),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 72,
                IntegralHeight = false
            };
            lstExam.DrawItem += LstExam_DrawItem;
            lstExam.SelectedIndexChanged += LstExam_SelectedIndexChanged;
            lstExam.MouseDown += LstExam_MouseDown;

            var leftFooter = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(0, 8, 0, 0) };
            btnTrash = CreateOutlineButton("Xem thùng rác", TEXT_SUB);
            btnTrash.Dock = DockStyle.Fill;
            btnTrash.FlatAppearance.BorderColor = BORDER;
            btnTrash.Click += (s, e) => MessageBox.Show("Thùng rác đang được phát triển.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            leftFooter.Controls.Add(btnTrash);
            leftFooter.Visible = false;
            leftFooter.Height = 0;

            // Dock Bottom trước, Fill sau để list không che footer
            pnlLeft.Controls.Add(leftFooter);
            pnlLeft.Controls.Add(lstExam);
            pnlLeft.Controls.Add(txtSearchExam);
            pnlLeft.Controls.Add(leftHeader);
        }

        private void BuildRightPanel()
        {
            pnlRight = CreateRoundedPanel();
            pnlRight.Dock = DockStyle.Fill;
            pnlRight.Padding = new Padding(18, 14, 18, 12);
            mainLayout.Controls.Add(pnlRight, 1, 0);

            var detailHeader = new Panel { Dock = DockStyle.Top, Height = 86 };
            lblDetailTitle = new Label
            {
                Text = "Chọn một bộ đề",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = TEXT_MAIN,
                AutoSize = true,
                Location = new Point(0, 4)
            };
            detailHeader.Controls.Add(lblDetailTitle);

            lblDetailBadge = new Label
            {
                Text = "0 câu",
                AutoSize = false,
                Size = new Size(72, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = BLUE,
                BackColor = Color.FromArgb(239, 246, 255),
                Location = new Point(0, 46)
            };
            detailHeader.Controls.Add(lblDetailBadge);

            lblDetailStatus = new Label
            {
                Text = "Chưa chọn bộ đề",
                AutoSize = false,
                Size = new Size(170, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = TEXT_SUB,
                BackColor = Color.FromArgb(241, 245, 249),
                Location = new Point(82, 46)
            };
            detailHeader.Controls.Add(lblDetailStatus);

            btnDeleteSet = CreateOutlineButton("Xóa bộ đề", RED);
            btnDeleteSet.Size = new Size(100, 32);
            btnDeleteSet.ForeColor = RED;
            btnDeleteSet.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnDeleteSet.Click += BtnDeleteSet_Click;
            detailHeader.Controls.Add(btnDeleteSet);

            btnShareExamLink = CreateOutlineButton("Chia sẻ link", BLUE);
            btnShareExamLink.Size = new Size(108, 32);
            btnShareExamLink.Click += BtnShareExamLink_Click;
            detailHeader.Controls.Add(btnShareExamLink);

            btnDuplicateSet = CreateOutlineButton("Nhân bản", TEXT_MAIN);
            btnDuplicateSet.Size = new Size(96, 32);
            btnDuplicateSet.Click += BtnDuplicateSet_Click;
            detailHeader.Controls.Add(btnDuplicateSet);

            btnEditSet = CreateOutlineButton("Chỉnh sửa", TEXT_MAIN);
            btnEditSet.Size = new Size(96, 32);
            btnEditSet.Click += BtnEditSet_Click;
            detailHeader.Controls.Add(btnEditSet);

            detailHeader.Resize += (s, e) =>
            {
                int right = detailHeader.Width;
                int buttonY = 8;
                btnDeleteSet.Location = new Point(right - btnDeleteSet.Width, buttonY);
                btnShareExamLink.Location = new Point(btnDeleteSet.Left - btnShareExamLink.Width - 8, buttonY);
                btnDuplicateSet.Location = new Point(btnShareExamLink.Left - btnDuplicateSet.Width - 8, buttonY);
                btnEditSet.Location = new Point(btnDuplicateSet.Left - btnEditSet.Width - 8, buttonY);
                lblDetailTitle.MaximumSize = new Size(Math.Max(220, btnEditSet.Left - 18), 0);
                lblDetailStatus.Width = Math.Min(170, Math.Max(112, right - 82));
            };

            lblDetailHint = new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Font = new Font("Segoe UI", 9F),
                ForeColor = TEXT_SUB,
                Text = "Hãy chọn một bộ đề ở bên trái để xem câu hỏi, cấu hình và các thao tác liên quan.",
                Padding = new Padding(0, 4, 0, 8)
            };

            var filterBar = new Panel { Dock = DockStyle.Top, Height = 86, Padding = new Padding(0, 8, 0, 0) };
            txtSearchQuestion = CreateSearchBox("Tìm kiếm câu hỏi...");
            txtSearchQuestion.Width = 200;
            txtSearchQuestion.Location = new Point(0, 4);
            txtSearchQuestion.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            txtSearchQuestion.TextChanged += TxtSearchQuestion_TextChanged;
            filterBar.Controls.Add(txtSearchQuestion);

            cboFilterType = CreateFilterCombo(new[] { "Loại câu hỏi", "Trắc nghiệm", "Tự luận" });
            cboFilterPoints = CreateFilterCombo(new[] { "Số điểm", "Chưa gán", "= 1 điểm", "2 điểm", "= 3 điểm" });
            cboFilterStatus = CreateFilterCombo(new[] { "Trạng thái", "Đã gán điểm", "Chưa gán điểm" });
            filterBar.Controls.AddRange(new Control[] { cboFilterType, cboFilterPoints, cboFilterStatus });

            btnApplyFilter = CreateSolidButton("Lọc", BLUE);
            btnApplyFilter.Size = new Size(92, 32);
            btnApplyFilter.Click += (s, e) => { currentPage = 1; LoadExamQuestions(); };
            filterBar.Controls.Add(btnApplyFilter);

            btnAddFromBank = CreateOutlineButton("Thêm có sẵn", TEXT_SUB);
            btnAddFromBank.Size = new Size(108, 32);
            btnAddFromBank.Click += BtnAddFromBank_Click;
            filterBar.Controls.Add(btnAddFromBank);

            btnDistributeScore = CreateOutlineButton("Chia đều điểm", ORANGE);
            btnDistributeScore.Size = new Size(116, 32);
            btnDistributeScore.Click += BtnDistributeScore_Click;
            filterBar.Controls.Add(btnDistributeScore);

            btnManualScore = CreateOutlineButton("Chỉnh điểm", PURPLE);
            btnManualScore.Size = new Size(102, 32);
            btnManualScore.Click += BtnManualScore_Click;
            filterBar.Controls.Add(btnManualScore);

            btnCreateQuestion = CreateSolidButton("+ Tạo câu hỏi", GREEN);
            btnCreateQuestion.Size = new Size(118, 32);
            btnCreateQuestion.BackColor = GREEN;
            btnCreateQuestion.Click += BtnCreateQuestion_Click;
            filterBar.Controls.Add(btnCreateQuestion);

            filterBar.Resize += (s, e) => LayoutFilterBar(filterBar);
            LayoutFilterBar(filterBar);

            dgvQuestions = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 42,
                RowTemplate = { Height = 44 },
                GridColor = BORDER,
                Margin = new Padding(0, 8, 0, 0)
            };
            UiTheme.ApplyGrid(dgvQuestions);
            dgvQuestions.CellPainting += DgvQuestions_CellPainting;
            dgvQuestions.CellClick += DgvQuestions_CellClick;

            pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, BackColor = CARD_BG };
            lblPageInfo = new Label
            {
                Text = "Hiển thị 0 - 0 của 0 câu hỏi",
                Font = new Font("Segoe UI", 9F),
                ForeColor = TEXT_SUB,
                AutoSize = true,
                Location = new Point(0, 14)
            };
            pnlFooter.Controls.Add(lblPageInfo);

            var lblPerPage = new Label { Text = "/ trang", Font = new Font("Segoe UI", 9F), ForeColor = TEXT_SUB, AutoSize = true };
            cboPageSize = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(56, 28),
                FlatStyle = FlatStyle.Flat
            };
            cboPageSize.Items.AddRange(new object[] { "10", "20", "50" });
            cboPageSize.SelectedIndex = 0;
            UiTheme.ApplyComboBox(cboPageSize);
            cboPageSize.SelectedIndexChanged += (s, e) =>
            {
                pageSize = int.Parse(cboPageSize.SelectedItem.ToString());
                currentPage = 1;
                RenderQuestionPage();
            };

            btnPrevPage = CreatePageBtn("«");
            btnPage1 = CreatePageBtn("1");
            btnPage2 = CreatePageBtn("2");
            btnNextPage = CreatePageBtn("»");
            btnPrevPage.Click += (s, e) => { if (currentPage > 1) { currentPage--; RenderQuestionPage(); } };
            btnPage1.Click += (s, e) => { currentPage = 1; RenderQuestionPage(); };
            btnPage2.Click += (s, e) => { currentPage = 2; RenderQuestionPage(); };
            btnNextPage.Click += (s, e) =>
            {
                int totalPages = Math.Max(1, (int)Math.Ceiling(totalQuestionRows / (double)pageSize));
                if (currentPage < totalPages) { currentPage++; RenderQuestionPage(); }
            };

            pnlFooter.Controls.AddRange(new Control[] { btnPrevPage, btnPage1, btnPage2, btnNextPage, cboPageSize, lblPerPage });

            pnlRight.Controls.Add(pnlFooter);
            pnlRight.Controls.Add(dgvQuestions);
            pnlRight.Controls.Add(filterBar);
            pnlRight.Controls.Add(lblDetailHint);
            pnlRight.Controls.Add(detailHeader);
            LayoutFooter();
        }

        private void LayoutFilterBar(Panel bar)
        {
            if (bar == null) return;

            int firstRowY = 4;
            int secondRowY = 40;
            int gap = 8;
            int usableWidth = Math.Max(520, bar.Width - 8);

            txtSearchQuestion.Width = Math.Min(220, Math.Max(150, usableWidth / 4));
            txtSearchQuestion.Location = new Point(0, firstRowY);

            int x = txtSearchQuestion.Right + gap;
            foreach (var c in new[] { cboFilterType, cboFilterPoints, cboFilterStatus })
            {
                c.Location = new Point(x, firstRowY);
                x += c.Width + gap;
            }
            btnApplyFilter.Location = new Point(x, firstRowY);

            int actionRight = usableWidth;
            btnCreateQuestion.Location = new Point(actionRight - btnCreateQuestion.Width, secondRowY);
            btnManualScore.Location = new Point(btnCreateQuestion.Left - btnManualScore.Width - gap, secondRowY);
            btnDistributeScore.Location = new Point(btnManualScore.Left - btnDistributeScore.Width - gap, secondRowY);
            btnAddFromBank.Location = new Point(btnDistributeScore.Left - btnAddFromBank.Width - gap, secondRowY);

            bar.Height = 86;
        }

        private void LayoutFooter()
        {
            if (pnlFooter == null) return;
            int w = pnlFooter.Width;
            btnNextPage.Location = new Point(w - 36, 10);
            btnPage2.Location = new Point(btnNextPage.Left - 36, 10);
            btnPage1.Location = new Point(btnPage2.Left - 36, 10);
            btnPrevPage.Location = new Point(btnPage1.Left - 36, 10);
            cboPageSize.Location = new Point(btnPrevPage.Left - 70, 12);
            foreach (Control c in pnlFooter.Controls)
                if (c is Label l && l.Text == "/ trang")
                    l.Location = new Point(cboPageSize.Left - 42, 14);
        }

        // ========================= UI HELPERS =========================

        private RoundPanel CreateRoundedPanel() =>
            new RoundPanel { BackColor = CARD_BG, BorderColor = BORDER, Radius = 14 };

        private Button CreateSolidButton(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiTheme.ApplyButton(btn,
                color == GREEN ? UiButtonVariant.Success :
                color == ORANGE ? UiButtonVariant.Warning :
                color == RED ? UiButtonVariant.Danger :
                color == PURPLE ? UiButtonVariant.Info :
                UiButtonVariant.Primary);
            return btn;
        }

        private Button CreateOutlineButton(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = CARD_BG,
                ForeColor = color,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            UiTheme.ApplyButton(btn, UiButtonVariant.Secondary);
            btn.ForeColor = color;
            return btn;
        }

        private Button CreatePageBtn(string text)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(32, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = CARD_BG,
                ForeColor = TEXT_SUB,
                Cursor = Cursors.Hand
            };
            UiTheme.ApplyButton(btn, UiButtonVariant.Secondary);
            return btn;
        }

        private ComboBox CreateFilterCombo(string[] items)
        {
            var cbo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(130, 28),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            UiTheme.ApplyComboBox(cbo);
            cbo.Items.AddRange(items);
            cbo.SelectedIndex = 0;
            return cbo;
        }

        private TextBox CreateSearchBox(string placeholder)
        {
            var box = new TextBox
            {
                Height = 32,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.Gray,
                Text = placeholder,
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.ApplyTextBox(box);
            box.ForeColor = Color.Gray;
            box.Text = placeholder;
            box.Enter += (s, e) =>
            {
                if (box.ForeColor == Color.Gray) { box.Text = ""; box.ForeColor = TEXT_MAIN; }
            };
            box.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(box.Text)) { box.Text = placeholder; box.ForeColor = Color.Gray; }
            };
            return box;
        }

        private string SqlText(string text) => (text ?? "").Replace("'", "''");

        private string OwnerExamWhere(string prefix = "WHERE")
        {
            return ownerUserId > 0 ? prefix + " e.CreatedByUserID = " + ownerUserId + " " : "";
        }

        private string OwnerQuestionCondition(string alias = "q")
        {
            return ownerUserId > 0 ? " AND " + alias + ".CreatedByUserID = " + ownerUserId + " " : "";
        }

        private bool IsPlaceholder(TextBox box, string placeholder) =>
            box.ForeColor == Color.Gray || box.Text == placeholder;

        private int QueryCount(string sql)
        {
            DataTable dt = db.ExecuteQuery(sql);
            if (dt == null || dt.Rows.Count == 0) return 0;
            return int.TryParse(dt.Rows[0][0].ToString(), out int n) ? n : 0;
        }

        private string SqlNullableText(string value) =>
            string.IsNullOrWhiteSpace(value) ? "NULL" : "N'" + SqlText(value.Trim()) + "'";

        private string SqlNullableDate(DateTime? value) =>
            value.HasValue ? "'" + value.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'" : "NULL";

        private string SqlNullableInt(int? value) =>
            value.HasValue && value.Value > 0 ? value.Value.ToString() : "NULL";

        private void EnsureQuickMenu()
        {
            if (examQuickMenu != null) return;
            examQuickMenu = new ContextMenuStrip();
            examQuickMenu.Items.Add("Chia sẻ lên server", null, (s, e) => TogglePublishSelectedExam(true));
            examQuickMenu.Items.Add("Ẩn khỏi server", null, (s, e) => TogglePublishSelectedExam(false));
            examQuickMenu.Items.Add("Chia sẻ link", null, BtnShareExamLink_Click);
            examQuickMenu.Items.Add("Thông tin bộ đề", null, (s, e) => ShowExamInfo());
            examQuickMenu.Items.Add("Chỉnh sửa bộ đề", null, BtnEditSet_Click);
            examQuickMenu.Items.Add("Xóa bộ đề", null, BtnDeleteSet_Click);
        }

        // ========================= DATA =========================

        private void LoadExamList()
        {
            string keyword = IsPlaceholder(txtSearchExam, "Tìm kiếm bộ đề...") ? "" : txtSearchExam.Text.Trim();
            string where = ownerUserId > 0 ? "WHERE e.CreatedByUserID = " + ownerUserId + " " : "";
            if (!string.IsNullOrWhiteSpace(keyword))
                where += (string.IsNullOrWhiteSpace(where) ? "WHERE " : "AND ") + "e.ExamName LIKE N'%" + SqlText(keyword) + "%' ";

            string query =
                "SELECT e.ExamID, e.ExamName, e.Duration, e.ExamMode, e.StartTime, e.EndTime, e.MaxAttempts, e.IsRandomQuestion, e.RandomQuestionCount, e.RoomCode, e.ExamLink, e.ScoreScale, e.PracticeMaxWrongPerQuestion, e.PracticeShowAnswer, e.PracticeShowPointGain, e.IsPublishedToServer, e.RequireClassBeforeStart, e.AllowStudentEditClass, e.PracticeAllowStudentOptions, e.PracticeNoLoginRequired, e.PracticeUnlimitedTime, " +
                "ISNULL((SELECT COUNT(*) FROM ExamQuestions eq WHERE eq.ExamID = e.ExamID), 0) AS QuestionCount " +
                "FROM Exams e " + where + "ORDER BY e.ExamID DESC";

            DataTable dt = db.ExecuteQuery(query);
            isLoadingExamList = true;
            lstExam.DataSource = null;
            lstExam.DataSource = dt;
            lstExam.DisplayMember = "ExamName";
            lstExam.ValueMember = "ExamID";
            isLoadingExamList = false;

            if (dt.Rows.Count > 0) lstExam.SelectedIndex = 0;
            else ClearDetail();

        }

        private string GetExamAvailabilityStatus(DataRow row)
        {
            if (row == null) return "Chưa chọn";

            int questionCount = row.Table.Columns.Contains("QuestionCount") && row["QuestionCount"] != DBNull.Value
                ? Convert.ToInt32(row["QuestionCount"])
                : 0;
            if (questionCount <= 0) return "Bản nháp";

            DateTime now = DateTime.Now;
            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MinValue;
            bool hasStart = row.Table.Columns.Contains("StartTime") && row["StartTime"] != DBNull.Value && DateTime.TryParse(row["StartTime"].ToString(), out start);
            bool hasEnd = row.Table.Columns.Contains("EndTime") && row["EndTime"] != DBNull.Value && DateTime.TryParse(row["EndTime"].ToString(), out end);

            if (hasStart && now < start) return "Sắp diễn ra";
            if (hasEnd && now > end) return "Đã kết thúc";

            bool sharedLink = row.Table.Columns.Contains("ExamLink") && row["ExamLink"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["ExamLink"].ToString());
            bool published = row.Table.Columns.Contains("IsPublishedToServer") && row["IsPublishedToServer"] != DBNull.Value && Convert.ToBoolean(row["IsPublishedToServer"]);
            if (sharedLink || published) return "Sẵn sàng giao";

            return "Đã soạn xong";
        }

        private string BuildExamNextStepHint(DataRow row)
        {
            if (row == null)
                return "Hãy chọn một bộ đề ở bên trái để xem câu hỏi, cấu hình và các thao tác liên quan.";

            int questionCount = row.Table.Columns.Contains("QuestionCount") && row["QuestionCount"] != DBNull.Value
                ? Convert.ToInt32(row["QuestionCount"])
                : 0;
            bool sharedLink = row.Table.Columns.Contains("ExamLink") && row["ExamLink"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["ExamLink"].ToString());
            bool published = row.Table.Columns.Contains("IsPublishedToServer") && row["IsPublishedToServer"] != DBNull.Value && Convert.ToBoolean(row["IsPublishedToServer"]);
            bool randomEnabled = row.Table.Columns.Contains("IsRandomQuestion") && row["IsRandomQuestion"] != DBNull.Value && Convert.ToBoolean(row["IsRandomQuestion"]);
            bool hasStart = row.Table.Columns.Contains("StartTime") && row["StartTime"] != DBNull.Value;
            bool hasEnd = row.Table.Columns.Contains("EndTime") && row["EndTime"] != DBNull.Value;

            if (questionCount <= 0)
                return "Bộ đề này chưa có câu hỏi. Bước tiếp theo nên làm là thêm từ ngân hàng hoặc nhập từ file.";
            if (!sharedLink && !published)
                return "Bộ đề đã có câu hỏi nhưng chưa mở cho người học. Bạn nên kiểm tra cấu hình rồi chia sẻ link hoặc xuất bản lên server.";
            if (!hasStart && !hasEnd)
                return "Bộ đề đã sẵn sàng. Nếu đây là bài thi chính thức, bạn nên đặt thời gian bắt đầu và kết thúc để tránh nhầm lẫn.";
            if (randomEnabled)
                return "Bộ đề đang bật random câu hỏi. Hãy kiểm tra số câu rút ra và tổng điểm trước khi giao cho người học.";
            return "Bộ đề đã có thể sử dụng. Bạn có thể tiếp tục rà soát câu hỏi, chia điểm hoặc theo dõi kết quả làm bài.";
        }

        private void UpdateSelectedExamSummary(DataRow row)
        {
            string status = GetExamAvailabilityStatus(row);
            lblDetailStatus.Text = status;
            lblDetailHint.Text = BuildExamNextStepHint(row);

            Color fg = TEXT_SUB;
            Color bg = Color.FromArgb(241, 245, 249);
            if (status == "Bản nháp")
            {
                fg = ORANGE;
                bg = Color.FromArgb(255, 247, 237);
            }
            else if (status == "Sắp diễn ra")
            {
                fg = BLUE;
                bg = Color.FromArgb(239, 246, 255);
            }
            else if (status == "Đã kết thúc")
            {
                fg = RED;
                bg = Color.FromArgb(254, 242, 242);
            }
            else if (status == "Sẵn sàng giao")
            {
                fg = GREEN;
                bg = Color.FromArgb(220, 252, 231);
            }
            else if (status == "Đã soạn xong")
            {
                fg = PURPLE;
                bg = Color.FromArgb(245, 243, 255);
            }

            lblDetailStatus.ForeColor = fg;
            lblDetailStatus.BackColor = bg;
        }

        private void UpdateActionState(bool hasExam)
        {
            foreach (Button btn in new[] { btnEditSet, btnDuplicateSet, btnDeleteSet, btnShareExamLink, btnCreateQuestion, btnAddFromBank, btnDistributeScore, btnManualScore })
                if (btn != null)
                    btn.Enabled = hasExam;
        }

        private string BuildQuestionSql(int examID)
        {
            string sql =
                "SELECT q.QuestionID AS [ID], q.QuestionContent AS [Nội dung câu hỏi], " +
                "CASE WHEN q.QuestionType = N'FillBlank' THEN N'Điền khuyết' ELSE N'Trắc nghiệm' END AS [Loại], " +
                "CASE WHEN eq.Points IS NULL THEN N'-' ELSE CAST(eq.Points AS NVARCHAR(20)) END AS [Số điểm], " +
                "CASE WHEN eq.Points IS NULL THEN N'Chưa gán' ELSE N'Đã gán' END AS [Trạng thái] " +
                "FROM ExamQuestions eq " +
                "INNER JOIN Questions q ON eq.QuestionID = q.QuestionID " +
                "WHERE eq.ExamID = " + examID + OwnerQuestionCondition("q");

            if (!IsPlaceholder(txtSearchQuestion, "Tìm kiếm câu hỏi..."))
                sql += " AND q.QuestionContent LIKE N'%" + SqlText(txtSearchQuestion.Text.Trim()) + "%'";

            if (cboFilterPoints.SelectedIndex == 1)
                sql += " AND eq.Points IS NULL";
            else if (cboFilterPoints.SelectedIndex == 2)
                sql += " AND eq.Points IS NOT NULL AND eq.Points <= 1";
            else if (cboFilterPoints.SelectedIndex == 3)
                sql += " AND eq.Points > 1 AND eq.Points <= 2";
            else if (cboFilterPoints.SelectedIndex == 4)
                sql += " AND eq.Points >= 3";

            if (cboFilterStatus.SelectedIndex == 1)
                sql += " AND eq.Points IS NOT NULL";
            else if (cboFilterStatus.SelectedIndex == 2)
                sql += " AND eq.Points IS NULL";

            sql += " ORDER BY q.QuestionID DESC";
            return sql;
        }

        private void LoadExamQuestions()
        {
            if (selectedExamID <= 0)
            {
                allQuestionsTable = null;
                dgvQuestions.DataSource = null;
                totalQuestionRows = 0;
                RenderQuestionPage();
                return;
            }

            allQuestionsTable = db.ExecuteQuery(BuildQuestionSql(selectedExamID));
            totalQuestionRows = allQuestionsTable?.Rows.Count ?? 0;
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalQuestionRows / (double)pageSize));
            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;
            RenderQuestionPage();
        }

        private void AddActionColumns()
        {
            foreach (string c in new[] { "colView", "colEdit", "colDelete" })
                if (dgvQuestions.Columns.Contains(c))
                    dgvQuestions.Columns.Remove(c);

            dgvQuestions.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colView", HeaderText = "Thao tác", Text = "Chi tiết", UseColumnTextForButtonValue = true,
                Width = 68, FlatStyle = FlatStyle.Flat
            });
            dgvQuestions.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colEdit", HeaderText = "", Text = "Sửa", UseColumnTextForButtonValue = true,
                Width = 52, FlatStyle = FlatStyle.Flat
            });
            dgvQuestions.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colDelete", HeaderText = "", Text = "Gỡ", UseColumnTextForButtonValue = true,
                Width = 52, FlatStyle = FlatStyle.Flat
            });
        }

        private void RenderQuestionPage()
        {
            if (allQuestionsTable == null)
            {
                dgvQuestions.DataSource = null;
                lblPageInfo.Text = "Hiển thị 0 - 0 của 0 câu hỏi";
                UpdatePaginationButtons();
                return;
            }

            DataTable page = allQuestionsTable.Clone();
            int start = (currentPage - 1) * pageSize;
            int end = Math.Min(start + pageSize, totalQuestionRows);
            for (int i = start; i < end; i++)
                page.ImportRow(allQuestionsTable.Rows[i]);

            dgvQuestions.DataSource = page;
            ConfigureGridColumns();
            AddActionColumns();

            int from = totalQuestionRows == 0 ? 0 : start + 1;
            lblPageInfo.Text = $"Hiển thị {from} - {end} của {totalQuestionRows} câu hỏi";
            lblDetailBadge.Text = totalQuestionRows + " câu";
            UpdatePaginationButtons();
            UpdateActionState(selectedExamID > 0);
        }

        private void ConfigureGridColumns()
        {
            if (dgvQuestions.Columns.Count == 0) return;

            void W(string name, int width, float fill = 0)
            {
                if (!dgvQuestions.Columns.Contains(name)) return;
                if (fill > 0) { dgvQuestions.Columns[name].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; dgvQuestions.Columns[name].FillWeight = fill; }
                else dgvQuestions.Columns[name].Width = width;
            }

            W("ID", 50);
            W("Nội dung câu hỏi", 0, 40F);
            W("Loại", 100);
            W("Số điểm", 90);
            W("Trạng thái", 100);
            if (dgvQuestions.Columns.Contains("colView")) dgvQuestions.Columns["colView"].Width = 68;
            if (dgvQuestions.Columns.Contains("colEdit")) dgvQuestions.Columns["colEdit"].Width = 52;
            if (dgvQuestions.Columns.Contains("colDelete")) dgvQuestions.Columns["colDelete"].Width = 52;

            dgvQuestions.Columns["Loại"].DefaultCellStyle.ForeColor = BLUE;
        }

        private void UpdatePaginationButtons()
        {
            int totalPages = Math.Max(1, (int)Math.Ceiling(totalQuestionRows / (double)pageSize));
            btnPage1.Text = "1";
            btnPage2.Text = "2";
            btnPage1.BackColor = currentPage == 1 ? BLUE : CARD_BG;
            btnPage1.ForeColor = currentPage == 1 ? Color.White : TEXT_SUB;
            btnPage2.BackColor = currentPage == 2 ? BLUE : CARD_BG;
            btnPage2.ForeColor = currentPage == 2 ? Color.White : TEXT_SUB;
            btnPage2.Visible = totalPages >= 2;
            btnPrevPage.Enabled = currentPage > 1;
            btnNextPage.Enabled = currentPage < totalPages;
        }

        private void ClearDetail()
        {
            selectedExamID = -1;
            lblDetailTitle.Text = "Chọn một bộ đề";
            lblDetailBadge.Text = "0 câu";
            lblDetailStatus.Text = "Chưa chọn bộ đề";
            lblDetailStatus.ForeColor = TEXT_SUB;
            lblDetailStatus.BackColor = Color.FromArgb(241, 245, 249);
            lblDetailHint.Text = "Hãy chọn một bộ đề ở bên trái để xem câu hỏi, cấu hình và các thao tác liên quan.";
            allQuestionsTable = null;
            dgvQuestions.DataSource = null;
            lblPageInfo.Text = "Hiển thị 0 - 0 của 0 câu hỏi";
            UpdateActionState(false);
        }

        private int CreateExamForImportedFile(string fileName)
        {
            string examName = Path.GetFileNameWithoutExtension(fileName);
            if (string.IsNullOrWhiteSpace(examName))
                examName = "Bộ đề nhập từ file";

            string ownerColumn = ownerUserId > 0 ? ", CreatedByUserID" : "";
            string ownerValue = ownerUserId > 0 ? ", " + ownerUserId : "";
            string roomCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();

            db.ExecuteNonQuery(
                "INSERT INTO Exams (ExamName, Duration, ExamPassword, ExamMode, StartTime, EndTime, MaxAttempts, IsRandomQuestion, RandomQuestionCount, RoomCode, ExamLink, ScoreScale, PracticeMaxWrongPerQuestion, PracticeShowAnswer, PracticeShowPointGain, PracticeAutoNextOnCorrect, IsPublishedToServer" + ownerColumn + ") VALUES (" +
                "N'" + SqlText(examName) + "', 30, NULL, N'" + (studentMode ? "Practice" : "Exam") + "', NULL, NULL, NULL, 0, NULL, N'" + SqlText(roomCode) + "', NULL, 10, 0, 1, 1, 0" + ownerValue + ")");

            return QueryCount("SELECT MAX(ExamID) FROM Exams" + (ownerUserId > 0 ? " WHERE CreatedByUserID=" + ownerUserId : ""));
        }

        private void SelectExamInList(int examId)
        {
            if (examId <= 0 || lstExam.Items.Count == 0) return;

            for (int i = 0; i < lstExam.Items.Count; i++)
            {
                var row = lstExam.Items[i] as DataRowView;
                if (row == null) continue;
                if (Convert.ToInt32(row["ExamID"]) != examId) continue;
                lstExam.SelectedIndex = i;
                return;
            }
        }

        // ========================= LIST DRAW =========================

        private void LstExam_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var row = lstExam.Items[e.Index] as DataRowView;
            if (row == null) return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Rectangle b = e.Bounds;
            b.Inflate(-2, -2);

            Color bg = selected ? Color.FromArgb(239, 246, 255) : Color.White;
            using (var brush = new SolidBrush(bg))
                e.Graphics.FillRectangle(brush, b);

            if (selected)
            {
                using (var pen = new Pen(BLUE, 3))
                    e.Graphics.DrawLine(pen, b.Left, b.Top + 4, b.Left, b.Bottom - 4);
            }

            using (var penB = new Pen(BORDER))
                e.Graphics.DrawRectangle(penB, b);

            var moreRect = new Rectangle(b.Right - 30, b.Y + 20, 24, 28);

            string name = row["ExamName"].ToString();
            string mode = row.Row.Table.Columns.Contains("ExamMode") && row["ExamMode"].ToString() == "Practice" ? "Ôn" : "Thi";
            string random = row.Row.Table.Columns.Contains("IsRandomQuestion") && row["IsRandomQuestion"] != DBNull.Value && Convert.ToBoolean(row["IsRandomQuestion"]) ? " • random" : "";
            bool published = row.Row.Table.Columns.Contains("IsPublishedToServer") && row["IsPublishedToServer"] != DBNull.Value && Convert.ToBoolean(row["IsPublishedToServer"]);
            bool sharedLink = row.Row.Table.Columns.Contains("ExamLink") && row["ExamLink"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["ExamLink"].ToString());
            string visibility = published ? " • server" : (sharedLink ? " • link" : " • riêng tư");
            string meta = mode + " • " + row["Duration"] + " phút" + random + visibility;
            string count = row["QuestionCount"].ToString();

            using (var tb = new SolidBrush(TEXT_MAIN))
                e.Graphics.DrawString(name, new Font("Segoe UI", 10F, FontStyle.Bold), tb, b.X + 14, b.Y + 12);

            using (var sb = new SolidBrush(TEXT_SUB))
                e.Graphics.DrawString(meta, new Font("Segoe UI", 8.5F), sb, b.X + 14, b.Y + 34);

            var pill = new Rectangle(moreRect.Left - 50, b.Y + 22, 40, 22);
            using (var pb = new SolidBrush(Color.FromArgb(241, 245, 249)))
                e.Graphics.FillRectangle(pb, pill);
            using (var tb2 = new SolidBrush(TEXT_SUB))
            {
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(count, new Font("Segoe UI", 8.5F, FontStyle.Bold), tb2, pill, sf);
            }
            using (var dotBrush = new SolidBrush(TEXT_SUB))
            {
                var dotFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString("⋮", new Font("Segoe UI", 14F, FontStyle.Bold), dotBrush, moreRect, dotFormat);
            }

        }

        private void DgvQuestions_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string col = dgvQuestions.Columns[e.ColumnIndex].Name;

            if ((col == "Số điểm" || col == "Trạng thái") && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, true);
                string val = e.Value.ToString();
                Color bg = Color.FromArgb(241, 245, 249);
                Color fg = TEXT_SUB;
                if (col == "Số điểm" && val != "-")
                {
                    bg = Color.FromArgb(239, 246, 255);
                    fg = BLUE;
                }
                else if (col == "Trạng thái" && val == "Đã gán")
                {
                    fg = GREEN;
                    bg = Color.FromArgb(220, 252, 231);
                }

                var r = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 10, e.CellBounds.Width - 16, e.CellBounds.Height - 18);
                using (var b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, r);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var tb = new SolidBrush(fg))
                    e.Graphics.DrawString(val, new Font("Segoe UI", 8.5F, FontStyle.Bold), tb, r, sf);
                e.Handled = true;
            }
        }

        private void DgvQuestions_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string col = dgvQuestions.Columns[e.ColumnIndex].Name;
            if (!dgvQuestions.Columns.Contains("ID")) return;
            int qid = Convert.ToInt32(dgvQuestions.Rows[e.RowIndex].Cells["ID"].Value);

            if (col == "colView")
            {
                DataTable dt = db.ExecuteQuery("SELECT QuestionContent, OptionA, OptionB, OptionC, OptionD, CorrectAnswer FROM Questions WHERE QuestionID=" + qid + OwnerQuestionCondition("Questions"));
                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    MessageBox.Show(
                        r["QuestionContent"] + "\n\nA. " + r["OptionA"] + "\nB. " + r["OptionB"] +
                        "\nC. " + r["OptionC"] + "\nD. " + r["OptionD"] + "\n\nĐáp án: " + r["CorrectAnswer"],
                        "Chi tiết câu hỏi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else if (col == "colEdit")
            {
                using (var dlg = new QuestionEditDialog(qid, null, ownerUserId))
                {
                    if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                    {
                        LoadExamQuestions();
                        LoadExamList();
                    }
                }
            }
            else if (col == "colDelete")
            {
                if (selectedExamID <= 0) return;
                if (MessageBox.Show("Gỡ câu hỏi khỏi bộ đề này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                db.ExecuteNonQuery("DELETE FROM ExamQuestions WHERE ExamID=" + selectedExamID + " AND QuestionID=" + qid);
                LoadExamQuestions();
                LoadExamList();
            }
        }

        // ========================= EVENTS =========================

        private void LstExam_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (isLoadingExamList) return;
            if (lstExam.SelectedValue == null) { ClearDetail(); return; }
            if (!int.TryParse(lstExam.SelectedValue.ToString(), out int id)) return;

            selectedExamID = id;
            DataRowView row = lstExam.SelectedItem as DataRowView;
            lblDetailTitle.Text = row?["ExamName"].ToString() ?? "Bộ đề";
            if (row != null) UpdateSelectedExamSummary(row.Row);
            currentPage = 1;
            LoadExamQuestions();
        }

        private void TxtSearchExam_TextChanged(object sender, EventArgs e)
        {
            if (IsPlaceholder(txtSearchExam, "Tìm kiếm bộ đề...")) return;
            LoadExamList();
        }

        private void TxtSearchQuestion_TextChanged(object sender, EventArgs e)
        {
            if (IsPlaceholder(txtSearchQuestion, "Tìm kiếm câu hỏi...")) return;
            if (selectedExamID > 0) { currentPage = 1; LoadExamQuestions(); }
        }

        private void BtnImportFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Question files (*.xlsx;*.csv;*.txt;*.tsv)|*.xlsx;*.csv;*.txt;*.tsv|All files (*.*)|*.*";
                ofd.Title = "Chọn file câu hỏi";
                if (ofd.ShowDialog(FindForm()) != DialogResult.OK) return;

                var service = new QuestionImportService();
                QuestionImportResult preview;
                try
                {
                    preview = service.PreviewImport(ofd.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(QuestionImportService.BuildReadFileErrorMessage(ex, ofd.FileName) + Environment.NewLine + Environment.NewLine + QuestionImportService.BuildGuide(),
                        "Đọc file thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!ShowImportPreviewDialog(ofd.FileName, preview))
                    return;

                bool createdNewExam = selectedExamID <= 0;
                int importExamId = selectedExamID;
                if (createdNewExam)
                    importExamId = CreateExamForImportedFile(ofd.FileName);

                try
                {
                    var result = service.Import(ofd.FileName, ownerUserId, importExamId > 0 ? (int?)importExamId : null);
                    string linked = importExamId > 0 ? Environment.NewLine + "Đã thêm vào bộ đề: " + result.Linked + " câu." : "";
                    string createdExamMessage = createdNewExam && importExamId > 0
                        ? Environment.NewLine + "Đã tạo bộ đề mới: " + Path.GetFileNameWithoutExtension(ofd.FileName) + "."
                        : "";
                    string errors = result.Errors.Count > 0 ? Environment.NewLine + "Một số dòng cần xem lại:" + Environment.NewLine + string.Join(Environment.NewLine, result.Errors) : "";
                    MessageBox.Show("Đã nhập " + result.Imported + " câu hỏi hợp lệ." + linked + createdExamMessage + Environment.NewLine + "Bỏ qua: " + result.Skipped + " dòng." + errors,
                        "Nhập file", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadExamList();
                    if (importExamId > 0)
                        SelectExamInList(importExamId);
                    else
                        LoadExamQuestions();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(QuestionImportService.BuildReadFileErrorMessage(ex, ofd.FileName),
                        "Nhập file thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool ShowImportPreviewDialog(string fileName, QuestionImportResult preview)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Xem trước khi nhập câu hỏi";
                dlg.Size = new Size(720, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = UiTheme.PageBg;
                dlg.Font = Font;

                var lblTitle = new Label
                {
                    Text = "Kiểm tra file trước khi nhập",
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(20, 18)
                };
                var lblFile = new Label
                {
                    Text = "File: " + Path.GetFileName(fileName),
                    AutoSize = true,
                    ForeColor = TEXT_SUB,
                    Location = new Point(22, 50)
                };
                var txtGuide = new TextBox
                {
                    Location = new Point(20, 80),
                    Size = new Size(660, 92),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = QuestionImportService.BuildGuide()
                };
                UiTheme.ApplyTextBox(txtGuide);

                var lblTotal = new Label { Text = "Tổng dòng đọc được: " + preview.TotalRows, AutoSize = true, Location = new Point(20, 192), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
                var lblValid = new Label { Text = "Dòng hợp lệ có thể nhập: " + preview.ValidRows, AutoSize = true, Location = new Point(20, 220), ForeColor = GREEN };
                var lblSkipped = new Label { Text = "Dòng bị bỏ qua: " + preview.Skipped, AutoSize = true, Location = new Point(20, 246), ForeColor = ORANGE };

                var lblErrorTitle = new Label
                {
                    Text = "Các lỗi phát hiện trước khi nhập",
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Location = new Point(20, 286)
                };
                var txtErrors = new TextBox
                {
                    Location = new Point(20, 314),
                    Size = new Size(660, 118),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = preview.Errors.Count == 0 ? "Không phát hiện lỗi định dạng phổ biến. Bạn có thể nhập file ngay." : string.Join(Environment.NewLine, preview.Errors)
                };
                UiTheme.ApplyTextBox(txtErrors);

                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(510, 446), Size = new Size(80, 32) };
                var btnOk = new Button { Text = "Xác nhận nhập", DialogResult = DialogResult.OK, Location = new Point(600, 446), Size = new Size(80, 32), Enabled = preview.ValidRows > 0 };
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);
                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);

                dlg.Controls.AddRange(new Control[] { lblTitle, lblFile, txtGuide, lblTotal, lblValid, lblSkipped, lblErrorTitle, txtErrors, btnCancel, btnOk });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                return dlg.ShowDialog(FindForm()) == DialogResult.OK;
            }
        }

        private void BtnDownloadTemplate_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel file (*.xlsx)|*.xlsx|CSV file (*.csv)|*.csv|TSV file (*.tsv)|*.tsv";
                sfd.FileName = "Mau_nhap_cau_hoi.xlsx";
                sfd.Title = "Lưu file mẫu câu hỏi";
                sfd.OverwritePrompt = true;
                if (sfd.ShowDialog(FindForm()) != DialogResult.OK) return;

                try
                {
                    QuestionImportService.CreateTemplateFile(sfd.FileName);
                    MessageBox.Show("Đã tạo file mẫu thành công. Bạn có thể điền câu hỏi vào file này rồi nhập lại vào phần mềm.",
                        "File mẫu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (IOException)
                {
                    MessageBox.Show("Không thể ghi đè file mẫu này vì file đang được mở hoặc đang bị chương trình khác sử dụng. Vui lòng đóng file cũ hoặc chọn tên khác.",
                        "Không thể tạo file mẫu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể tạo file mẫu." + Environment.NewLine + ex.Message,
                        "Không thể tạo file mẫu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnCreateSet_Click(object sender, EventArgs e)
        {
            var defaults = ExamSettings.Default();
            if (studentMode)
            {
                defaults.Mode = "Practice";
                defaults.MaxAttempts = null;
            }
            if (!ShowExamDialog(studentMode ? "Tạo đề ôn mới" : "Tạo bộ đề mới", defaults, out ExamSettings settings)) return;
            string ownerColumn = ownerUserId > 0 ? ", CreatedByUserID" : "";
            string ownerValue = ownerUserId > 0 ? ", " + ownerUserId : "";
            db.ExecuteNonQuery(
                "INSERT INTO Exams (ExamName, Duration, ExamPassword, ExamMode, StartTime, EndTime, MaxAttempts, IsRandomQuestion, RandomQuestionCount, RoomCode, ExamLink, ScoreScale, PracticeMaxWrongPerQuestion, PracticeShowAnswer, PracticeShowPointGain, PracticeAutoNextOnCorrect, IsPublishedToServer, RequireClassBeforeStart, AllowStudentEditClass, PracticeAllowStudentOptions, PracticeNoLoginRequired, PracticeUnlimitedTime" + ownerColumn + ") VALUES (" +
                "N'" + SqlText(settings.Name) + "', " + settings.Duration + ", " + SqlNullableText(settings.Password) + ", N'" + settings.Mode + "', " +
                SqlNullableDate(settings.StartTime) + ", " + SqlNullableDate(settings.EndTime) + ", " + SqlNullableInt(settings.MaxAttempts) + ", " +
                (settings.IsRandomQuestion ? "1" : "0") + ", " + SqlNullableInt(settings.RandomQuestionCount) + ", " + SqlNullableText(settings.RoomCode) + ", " + SqlNullableText(settings.ExamLink) + ", " +
                settings.ScoreScale.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + SqlNullableInt(settings.PracticeMaxWrongPerQuestion) + ", " + (settings.PracticeShowAnswer ? "1" : "0") + ", " + (settings.PracticeShowPointGain ? "1" : "0") + ", " + (settings.PracticeAutoNextOnCorrect ? "1" : "0") + ", 0, " + (settings.RequireClassBeforeStart ? "1" : "0") + ", " + (settings.AllowStudentEditClass ? "1" : "0") + ", " + (settings.PracticeAllowStudentOptions ? "1" : "0") + ", " + (settings.PracticeNoLoginRequired ? "1" : "0") + ", " + (settings.PracticeUnlimitedTime ? "1" : "0") + ownerValue + ")");
            int newId = QueryCount("SELECT MAX(ExamID) FROM Exams" + (ownerUserId > 0 ? " WHERE CreatedByUserID=" + ownerUserId : ""));
            MessageBox.Show(
                (studentMode ? "Tạo đề ôn thành công." : "Tạo bộ đề thành công.") + Environment.NewLine +
                "Bước tiếp theo: thêm câu hỏi cho bộ đề này rồi mới chia sẻ cho người học.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadExamList();
            SelectExamInList(newId);
        }

        private void BtnEditSet_Click(object sender, EventArgs e)
        {
            if (selectedExamID <= 0) { MessageBox.Show("Vui lòng chọn bộ đề.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            DataTable dt = db.ExecuteQuery("SELECT ExamName, Duration, ExamPassword, ExamMode, StartTime, EndTime, MaxAttempts, IsRandomQuestion, RandomQuestionCount, RoomCode, ExamLink, ScoreScale, PracticeMaxWrongPerQuestion, PracticeShowAnswer, PracticeShowPointGain, PracticeAutoNextOnCorrect, RequireClassBeforeStart, AllowStudentEditClass, PracticeAllowStudentOptions, PracticeNoLoginRequired, PracticeUnlimitedTime FROM Exams WHERE ExamID=" + selectedExamID + (ownerUserId > 0 ? " AND CreatedByUserID=" + ownerUserId : ""));
            if (dt.Rows.Count == 0) return;
            if (!ShowExamDialog("Chỉnh sửa bộ đề", ExamSettings.FromRow(dt.Rows[0]), out ExamSettings settings)) return;
            db.ExecuteNonQuery(
                "UPDATE Exams SET ExamName=N'" + SqlText(settings.Name) + "', Duration=" + settings.Duration +
                ", ExamPassword=" + SqlNullableText(settings.Password) +
                ", ExamMode=N'" + settings.Mode + "'" +
                ", StartTime=" + SqlNullableDate(settings.StartTime) +
                ", EndTime=" + SqlNullableDate(settings.EndTime) +
                ", MaxAttempts=" + SqlNullableInt(settings.MaxAttempts) +
                ", IsRandomQuestion=" + (settings.IsRandomQuestion ? "1" : "0") +
                ", RandomQuestionCount=" + SqlNullableInt(settings.RandomQuestionCount) +
                ", RoomCode=" + SqlNullableText(settings.RoomCode) +
                ", ExamLink=" + SqlNullableText(settings.ExamLink) +
                ", ScoreScale=" + settings.ScoreScale.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                ", PracticeMaxWrongPerQuestion=" + SqlNullableInt(settings.PracticeMaxWrongPerQuestion) +
                ", PracticeShowAnswer=" + (settings.PracticeShowAnswer ? "1" : "0") +
                ", PracticeShowPointGain=" + (settings.PracticeShowPointGain ? "1" : "0") +
                ", PracticeAutoNextOnCorrect=" + (settings.PracticeAutoNextOnCorrect ? "1" : "0") +
                ", RequireClassBeforeStart=" + (settings.RequireClassBeforeStart ? "1" : "0") +
                ", AllowStudentEditClass=" + (settings.AllowStudentEditClass ? "1" : "0") +
                ", PracticeAllowStudentOptions=" + (settings.PracticeAllowStudentOptions ? "1" : "0") +
                ", PracticeNoLoginRequired=" + (settings.PracticeNoLoginRequired ? "1" : "0") +
                ", PracticeUnlimitedTime=" + (settings.PracticeUnlimitedTime ? "1" : "0") +
                " WHERE ExamID=" + selectedExamID + (ownerUserId > 0 ? " AND CreatedByUserID=" + ownerUserId : ""));
            MessageBox.Show("Cập nhật thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadExamList();
        }

        private void BtnDuplicateSet_Click(object sender, EventArgs e)
        {
            if (selectedExamID <= 0) { MessageBox.Show("Vui lòng chọn bộ đề.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            DataTable dt = db.ExecuteQuery("SELECT ExamName, Duration, ExamPassword, ExamMode, StartTime, EndTime, MaxAttempts, IsRandomQuestion, RandomQuestionCount, RoomCode, ExamLink, ScoreScale, PracticeMaxWrongPerQuestion, PracticeShowAnswer, PracticeShowPointGain, PracticeAutoNextOnCorrect, IsPublishedToServer, RequireClassBeforeStart, AllowStudentEditClass, PracticeAllowStudentOptions, PracticeNoLoginRequired, PracticeUnlimitedTime FROM Exams WHERE ExamID=" + selectedExamID + (ownerUserId > 0 ? " AND CreatedByUserID=" + ownerUserId : ""));
            if (dt.Rows.Count == 0) return;
            ExamSettings copy = ExamSettings.FromRow(dt.Rows[0]);
            copy.Name = copy.Name + " (bản sao)";
            string ownerColumn = ownerUserId > 0 ? ", CreatedByUserID" : "";
            string ownerValue = ownerUserId > 0 ? ", " + ownerUserId : "";
            db.ExecuteNonQuery(
                "INSERT INTO Exams (ExamName, Duration, ExamPassword, ExamMode, StartTime, EndTime, MaxAttempts, IsRandomQuestion, RandomQuestionCount, RoomCode, ExamLink, ScoreScale, PracticeMaxWrongPerQuestion, PracticeShowAnswer, PracticeShowPointGain, PracticeAutoNextOnCorrect, IsPublishedToServer, RequireClassBeforeStart, AllowStudentEditClass, PracticeAllowStudentOptions, PracticeNoLoginRequired, PracticeUnlimitedTime" + ownerColumn + ") VALUES (" +
                "N'" + SqlText(copy.Name) + "', " + copy.Duration + ", " + SqlNullableText(copy.Password) + ", N'" + copy.Mode + "', " +
                SqlNullableDate(copy.StartTime) + ", " + SqlNullableDate(copy.EndTime) + ", " + SqlNullableInt(copy.MaxAttempts) + ", " +
                (copy.IsRandomQuestion ? "1" : "0") + ", " + SqlNullableInt(copy.RandomQuestionCount) + ", " + SqlNullableText(copy.RoomCode + "-COPY") + ", NULL, " +
                copy.ScoreScale.ToString(System.Globalization.CultureInfo.InvariantCulture) + ", " + SqlNullableInt(copy.PracticeMaxWrongPerQuestion) + ", " + (copy.PracticeShowAnswer ? "1" : "0") + ", " + (copy.PracticeShowPointGain ? "1" : "0") + ", " + (copy.PracticeAutoNextOnCorrect ? "1" : "0") + ", 0, " + (copy.RequireClassBeforeStart ? "1" : "0") + ", " + (copy.AllowStudentEditClass ? "1" : "0") + ", " + (copy.PracticeAllowStudentOptions ? "1" : "0") + ", " + (copy.PracticeNoLoginRequired ? "1" : "0") + ", " + (copy.PracticeUnlimitedTime ? "1" : "0") + ownerValue + ")");
            int newId = QueryCount("SELECT MAX(ExamID) FROM Exams" + (ownerUserId > 0 ? " WHERE CreatedByUserID=" + ownerUserId : ""));
            db.ExecuteNonQuery("INSERT INTO ExamQuestions (ExamID, QuestionID, Points) SELECT " + newId + ", QuestionID, Points FROM ExamQuestions WHERE ExamID=" + selectedExamID);
            MessageBox.Show("Nhân bản bộ đề thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadExamList();
        }

        private void BtnDeleteSet_Click(object sender, EventArgs e)
        {
            if (selectedExamID <= 0) { MessageBox.Show("Vui lòng chọn bộ đề.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (MessageBox.Show("Bạn có chắc muốn xóa bộ đề này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            db.ExecuteNonQuery("DELETE FROM ExamAttemptAnswers WHERE ExamID=" + selectedExamID);
            db.ExecuteNonQuery("DELETE FROM Results WHERE ExamID=" + selectedExamID);
            db.ExecuteNonQuery("DELETE FROM ExamQuestions WHERE ExamID=" + selectedExamID);
            db.ExecuteNonQuery("DELETE FROM Exams WHERE ExamID=" + selectedExamID + (ownerUserId > 0 ? " AND CreatedByUserID=" + ownerUserId : ""));
            MessageBox.Show("Đã xóa bộ đề.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            selectedExamID = -1;
            LoadExamList();
            ClearDetail();
        }

        private void TogglePublishSelectedExam(bool publish)
        {
            if (selectedExamID <= 0)
            {
                MessageBox.Show("Vui lòng chọn bộ đề.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            db.ExecuteNonQuery("UPDATE Exams SET IsPublishedToServer=" + (publish ? "1" : "0") + " WHERE ExamID=" + selectedExamID + (ownerUserId > 0 ? " AND CreatedByUserID=" + ownerUserId : ""));
            MessageBox.Show(publish ? "Bộ đề đã được chia sẻ lên server." : "Bộ đề đã được ẩn khỏi server.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadExamList();
        }

        private void ShowExamInfo()
        {
            if (selectedExamID <= 0)
            {
                MessageBox.Show("Vui lòng chọn bộ đề.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable dt = db.ExecuteQuery(
                "SELECT ExamName, Duration, ExamMode, ScoreScale, MaxAttempts, RoomCode, ExamLink, IsPublishedToServer, PracticeMaxWrongPerQuestion, PracticeShowAnswer, PracticeShowPointGain, PracticeNoLoginRequired, PracticeUnlimitedTime " +
                "FROM Exams WHERE ExamID=" + selectedExamID + (ownerUserId > 0 ? " AND CreatedByUserID=" + ownerUserId : ""));
            if (dt.Rows.Count == 0) return;
            DataRow row = dt.Rows[0];
            string mode = row["ExamMode"].ToString() == "Practice" ? "Ôn" : "Thi";
            string info =
                "Tên bộ đề: " + row["ExamName"] + Environment.NewLine +
                "Loại: " + mode + Environment.NewLine +
                "Thời gian: " + row["Duration"] + " phút" + Environment.NewLine +
                "Thang điểm: " + row["ScoreScale"] + Environment.NewLine +
                "Chia sẻ server: " + ((row["IsPublishedToServer"] != DBNull.Value && Convert.ToBoolean(row["IsPublishedToServer"])) ? "Có" : "Không") + Environment.NewLine +
                "Chia sẻ link: " + ((row["ExamLink"] != DBNull.Value && !string.IsNullOrWhiteSpace(row["ExamLink"].ToString())) ? "Có" : "Không") + Environment.NewLine +
                "Mã phòng: " + (row["RoomCode"] == DBNull.Value ? "" : row["RoomCode"].ToString());

            if (row["ExamMode"].ToString() == "Practice")
            {
                info += Environment.NewLine +
                    "Sai tối đa / câu: " + (row["PracticeMaxWrongPerQuestion"] == DBNull.Value ? "0" : row["PracticeMaxWrongPerQuestion"].ToString()) + Environment.NewLine +
                    "Hiện đáp án: " + (Convert.ToBoolean(row["PracticeShowAnswer"]) ? "Có" : "Không") + Environment.NewLine +
                    "Hiện điểm cộng dần: " + (Convert.ToBoolean(row["PracticeShowPointGain"]) ? "Có" : "Không");
            }
            else
            {
                info += Environment.NewLine + "Số lần làm / tài khoản: " + (row["MaxAttempts"] == DBNull.Value ? "Không giới hạn" : row["MaxAttempts"].ToString());
            }

            MessageBox.Show(info, "Thông tin bộ đề", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCreateQuestion_Click(object sender, EventArgs e)
        {
            if (selectedExamID <= 0)
            {
                MessageBox.Show("Vui lòng chọn bộ đề trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            using (var dlg = new QuestionEditDialog(null, selectedExamID, ownerUserId))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    LoadExamQuestions();
                    LoadExamList();
                }
            }
        }

        private void BtnAddFromBank_Click(object sender, EventArgs e)
        {
            if (selectedExamID <= 0) { MessageBox.Show("Vui lòng chọn bộ đề trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            DataTable bank = db.ExecuteQuery(
                "SELECT TOP 200 QuestionID, QuestionContent FROM Questions " +
                "WHERE QuestionID NOT IN (SELECT QuestionID FROM ExamQuestions WHERE ExamID=" + selectedExamID + ") " +
                (ownerUserId > 0 ? "AND (CreatedByUserID=" + ownerUserId + " OR ISNULL(IsPublishedToServer,0)=1) " : "") +
                "ORDER BY QuestionID DESC");

            if (bank.Rows.Count == 0) { MessageBox.Show("Không còn câu hỏi để thêm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

            using (var dlg = new Form())
            {
                dlg.Text = "Thêm câu hỏi vào bộ đề";
                dlg.Size = new Size(520, 420);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UiTheme.PageBg;
                dlg.Font = Font;

                var lst = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F) };
                foreach (DataRow r in bank.Rows)
                    lst.Items.Add(new ListItem { Id = Convert.ToInt32(r["QuestionID"]), Text = r["QuestionID"] + " - " + r["QuestionContent"] });

                var btnOk = new Button { Text = "Thêm", Dock = DockStyle.Bottom, Height = 36, DialogResult = DialogResult.OK };
                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);
                dlg.Controls.Add(lst);
                dlg.Controls.Add(btnOk);
                dlg.AcceptButton = btnOk;

                if (dlg.ShowDialog(FindForm()) == DialogResult.OK && lst.SelectedItem is ListItem item)
                {
                    db.ExecuteNonQuery("INSERT INTO ExamQuestions (ExamID, QuestionID, Points) SELECT " + selectedExamID + ", QuestionID, ISNULL(Points, 1) FROM Questions WHERE QuestionID=" + item.Id);
                    LoadExamQuestions();
                    LoadExamList();
                }
            }
        }

        private void BtnDistributeScore_Click(object sender, EventArgs e)
        {
            if (selectedExamID <= 0)
            {
                MessageBox.Show("Vui lòng chọn bộ đề trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable dt = db.ExecuteQuery("SELECT ScoreScale, IsRandomQuestion, RandomQuestionCount FROM Exams WHERE ExamID=" + selectedExamID);
            decimal scale = 10M;
            bool isRandomQuestion = false;
            int randomQuestionCount = 0;
            if (dt.Rows.Count > 0 && dt.Rows[0]["ScoreScale"] != DBNull.Value)
            {
                scale = Convert.ToDecimal(dt.Rows[0]["ScoreScale"]);
                if (dt.Rows[0]["IsRandomQuestion"] != DBNull.Value)
                    isRandomQuestion = Convert.ToBoolean(dt.Rows[0]["IsRandomQuestion"]);
                if (dt.Rows[0]["RandomQuestionCount"] != DBNull.Value)
                    randomQuestionCount = Convert.ToInt32(dt.Rows[0]["RandomQuestionCount"]);
            }

            int totalQuestionCount = QueryCount("SELECT COUNT(*) FROM ExamQuestions WHERE ExamID=" + selectedExamID);
            int questionCount = totalQuestionCount;
            if (isRandomQuestion && randomQuestionCount > 0)
                questionCount = Math.Min(totalQuestionCount, randomQuestionCount);

            if (questionCount <= 0)
            {
                MessageBox.Show("Bộ đề chưa có câu hỏi để chia điểm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal pointPerQuestion = Math.Round(scale / questionCount, 2);
            string scopeText = isRandomQuestion && randomQuestionCount > 0
                ? "Đề đang random " + questionCount + " câu từ tổng " + totalQuestionCount + " câu."
                : "Đề hiện dùng toàn bộ " + questionCount + " câu hỏi.";
            if (MessageBox.Show(scopeText + "\nChia đều " + scale.ToString("0.##") + " điểm cho " + questionCount + " câu hỏi dùng để thi?\nMỗi câu sẽ được khoảng " + pointPerQuestion.ToString("0.##") + " điểm.",
                "Chia đều điểm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            db.ExecuteNonQuery("UPDATE ExamQuestions SET Points=" + pointPerQuestion.ToString(System.Globalization.CultureInfo.InvariantCulture) + " WHERE ExamID=" + selectedExamID);
            LoadExamQuestions();
        }

        private void LstExam_MouseDown(object sender, MouseEventArgs e)
        {
            int index = lstExam.IndexFromPoint(e.Location);
            if (index < 0) return;

            Rectangle itemRect = lstExam.GetItemRectangle(index);
            Rectangle moreRect = new Rectangle(itemRect.Right - 34, itemRect.Top + 20, 24, 28);
            if (!moreRect.Contains(e.Location)) return;

            lstExam.SelectedIndex = index;
            EnsureQuickMenu();
            examQuickMenu.Show(lstExam, new Point(moreRect.Left, moreRect.Bottom));
        }

        private void BtnManualScore_Click(object sender, EventArgs e)
        {
            if (selectedExamID <= 0)
            {
                MessageBox.Show("Vui lòng chọn bộ đề trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable dt = db.ExecuteQuery(
                "SELECT q.QuestionID, q.QuestionContent, ISNULL(eq.Points, 1) AS Points, ISNULL(e.ScoreScale, 10) AS ScoreScale " +
                "FROM ExamQuestions eq INNER JOIN Questions q ON eq.QuestionID=q.QuestionID INNER JOIN Exams e ON e.ExamID=eq.ExamID " +
                "WHERE eq.ExamID=" + selectedExamID + " ORDER BY q.QuestionID");
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Bộ đề chưa có câu hỏi để chỉnh điểm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal scoreScale = Convert.ToDecimal(dt.Rows[0]["ScoreScale"]);
            using (var dlg = new Form())
            {
                dlg.Text = "Chỉnh điểm thủ công";
                dlg.Size = new Size(760, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.BackColor = UiTheme.PageBg;
                dlg.Font = Font;

                var lbl = new Label { Text = "Thang điểm bộ đề: " + scoreScale.ToString("0.##"), Location = new Point(20, 18), AutoSize = true };
                var grid = new DataGridView
                {
                    Location = new Point(20, 46),
                    Size = new Size(700, 380),
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    RowHeadersVisible = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
                };
                UiTheme.ApplyGrid(grid);
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", ReadOnly = true, FillWeight = 15 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colQuestion", HeaderText = "Câu hỏi", ReadOnly = true, FillWeight = 60 });
                grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPoint", HeaderText = "Điểm", FillWeight = 25 });

                foreach (DataRow row in dt.Rows)
                    grid.Rows.Add(row["QuestionID"], row["QuestionContent"], Convert.ToDecimal(row["Points"]).ToString("0.##"));

                var btnOk = new Button { Text = "Lưu điểm", DialogResult = DialogResult.OK, Location = new Point(540, 440), Size = new Size(85, 32) };
                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(635, 440), Size = new Size(85, 32) };
                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);
                dlg.Controls.AddRange(new Control[] { lbl, grid, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;

                decimal totalManual = 0M;
                var updates = new System.Collections.Generic.List<string>();
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (!decimal.TryParse((row.Cells["colPoint"].Value ?? "0").ToString(), out decimal point) || point < 0)
                    {
                        MessageBox.Show("Điểm nhập tay phải là số không âm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    totalManual += point;
                    int questionId = Convert.ToInt32(row.Cells["colId"].Value);
                    updates.Add("UPDATE ExamQuestions SET Points=" + point.ToString(System.Globalization.CultureInfo.InvariantCulture) + " WHERE ExamID=" + selectedExamID + " AND QuestionID=" + questionId);
                }

                if (totalManual != scoreScale)
                {
                    DialogResult confirm = MessageBox.Show(
                        "Tổng điểm bạn nhập là " + totalManual.ToString("0.##") + " nhưng thang điểm bộ đề là " + scoreScale.ToString("0.##") + ".\nBạn có chắc vẫn muốn tạo/cập nhật theo cấu hình này không?",
                        "Kiểm tra thang điểm",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;
                }

                foreach (string sql in updates)
                    db.ExecuteNonQuery(sql);

                LoadExamQuestions();
            }
        }

        private void BtnShareExamLink_Click(object sender, EventArgs e)
        {
            if (selectedExamID <= 0)
            {
                MessageBox.Show("Vui lòng chọn bộ đề cần chia sẻ.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable dt = db.ExecuteQuery("SELECT ExamName, RoomCode, ExamLink, Duration FROM Exams WHERE ExamID=" + selectedExamID + (ownerUserId > 0 ? " AND CreatedByUserID=" + ownerUserId : ""));
            if (dt.Rows.Count == 0) return;

            string room = dt.Rows[0]["RoomCode"] == DBNull.Value ? "" : dt.Rows[0]["RoomCode"].ToString();
            if (string.IsNullOrWhiteSpace(room))
            {
                room = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            }

            try
            {
                StudentExamWebServer.EnsureStarted();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động web làm bài cho sinh viên.\n" + ex.Message,
                    "Chia sẻ link", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int durationMinutes = 30;
            if (dt.Rows[0].Table.Columns.Contains("Duration") && dt.Rows[0]["Duration"] != DBNull.Value)
                durationMinutes = Math.Max(1, Convert.ToInt32(dt.Rows[0]["Duration"]));

            string link = ShowShareLinkDialog(room, durationMinutes);
            if (string.IsNullOrWhiteSpace(link)) return;

            db.ExecuteNonQuery("UPDATE Exams SET RoomCode=N'" + SqlText(room) + "', ExamLink=N'" + SqlText(link) + "' WHERE ExamID=" + selectedExamID);

            Clipboard.SetText(link);
            MessageBox.Show(
                "Đã tạo link làm bài và sao chép sẵn cho bạn:" + Environment.NewLine +
                link,
                "Chia sẻ link làm bài", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadExamList();
        }

        private string ShowShareLinkDialog(string room, int durationMinutes)
        {
            string lanLink = ExamLinkShareService.BuildLanExamUrl(room);
            string configuredBaseUrl = ExamLinkShareService.GetConfiguredPublicBaseUrl();
            string publicLink = ExamLinkShareService.BuildPublicExamUrl(room);

            using (var dlg = new Form())
            {
                dlg.Text = "Chọn cách chia sẻ đề";
                dlg.Size = new Size(760, 380);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = UiTheme.PageBg;
                dlg.Font = Font;

                var lblMode = new Label { Text = "Kiểu link:", Location = new Point(20, 22), AutoSize = true };
                var cboMode = new ComboBox { Location = new Point(120, 18), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
                UiTheme.ApplyComboBox(cboMode);
                cboMode.Items.AddRange(new object[] { "Chia sẻ qua Internet", "Chỉ dùng trong cùng mạng LAN" });
                cboMode.SelectedIndex = string.IsNullOrWhiteSpace(publicLink) ? 1 : 0;

                var lblPublicBase = new Label { Text = "Đường dẫn chia sẻ:", Location = new Point(20, 62), AutoSize = true };
                var txtPublicBase = new TextBox { Location = new Point(120, 58), Width = 540, Text = configuredBaseUrl };
                var btnAutoTunnel = new Button { Text = "Tạo link tự động", Location = new Point(120, 92), Size = new Size(170, 30) };
                UiTheme.ApplyTextBox(txtPublicBase);
                UiTheme.ApplyButton(btnAutoTunnel, UiButtonVariant.Info);
                var lblHint = new Label
                {
                    Text = "Bạn có thể dán đường dẫn chia sẻ riêng hoặc để phần mềm tự tạo link mới.",
                    Location = new Point(300, 92),
                    Size = new Size(420, 40),
                    ForeColor = TEXT_SUB
                };

                var lblPreview = new Label { Text = "Link xem trước:", Location = new Point(20, 138), AutoSize = true };
                var txtPreview = new TextBox { Location = new Point(120, 134), Width = 600, Height = 72, Multiline = true, ReadOnly = true };
                UiTheme.ApplyTextBox(txtPreview);

                var lblGuide = new Label
                {
                    Text = "Nếu tạo link Internet mới, link Cloudflare cũ sẽ không được giữ lại khi đã hết hạn.",
                    Location = new Point(20, 220),
                    Size = new Size(700, 60),
                    ForeColor = TEXT_SUB
                };

                var btnOk = new Button { Text = "Dùng link này", DialogResult = DialogResult.OK, Location = new Point(520, 286), Size = new Size(100, 32) };
                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(630, 286), Size = new Size(90, 32) };
                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);

                Action clearStaleConfiguredUrl = () =>
                {
                    string currentBase = ExamLinkShareService.NormalizeBaseUrl(txtPublicBase.Text);
                    if (ExamLinkShareService.IsTryCloudflareUrl(currentBase) &&
                        string.IsNullOrWhiteSpace(ExamLinkShareService.GetConfiguredPublicBaseUrl()))
                    {
                        txtPublicBase.Text = "";
                    }
                };

                Action refreshPreview = () =>
                {
                    string baseUrl = ExamLinkShareService.NormalizeBaseUrl(txtPublicBase.Text);
                    string preview = cboMode.SelectedIndex == 0
                        ? (string.IsNullOrWhiteSpace(baseUrl) ? "" : baseUrl + "/exam/" + room)
                        : lanLink;
                    txtPreview.Text = preview;
                    txtPublicBase.Enabled = cboMode.SelectedIndex == 0;
                    btnAutoTunnel.Enabled = cboMode.SelectedIndex == 0;

                    if (cboMode.SelectedIndex == 0)
                    {
                        string warning = ExamLinkShareService.BuildDurationWarning(baseUrl, durationMinutes);
                        lblGuide.Text = string.IsNullOrWhiteSpace(warning)
                            ? "Link Internet đang dùng URL cố định. Bạn có thể giữ nguyên link này cho các bài thi dài hơn."
                            : warning;
                    }
                    else
                    {
                        lblGuide.Text = "Link LAN ổn định hơn cho các ca thi dài, nhưng chỉ dùng được khi thí sinh ở cùng mạng nội bộ.";
                    }
                };

                cboMode.SelectedIndexChanged += (s, e) => refreshPreview();
                txtPublicBase.TextChanged += (s, e) => refreshPreview();
                btnAutoTunnel.Click += async (s, e) =>
                {
                    btnAutoTunnel.Enabled = false;
                    btnAutoTunnel.Text = "Đang tạo link...";
                    try
                    {
                        await Task.Run(() =>
                        {
                            if (!ExamLinkShareService.TryStartCloudflareTunnel(out string autoUrl, out string autoMessage))
                                throw new InvalidOperationException(autoMessage);
                            if (dlg.IsDisposed || !dlg.IsHandleCreated) return;
                            dlg.BeginInvoke((Action)(() =>
                            {
                                if (dlg.IsDisposed) return;
                                txtPublicBase.Text = autoUrl;
                                cboMode.SelectedIndex = 0;
                                MessageBox.Show(dlg, autoMessage + Environment.NewLine + autoUrl,
                                    "Cloudflare Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(dlg, ex.Message, "Cloudflare Tunnel", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    finally
                    {
                        btnAutoTunnel.Enabled = true;
                        btnAutoTunnel.Text = "Tạo link tự động";
                        refreshPreview();
                    }
                };
                clearStaleConfiguredUrl();
                refreshPreview();

                dlg.Controls.AddRange(new Control[] { lblMode, cboMode, lblPublicBase, txtPublicBase, btnAutoTunnel, lblHint, lblPreview, txtPreview, lblGuide, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return "";

                if (cboMode.SelectedIndex == 0)
                {
                    string baseUrl = ExamLinkShareService.NormalizeBaseUrl(txtPublicBase.Text);
                    if (string.IsNullOrWhiteSpace(baseUrl))
                    {
                        MessageBox.Show("Vui lòng nhập đường dẫn chia sẻ trước khi tạo link cho sinh viên.",
                            "Thiếu đường dẫn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return "";
                    }

                    if (ExamLinkShareService.IsTemporaryPublicBaseUrl(baseUrl) && durationMinutes >= 45)
                    {
                        DialogResult confirm = MessageBox.Show(
                            "Bài thi này khá dài nhưng đang dùng link Cloudflare tạm thời." + Environment.NewLine +
                            "Link loại này có thể đổi hoặc mất hiệu lực nếu tunnel bị ngắt." + Environment.NewLine + Environment.NewLine +
                            "Bạn vẫn muốn dùng link này chứ?",
                            "Cảnh báo link tạm thời",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        if (confirm != DialogResult.Yes) return "";
                    }

                    ExamLinkShareService.SaveConfiguredPublicBaseUrl(baseUrl);
                    return baseUrl + "/exam/" + room;
                }

                return lanLink;
            }
        }

        private bool ShowExamDialog(string title, ExamSettings defaults, out ExamSettings settings)
        {
            settings = null;
            using (var dlg = new Form())
            {
                dlg.Text = title;
                dlg.ClientSize = new Size(560, 700);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = UiTheme.PageBg;
                dlg.Font = Font;
                dlg.AutoScroll = true;

                var lblIntro = new Label { Text = "Thiết lập thông tin cơ bản trước, sau đó mới thêm câu hỏi và chia sẻ cho người học.", Location = new Point(20, 18), Size = new Size(500, 22), ForeColor = TEXT_SUB };
                var lblN = new Label { Text = "Tên bộ đề:", Location = new Point(20, 52), AutoSize = true };
                var txtN = new TextBox { Text = defaults.Name, Location = new Point(20, 76), Width = 500 };
                var lblD = new Label { Text = "Thời gian làm bài (phút):", Location = new Point(20, 112), AutoSize = true };
                var nud = new NumericUpDown { Location = new Point(20, 136), Width = 120, Minimum = 1, Maximum = 300, Value = Math.Max(1, Math.Min(300, defaults.Duration)) };
                var lblMode = new Label { Text = "Loại:", Location = new Point(160, 112), AutoSize = true };
                var cboMode = new ComboBox { Location = new Point(160, 136), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
                UiTheme.ApplyTextBox(txtN);
                UiTheme.ApplyComboBox(cboMode);
                cboMode.Items.AddRange(new object[] { "Thi", "Ôn" });
                cboMode.SelectedIndex = defaults.Mode == "Practice" ? 1 : 0;
                if (studentMode)
                {
                    cboMode.SelectedIndex = 1;
                    cboMode.Enabled = false;
                }
                var chkRandom = new CheckBox { Text = "Random câu hỏi khi học sinh vào làm", Checked = defaults.IsRandomQuestion, Location = new Point(320, 138), AutoSize = true };
                var lblRandomCount = new Label { Text = "Số câu random:", Location = new Point(20, 170), AutoSize = true };
                var nudRandomCount = new NumericUpDown { Location = new Point(130, 166), Width = 80, Minimum = 1, Maximum = 300, Value = Math.Max(1, Math.Min(300, defaults.RandomQuestionCount ?? 50)) };
                var lblRandomHint = new Label { Text = "Ví dụ: rút 50 câu từ ngân hàng câu hỏi của đề", Location = new Point(220, 170), AutoSize = true, ForeColor = TEXT_SUB };
                void SyncRandomCount()
                {
                    nudRandomCount.Enabled = lblRandomCount.Enabled = lblRandomHint.Enabled = chkRandom.Checked;
                }
                chkRandom.CheckedChanged += (s, e) => SyncRandomCount();
                SyncRandomCount();

                var lblPass = new Label { Text = "Mật khẩu bộ đề (không bắt buộc):", Location = new Point(20, 206), AutoSize = true };
                var txtPass = new TextBox { Text = defaults.Password, Location = new Point(20, 230), Width = 230 };
                UiTheme.ApplyTextBox(txtPass);
                var lblAttempts = new Label { Text = "Số lần làm / tài khoản:", Location = new Point(280, 206), AutoSize = true };
                var nudAttempts = new NumericUpDown { Location = new Point(280, 230), Width = 120, Minimum = 0, Maximum = 100, Value = defaults.MaxAttempts ?? 0 };
                var lblAttemptHint = new Label { Text = "0 = không giới hạn", Location = new Point(405, 234), AutoSize = true, ForeColor = TEXT_SUB };
                var lblScale = new Label { Text = "Thang điểm bộ đề:", Location = new Point(20, 268), AutoSize = true };
                var nudScale = new NumericUpDown { Location = new Point(20, 292), Width = 120, Minimum = 1, Maximum = 100, DecimalPlaces = 2, Value = defaults.ScoreScale };
                var lblScaleHint = new Label { Text = "Dùng khi chia đều điểm hoặc kiểm tra tổng điểm nhập tay.", Location = new Point(150, 296), AutoSize = true, ForeColor = TEXT_SUB };

                var grpPractice = new GroupBox { Text = "Tùy chọn luyện đề mặc định", Location = new Point(20, 330), Size = new Size(490, 270) };
                var lblPracticeWrong = new Label { Text = "Sai tối đa / câu:", Location = new Point(16, 30), AutoSize = true, Parent = grpPractice };
                var nudPracticeWrong = new NumericUpDown { Location = new Point(130, 26), Width = 70, Minimum = 0, Maximum = 5, Value = defaults.PracticeMaxWrongPerQuestion ?? 0, Parent = grpPractice };
                var chkPracticeShowAnswer = new CheckBox { Text = "Hiện đáp án", Location = new Point(16, 64), Size = new Size(120, 28), Checked = defaults.PracticeShowAnswer, Parent = grpPractice, CheckAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, UseVisualStyleBackColor = true };
                var chkPracticeShowPoint = new CheckBox { Text = "Hiện điểm cộng dần", Location = new Point(160, 64), Size = new Size(160, 28), Checked = defaults.PracticeShowPointGain, Parent = grpPractice, CheckAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, UseVisualStyleBackColor = true };
                var chkRequireClass = new CheckBox { Text = "Bắt nhập lớp trước khi bắt đầu", Location = new Point(16, 100), Size = new Size(220, 28), Checked = defaults.RequireClassBeforeStart, Parent = grpPractice, CheckAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, UseVisualStyleBackColor = true };
                var chkAllowClassEdit = new CheckBox { Text = "Cho người làm bài tự nhập / sửa lớp", Location = new Point(250, 100), Size = new Size(220, 28), Checked = defaults.AllowStudentEditClass, Parent = grpPractice, CheckAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, UseVisualStyleBackColor = true };
                var chkPracticeAllowStudentOptions = new CheckBox { Text = "Cho người ôn tự chọn đáp án/điểm/số lần sai", Location = new Point(16, 136), Size = new Size(360, 28), Checked = defaults.PracticeAllowStudentOptions, Parent = grpPractice, CheckAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, UseVisualStyleBackColor = true };
                var chkPracticeAutoNext = new CheckBox { Text = "Cho phép tự chuyển câu sau khi chọn đúng", Location = new Point(16, 168), Size = new Size(360, 28), Checked = defaults.PracticeAutoNextOnCorrect, Parent = grpPractice, CheckAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, UseVisualStyleBackColor = true };
                var chkPracticeNoLogin = new CheckBox { Text = "Cho vào ôn mà không cần đăng nhập", Location = new Point(16, 202), Size = new Size(240, 28), Checked = defaults.PracticeNoLoginRequired, Parent = grpPractice, CheckAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, UseVisualStyleBackColor = true };
                var chkPracticeUnlimitedTime = new CheckBox { Text = "Không giới hạn thời gian khi ôn", Location = new Point(250, 202), Size = new Size(220, 28), Checked = defaults.PracticeUnlimitedTime, Parent = grpPractice, CheckAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleLeft, UseVisualStyleBackColor = true };

                var lblAccessTitle = new Label { Text = "Cách người học vào bài", Location = new Point(20, 574), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = TEXT_MAIN };
                var lblAccessHint = new Label { Text = "Bạn có thể dùng mã phòng, link chia sẻ hoặc giới hạn theo khung thời gian ở bước sau.", Location = new Point(20, 598), Size = new Size(500, 20), ForeColor = TEXT_SUB };
                var lblRoom = new Label { Text = "Mã phòng thi:", Location = new Point(20, 626), AutoSize = true };
                var txtRoom = new TextBox { Text = defaults.RoomCode, Location = new Point(110, 622), Width = 140 };
                var lblLink = new Label { Text = "Link thi:", Location = new Point(282, 626), AutoSize = true };
                var txtLink = new TextBox { Text = defaults.ExamLink, Location = new Point(340, 622), Width = 170, ReadOnly = true };
                UiTheme.ApplyTextBox(txtRoom);
                UiTheme.ApplyTextBox(txtLink);

                var chkSchedule = new CheckBox { Text = "Giới hạn ngày giờ được làm", Checked = defaults.StartTime.HasValue || defaults.EndTime.HasValue, Location = new Point(20, 664), AutoSize = true };
                var lblStart = new Label { Text = "Từ:", Location = new Point(20, 698), AutoSize = true };
                var dtStart = new DateTimePicker { Location = new Point(60, 694), Width = 210, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Value = defaults.StartTime ?? DateTime.Now };
                var lblEnd = new Label { Text = "Đến:", Location = new Point(20, 738), AutoSize = true };
                var dtEnd = new DateTimePicker { Location = new Point(60, 734), Width = 210, Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm", Value = defaults.EndTime ?? DateTime.Now.AddDays(7) };

                void SyncSchedule()
                {
                    dtStart.Enabled = dtEnd.Enabled = lblStart.Enabled = lblEnd.Enabled = chkSchedule.Checked;
                }
                chkSchedule.CheckedChanged += (s, e) => SyncSchedule();
                SyncSchedule();

                void SyncModeOptions()
                {
                    bool practice = cboMode.SelectedIndex == 1;
                    nudAttempts.Enabled = lblAttempts.Enabled = lblAttemptHint.Enabled = !practice;
                    grpPractice.Enabled = practice;
                }
                cboMode.SelectedIndexChanged += (s, e) => SyncModeOptions();
                SyncModeOptions();

                var btnOk = new Button { Text = "Lưu và quay lại bộ đề", DialogResult = DialogResult.OK, Location = new Point(250, 815), Size = new Size(170, 32) };
                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(430, 815), Size = new Size(80, 32) };
                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);
                dlg.Controls.AddRange(new Control[] { lblIntro, lblN, txtN, lblD, nud, lblMode, cboMode, chkRandom, lblRandomCount, nudRandomCount, lblRandomHint, lblPass, txtPass, lblAttempts, nudAttempts, lblAttemptHint, lblScale, nudScale, lblScaleHint, grpPractice, lblAccessTitle, lblAccessHint, lblRoom, txtRoom, lblLink, txtLink, chkSchedule, lblStart, dtStart, lblEnd, dtEnd, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return false;
                if (string.IsNullOrWhiteSpace(txtN.Text))
                {
                    MessageBox.Show("Tên bộ đề không được để trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                if (chkSchedule.Checked && dtEnd.Value <= dtStart.Value)
                {
                    MessageBox.Show("Thời gian kết thúc phải sau thời gian bắt đầu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                settings = new ExamSettings
                {
                    Name = txtN.Text.Trim(),
                    Duration = nud.Value,
                    Password = txtPass.Text.Trim(),
                    Mode = cboMode.SelectedIndex == 1 ? "Practice" : "Exam",
                    StartTime = chkSchedule.Checked ? (DateTime?)dtStart.Value : null,
                    EndTime = chkSchedule.Checked ? (DateTime?)dtEnd.Value : null,
                    MaxAttempts = cboMode.SelectedIndex == 1 ? null : (nudAttempts.Value > 0 ? (int?)nudAttempts.Value : null),
                    IsRandomQuestion = chkRandom.Checked,
                    RandomQuestionCount = chkRandom.Checked ? (int?)nudRandomCount.Value : null,
                    ScoreScale = nudScale.Value,
                    PracticeMaxWrongPerQuestion = cboMode.SelectedIndex == 1 ? (int?)nudPracticeWrong.Value : null,
                    PracticeShowAnswer = chkPracticeShowAnswer.Checked,
                    PracticeShowPointGain = chkPracticeShowPoint.Checked,
                    PracticeAllowStudentOptions = chkPracticeAllowStudentOptions.Checked,
                    PracticeAutoNextOnCorrect = chkPracticeAutoNext.Checked,
                    PracticeNoLoginRequired = chkPracticeNoLogin.Checked,
                    PracticeUnlimitedTime = chkPracticeUnlimitedTime.Checked,
                    RequireClassBeforeStart = chkRequireClass.Checked,
                    AllowStudentEditClass = chkAllowClassEdit.Checked,
                    RoomCode = string.IsNullOrWhiteSpace(txtRoom.Text) ? Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant() : txtRoom.Text.Trim(),
                    ExamLink = ""
                };
                settings.ExamLink = "";
                return true;
            }
        }

        private class ExamSettings
        {
            public string Name;
            public decimal Duration;
            public string Password;
            public string Mode;
            public DateTime? StartTime;
            public DateTime? EndTime;
            public int? MaxAttempts;
            public bool IsRandomQuestion;
            public int? RandomQuestionCount;
            public decimal ScoreScale;
            public int? PracticeMaxWrongPerQuestion;
            public bool PracticeShowAnswer;
            public bool PracticeShowPointGain;
            public bool PracticeAllowStudentOptions;
            public bool PracticeAutoNextOnCorrect;
            public bool PracticeNoLoginRequired;
            public bool PracticeUnlimitedTime;
            public bool RequireClassBeforeStart;
            public bool AllowStudentEditClass;
            public string RoomCode;
            public string ExamLink;

            public static ExamSettings Default() =>
                new ExamSettings { Name = "", Duration = 30, Password = "", Mode = "Exam", StartTime = null, EndTime = null, MaxAttempts = null, IsRandomQuestion = false, RandomQuestionCount = 50, ScoreScale = 10, PracticeMaxWrongPerQuestion = 0, PracticeShowAnswer = true, PracticeShowPointGain = true, PracticeAllowStudentOptions = true, PracticeAutoNextOnCorrect = false, PracticeNoLoginRequired = false, PracticeUnlimitedTime = false, RequireClassBeforeStart = true, AllowStudentEditClass = true, RoomCode = "", ExamLink = "" };

            public static ExamSettings FromRow(DataRow row)
            {
                var settings = Default();
                settings.Name = row["ExamName"].ToString();
                decimal dur;
                settings.Duration = decimal.TryParse(row["Duration"].ToString(), out dur) ? dur : 30;
                settings.Password = row.Table.Columns.Contains("ExamPassword") && row["ExamPassword"] != DBNull.Value ? row["ExamPassword"].ToString() : "";
                settings.Mode = row.Table.Columns.Contains("ExamMode") && row["ExamMode"] != DBNull.Value ? row["ExamMode"].ToString() : "Exam";
                if (row.Table.Columns.Contains("StartTime") && row["StartTime"] != DBNull.Value) settings.StartTime = Convert.ToDateTime(row["StartTime"]);
                if (row.Table.Columns.Contains("EndTime") && row["EndTime"] != DBNull.Value) settings.EndTime = Convert.ToDateTime(row["EndTime"]);
                if (row.Table.Columns.Contains("MaxAttempts") && row["MaxAttempts"] != DBNull.Value) settings.MaxAttempts = Convert.ToInt32(row["MaxAttempts"]);
                if (row.Table.Columns.Contains("IsRandomQuestion") && row["IsRandomQuestion"] != DBNull.Value) settings.IsRandomQuestion = Convert.ToBoolean(row["IsRandomQuestion"]);
                if (row.Table.Columns.Contains("RandomQuestionCount") && row["RandomQuestionCount"] != DBNull.Value) settings.RandomQuestionCount = Convert.ToInt32(row["RandomQuestionCount"]);
                if (row.Table.Columns.Contains("ScoreScale") && row["ScoreScale"] != DBNull.Value) settings.ScoreScale = Convert.ToDecimal(row["ScoreScale"]);
                if (row.Table.Columns.Contains("PracticeMaxWrongPerQuestion") && row["PracticeMaxWrongPerQuestion"] != DBNull.Value) settings.PracticeMaxWrongPerQuestion = Convert.ToInt32(row["PracticeMaxWrongPerQuestion"]);
                if (row.Table.Columns.Contains("PracticeShowAnswer") && row["PracticeShowAnswer"] != DBNull.Value) settings.PracticeShowAnswer = Convert.ToBoolean(row["PracticeShowAnswer"]);
                if (row.Table.Columns.Contains("PracticeShowPointGain") && row["PracticeShowPointGain"] != DBNull.Value) settings.PracticeShowPointGain = Convert.ToBoolean(row["PracticeShowPointGain"]);
                if (row.Table.Columns.Contains("PracticeAllowStudentOptions") && row["PracticeAllowStudentOptions"] != DBNull.Value) settings.PracticeAllowStudentOptions = Convert.ToBoolean(row["PracticeAllowStudentOptions"]);
                if (row.Table.Columns.Contains("PracticeAutoNextOnCorrect") && row["PracticeAutoNextOnCorrect"] != DBNull.Value) settings.PracticeAutoNextOnCorrect = Convert.ToBoolean(row["PracticeAutoNextOnCorrect"]);
                if (row.Table.Columns.Contains("PracticeNoLoginRequired") && row["PracticeNoLoginRequired"] != DBNull.Value) settings.PracticeNoLoginRequired = Convert.ToBoolean(row["PracticeNoLoginRequired"]);
                if (row.Table.Columns.Contains("PracticeUnlimitedTime") && row["PracticeUnlimitedTime"] != DBNull.Value) settings.PracticeUnlimitedTime = Convert.ToBoolean(row["PracticeUnlimitedTime"]);
                if (row.Table.Columns.Contains("RequireClassBeforeStart") && row["RequireClassBeforeStart"] != DBNull.Value) settings.RequireClassBeforeStart = Convert.ToBoolean(row["RequireClassBeforeStart"]);
                if (row.Table.Columns.Contains("AllowStudentEditClass") && row["AllowStudentEditClass"] != DBNull.Value) settings.AllowStudentEditClass = Convert.ToBoolean(row["AllowStudentEditClass"]);
                if (row.Table.Columns.Contains("RoomCode") && row["RoomCode"] != DBNull.Value) settings.RoomCode = row["RoomCode"].ToString();
                if (row.Table.Columns.Contains("ExamLink") && row["ExamLink"] != DBNull.Value) settings.ExamLink = row["ExamLink"].ToString();
                return settings;
            }
        }

        private class ListItem
        {
            public int Id;
            public string Text;
            public override string ToString() => Text;
        }

        private class RoundPanel : Panel
        {
            public int Radius { get; set; } = 14;
            public Color BorderColor { get; set; } = Color.Gainsboro;

            public RoundPanel()
            {
                DoubleBuffered = true;
                Resize += (s, e) => UpdateRegion();
            }

            protected override void OnCreateControl()
            {
                base.OnCreateControl();
                UpdateRegion();
            }

            private void UpdateRegion()
            {
                if (Width <= 0 || Height <= 0) return;
                using (var path = GetRoundPath(new Rectangle(0, 0, Width, Height), Radius))
                    Region = new Region(path);
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                UiTheme.PaintSoftCard(this, e, Radius);
            }

            private static GraphicsPath GetRoundPath(Rectangle rect, int radius)
            {
                int d = radius * 2;
                var path = new GraphicsPath();
                if (radius <= 0) { path.AddRectangle(rect); path.CloseFigure(); return path; }
                path.AddArc(rect.X, rect.Y, d, d, 180, 90);
                path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}





