using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QuizManagement.BUS;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class frmTeacher : Form
    {
        private readonly Database db = new Database();
        private Panel pnlExamManagerHost;
        private Panel pnlHomeHost;
        private Panel pnlScoreHost;
        private Panel pnlStatsHost;
        private ucTrangChuTeacher ucTrangChu;
        private ucThongKeTeacher ucThongKe;
        private frmScoreReport scoreReportView;
        private readonly int currentUserID;
        private readonly string currentUserName;
        private DataTable printExamQuestions;
        private string printExamName;
        private int printQuestionIndex;

        // =========================
        // PANELS
        // =========================
        private Panel pnlSidebar;
        private Panel pnlContent;
        private Panel pnlHeader;
        private Panel pnlMain;

        private Panel pnlBody;
        private Panel pnlFilter;
        private Panel pnlChartBox;
        private Panel pnlQuickAction;

        private Panel pnlGridWrapper;
        private Panel pnlGridHeader;
        private Panel pnlGridFooter;

        // =========================
        // SIDEBAR SECTIONS
        // =========================
        private Label lblTitle;
        private Label lblSubTitle;

        private Label lblGridInfo;
        private Label lblPageSizeCaption;
        private Label lblGridEmptyState;

        // =========================
        // BUTTONS
        // =========================
        private Button btnTrangChu;
        private Button btnQuanLyDeThi;
        private Button btnNganHangCauHoi;
        private Button btnBangDiem;
        private Button btnThongKe;
        private Button btnLogout;

        private Button btnTopRefresh;
        private Button btnSearch;
        private Button btnClearFilter;

        private Button btnQuickAdd;
        private Button btnQuickImport;
        private Button btnQuickTemplate;
        private Button btnQuickExport;
        private Button btnQuickPrint;

        private Button btnPrevPage;
        private Button btnNextPage;
        private Button btnPage1;
        private Button btnPage2;

        // =========================
        // INPUTS
        // =========================
        private TextBox txtQuestion;
        private TextBox txtA;
        private TextBox txtB;
        private TextBox txtC;
        private TextBox txtD;
        private Label lblFilterHint;
        private Label lblKeywordTitle;
        private Label lblAdvancedTitle;
        private Label lblCorrect;
        private Label lblExam;

        private ComboBox cboCorrect;
        private ComboBox cboExam;
        private ComboBox cboPageSize;

        // =========================
        // GRID
        // =========================
        private DataGridView dgvQuestion;

        // =========================
        // CHART
        // =========================
        private Chart chartDifficulty;

        // =========================
        // STATE
        // =========================
        private int selectedID = -1;
        private int currentPage = 1;
        private int pageSize = 10;
        private DataTable fullData = null;
        private bool showingFilteredResults;

        private const int SidebarWidth = 200;
        private const int HeaderHeight = 110;

        // Colors
        private readonly Color SidebarBg = Color.FromArgb(18, 24, 42);
        private readonly Color SidebarActive = Color.FromArgb(45, 107, 233);
        private readonly Color SidebarHover = Color.FromArgb(28, 36, 60);
        private readonly Color SidebarText = Color.FromArgb(200, 210, 230);
        private readonly Color SidebarSection = Color.FromArgb(90, 100, 130);
        private readonly Color PageBg = Color.FromArgb(245, 247, 252);
        private readonly Color CardBorder = Color.FromArgb(230, 234, 242);
        private readonly Color TableHeaderBg = Color.FromArgb(248, 250, 252);
        private readonly Color TableHeaderFg = Color.FromArgb(100, 110, 130);
        private readonly Color TableBorder = Color.FromArgb(230, 235, 245);

        public frmTeacher() : this(0, "Giáo viên")
        {
        }

        public frmTeacher(string fullName) : this(0, fullName)
        {
        }

        public frmTeacher(int userID, string fullName)
        {
            currentUserID = userID;
            currentUserName = string.IsNullOrWhiteSpace(fullName) ? "Giáo viên" : fullName.Trim();
            InitUI();
            DoubleBuffered = true;
            Load += FrmTeacher_Load;
            Resize += (sender2, args2) => UpdateLayout();
        }

        private void FrmTeacher_Load(object sender, EventArgs e)
        {
            Database.EnsureQuestionPointsColumn();
            Database.EnsureTeacherOwnershipColumns();
            Database.EnsureStudentAssessmentColumns();
            try
            {
                StudentExamWebServer.EnsureStarted();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể khởi động web làm bài cho sinh viên.\n" + ex.Message,
                    "Web làm bài", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            LoadExamToComboBox();
            ClearEditors();
            UpdateLayout();
            ShowHomeView();
        }

        // ============================================================
        // INIT UI
        // ============================================================
        private void InitUI()
        {
            SuspendLayout();
            Text = "Teacher Panel";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1280, 768);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9.5f);

            // ---- SIDEBAR ----
            pnlSidebar = new Panel();
            pnlSidebar.Location = new Point(0, 0);
            pnlSidebar.Size = new Size(SidebarWidth, ClientSize.Height);
            pnlSidebar.BackColor = SidebarBg;
            Controls.Add(pnlSidebar);

            // Logo area
            Panel pnlLogo = new Panel();
            pnlLogo.Location = new Point(0, 0);
            pnlLogo.Size = new Size(SidebarWidth, 70);
            pnlLogo.BackColor = Color.FromArgb(14, 18, 34);
            pnlSidebar.Controls.Add(pnlLogo);

            Label lblLogo = new Label();
            lblLogo.Text = "TEACHER";
            lblLogo.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            lblLogo.ForeColor = Color.White;
            lblLogo.AutoSize = false;
            lblLogo.Size = new Size(SidebarWidth, 70);
            lblLogo.TextAlign = ContentAlignment.MiddleCenter;
            pnlLogo.Controls.Add(lblLogo);

            // Main nav items (no section label)
            int y = 80;
            btnTrangChu = CreateSideBtn("  🏠  Trang Chủ", y); y += 46;
            btnTrangChu.Click += BtnTrangChu_Click;
            pnlSidebar.Controls.Add(btnTrangChu);

            // Section: QUẢN LÝ ĐỀ THI
            Label lbl1 = CreateSideSection("QUẢN LÝ ĐỀ THI", y); y += 30;
            pnlSidebar.Controls.Add(lbl1);

            btnQuanLyDeThi = CreateSideBtn("  📋  Quản Lý Đề Thi", y); y += 46;
            btnQuanLyDeThi.Click += BtnQuanLyDeThi_Click;
            pnlSidebar.Controls.Add(btnQuanLyDeThi);

            btnNganHangCauHoi = CreateSideBtn("  ❓  Ngân Hàng Câu Hỏi", y); y += 46;
            btnNganHangCauHoi.Click += BtnNganHangCauHoi_Click;
            pnlSidebar.Controls.Add(btnNganHangCauHoi);

            // Section: BÁO CÁO - THỐNG KÊ
            Label lbl2 = CreateSideSection("BÁO CÁO - THỐNG KÊ", y); y += 30;
            pnlSidebar.Controls.Add(lbl2);

            btnBangDiem = CreateSideBtn("  🧾  Bảng Điểm", y); y += 46;
            btnBangDiem.Click += BtnBangDiem_Click;
            pnlSidebar.Controls.Add(btnBangDiem);

            btnThongKe = CreateSideBtn("  📊  Thống Kê", y); y += 46;
            btnThongKe.Click += BtnThongKe_Click;
            pnlSidebar.Controls.Add(btnThongKe);

            // Logout
            btnLogout = new Button();
            btnLogout.Text = "  🚪  Đăng Xuất";
            btnLogout.Size = new Size(SidebarWidth, 50);
            btnLogout.Location = new Point(0, 600);
            btnLogout.BackColor = Color.FromArgb(200, 40, 40);
            btnLogout.ForeColor = Color.White;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnLogout.TextAlign = ContentAlignment.MiddleLeft;
            btnLogout.Cursor = Cursors.Hand;
            UiTheme.ApplyButton(btnLogout, UiButtonVariant.Danger, true);
            btnLogout.BackColor = UiTheme.Danger;
            btnLogout.ForeColor = Color.White;
            btnLogout.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            btnLogout.Click += BtnLogout_Click;
            pnlSidebar.Controls.Add(btnLogout);

            // ---- CONTENT ----
            pnlContent = new Panel();
            pnlContent.Location = new Point(SidebarWidth, 0);
            pnlContent.BackColor = PageBg;
            Controls.Add(pnlContent);

            // ---- HEADER ----
            pnlHeader = new Panel();
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.BackColor = Color.White;
            pnlContent.Controls.Add(pnlHeader);

            lblTitle = new Label();
            lblTitle.Text = "QUẢN LÝ NGÂN HÀNG CÂU HỎI";
            lblTitle.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(20, 30, 60);
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(28, 18);
            pnlHeader.Controls.Add(lblTitle);

            lblSubTitle = new Label();
            lblSubTitle.Text = "Quản lý, tìm kiếm và theo dõi câu hỏi trong hệ thống";
            lblSubTitle.ForeColor = Color.Gray;
            lblSubTitle.Font = new Font("Segoe UI", 10);
            lblSubTitle.AutoSize = true;
            lblSubTitle.Location = new Point(30, 52);
            pnlHeader.Controls.Add(lblSubTitle);

            btnTopRefresh = new Button();
            btnTopRefresh.Text = "🔄  Làm Mới Dữ Liệu";
            btnTopRefresh.Size = new Size(170, 36);
            btnTopRefresh.BackColor = Color.White;
            btnTopRefresh.ForeColor = Color.FromArgb(60, 70, 100);
            btnTopRefresh.FlatStyle = FlatStyle.Flat;
            btnTopRefresh.FlatAppearance.BorderColor = Color.FromArgb(210, 218, 235);
            btnTopRefresh.Font = new Font("Segoe UI", 9.5f);
            btnTopRefresh.Cursor = Cursors.Hand;
            UiTheme.ApplyButton(btnTopRefresh, UiButtonVariant.Secondary);
            btnTopRefresh.Click += BtnTopRefresh_Click;
            pnlHeader.Controls.Add(btnTopRefresh);

            // ---- MAIN SCROLL AREA ----
            pnlMain = new Panel();
            pnlMain.AutoScroll = true;
            pnlMain.BackColor = PageBg;
            pnlContent.Controls.Add(pnlMain);

            pnlExamManagerHost = new Panel();
            pnlExamManagerHost.Dock = DockStyle.Fill;
            pnlExamManagerHost.BackColor = PageBg;
            pnlExamManagerHost.Visible = false;
            pnlContent.Controls.Add(pnlExamManagerHost);

            pnlScoreHost = new Panel();
            pnlScoreHost.Dock = DockStyle.Fill;
            pnlScoreHost.BackColor = PageBg;
            pnlScoreHost.Visible = false;
            pnlContent.Controls.Add(pnlScoreHost);

            pnlHomeHost = new Panel { BackColor = PageBg, Visible = false };
            pnlStatsHost = new Panel { BackColor = PageBg, Visible = false };
            pnlContent.Controls.Add(pnlHomeHost);
            pnlContent.Controls.Add(pnlStatsHost);

            ucTrangChu = new ucTrangChuTeacher(currentUserID, currentUserName) { Dock = DockStyle.Fill };
            ucTrangChu.RequestOpenExamManager += () => BtnQuanLyDeThi_Click(null, EventArgs.Empty);
            ucTrangChu.RequestOpenQuestionBank += () => BtnNganHangCauHoi_Click(null, EventArgs.Empty);
            pnlHomeHost.Controls.Add(ucTrangChu);

            ucThongKe = new ucThongKeTeacher(currentUserID) { Dock = DockStyle.Fill };
            pnlStatsHost.Controls.Add(ucThongKe);

            // ---- BODY (left + right) — chỉ dùng cho Ngân hàng câu hỏi ----
            pnlBody = new Panel();
            pnlBody.BackColor = Color.Transparent;
            pnlMain.Controls.Add(pnlBody);

            // LEFT: Filter
            pnlFilter = new Panel();
            pnlFilter.BackColor = Color.FromArgb(248, 250, 252);
            pnlFilter.Paint += PaintRoundedPanel;
            UiTheme.AttachRoundedRegion(pnlFilter, 20);
            pnlBody.Controls.Add(pnlFilter);

            Label lblFilterTitle = new Label();
            lblFilterTitle.Text = "BỘ LỌC TÌM KIẾM";
            lblFilterTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblFilterTitle.ForeColor = Color.FromArgb(45, 107, 233);
            lblFilterTitle.AutoSize = true;
            lblFilterTitle.Location = new Point(20, 16);
            pnlFilter.Controls.Add(lblFilterTitle);

            lblFilterHint = new Label();
            lblFilterHint.Text = "Tìm theo từ khóa chính trước, sau đó dùng bộ lọc chi tiết để thu hẹp kết quả.";
            lblFilterHint.Font = new Font("Segoe UI", 9f);
            lblFilterHint.ForeColor = Color.FromArgb(100, 116, 139);
            lblFilterHint.AutoSize = false;
            lblFilterHint.Size = new Size(620, 18);
            lblFilterHint.Location = new Point(20, 42);
            pnlFilter.Controls.Add(lblFilterHint);

            lblKeywordTitle = new Label();
            lblKeywordTitle.Text = "TÌM NHANH";
            lblKeywordTitle.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblKeywordTitle.ForeColor = Color.FromArgb(30, 64, 175);
            lblKeywordTitle.BackColor = Color.Transparent;
            lblKeywordTitle.Padding = new Padding(0);
            lblKeywordTitle.AutoSize = true;
            lblKeywordTitle.Location = new Point(34, 74);
            Panel keywordAccent = new Panel();
            keywordAccent.BackColor = Color.FromArgb(59, 130, 246);
            keywordAccent.Location = new Point(20, 73);
            keywordAccent.Size = new Size(6, 18);
            pnlFilter.Controls.Add(keywordAccent);
            pnlFilter.Controls.Add(lblKeywordTitle);

            AddFilterRow(pnlFilter, "Từ khóa", 96, out txtQuestion, "Ví dụ: nội dung câu hỏi, cụm từ cần tìm...");

            lblAdvancedTitle = new Label();
            lblAdvancedTitle.Text = "LỌC NÂNG CAO";
            lblAdvancedTitle.Font = new Font("Segoe UI", 8.8f, FontStyle.Bold);
            lblAdvancedTitle.ForeColor = Color.FromArgb(14, 116, 144);
            lblAdvancedTitle.BackColor = Color.Transparent;
            lblAdvancedTitle.Padding = new Padding(0);
            lblAdvancedTitle.AutoSize = true;
            lblAdvancedTitle.Location = new Point(34, 148);
            Panel advancedAccent = new Panel();
            advancedAccent.BackColor = Color.FromArgb(6, 182, 212);
            advancedAccent.Location = new Point(20, 147);
            advancedAccent.Size = new Size(6, 18);
            pnlFilter.Controls.Add(advancedAccent);
            pnlFilter.Controls.Add(lblAdvancedTitle);

            AddFilterRow(pnlFilter, "Đáp án A", 170, out txtA, "Chứa từ khóa trong đáp án A"); 
            AddFilterRow(pnlFilter, "Đáp án B", 170, out txtB, "Chứa từ khóa trong đáp án B", 372); 
            AddFilterRow(pnlFilter, "Đáp án C", 216, out txtC, "Chứa từ khóa trong đáp án C"); 
            AddFilterRow(pnlFilter, "Đáp án D", 216, out txtD, "Chứa từ khóa trong đáp án D", 372); 

            lblCorrect = new Label();
            lblCorrect.Text = "Đáp án đúng";
            lblCorrect.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblCorrect.ForeColor = Color.FromArgb(30, 41, 59);
            lblCorrect.AutoSize = true;
            lblCorrect.Location = new Point(20, 268);
            pnlFilter.Controls.Add(lblCorrect);

            cboCorrect = new ComboBox();
            cboCorrect.Items.AddRange(new object[] { "-- Tất cả --", "A", "B", "C", "D" });
            cboCorrect.SelectedIndex = 0;
            cboCorrect.Location = new Point(20, 292);
            cboCorrect.Size = new Size(160, 30);
            cboCorrect.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCorrect.FlatStyle = FlatStyle.Flat;
            cboCorrect.BackColor = Color.FromArgb(239, 246, 255);
            cboCorrect.ForeColor = Color.FromArgb(15, 23, 42);
            UiTheme.ApplyComboBox(cboCorrect);
            pnlFilter.Controls.Add(cboCorrect);

            lblExam = new Label();
            lblExam.Text = "Đề Thi";
            lblExam.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lblExam.ForeColor = Color.FromArgb(30, 41, 59);
            lblExam.AutoSize = true;
            lblExam.Location = new Point(200, 268);
            pnlFilter.Controls.Add(lblExam);

            cboExam = new ComboBox();
            cboExam.Location = new Point(200, 292);
            cboExam.Size = new Size(220, 30);
            cboExam.DropDownStyle = ComboBoxStyle.DropDownList;
            cboExam.FlatStyle = FlatStyle.Flat;
            cboExam.BackColor = Color.FromArgb(240, 253, 250);
            cboExam.ForeColor = Color.FromArgb(15, 23, 42);
            UiTheme.ApplyComboBox(cboExam);
            pnlFilter.Controls.Add(cboExam);

            btnClearFilter = new Button();
            btnClearFilter.Text = "Xóa lọc";
            btnClearFilter.Size = new Size(110, 36);
            btnClearFilter.Location = new Point(438, 288);
            btnClearFilter.BackColor = Color.FromArgb(241, 245, 249);
            btnClearFilter.ForeColor = Color.FromArgb(30, 41, 59);
            btnClearFilter.FlatStyle = FlatStyle.Flat;
            btnClearFilter.FlatAppearance.BorderColor = Color.FromArgb(148, 163, 184);
            btnClearFilter.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btnClearFilter.Cursor = Cursors.Hand;
            UiTheme.ApplyButton(btnClearFilter, UiButtonVariant.Secondary);
            btnClearFilter.Click += (s, e) => { ClearEditors(); LoadQuestion(); };
            pnlFilter.Controls.Add(btnClearFilter);

            btnSearch = new Button();
            btnSearch.Text = "🔍  Tìm kiếm";
            btnSearch.Size = new Size(132, 36);
            btnSearch.Location = new Point(566, 288);
            btnSearch.BackColor = Color.FromArgb(45, 107, 233);
            btnSearch.ForeColor = Color.White;
            btnSearch.FlatStyle = FlatStyle.Flat;
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnSearch.Cursor = Cursors.Hand;
            UiTheme.ApplyButton(btnSearch, UiButtonVariant.Primary);
            btnSearch.Click += BtnSearch_Click;
            pnlFilter.Controls.Add(btnSearch);

            // RIGHT: Chart
            pnlChartBox = new Panel();
            pnlChartBox.BackColor = Color.White;
            pnlChartBox.Paint += PaintRoundedPanel;
            pnlBody.Controls.Add(pnlChartBox);

            Label lblChartTitle = new Label();
            lblChartTitle.Text = "PHÂN LOẠI THEO SỐ ĐIỂM";
            lblChartTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblChartTitle.ForeColor = Color.FromArgb(45, 107, 233);
            lblChartTitle.AutoSize = true;
            lblChartTitle.Location = new Point(16, 14);
            pnlChartBox.Controls.Add(lblChartTitle);

            chartDifficulty = new Chart();
            chartDifficulty.Location = new Point(0, 44);
            chartDifficulty.BackColor = Color.Transparent;

            ChartArea ca = new ChartArea("Main");
            ca.BackColor = Color.Transparent;
            ca.AxisX.Enabled = AxisEnabled.False;
            ca.AxisY.Enabled = AxisEnabled.False;
            chartDifficulty.ChartAreas.Add(ca);

            Legend lg = new Legend("Leg");
            lg.Docking = Docking.Right;
            lg.IsDockedInsideChartArea = false;
            lg.Alignment = StringAlignment.Center;
            lg.Font = new Font("Segoe UI", 9f);
            chartDifficulty.Legends.Add(lg);

            Series seriesChart = new Series("Số điểm");
            seriesChart.ChartType = SeriesChartType.Doughnut;
            seriesChart.ChartArea = "Main";
            seriesChart.Legend = "Leg";
            seriesChart.IsValueShownAsLabel = true;
            seriesChart["DoughnutRadius"] = "25";
            chartDifficulty.Series.Add(seriesChart);

            pnlChartBox.Controls.Add(chartDifficulty);

            // RIGHT: Quick Actions
            pnlQuickAction = new Panel();
            pnlQuickAction.BackColor = Color.White;
            pnlQuickAction.Paint += PaintRoundedPanel;
            pnlBody.Controls.Add(pnlQuickAction);

            Label lblActionTitle = new Label();
            lblActionTitle.Text = "THAO TÁC NHANH";
            lblActionTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblActionTitle.ForeColor = Color.FromArgb(45, 107, 233);
            lblActionTitle.AutoSize = true;
            lblActionTitle.Location = new Point(16, 14);
            pnlQuickAction.Controls.Add(lblActionTitle);

            btnQuickAdd = CreateActionBtn("+ Thêm Câu Hỏi Mới", Color.FromArgb(34, 190, 121), 14, 50);
            btnQuickAdd.Click += BtnQuickAdd_Click;
            pnlQuickAction.Controls.Add(btnQuickAdd);

            btnQuickImport = CreateActionBtn("⬇ Import Câu Hỏi", Color.FromArgb(45, 107, 233), 170, 50);
            btnQuickImport.Click += BtnQuickImport_Click;
            pnlQuickAction.Controls.Add(btnQuickImport);

            btnQuickTemplate = CreateActionBtn("📄 Tải File Mẫu", Color.FromArgb(14, 165, 233), 14, 100);
            btnQuickTemplate.Click += BtnQuickTemplate_Click;
            pnlQuickAction.Controls.Add(btnQuickTemplate);

            btnQuickExport = CreateActionBtn("📊 Xuất Excel", Color.FromArgb(245, 155, 30), 170, 100);
            btnQuickExport.Click += BtnQuickExport_Click;
            pnlQuickAction.Controls.Add(btnQuickExport);

            btnQuickPrint = CreateActionBtn("🖨 In Đề Thi", Color.FromArgb(100, 120, 180), 92, 150);
            btnQuickPrint.Click += BtnQuickPrint_Click;
            pnlQuickAction.Controls.Add(btnQuickPrint);

            // ---- GRID WRAPPER ----
            pnlGridWrapper = new Panel();
            pnlGridWrapper.BackColor = Color.White;
            pnlGridWrapper.Paint += PaintRoundedPanel;
            pnlMain.Controls.Add(pnlGridWrapper);

            pnlGridHeader = new Panel();
            pnlGridHeader.Location = new Point(0, 0);
            pnlGridHeader.Height = 46;
            pnlGridHeader.BackColor = Color.White;
            pnlGridWrapper.Controls.Add(pnlGridHeader);

            Label lblGridTitle = new Label();
            lblGridTitle.Text = "DANH SÁCH CÂU HỎI";
            lblGridTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblGridTitle.ForeColor = Color.FromArgb(45, 107, 233);
            lblGridTitle.AutoSize = true;
            lblGridTitle.Location = new Point(16, 12);
            pnlGridHeader.Controls.Add(lblGridTitle);

            // DataGridView
            dgvQuestion = new DataGridView();
            dgvQuestion.Location = new Point(0, 46);
            dgvQuestion.BackgroundColor = Color.White;
            dgvQuestion.BorderStyle = BorderStyle.None;
            dgvQuestion.ReadOnly = true;
            dgvQuestion.AllowUserToAddRows = false;
            dgvQuestion.AllowUserToDeleteRows = false;
            dgvQuestion.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvQuestion.MultiSelect = false;
            dgvQuestion.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvQuestion.RowTemplate.Height = 40;
            dgvQuestion.RowHeadersVisible = false;
            dgvQuestion.ColumnHeadersHeight = 44;
            dgvQuestion.EnableHeadersVisualStyles = false;
            dgvQuestion.ColumnHeadersDefaultCellStyle.BackColor = TableHeaderBg;
            dgvQuestion.ColumnHeadersDefaultCellStyle.ForeColor = TableHeaderFg;
            dgvQuestion.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvQuestion.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvQuestion.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            dgvQuestion.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 255);
            dgvQuestion.DefaultCellStyle.SelectionForeColor = Color.FromArgb(20, 30, 60);
            dgvQuestion.DefaultCellStyle.BackColor = Color.White;
            dgvQuestion.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvQuestion.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);
            dgvQuestion.GridColor = TableBorder;
            UiTheme.ApplyGrid(dgvQuestion);
            dgvQuestion.CellPainting += DgvQuestion_CellPainting;
            dgvQuestion.CellClick += DgvQuestion_CellClick;
            dgvQuestion.CellContentClick += DgvQuestion_CellContentClick;
            dgvQuestion.CellToolTipTextNeeded += DgvQuestion_CellToolTipTextNeeded;
            dgvQuestion.CellDoubleClick += DgvQuestion_CellDoubleClick;
            pnlGridWrapper.Controls.Add(dgvQuestion);

            lblGridEmptyState = new Label();
            lblGridEmptyState.Text = "";
            lblGridEmptyState.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            lblGridEmptyState.ForeColor = Color.FromArgb(100, 116, 139);
            lblGridEmptyState.AutoSize = false;
            lblGridEmptyState.TextAlign = ContentAlignment.MiddleCenter;
            lblGridEmptyState.Visible = false;
            pnlGridWrapper.Controls.Add(lblGridEmptyState);

            // ---- GRID FOOTER ----
            pnlGridFooter = new Panel();
            pnlGridFooter.BackColor = Color.White;
            pnlGridWrapper.Controls.Add(pnlGridFooter);

            lblGridInfo = new Label();
            lblGridInfo.Text = "";
            lblGridInfo.Font = new Font("Segoe UI", 9.5f);
            lblGridInfo.ForeColor = Color.FromArgb(100, 110, 140);
            lblGridInfo.AutoSize = true;
            lblGridInfo.Location = new Point(16, 14);
            pnlGridFooter.Controls.Add(lblGridInfo);

            // Pagination
            btnPrevPage = CreatePageBtn("«", 0);
            btnPrevPage.Click += (s2, e2) => { if (currentPage > 1) { currentPage--; RenderPage(); } };
            pnlGridFooter.Controls.Add(btnPrevPage);

            btnPage1 = CreatePageBtn("1", 0);
            btnPage1.Click += (s2, e2) => { if (int.TryParse(btnPage1.Text, out int page)) { currentPage = page; RenderPage(); } };
            pnlGridFooter.Controls.Add(btnPage1);

            btnPage2 = CreatePageBtn("2", 0);
            btnPage2.Click += (s2, e2) => { if (int.TryParse(btnPage2.Text, out int page)) { currentPage = page; RenderPage(); } };
            pnlGridFooter.Controls.Add(btnPage2);

            btnNextPage = CreatePageBtn("»", 0);
            btnNextPage.Click += (s2, e2) => { currentPage++; RenderPage(); };
            pnlGridFooter.Controls.Add(btnNextPage);

            // Page size
            lblPageSizeCaption = new Label();
            lblPageSizeCaption.Text = "Hiển thị:";
            lblPageSizeCaption.Font = new Font("Segoe UI", 9.5f);
            lblPageSizeCaption.ForeColor = Color.FromArgb(100, 110, 140);
            lblPageSizeCaption.AutoSize = true;
            pnlGridFooter.Controls.Add(lblPageSizeCaption);

            cboPageSize = new ComboBox();
            cboPageSize.Items.AddRange(new object[] { "10", "20", "50" });
            cboPageSize.SelectedIndex = 0;
            cboPageSize.Size = new Size(55, 28);
            cboPageSize.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPageSize.FlatStyle = FlatStyle.Flat;
            UiTheme.ApplyComboBox(cboPageSize);
            cboPageSize.SelectedIndexChanged += (s2, e2) =>
            {
                pageSize = int.Parse(cboPageSize.SelectedItem.ToString());
                currentPage = 1;
                RenderPage();
            };
            pnlGridFooter.Controls.Add(cboPageSize);

            ResumeLayout(false);
        }

        // ============================================================
        // PAINT HELPERS
        // ============================================================
        private void PaintRoundedPanel(object sender, PaintEventArgs e)
        {
            Panel p = sender as Panel;
            if (p == null) return;
            UiTheme.PaintSoftCard(p, e, 20);
        }

        private void DgvQuestion_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex < 0) return;

            string colName = dgvQuestion.Columns[e.ColumnIndex].Name;

            // Badge số điểm
            if ((colName == "colDiem" || colName == "colStatus") && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, true);
                string val = e.Value.ToString();
                Color bg = Color.FromArgb(220, 230, 255);
                Color fg = Color.FromArgb(60, 100, 200);
                if (colName == "colDiem")
                {
                    if (val == "—" || val == "Chưa gán") { bg = Color.FromArgb(241, 245, 249); fg = Color.Gray; }
                }
                else
                {
                    if (val == "Hợp lệ") { bg = Color.FromArgb(220, 252, 231); fg = Color.FromArgb(22, 101, 52); }
                    else if (val == "Chưa gán điểm") { bg = Color.FromArgb(255, 247, 237); fg = Color.FromArgb(180, 83, 9); }
                    else if (val == "Thiếu đáp án") { bg = Color.FromArgb(254, 242, 242); fg = Color.FromArgb(185, 28, 28); }
                    else if (val == "Điền khuyết") { bg = Color.FromArgb(243, 232, 255); fg = Color.FromArgb(107, 33, 168); }
                }

                Rectangle r = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 7,
                    e.CellBounds.Width - 12, e.CellBounds.Height - 14);
                using (SolidBrush b = new SolidBrush(bg))
                    e.Graphics.FillRectangle(b, r);

                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (SolidBrush tb = new SolidBrush(fg))
                    e.Graphics.DrawString(val, new Font("Segoe UI", 8.5f, FontStyle.Bold), tb, r, sf);

                e.Handled = true;
                return;
            }

            if (IsPreviewableColumn(colName) && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, true);
                string val = e.Value.ToString();
                string compact = CompactCellText(val, colName == "Câu Hỏi" ? 54 : 34);
                Rectangle textRect = new Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + 5, e.CellBounds.Width - 30, e.CellBounds.Height - 10);
                Rectangle hintRect = new Rectangle(e.CellBounds.Right - 20, e.CellBounds.Y + 7, 14, e.CellBounds.Height - 14);
                Color fg = colName == "colCorrect" ? Color.FromArgb(45, 107, 233) : Color.FromArgb(51, 65, 85);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                using (SolidBrush tb = new SolidBrush(fg))
                    e.Graphics.DrawString(compact, new Font("Segoe UI", 9.0f, colName == "colCorrect" ? FontStyle.Bold : FontStyle.Regular), tb, textRect, sf);

                if (NeedsPreviewHint(val))
                {
                    using (SolidBrush b = new SolidBrush(Color.FromArgb(219, 234, 254)))
                        e.Graphics.FillRectangle(b, hintRect);
                    using (SolidBrush tb = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    {
                        StringFormat hintFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString("...", new Font("Segoe UI", 7.2f, FontStyle.Bold), tb, hintRect, hintFormat);
                    }
                }

                e.Handled = true;
                return;
            }
        }

        private string CompactCellText(string text, int maxLength)
        {
            string value = (text ?? "").Replace(Environment.NewLine, " ").Replace("\n", " ").Replace("\r", " ").Trim();
            while (value.Contains("  "))
                value = value.Replace("  ", " ");
            if (value.Length <= maxLength) return value;
            return value.Substring(0, Math.Max(0, maxLength - 1)).TrimEnd() + "…";
        }

        private void DgvQuestion_CellToolTipTextNeeded(object sender, DataGridViewCellToolTipTextNeededEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string colName = dgvQuestion.Columns[e.ColumnIndex].Name;
            if (!IsPreviewableColumn(colName)) return;

            object value = dgvQuestion.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            if (value == null) return;
            e.ToolTipText = value + Environment.NewLine + "Nhấp đúp để xem đầy đủ.";
        }

        private void DgvQuestion_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            string colName = dgvQuestion.Columns[e.ColumnIndex].Name;
            if (!IsPreviewableColumn(colName)) return;

            object value = dgvQuestion.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            if (value == null || string.IsNullOrWhiteSpace(value.ToString())) return;

            ShowCellContentPopup(GetPreviewColumnTitle(colName), value.ToString(), dgvQuestion.Rows[e.RowIndex], dgvQuestion.Rows[e.RowIndex].Cells[e.ColumnIndex]);
        }

        private bool IsPreviewableColumn(string colName)
        {
            return colName == "Câu Hỏi" || colName == "A" || colName == "B" || colName == "C" || colName == "D" || colName == "colCorrect";
        }

        private bool NeedsPreviewHint(string text)
        {
            string compact = CompactCellText(text, 28);
            return !string.Equals(compact, (text ?? "").Trim(), StringComparison.Ordinal);
        }

        private string GetPreviewColumnTitle(string colName)
        {
            if (colName == "colCorrect") return "Đáp án đúng";
            if (colName == "Câu Hỏi") return "Nội dung câu hỏi";
            return "Đáp án " + colName;
        }

        private void ShowCellContentPopup(string title, string content, DataGridViewRow row, DataGridViewCell cell)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Xem nhanh nội dung";
                dlg.Size = new Size(620, 340);
                dlg.StartPosition = FormStartPosition.Manual;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.ShowInTaskbar = false;
                dlg.BackColor = Color.FromArgb(244, 247, 251);
                dlg.Font = new Font("Segoe UI", 9.5f);

                Point anchor = System.Windows.Forms.Cursor.Position;
                Rectangle working = Screen.FromPoint(anchor).WorkingArea;
                int x = Math.Min(Math.Max(working.Left + 12, anchor.X - 180), Math.Max(working.Left + 12, working.Right - dlg.Width - 12));
                int y = Math.Min(Math.Max(working.Top + 12, anchor.Y - 40), Math.Max(working.Top + 12, working.Bottom - dlg.Height - 12));
                dlg.Location = new Point(x, y);

                var card = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(18, 16, 18, 16)
                };
                dlg.Controls.Add(card);

                var lblTag = new Label
                {
                    Text = title.ToUpperInvariant(),
                    AutoSize = false,
                    Size = new Size(150, 28),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                    BackColor = Color.FromArgb(219, 234, 254),
                    ForeColor = Color.FromArgb(37, 99, 235),
                    Location = new Point(0, 0)
                };
                card.Controls.Add(lblTag);

                var lblHint = new Label
                {
                    Text = "Hiển thị đầy đủ nội dung đang bị rút gọn trong bảng.",
                    AutoSize = true,
                    ForeColor = Color.FromArgb(100, 116, 139),
                    Location = new Point(0, 38)
                };
                card.Controls.Add(lblHint);

                string questionId = row.Cells["Mã"]?.Value?.ToString() ?? "";
                string questionTitle = row.Cells["Câu Hỏi"]?.Value?.ToString() ?? "";
                var lblMeta = new Label
                {
                    Text = "Mã câu hỏi: " + questionId + "    |    Tóm tắt: " + CompactCellText(questionTitle, 60),
                    AutoSize = false,
                    Size = new Size(560, 34),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Location = new Point(0, 62)
                };
                card.Controls.Add(lblMeta);

                var txtContent = new TextBox
                {
                    Location = new Point(0, 104),
                    Size = new Size(566, 140),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    BackColor = Color.FromArgb(248, 250, 252),
                    BorderStyle = BorderStyle.FixedSingle,
                    Text = content
                };
                UiTheme.ApplyTextBox(txtContent);
                card.Controls.Add(txtContent);

                var btnCopy = new Button
                {
                    Text = "Sao chép",
                    Location = new Point(370, 260),
                    Size = new Size(90, 34),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                UiTheme.ApplyButton(btnCopy, UiButtonVariant.Primary);
                btnCopy.Click += (s, e) =>
                {
                    Clipboard.SetText(content);
                    btnCopy.Text = "Đã chép";
                };
                card.Controls.Add(btnCopy);

                var btnClose = new Button
                {
                    Text = "Đóng",
                    DialogResult = DialogResult.OK,
                    Location = new Point(476, 260),
                    Size = new Size(90, 34),
                    BackColor = Color.FromArgb(15, 23, 42),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                UiTheme.ApplyButton(btnClose, UiButtonVariant.Secondary);
                card.Controls.Add(btnClose);

                dlg.AcceptButton = btnClose;
                dlg.ShowDialog(this);
            }
        }

        // ============================================================
        // STAT CARD
        // ============================================================
        private Panel CreateStatCard(string value, string title, string sub, Color accent, string icon, Color iconBg, out Label valLabel)
        {
            Panel p = new Panel();
            p.Size = new Size(220, 100);
            p.BackColor = Color.White;
            p.Paint += PaintRoundedPanel;

            // Icon circle
            Panel iconPanel = new Panel();
            iconPanel.Size = new Size(52, 52);
            iconPanel.Location = new Point(16, 24);
            iconPanel.BackColor = iconBg;
            iconPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush b = new SolidBrush(iconBg))
                    e.Graphics.FillEllipse(b, 0, 0, iconPanel.Width, iconPanel.Height);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (SolidBrush tb = new SolidBrush(accent))
                    e.Graphics.DrawString(icon, new Font("Segoe UI Emoji", 18), tb, new Rectangle(0, 0, iconPanel.Width, iconPanel.Height), sf);
            };
            p.Controls.Add(iconPanel);

            valLabel = new Label();
            valLabel.Name = "value";
            valLabel.Text = value;
            valLabel.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            valLabel.ForeColor = accent;
            valLabel.AutoSize = true;
            valLabel.Location = new Point(78, 14);
            p.Controls.Add(valLabel);

            Label lTitle = new Label();
            lTitle.Text = title;
            lTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lTitle.ForeColor = accent;
            lTitle.AutoSize = true;
            lTitle.Location = new Point(78, 48);
            p.Controls.Add(lTitle);

            Label lSub = new Label();
            lSub.Text = sub;
            lSub.Font = new Font("Segoe UI", 7.5f);
            lSub.ForeColor = Color.Gray;
            lSub.AutoSize = false;
            lSub.Size = new Size(130, 30);
            lSub.Location = new Point(78, 70);
            p.Controls.Add(lSub);

            return p;
        }

        // ============================================================
        // BUTTON HELPERS
        // ============================================================
        private Button CreateSideBtn(string text, int top)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(SidebarWidth, 46);
            btn.Location = new Point(0, top);
            btn.BackColor = SidebarBg;
            btn.ForeColor = SidebarText;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(10, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            UiTheme.ApplyButton(btn, UiButtonVariant.Ghost, true);
            btn.BackColor = SidebarBg;
            btn.ForeColor = SidebarText;
            btn.MouseEnter += (sender2, args2) => { Button b = sender2 as Button; if (b != null && b.BackColor != SidebarActive) b.BackColor = SidebarHover; };
            btn.MouseLeave += (sender2, args2) => { Button b = sender2 as Button; if (b != null && b.BackColor != SidebarActive) b.BackColor = SidebarBg; };
            return btn;
        }

        private Label CreateSideSection(string text, int top)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
            lbl.ForeColor = SidebarSection;
            lbl.Size = new Size(SidebarWidth - 20, 22);
            lbl.Location = new Point(14, top + 4);
            return lbl;
        }

        private Button CreateActionBtn(string text, Color color, int left, int top)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(148, 38);
            btn.Location = new Point(left, top);
            btn.BackColor = color;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            UiTheme.ApplyButton(btn,
                color == Color.FromArgb(34, 190, 121) ? UiButtonVariant.Success :
                color == Color.FromArgb(245, 155, 30) ? UiButtonVariant.Warning :
                color == Color.FromArgb(14, 165, 233) ? UiButtonVariant.Info :
                UiButtonVariant.Primary);
            return btn;
        }

        private Button CreatePageBtn(string text, int left)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Size = new Size(34, 30);
            btn.Location = new Point(left, 8);
            btn.BackColor = Color.White;
            btn.ForeColor = Color.FromArgb(60, 80, 130);
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            UiTheme.ApplyButton(btn, UiButtonVariant.Secondary);
            return btn;
        }

        private void AddFilterRow(Panel parent, string labelText, int top, out TextBox txt, string placeholder, int left = 20)
        {
            Panel fieldBg = new Panel();
            fieldBg.Location = new Point(left + 82, top - 2);
            fieldBg.Size = new Size(254, 34);
            fieldBg.BackColor = UiTheme.SurfaceRaised;
            fieldBg.BorderStyle = BorderStyle.FixedSingle;
            parent.Controls.Add(fieldBg);

            Label lbl = new Label();
            lbl.Text = labelText;
            lbl.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            lbl.ForeColor = Color.FromArgb(60, 70, 100);
            lbl.AutoSize = true;
            lbl.Location = new Point(left, top + 7);
            parent.Controls.Add(lbl);

            txt = new TextBox();
            txt.Location = new Point(left + 88, top + 2);
            txt.Size = new Size(242, 26);
            txt.ForeColor = Color.Gray;
            txt.Text = placeholder;
            txt.Font = new Font("Segoe UI", 9.5f);
            txt.BackColor = UiTheme.SurfaceRaised;
            txt.BorderStyle = BorderStyle.None;
            txt.Tag = fieldBg;
            string ph = placeholder;
            txt.Enter += (sender2, args2) => { TextBox t = sender2 as TextBox; if (t.Text == ph) { t.Text = ""; t.ForeColor = Color.Black; } };
            txt.Leave += (sender2, args2) => { TextBox t = sender2 as TextBox; if (string.IsNullOrWhiteSpace(t.Text)) { t.Text = ph; t.ForeColor = Color.Gray; } };
            parent.Controls.Add(txt);
        }

        private void MoveFilterLabel(string text, int x, int y)
        {
            if (pnlFilter == null) return;
            foreach (Control c in pnlFilter.Controls)
            {
                if (c is Label lbl && lbl.Text == text)
                {
                    lbl.Location = new Point(x, y);
                    return;
                }
            }
        }

        private void SyncFilterFieldBackground(TextBox txt)
        {
            if (txt?.Tag is Panel bg)
            {
                bg.Location = new Point(txt.Left - 2, txt.Top - 2);
                bg.Size = new Size(txt.Width + 4, txt.Height + 4);
            }
        }

        // ============================================================
        // LAYOUT
        // ============================================================
        private void UpdateLayout()
        {
            if (ClientSize.Width <= 0) return;

            // Sidebar
            if (pnlSidebar != null)
                pnlSidebar.Size = new Size(SidebarWidth, ClientSize.Height);

            // Logout pin bottom
            if (btnLogout != null)
                btnLogout.Location = new Point(0, ClientSize.Height - 56);

            // Content
            int cw = ClientSize.Width - SidebarWidth;
            int ch = ClientSize.Height;
            if (pnlContent != null)
            {
                pnlContent.Location = new Point(SidebarWidth, 0);
                pnlContent.Size = new Size(cw, ch);
            }

            // Header
            if (pnlHeader != null)
            {
                pnlHeader.Location = new Point(0, 0);
                pnlHeader.Size = new Size(cw, HeaderHeight);
            }

            if (btnTopRefresh != null && pnlHeader != null)
                btnTopRefresh.Location = new Point(pnlHeader.Width - 190, 38);

            // Main / Home / Stats (dưới header)
            int contentH = ch - HeaderHeight;
            if (pnlMain != null)
            {
                pnlMain.Location = new Point(0, HeaderHeight);
                pnlMain.Size = new Size(cw, contentH);
            }
            if (pnlHomeHost != null)
            {
                pnlHomeHost.Location = new Point(0, HeaderHeight);
                pnlHomeHost.Size = new Size(cw, contentH);
            }
            if (pnlStatsHost != null)
            {
                pnlStatsHost.Location = new Point(0, HeaderHeight);
                pnlStatsHost.Size = new Size(cw, contentH);
            }

            int margin = 20;
            int mainW = cw;

            // Body (ngân hàng câu hỏi)
            int bodyTop = margin;
            int rightW = 340;
            int leftW = mainW - margin * 2 - rightW - 16;

            // Right column: chart + gap + quickaction
            int chartH = 200;
            int quickH = 212;
            int rightGap = 10;
            int bodyH = chartH + rightGap + quickH; // = 370

            if (pnlBody != null)
            {
                pnlBody.Location = new Point(margin, bodyTop);
                pnlBody.Size = new Size(mainW - margin * 2, bodyH);
            }

            if (pnlFilter != null)
            {
                pnlFilter.Location = new Point(0, 0);
                pnlFilter.Size = new Size(leftW, bodyH);
                int innerWidth = pnlFilter.Width - 40;
                int labelWidth = 72;
                int colGap = 24;
                int colWidth = Math.Max(240, (innerWidth - colGap) / 2);
                int leftX = 20;
                int rightX = leftX + colWidth + colGap;
                int fieldGap = 10;
                int fieldWidth = Math.Max(130, colWidth - labelWidth - fieldGap);
                int keywordFieldWidth = Math.Max(260, innerWidth - labelWidth - fieldGap);
                int actionZoneWidth = 270;
                int comboBlockWidth = Math.Max(260, pnlFilter.Width - actionZoneWidth - 60);

                if (lblFilterHint != null) lblFilterHint.Width = innerWidth;

                MoveFilterLabel("Từ khóa", leftX, 103);
                if (txtQuestion != null)
                {
                    txtQuestion.Location = new Point(leftX + labelWidth + fieldGap, 96);
                    txtQuestion.Size = new Size(keywordFieldWidth, 30);
                    SyncFilterFieldBackground(txtQuestion);
                }

                MoveFilterLabel("Đáp án A", leftX, 177);
                MoveFilterLabel("Đáp án B", rightX, 177);
                MoveFilterLabel("Đáp án C", leftX, 223);
                MoveFilterLabel("Đáp án D", rightX, 223);

                if (txtA != null)
                {
                    txtA.Location = new Point(leftX + labelWidth + fieldGap, 170);
                    txtA.Size = new Size(fieldWidth, 30);
                    SyncFilterFieldBackground(txtA);
                }
                if (txtB != null)
                {
                    txtB.Location = new Point(rightX + labelWidth + fieldGap, 170);
                    txtB.Size = new Size(fieldWidth, 30);
                    SyncFilterFieldBackground(txtB);
                }
                if (txtC != null)
                {
                    txtC.Location = new Point(leftX + labelWidth + fieldGap, 216);
                    txtC.Size = new Size(fieldWidth, 30);
                    SyncFilterFieldBackground(txtC);
                }
                if (txtD != null)
                {
                    txtD.Location = new Point(rightX + labelWidth + fieldGap, 216);
                    txtD.Size = new Size(fieldWidth, 30);
                    SyncFilterFieldBackground(txtD);
                }

                if (lblCorrect != null) lblCorrect.Location = new Point(leftX, 268);
                if (cboCorrect != null) cboCorrect.Location = new Point(leftX, 292);
                if (lblExam != null) lblExam.Location = new Point(200, 268);
                if (cboExam != null)
                {
                    cboExam.Location = new Point(200, 292);
                    cboExam.Width = Math.Max(150, comboBlockWidth - 180);
                }
                if (btnClearFilter != null)
                    btnClearFilter.Location = new Point(pnlFilter.Width - actionZoneWidth, 288);
                if (btnSearch != null)
                    btnSearch.Location = new Point(pnlFilter.Width - 152, 288);
            }

            if (pnlChartBox != null)
            {
                pnlChartBox.Location = new Point(leftW + 16, 0);
                pnlChartBox.Size = new Size(rightW, chartH);
                if (chartDifficulty != null)
                    chartDifficulty.Size = new Size(rightW - 8, chartH - 52);
            }

            if (pnlQuickAction != null)
            {
                pnlQuickAction.Location = new Point(leftW + 16, chartH + rightGap);
                pnlQuickAction.Size = new Size(rightW, quickH);
                // Resize action buttons
                int bw = (rightW - 42) / 2;
                if (btnQuickAdd != null) { btnQuickAdd.Size = new Size(bw, 38); btnQuickAdd.Location = new Point(14, 50); }
                if (btnQuickImport != null) { btnQuickImport.Size = new Size(bw, 38); btnQuickImport.Location = new Point(14 + bw + 14, 50); }
                if (btnQuickTemplate != null) { btnQuickTemplate.Size = new Size(bw, 38); btnQuickTemplate.Location = new Point(14, 98); }
                if (btnQuickExport != null) { btnQuickExport.Size = new Size(bw, 38); btnQuickExport.Location = new Point(14 + bw + 14, 98); }
                if (btnQuickPrint != null) { btnQuickPrint.Size = new Size(rightW - 28, 38); btnQuickPrint.Location = new Point(14, 146); }
            }

            // Grid wrapper
            int gridTop = bodyTop + bodyH + 14;
            int availableGridWrapperHeight = Math.Max(320, contentH - gridTop - 18);
            int gridH = Math.Max(220, availableGridWrapperHeight - 92);
            if (pnlGridWrapper != null)
            {
                pnlGridWrapper.Location = new Point(margin, gridTop);
                pnlGridWrapper.Size = new Size(mainW - margin * 2, availableGridWrapperHeight);
            }

            if (pnlGridHeader != null)
                pnlGridHeader.Width = (pnlGridWrapper != null ? pnlGridWrapper.Width : 900);

            if (dgvQuestion != null)
            {
                dgvQuestion.Location = new Point(0, 46);
                dgvQuestion.Size = new Size(pnlGridWrapper != null ? pnlGridWrapper.Width : 900, gridH);
            }

            if (lblGridEmptyState != null && dgvQuestion != null)
            {
                lblGridEmptyState.Location = new Point(40, 120);
                lblGridEmptyState.Size = new Size(Math.Max(280, dgvQuestion.Width - 80), 80);
            }

            if (pnlGridFooter != null)
            {
                pnlGridFooter.Location = new Point(0, dgvQuestion != null ? dgvQuestion.Bottom : 300);
                pnlGridFooter.Size = new Size(pnlGridWrapper != null ? pnlGridWrapper.Width : 900, 46);

                // Reposition pagination
                int rightMargin = 16;
                int pageSizeBlockWidth = (lblPageSizeCaption != null ? lblPageSizeCaption.Width + 6 : 0) + (cboPageSize != null ? cboPageSize.Width : 55);
                int paginationWidth = 38 + 38 + 38 + 48;
                int px = pnlGridFooter.Width - rightMargin - pageSizeBlockWidth - 16 - paginationWidth;
                if (btnPrevPage != null) { btnPrevPage.Location = new Point(px, 8); px += 38; }
                if (btnPage1 != null) { btnPage1.Location = new Point(px, 8); px += 38; }
                if (btnPage2 != null) { btnPage2.Location = new Point(px, 8); px += 38; }
                if (btnNextPage != null) { btnNextPage.Location = new Point(px, 8); px += 48; }

                if (lblPageSizeCaption != null) lblPageSizeCaption.Location = new Point(px + 8, 14);
                if (cboPageSize != null) cboPageSize.Location = new Point(px + 8 + (lblPageSizeCaption != null ? lblPageSizeCaption.Width + 6 : 0), 10);
            }

            if (pnlMain != null && pnlGridWrapper != null)
                pnlMain.AutoScrollMinSize = new Size(0, pnlGridWrapper.Bottom + 20);
        }

        // ============================================================
        // LOAD DATA
        // ============================================================
        private void LoadQuestion()
        {
            string sql = BuildQuestionSql(string.Empty);
            fullData = db.ExecuteQuery(sql);
            showingFilteredResults = false;
            currentPage = 1;
            RenderPage();
            LoadPointsChart();
        }

        private void LoadQuestionByFilter()
        {
            string keyword = string.Empty;
            string sql = BuildQuestionSql(BuildWhereClause(keyword));
            fullData = db.ExecuteQuery(sql);
            showingFilteredResults = true;
            currentPage = 1;
            RenderPage();
            LoadPointsChart();
        }

        private void RenderPage()
        {
            if (fullData == null) return;

            int total = fullData.Rows.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)total / pageSize));
            if (currentPage > totalPages) currentPage = totalPages;
            if (currentPage < 1) currentPage = 1;

            int start = (currentPage - 1) * pageSize;
            int end = Math.Min(start + pageSize, total);

            DataTable page = fullData.Clone();
            for (int i = start; i < end; i++)
                page.ImportRow(fullData.Rows[i]);

            RemoveActionColumns();
            dgvQuestion.DataSource = page;
            AddActionColumns();

            // Update footer
            if (lblGridInfo != null)
                lblGridInfo.Text = total == 0
                    ? (showingFilteredResults ? "Không có câu hỏi phù hợp với bộ lọc hiện tại" : "Chưa có câu hỏi nào trong ngân hàng")
                    : $"Hiển thị {start + 1} đến {end} của {total} câu hỏi";

            if (lblGridEmptyState != null)
            {
                lblGridEmptyState.Visible = total == 0;
                if (total == 0)
                {
                    lblGridEmptyState.Text = showingFilteredResults
                        ? "Không tìm thấy câu hỏi phù hợp.\nHãy nới lỏng bộ lọc hoặc xóa điều kiện tìm kiếm."
                        : "Ngân hàng câu hỏi còn trống.\nBạn có thể thêm mới hoặc nhập từ file mẫu.";
                }
            }

            if (dgvQuestion != null)
                dgvQuestion.Visible = total > 0;

            // Pagination buttons
            UpdatePageButtons(totalPages);
        }

        private void UpdatePageButtons(int totalPages)
        {
            int pageA = totalPages <= 2 ? 1 : Math.Min(Math.Max(1, currentPage), totalPages - 1);
            int pageB = totalPages <= 1 ? 1 : Math.Min(totalPages, pageA + 1);

            if (btnPage1 != null)
            {
                btnPage1.Text = pageA.ToString();
                btnPage1.BackColor = currentPage == pageA ? Color.FromArgb(45, 107, 233) : Color.White;
                btnPage1.ForeColor = currentPage == pageA ? Color.White : Color.FromArgb(60, 80, 130);
                btnPage1.Visible = totalPages >= 1;
            }
            if (btnPage2 != null)
            {
                btnPage2.Text = pageB.ToString();
                btnPage2.BackColor = currentPage == pageB ? Color.FromArgb(45, 107, 233) : Color.White;
                btnPage2.ForeColor = currentPage == pageB ? Color.White : Color.FromArgb(60, 80, 130);
                btnPage2.Visible = totalPages >= 2;
            }
            if (btnPrevPage != null)
                btnPrevPage.Enabled = currentPage > 1;
            if (btnNextPage != null)
                btnNextPage.Enabled = currentPage < totalPages;
        }

        private string BuildQuestionSql(string whereClause)
        {
            string usedSub = "(SELECT QuestionID, COUNT(*) AS UsedCount FROM ExamQuestions GROUP BY QuestionID) u";
            string examSub = "(SELECT QuestionID, COUNT(DISTINCT ExamID) AS ExamCount FROM ExamQuestions GROUP BY QuestionID) e";

            string sql =
                "SELECT " +
                "q.QuestionID AS [Mã], " +
                "q.QuestionContent AS [Câu Hỏi], " +
                "q.OptionA AS [A], " +
                "q.OptionB AS [B], " +
                "q.OptionC AS [C], " +
                "q.OptionD AS [D], " +
                "q.CorrectAnswer AS [Đáp Án Đúng], " +
                "ISNULL(q.IsPublishedToServer,0) AS [Chia sẻ server], " +
                "CASE WHEN q.Points IS NULL THEN N'—' ELSE CAST(q.Points AS NVARCHAR(20)) END AS [Số điểm], " +
                "CASE " +
                "WHEN q.CorrectAnswer IS NULL OR LTRIM(RTRIM(q.CorrectAnswer)) = N'' THEN N'Thiếu đáp án' " +
                "WHEN (q.OptionA IS NULL OR LTRIM(RTRIM(q.OptionA)) = N'') AND (q.OptionB IS NULL OR LTRIM(RTRIM(q.OptionB)) = N'') AND (q.OptionC IS NULL OR LTRIM(RTRIM(q.OptionC)) = N'') AND (q.OptionD IS NULL OR LTRIM(RTRIM(q.OptionD)) = N'') THEN N'Điền khuyết' " +
                "WHEN q.Points IS NULL THEN N'Chưa gán điểm' " +
                "ELSE N'Hợp lệ' END AS [Trạng thái], " +
                "ISNULL(e.ExamCount,0) AS [Đề Thi], " +
                "ISNULL(u.UsedCount,0) AS [Lượt Sử Dụng] " +
                "FROM Questions q " +
                "LEFT JOIN " + usedSub + " ON q.QuestionID=u.QuestionID " +
                "LEFT JOIN " + examSub + " ON q.QuestionID=e.QuestionID ";

            string ownerClause = currentUserID > 0 ? "q.CreatedByUserID = " + currentUserID + " " : "1=1 ";
            if (!string.IsNullOrWhiteSpace(whereClause))
                sql += whereClause + "AND " + ownerClause;
            else
                sql += "WHERE " + ownerClause;

            sql += "ORDER BY q.QuestionID DESC";
            return sql;
        }

        private string BuildWhereClause(string keyword)
        {
            keyword = Escape(keyword);
            string where = "WHERE 1=1 ";

            string GetTxt(TextBox t) => (t == null || string.IsNullOrWhiteSpace(t.Text) || t.ForeColor == Color.Gray) ? "" : t.Text;

            string q = GetTxt(txtQuestion);
            string a = GetTxt(txtA);
            string b = GetTxt(txtB);
            string c = GetTxt(txtC);
            string d = GetTxt(txtD);

            if (!string.IsNullOrWhiteSpace(q)) where += $"AND q.QuestionContent LIKE N'%{Escape(q)}%' ";
            if (!string.IsNullOrWhiteSpace(a)) where += $"AND q.OptionA LIKE N'%{Escape(a)}%' ";
            if (!string.IsNullOrWhiteSpace(b)) where += $"AND q.OptionB LIKE N'%{Escape(b)}%' ";
            if (!string.IsNullOrWhiteSpace(c)) where += $"AND q.OptionC LIKE N'%{Escape(c)}%' ";
            if (!string.IsNullOrWhiteSpace(d)) where += $"AND q.OptionD LIKE N'%{Escape(d)}%' ";

            if (cboCorrect != null && cboCorrect.SelectedIndex > 0)
                where += $"AND q.CorrectAnswer = N'{cboCorrect.Text}' ";

            int examID = GetSelectedExamID();
            if (examID > 0)
                where += $"AND EXISTS (SELECT 1 FROM ExamQuestions eq WHERE eq.QuestionID=q.QuestionID AND eq.ExamID={examID}) ";

            if (!string.IsNullOrWhiteSpace(keyword))
                where += $"AND (q.QuestionContent LIKE N'%{keyword}%' OR q.OptionA LIKE N'%{keyword}%' OR q.OptionB LIKE N'%{keyword}%' OR q.OptionC LIKE N'%{keyword}%' OR q.OptionD LIKE N'%{keyword}%' OR q.CorrectAnswer LIKE N'%{keyword}%') ";

            return where;
        }

        // ============================================================
        // GRID COLUMNS
        // ============================================================
        private void RemoveActionColumns()
        {
            foreach (string col in new[] { "colEdit", "colDelete", "colDiem", "colCorrect", "colStatus" })
                if (dgvQuestion.Columns.Contains(col))
                    dgvQuestion.Columns.Remove(col);
        }

        private void AddActionColumns()
        {
            if (dgvQuestion.Columns.Count == 0) return;

            // Rename auto columns for custom painting
            if (dgvQuestion.Columns.Contains("Số điểm"))
                dgvQuestion.Columns["Số điểm"].Name = "colDiem";
            if (dgvQuestion.Columns.Contains("Đáp Án Đúng"))
                dgvQuestion.Columns["Đáp Án Đúng"].Name = "colCorrect";
            if (dgvQuestion.Columns.Contains("Trạng thái"))
                dgvQuestion.Columns["Trạng thái"].Name = "colStatus";

            if (!dgvQuestion.Columns.Contains("colEdit"))
            {
                DataGridViewButtonColumn edit = new DataGridViewButtonColumn();
                edit.Name = "colEdit";
                edit.HeaderText = "Thao Tác";
                edit.Text = "✎";
                edit.UseColumnTextForButtonValue = true;
                edit.Width = 42;
                edit.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                edit.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                edit.DefaultCellStyle.BackColor = Color.FromArgb(230, 240, 255);
                edit.DefaultCellStyle.ForeColor = Color.FromArgb(45, 107, 233);
                dgvQuestion.Columns.Add(edit);
            }

            if (!dgvQuestion.Columns.Contains("colDelete"))
            {
                DataGridViewButtonColumn del = new DataGridViewButtonColumn();
                del.Name = "colDelete";
                del.HeaderText = "";
                del.Text = "🗑";
                del.UseColumnTextForButtonValue = true;
                del.Width = 42;
                del.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                del.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                del.DefaultCellStyle.BackColor = Color.FromArgb(255, 230, 230);
                del.DefaultCellStyle.ForeColor = Color.FromArgb(200, 40, 40);
                dgvQuestion.Columns.Add(del);
            }

            // Column widths
            void W(string n, int fw) { if (dgvQuestion.Columns.Contains(n)) dgvQuestion.Columns[n].FillWeight = fw; }
            W("Mã", 40); W("Câu Hỏi", 220); W("A", 105); W("B", 105); W("C", 105); W("D", 105);
            W("colCorrect", 100); W("Chia sẻ server", 75); W("colDiem", 90); W("colStatus", 95); W("Đề Thi", 55); W("Lượt Sử Dụng", 70);
        }

        private void DgvQuestion_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            selectedID = GetQuestionIdFromRow(dgvQuestion.Rows[e.RowIndex]);

            if (dgvQuestion.Columns[e.ColumnIndex].Name == "colEdit")
            {
                UpdateQuestion();
            }
            else if (dgvQuestion.Columns[e.ColumnIndex].Name == "Chia sẻ server")
            {
                ToggleQuestionPublish(dgvQuestion.Rows[e.RowIndex]);
            }
            else if (dgvQuestion.Columns[e.ColumnIndex].Name == "colDelete")
            {
                DeleteQuestion();
            }
        }

        private void ToggleQuestionPublish(DataGridViewRow row)
        {
            int questionId = GetQuestionIdFromRow(row);
            if (questionId <= 0) return;

            bool current = false;
            if (row.DataGridView.Columns.Contains("Chia sẻ server") && row.Cells["Chia sẻ server"].Value != null && row.Cells["Chia sẻ server"].Value != DBNull.Value)
                current = Convert.ToBoolean(row.Cells["Chia sẻ server"].Value);

            db.ExecuteNonQuery("UPDATE Questions SET IsPublishedToServer=" + (current ? "0" : "1") + " WHERE QuestionID=" + questionId + (currentUserID > 0 ? " AND CreatedByUserID=" + currentUserID : ""));
            LoadQuestion();
        }

        private void DgvQuestion_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            selectedID = GetQuestionIdFromRow(dgvQuestion.Rows[e.RowIndex]);
        }

        private void FillEditorsFromRow(DataGridViewRow row)
        {
            if (row == null) return;
            selectedID = GetQuestionIdFromRow(row);
            SetTxt(txtQuestion, row.Cells["Câu Hỏi"].Value?.ToString());
            SetTxt(txtA, row.Cells["A"].Value?.ToString());
            SetTxt(txtB, row.Cells["B"].Value?.ToString());
            SetTxt(txtC, row.Cells["C"].Value?.ToString());
            SetTxt(txtD, row.Cells["D"].Value?.ToString());

            string correct = row.Cells["colCorrect"].Value?.ToString();
            if (correct == "A" || correct == "B" || correct == "C" || correct == "D")
                cboCorrect.Text = correct;
            else
                cboCorrect.SelectedIndex = 0;
        }

        private int GetQuestionIdFromRow(DataGridViewRow row)
        {
            if (row == null || row.DataGridView == null || !row.DataGridView.Columns.Contains("Mã") || row.Cells["Mã"].Value == null)
                return -1;
            return int.TryParse(row.Cells["Mã"].Value.ToString(), out int id) ? id : -1;
        }

        private void SetTxt(TextBox t, string val)
        {
            if (t == null) return;
            t.Text = val ?? "";
            t.ForeColor = string.IsNullOrWhiteSpace(val) ? Color.Gray : Color.Black;
        }

        // ============================================================
        // COMBO EXAM
        // ============================================================
        private void LoadExamToComboBox()
        {
            string ownerWhere = currentUserID > 0 ? "WHERE CreatedByUserID = " + currentUserID + " " : "";
            DataTable dt = db.ExecuteQuery("SELECT ExamID, ExamName FROM Exams " + ownerWhere + "ORDER BY ExamID DESC");
            DataRow row = dt.NewRow();
            row["ExamID"] = 0; row["ExamName"] = "-- NONE --";
            dt.Rows.InsertAt(row, 0);
            cboExam.DataSource = dt;
            cboExam.DisplayMember = "ExamName";
            cboExam.ValueMember = "ExamID";
        }

        // ============================================================
        // STATISTICS
        // ============================================================
        private void LoadPointsChart()
        {
            if (chartDifficulty == null || chartDifficulty.Series.Count == 0) return;
            int chua = 0, d1 = 0, d2 = 0, d3 = 0;

            DataTable dt;
            string ownerWhere = currentUserID > 0 ? " WHERE CreatedByUserID = " + currentUserID : "";
            try { dt = db.ExecuteQuery("SELECT Points FROM Questions" + ownerWhere); }
            catch { return; }
            foreach (DataRow r in dt.Rows)
            {
                if (r["Points"] == DBNull.Value || r["Points"] == null) { chua++; continue; }
                decimal p = Convert.ToDecimal(r["Points"]);
                if (p <= 1) d1++;
                else if (p <= 2) d2++;
                else d3++;
            }

            int total = chua + d1 + d2 + d3;
            Series s = chartDifficulty.Series[0];
            s.Points.Clear();

            Action<string, int, Color> addPt = (label, val, color) =>
            {
                if (val > 0)
                {
                    s.Points.AddXY(label, val);
                    int idx = s.Points.Count - 1;
                    s.Points[idx].Color = color;
                    string pct = total > 0 ? $" ({(val * 100.0 / total):0.0}%)" : "";
                    s.Points[idx].LegendText = label + pct;
                    s.Points[idx].Label = "";
                }
            };

            addPt("Chưa gán", chua, Color.FromArgb(148, 163, 184));
            addPt("≤ 1 điểm", d1, Color.FromArgb(46, 213, 115));
            addPt("2 điểm", d2, Color.FromArgb(255, 165, 2));
            addPt("≥ 3 điểm", d3, Color.FromArgb(235, 47, 47));

            // Center label
            chartDifficulty.Titles.Clear();
            Title centerTitle = new Title(total.ToString() + "\nTổng");
            centerTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            centerTitle.ForeColor = Color.FromArgb(40, 50, 80);
            centerTitle.IsDockedInsideChartArea = true;
            centerTitle.DockedToChartArea = "Main";
            centerTitle.Docking = Docking.Top;
            chartDifficulty.Titles.Add(centerTitle);
        }

        private int GetCount(string sql) { DataTable dt = db.ExecuteQuery(sql); if (dt.Rows.Count == 0) return 0; return Convert.ToInt32(dt.Rows[0][0]); }

        // ============================================================
        // CRUD
        // ============================================================
        private void BtnSearch_Click(object sender, EventArgs e) { LoadQuestionByFilter(); }

        private void BtnQuickAdd_Click(object sender, EventArgs e)
        {
            int examID = GetSelectedExamID();
            using (var dlg = new QuestionEditDialog(null, examID > 0 ? examID : (int?)null, currentUserID))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    MessageBox.Show("Thêm câu hỏi thành công!");
                    LoadQuestion();
                    ClearEditors();
                }
            }
        }

        private void BtnQuickImport_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Question files (*.xlsx;*.csv;*.txt;*.tsv)|*.xlsx;*.csv;*.txt;*.tsv|All files (*.*)|*.*";
                ofd.Title = "Chọn file câu hỏi";
                if (ofd.ShowDialog(this) != DialogResult.OK) return;

                int examID = GetSelectedExamID();
                try
                {
                    var service = new QuestionImportService();
                    QuestionImportResult preview = service.PreviewImport(ofd.FileName);
                    if (!ShowQuestionImportPreviewDialog(ofd.FileName, preview))
                        return;

                    QuestionImportResult result = service.Import(ofd.FileName, currentUserID, examID > 0 ? (int?)examID : null);
                    string linked = examID > 0 ? Environment.NewLine + "Đã thêm vào đề đang chọn: " + result.Linked + " câu." : "";
                    string errors = result.Errors.Count > 0 ? Environment.NewLine + "Một số dòng cần xem lại:" + Environment.NewLine + string.Join(Environment.NewLine, result.Errors) : "";
                    MessageBox.Show("Đã nhập " + result.Imported + " câu hỏi hợp lệ." + linked + Environment.NewLine + "Bỏ qua: " + result.Skipped + " dòng." + errors,
                        "Import câu hỏi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadQuestion();
                    LoadExamToComboBox();
                    LoadPointsChart();
                    ClearEditors();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể import file câu hỏi." + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine + QuestionImportService.BuildGuide(),
                        "Lỗi import", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private bool ShowQuestionImportPreviewDialog(string fileName, QuestionImportResult preview)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Xem trước khi import câu hỏi";
                dlg.Size = new Size(720, 520);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.Font = Font;

                var lblTitle = new Label { Text = "Kiểm tra file trước khi import", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 18) };
                var lblFile = new Label { Text = "File: " + Path.GetFileName(fileName), AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(22, 50) };
                var txtGuide = new TextBox
                {
                    Location = new Point(20, 80),
                    Size = new Size(660, 92),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = QuestionImportService.BuildGuide()
                };
                var lblTotal = new Label { Text = "Tổng dòng đọc được: " + preview.TotalRows, AutoSize = true, Location = new Point(20, 192), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
                var lblValid = new Label { Text = "Dòng hợp lệ có thể import: " + preview.ValidRows, AutoSize = true, Location = new Point(20, 220), ForeColor = Color.FromArgb(22, 163, 74) };
                var lblSkipped = new Label { Text = "Dòng bị bỏ qua: " + preview.Skipped, AutoSize = true, Location = new Point(20, 246), ForeColor = Color.FromArgb(245, 158, 11) };
                var lblErrorTitle = new Label { Text = "Các lỗi phát hiện trước khi import", AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(20, 286) };
                var txtErrors = new TextBox
                {
                    Location = new Point(20, 314),
                    Size = new Size(660, 118),
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Text = preview.Errors.Count == 0 ? "Không phát hiện lỗi định dạng phổ biến. Bạn có thể import file ngay." : string.Join(Environment.NewLine, preview.Errors)
                };
                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(510, 446), Size = new Size(80, 32) };
                var btnOk = new Button { Text = "Xác nhận import", DialogResult = DialogResult.OK, Location = new Point(600, 446), Size = new Size(80, 32), Enabled = preview.ValidRows > 0 };
                UiTheme.ApplyTextBox(txtGuide);
                UiTheme.ApplyTextBox(txtErrors);
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);
                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);

                dlg.Controls.AddRange(new Control[] { lblTitle, lblFile, txtGuide, lblTotal, lblValid, lblSkipped, lblErrorTitle, txtErrors, btnCancel, btnOk });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;
                return dlg.ShowDialog(this) == DialogResult.OK;
            }
        }

        private void BtnQuickTemplate_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel file (*.xlsx)|*.xlsx|CSV file (*.csv)|*.csv|TSV file (*.tsv)|*.tsv";
                sfd.FileName = "Mau_nhap_cau_hoi.xlsx";
                sfd.Title = "Lưu file mẫu câu hỏi";
                sfd.OverwritePrompt = true;
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

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

        private void UpdateQuestion()
        {
            if (selectedID <= 0) { MessageBox.Show("Chọn câu hỏi cần sửa!"); return; }
            using (var dlg = new QuestionEditDialog(selectedID, null, currentUserID))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    MessageBox.Show("Cập nhật thành công!");
                    LoadQuestion();
                    ClearEditors();
                }
            }
        }

        private void DeleteQuestion()
        {
            if (selectedID <= 0) { MessageBox.Show("Chọn câu hỏi cần xóa!"); return; }
            int inUse = GetCount($"SELECT COUNT(*) FROM ExamQuestions WHERE QuestionID={selectedID}");
            if (inUse > 0) { MessageBox.Show("Không thể xóa vì câu hỏi đã nằm trong đề thi!"); return; }

            if (MessageBox.Show("Bạn có chắc muốn xóa câu hỏi này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { db.ExecuteNonQuery($"DELETE FROM Questions WHERE QuestionID={selectedID}"); MessageBox.Show("Xóa thành công!"); LoadQuestion(); ClearEditors(); }
            catch (Exception ex) { MessageBox.Show("Không thể xóa câu hỏi này.\n" + ex.Message); }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(GetTxtVal(txtQuestion))) { MessageBox.Show("Nhập câu hỏi!"); return false; }
            if (string.IsNullOrWhiteSpace(GetTxtVal(txtA)) || string.IsNullOrWhiteSpace(GetTxtVal(txtB)) ||
                string.IsNullOrWhiteSpace(GetTxtVal(txtC)) || string.IsNullOrWhiteSpace(GetTxtVal(txtD)))
            { MessageBox.Show("Nhập đầy đủ 4 đáp án!"); return false; }
            if (cboCorrect.SelectedIndex <= 0) { MessageBox.Show("Chọn đáp án đúng!"); return false; }
            return true;
        }

        private string GetTxtVal(TextBox t) => (t == null || t.ForeColor == Color.Gray) ? "" : t.Text.Trim();

        private void AddQuestionToExam(int examID, int questionID)
        {
            int exist = GetCount($"SELECT COUNT(*) FROM ExamQuestions WHERE ExamID={examID} AND QuestionID={questionID}");
            if (exist > 0) return;
            db.ExecuteNonQuery($"INSERT INTO ExamQuestions (ExamID,QuestionID) VALUES ({examID},{questionID})");
        }

        private void UpdateExamTotal(int examID)
        {
            int total = GetCount($"SELECT COUNT(*) FROM ExamQuestions WHERE ExamID={examID}");
            db.ExecuteNonQuery($"UPDATE Exams SET TotalQuestion={total} WHERE ExamID={examID}");
        }

        // ============================================================
        // PRINT EXAM PAPER
        // ============================================================
        private void BtnQuickPrint_Click(object sender, EventArgs e)
        {
            Database.EnsureAdvancedExamColumns();
            DataTable exams = db.ExecuteQuery(
                "SELECT e.ExamID, e.ExamName, ISNULL((SELECT COUNT(*) FROM ExamQuestions eq WHERE eq.ExamID=e.ExamID),0) AS QuestionCount " +
                "FROM Exams e " + (currentUserID > 0 ? "WHERE e.CreatedByUserID=" + currentUserID + " " : "") +
                "ORDER BY e.ExamID DESC");

            if (exams.Rows.Count == 0)
            {
                MessageBox.Show("Chưa có bộ đề để in.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new Form())
            {
                dlg.Text = "Chọn bộ đề để in";
                dlg.Size = new Size(460, 180);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.Font = Font;

                var lbl = new Label { Text = "Bộ đề:", Location = new Point(18, 22), AutoSize = true };
                var cbo = new ComboBox { Location = new Point(70, 18), Width = 340, DropDownStyle = ComboBoxStyle.DropDownList };
                UiTheme.ApplyComboBox(cbo);
                cbo.DataSource = exams;
                cbo.DisplayMember = "ExamName";
                cbo.ValueMember = "ExamID";

                var btnPreview = new Button { Text = "Xem trước / In", Location = new Point(220, 78), Size = new Size(110, 34), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Hủy", Location = new Point(340, 78), Size = new Size(70, 34), DialogResult = DialogResult.Cancel };
                UiTheme.ApplyButton(btnPreview, UiButtonVariant.Primary);
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);
                dlg.Controls.AddRange(new Control[] { lbl, cbo, btnPreview, btnCancel });
                dlg.AcceptButton = btnPreview;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                int examId = Convert.ToInt32(cbo.SelectedValue);
                printExamName = ((DataRowView)cbo.SelectedItem)["ExamName"].ToString();
                LoadPrintExamQuestions(examId);
                if (printExamQuestions.Rows.Count == 0)
                {
                    MessageBox.Show("Bộ đề này chưa có câu hỏi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                PrintExamPaper();
            }
        }

        private void LoadPrintExamQuestions(int examId)
        {
            printExamQuestions = db.ExecuteQuery(
                "SELECT q.QuestionID, q.QuestionContent, q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.CorrectAnswer, " +
                "CASE WHEN q.QuestionType = N'FillBlank' THEN N'FillBlank' ELSE N'MultipleChoice' END AS QuestionType " +
                "FROM ExamQuestions eq INNER JOIN Questions q ON eq.QuestionID=q.QuestionID " +
                "WHERE eq.ExamID=" + examId + " " +
                "ORDER BY q.QuestionID");
        }

        private void PrintExamPaper()
        {
            using (var doc = new PrintDocument())
            using (var preview = new PrintPreviewDialog())
            {
                doc.DocumentName = printExamName;
                doc.BeginPrint += (s, e) => printQuestionIndex = 0;
                doc.PrintPage += PrintExamPage;
                preview.Document = doc;
                preview.Width = 1000;
                preview.Height = 720;
                preview.ShowDialog(this);
            }
        }

        private void PrintExamPage(object sender, PrintPageEventArgs e)
        {
            float y = e.MarginBounds.Top;
            using (var titleFont = new Font("Segoe UI", 15, FontStyle.Bold))
            using (var metaFont = new Font("Segoe UI", 10, FontStyle.Italic))
            using (var font = new Font("Segoe UI", 10))
            {
                if (printQuestionIndex == 0)
                {
                    e.Graphics.DrawString(printExamName, titleFont, Brushes.Black, e.MarginBounds.Left, y);
                    y += 28;
                    e.Graphics.DrawString("Họ tên: ____________________________    Lớp: __________    Ngày: ____/____/______", metaFont, Brushes.Black, e.MarginBounds.Left, y);
                    y += 34;
                }

                while (printQuestionIndex < printExamQuestions.Rows.Count)
                {
                    DataRow r = printExamQuestions.Rows[printQuestionIndex];
                    bool fillBlank = r["QuestionType"].ToString() == "FillBlank";
                    string text = "Câu " + (printQuestionIndex + 1) + ": " + r["QuestionContent"] + Environment.NewLine;
                    if (fillBlank)
                    {
                        text += "Trả lời: ________________________________________________" + Environment.NewLine;
                    }
                    else
                    {
                        text += "A. " + r["OptionA"] + Environment.NewLine +
                                "B. " + r["OptionB"] + Environment.NewLine +
                                "C. " + r["OptionC"] + Environment.NewLine +
                                "D. " + r["OptionD"] + Environment.NewLine;
                    }

                    SizeF size = e.Graphics.MeasureString(text, font, e.MarginBounds.Width);
                    if (y + size.Height > e.MarginBounds.Bottom)
                    {
                        e.HasMorePages = true;
                        return;
                    }

                    e.Graphics.DrawString(text, font, Brushes.Black, new RectangleF(e.MarginBounds.Left, y, e.MarginBounds.Width, size.Height));
                    y += size.Height + 12;
                    printQuestionIndex++;
                }

                e.HasMorePages = false;
            }
        }

        // ============================================================
        // EXPORT
        // ============================================================
        private void BtnQuickExport_Click(object sender, EventArgs e)
        {
            if (fullData == null || fullData.Rows.Count == 0) { MessageBox.Show("Không có dữ liệu để xuất."); return; }
            SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV File (*.csv)|*.csv", FileName = "Questions.csv" };
            if (sfd.ShowDialog() != DialogResult.OK) return;
            StringBuilder sb = new StringBuilder();

            foreach (DataColumn col in fullData.Columns)
            {
                sb.Append(EscapeCsv(col.ColumnName));
                if (fullData.Columns.IndexOf(col) < fullData.Columns.Count - 1) sb.Append(",");
            }
            sb.AppendLine();
            foreach (DataRow row in fullData.Rows)
            {
                for (int i = 0; i < fullData.Columns.Count; i++)
                {
                    sb.Append(EscapeCsv(row[i]?.ToString() ?? ""));
                    if (i < fullData.Columns.Count - 1) sb.Append(",");
                }
                sb.AppendLine();
            }
            File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("Xuất CSV thành công!");
        }

        private string EscapeCsv(string v) => "\"" + (v ?? "").Replace("\"", "\"\"") + "\"";

        // ============================================================
        // NAVIGATION
        // ============================================================
        private void BtnTopRefresh_Click(object sender, EventArgs e)
        {
            if (pnlHomeHost != null && pnlHomeHost.Visible)
                ucTrangChu?.RefreshData();
            else if (pnlStatsHost != null && pnlStatsHost.Visible)
                ucThongKe?.RefreshData();
            else if (pnlMain != null && pnlMain.Visible)
            {
                LoadExamToComboBox();
                LoadQuestion();
                ClearEditors();
            }
        }

        private void BtnBangDiem_Click(object sender, EventArgs e)
        {
            HideAllContentViews();
            pnlHeader.Visible = true;
            if (btnTopRefresh != null) btnTopRefresh.Visible = false;
            if (pnlScoreHost != null) pnlScoreHost.Visible = true;

            lblTitle.Text = "BẢNG ĐIỂM HỌC SINH";
            lblSubTitle.Text = "Theo dõi kết quả theo lớp, bài thi và xem chi tiết bài làm ngay trong khung chính.";

            EnsureScoreReportView();
            SetMenuActive(btnBangDiem);
        }

        private void BtnTrangChu_Click(object sender, EventArgs e) => ShowHomeView();

        private void BtnThongKe_Click(object sender, EventArgs e) => ShowStatsView();

        private void HideAllContentViews()
        {
            if (pnlHomeHost != null) pnlHomeHost.Visible = false;
            if (pnlStatsHost != null) pnlStatsHost.Visible = false;
            if (pnlMain != null) pnlMain.Visible = false;
            if (pnlExamManagerHost != null) pnlExamManagerHost.Visible = false;
            if (pnlScoreHost != null) pnlScoreHost.Visible = false;
        }

        private void ShowHomeView()
        {
            HideAllContentViews();
            pnlHeader.Visible = true;
            if (btnTopRefresh != null) btnTopRefresh.Visible = true;
            pnlHomeHost.Visible = true;
            lblTitle.Text = "Xin chào, " + currentUserName + "! 👋";
            lblSubTitle.Text = "Chào mừng bạn đến với hệ thống quản lý đề thi.";
            ucTrangChu?.RefreshData();
            SetMenuActive(btnTrangChu);
        }

        private void ShowStatsView()
        {
            HideAllContentViews();
            pnlHeader.Visible = true;
            if (btnTopRefresh != null) btnTopRefresh.Visible = true;
            pnlStatsHost.Visible = true;
            lblTitle.Text = "THỐNG KÊ 📊";
            lblSubTitle.Text = "Tổng quan tình hình sử dụng hệ thống";
            ucThongKe?.RefreshData();
            SetMenuActive(btnThongKe);
        }

        private void BtnNganHangCauHoi_Click(object sender, EventArgs e)
        {
            HideAllContentViews();
            pnlHeader.Visible = true;
            if (btnTopRefresh != null) btnTopRefresh.Visible = true;
            pnlMain.Visible = true;
            if (pnlFilter != null) pnlFilter.Visible = true;
            if (pnlBody != null) pnlBody.Visible = true;
            if (pnlChartBox != null) pnlChartBox.Visible = true;
            if (pnlQuickAction != null) pnlQuickAction.Visible = true;
            if (pnlGridWrapper != null) pnlGridWrapper.Visible = true;

            lblTitle.Text = "QUẢN LÝ NGÂN HÀNG CÂU HỎI";
            lblSubTitle.Text = "Quản lý, tìm kiếm và theo dõi câu hỏi trong hệ thống";

            LoadQuestion();
            LoadExamToComboBox();
            SetMenuActive(btnNganHangCauHoi);
        }

        private void BtnQuanLyDeThi_Click(object sender, EventArgs e)
        {
            HideAllContentViews();
            pnlHeader.Visible = false;
            pnlExamManagerHost.Visible = true;

            pnlExamManagerHost.Controls.Clear();

            ucManageExam uc = new ucManageExam(currentUserID);
            uc.Dock = DockStyle.Fill;
            pnlExamManagerHost.Controls.Add(uc);

            SetMenuActive(btnQuanLyDeThi);
        }

        private void RestoreActiveMenuForCurrentView()
        {
            if (pnlExamManagerHost != null && pnlExamManagerHost.Visible)
                SetMenuActive(btnQuanLyDeThi);
            else if (pnlScoreHost != null && pnlScoreHost.Visible)
                SetMenuActive(btnBangDiem);
            else if (pnlStatsHost != null && pnlStatsHost.Visible)
                SetMenuActive(btnThongKe);
            else if (pnlMain != null && pnlMain.Visible)
                SetMenuActive(btnNganHangCauHoi);
            else
                SetMenuActive(btnTrangChu);
        }

        private void EnsureScoreReportView()
        {
            if (pnlScoreHost == null) return;
            if (scoreReportView != null && !scoreReportView.IsDisposed)
            {
                if (!pnlScoreHost.Controls.Contains(scoreReportView))
                {
                    pnlScoreHost.Controls.Clear();
                    pnlScoreHost.Controls.Add(scoreReportView);
                }
                return;
            }

            scoreReportView = new frmScoreReport(currentUserID);
            scoreReportView.TopLevel = false;
            scoreReportView.FormBorderStyle = FormBorderStyle.None;
            scoreReportView.Dock = DockStyle.Fill;
            scoreReportView.ShowInTaskbar = false;
            scoreReportView.StartPosition = FormStartPosition.Manual;

            pnlScoreHost.Controls.Clear();
            pnlScoreHost.Controls.Add(scoreReportView);
            scoreReportView.Show();
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn có muốn đăng xuất không?", "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                frmLogin f = new frmLogin();
                f.Show();
                Hide();
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================
        private int GetSelectedExamID()
        {
            if (cboExam?.DataSource == null || cboExam.SelectedValue == null) return 0;
            return int.TryParse(cboExam.SelectedValue.ToString(), out int id) ? id : 0;
        }

        private string Escape(string text) => (text ?? "").Replace("'", "''").Trim();

        private void ClearEditors()
        {
            selectedID = -1;
            void ClearTxt(TextBox t, string ph) { if (t != null) { t.Text = ph; t.ForeColor = Color.Gray; } }
            ClearTxt(txtQuestion, "Nhập nội dung câu hỏi...");
            ClearTxt(txtA, "Nhập đáp án A...");
            ClearTxt(txtB, "Nhập đáp án B...");
            ClearTxt(txtC, "Nhập đáp án C...");
            ClearTxt(txtD, "Nhập đáp án D...");
            if (cboCorrect != null) cboCorrect.SelectedIndex = 0;
            if (cboExam?.DataSource != null && cboExam.Items.Count > 0) cboExam.SelectedIndex = 0;
        }

        private void SetMenuActive(Button active)
        {
            Button[] btns = { btnTrangChu, btnQuanLyDeThi, btnNganHangCauHoi, btnBangDiem, btnThongKe };
            foreach (Button b in btns)
            {
                if (b == null) continue;
                b.BackColor = (b == active) ? SidebarActive : SidebarBg;
                b.ForeColor = Color.White;
            }
        }
    }
}
