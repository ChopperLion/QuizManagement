using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QuizManagement.BUS;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class frmStudent : Form
    {
        private readonly Database db = new Database();

        private int userID;
        private string currentUserName;
        private readonly string pendingExamLink;
        private string currentStudentClassName = "";
        private string currentAvatarPath = "";
        private RoundPictureBox sidebarAvatarBox;
        private RoundPictureBox accountAvatarBox;

        // =========================
        // MAIN LAYOUT
        // =========================
        private Panel pnlSidebar;
        private Panel pnlMain;
        private Panel pnlTopBar;
        private Panel pnlPageHost;

        private Button btnHome;
        private Button btnHistory;
        private Button btnRegisterExam;
        private Button btnNotifications;
        private Button btnAccount;
        private Button btnLogout;

        // =========================
        // PAGES
        // =========================
        private Panel pnlDashboardPage;
        private Panel pnlRegisterPage;
        private Panel pnlNotificationsPage;
        private Panel pnlAccountPage;
        private Panel pnlHistoryPage;

        // =========================
        // HISTORY PAGE CONTROLS
        // =========================
        private DataGridView dgvHistory;
        private Label lblHistorySummary;
        private Label lblHistoryEmptyState;

        // =========================
        // DASHBOARD CONTROLS
        // =========================
        private Label lblDashboardExamCount;
        private Label lblDashboardAverageScore;
        private Label lblDashboardBestScore;
        private Label lblDashboardRank;

        private Panel pnlDashboardScheduleCard;
        private Panel pnlDashboardRecentResultCard;
        private Panel pnlDashboardProgressCard;
        private Panel pnlDashboardChartCard;

        private LinkLabel lnkSeeAllSchedule;

        // =========================
        // REGISTRATION PAGE
        // =========================
        private TextBox txtExamSearch;
        private DataGridView dgvExams;
        private Label lblExamStatusHint;
        private TabControl tabRegister;
        private TabPage tabExamJoin;
        private TabPage tabMyPracticeSets;
        private ucManageExam studentPracticeManager;

        // =========================
        // RESULTS PAGE
        // =========================

        // =========================
        // NOTIFICATIONS PAGE
        // =========================
        private DataGridView dgvNotifications;
        private Label lblNotificationDetailTitle;
        private TextBox txtNotificationDetail;
        private Label lblNotificationEmptyState;

        // =========================
        // ACCOUNT PAGE
        // =========================
        private TextBox txtAccountFullName;
        private TextBox txtAccountClassName;
        private TextBox txtAccountUserName;
        private TextBox txtAccountCurrentPass;
        private TextBox txtAccountNewPass;
        private TextBox txtAccountConfirmPass;
        private Label lblAccountRole;
        private Label lblAccountStatus;
        private Label lblAccountId;
        private Label lblAccountCreated;
        private DataRow _currentUserRow;

        // =========================
        // COLORS
        // =========================
        private static readonly Color C_BLUE = Color.FromArgb(60, 91, 250);
        private static readonly Color C_BLUE_LIGHT = Color.FromArgb(238, 241, 255);
        private static readonly Color C_GREEN = Color.FromArgb(0, 180, 122);
        private static readonly Color C_GREEN_LIGHT = Color.FromArgb(230, 249, 241);
        private static readonly Color C_ORANGE = Color.FromArgb(255, 140, 0);
        private static readonly Color C_ORANGE_LIGHT = Color.FromArgb(255, 244, 230);
        private static readonly Color C_PURPLE = Color.FromArgb(123, 94, 167);
        private static readonly Color C_PURPLE_LIGHT = Color.FromArgb(243, 238, 255);
        private static readonly Color C_BG = Color.FromArgb(245, 247, 250);
        private static readonly Color C_WHITE = Color.White;
        private static readonly Color C_BORDER = Color.FromArgb(230, 232, 236);
        private static readonly Color C_TEXT = Color.FromArgb(30, 30, 30);
        private static readonly Color C_MUTED = Color.FromArgb(130, 135, 150);
        private static readonly Color C_RED = Color.FromArgb(220, 38, 38);

        private const int SidebarWidth = 230;
        private const int TopBarHeight = 80;

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public frmStudent(int userID) : this(userID, "Thí Sinh")
        {
        }

        public frmStudent(int userID, string fullName) : this(userID, fullName, "")
        {
        }

        public frmStudent(int userID, string fullName, string pendingExamLink)
        {
            this.userID = userID;
            this.currentUserName = string.IsNullOrWhiteSpace(fullName) ? "Thí Sinh" : fullName.Trim();
            this.pendingExamLink = pendingExamLink ?? "";
            InitUI();
            LoadAllData();
            Shown += FrmStudent_Shown;
        }

        // =====================================================
        // UI INIT
        // =====================================================
        private void InitUI()
        {
            Text = "Student Panel";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1300, 800);
            BackColor = C_BG;
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Font;
            DoubleBuffered = true;

            BuildSidebar();
            BuildMain();

            Load += (s, e) => UpdateLayout();
            Resize += (s, e) => UpdateLayout();
        }

        private void BuildSidebar()
        {
            pnlSidebar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(SidebarWidth, ClientSize.Height),
                BackColor = C_WHITE,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            pnlSidebar.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(C_BORDER, 1),
                    pnlSidebar.Width - 1, 0,
                    pnlSidebar.Width - 1, pnlSidebar.Height);
            };
            Controls.Add(pnlSidebar);

            // Logo
            var pnlLogo = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(SidebarWidth, 70),
                BackColor = C_WHITE
            };
            pnlLogo.Paint += (s, e) => e.Graphics.DrawLine(new Pen(C_BORDER, 1), 0, 69, SidebarWidth, 69);

            var lblIcon = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 20),
                Location = new Point(18, 18),
                Size = new Size(40, 36),
                AutoSize = false
            };
            var lblLogoText = new Label
            {
                Text = "Quiz System",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = C_BLUE,
                Location = new Point(58, 16),
                AutoSize = true
            };
            var lblLogoSub = new Label
            {
                Text = "Hệ thống thi trắc nghiệm",
                Font = new Font("Segoe UI", 8),
                ForeColor = C_MUTED,
                Location = new Point(60, 42),
                AutoSize = true
            };
            pnlLogo.Controls.AddRange(new Control[] { lblIcon, lblLogoText, lblLogoSub });
            pnlSidebar.Controls.Add(pnlLogo);

            // User info
            var pnlUserInfo = new Panel
            {
                Location = new Point(0, 70),
                Size = new Size(SidebarWidth, 90),
                BackColor = C_WHITE
            };
            pnlUserInfo.Paint += (s, e) => e.Graphics.DrawLine(new Pen(C_BORDER, 1), 0, 89, SidebarWidth, 89);

            sidebarAvatarBox = new RoundPictureBox
            {
                Location = new Point(18, 20),
                Size = new Size(48, 48),
                BackColor = C_BLUE_LIGHT,
                Text = "",
                Font = new Font("Segoe UI", 20)
            };
            var lblUserName = new Label
            {
                Text = currentUserName,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(76, 20),
                AutoSize = false,
                Size = new Size(140, 22)
            };
            var lblUserId = new Label
            {
                Text = "User ID: " + userID,
                Font = new Font("Segoe UI", 8),
                ForeColor = C_MUTED,
                Location = new Point(76, 42),
                AutoSize = true
            };
            var lblStatus = new StatusBadge
            {
                Location = new Point(76, 60),
                Size = new Size(110, 20),
                Text = "Đang hoạt động",
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 163, 92),
                BackColor = Color.FromArgb(230, 249, 241)
            };
            pnlUserInfo.Controls.AddRange(new Control[] { sidebarAvatarBox, lblUserName, lblUserId, lblStatus });
            pnlSidebar.Controls.Add(pnlUserInfo);

            // Nav
            var pnlNavMenu = new Panel
            {
                Location = new Point(0, 160),
                Size = new Size(SidebarWidth, 380),
                BackColor = C_WHITE
            };

            btnHome = CreateNavButton("Trang chủ", 0, true, (s, e) => ShowPage(PageKind.Dashboard));
            btnHistory = CreateNavButton("Lịch sử thi", 52, false, BtnHistory_Click);
            btnRegisterExam = CreateNavButton("Bài thi", 104, false, (s, e) => ShowPage(PageKind.RegisterExam));
            btnNotifications = CreateNavButton("Thông báo", 156, false, (s, e) => ShowPage(PageKind.Notifications));
            btnAccount = CreateNavButton("Tài khoản", 208, false, (s, e) => ShowPage(PageKind.Account));
            btnLogout = CreateNavButton("Đăng xuất", 260, false, BtnLogout_Click);

            pnlNavMenu.Controls.AddRange(new Control[]
            {
                btnHome, btnHistory, btnRegisterExam, btnNotifications, btnAccount, btnLogout
            });

            pnlSidebar.Controls.Add(pnlNavMenu);

            // Support
            var pnlSupport = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 84,
                BackColor = C_WHITE
            };
            pnlSupport.Paint += (s, e) => e.Graphics.DrawLine(new Pen(C_BORDER, 1), 0, 0, SidebarWidth, 0);

            var pnlSupportBox = new RoundedPanel
            {
                Location = new Point(12, 12),
                Size = new Size(206, 54),
                BackColor = C_BG
            };
            var lblSupportIcon = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 16),
                Location = new Point(10, 12),
                Size = new Size(30, 36),
                AutoSize = false
            };
            var lblSupportTitle = new Label
            {
                Text = "Hỗ trợ nhanh",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(18, 9),
                AutoSize = true
            };
            var lblSupportSub = new Label
            {
                Text = "Liên hệ nếu cần hỗ trợ",
                Font = new Font("Segoe UI", 8),
                ForeColor = C_MUTED,
                Location = new Point(18, 29),
                Size = new Size(148, 30),
                AutoSize = false
            };
            pnlSupportBox.Controls.AddRange(new Control[] { lblSupportTitle, lblSupportSub });
            pnlSupportBox.Cursor = Cursors.Hand;
            EventHandler showSupport = (s, e) =>
            {
                MessageBox.Show("Vui lòng liên hệ giáo vụ hoặc quản trị viên để được hỗ trợ.", "Hỗ trợ", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            pnlSupportBox.Click += showSupport;
            lblSupportTitle.Click += showSupport;
            lblSupportSub.Click += showSupport;

            pnlSupport.Controls.Add(pnlSupportBox);
            pnlSidebar.Controls.Add(pnlSupport);
        }

        private void BuildMain()
        {
            pnlMain = new Panel
            {
                Location = new Point(SidebarWidth, 0),
                Size = new Size(Math.Max(0, ClientSize.Width - SidebarWidth), ClientSize.Height),
                BackColor = C_BG,
                AutoScroll = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(pnlMain);

            BuildTopBar();

            pnlPageHost = new Panel
            {
                Location = new Point(0, TopBarHeight),
                Size = new Size(pnlMain.ClientSize.Width, pnlMain.ClientSize.Height - TopBarHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = C_BG
            };
            pnlMain.Controls.Add(pnlPageHost);

            BuildDashboardPage();
            BuildHistoryPage();
            BuildRegisterPage();
            BuildNotificationsPage();
            BuildAccountPage();

            pnlPageHost.Controls.AddRange(new Control[]
            {
                pnlDashboardPage,
                pnlHistoryPage,
                pnlRegisterPage,
                pnlNotificationsPage,
                pnlAccountPage
            });

            ShowPage(PageKind.Dashboard);

            pnlMain.Resize += (s, e) =>
            {
                pnlPageHost.Size = new Size(pnlMain.ClientSize.Width, pnlMain.ClientSize.Height - TopBarHeight);
                RepositionPages();
            };
        }

        private void BuildTopBar()
        {
            pnlTopBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(pnlMain.ClientSize.Width, TopBarHeight),
                Height = TopBarHeight,
                BackColor = C_WHITE,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlTopBar.Paint += (s, e) => e.Graphics.DrawLine(new Pen(C_BORDER, 1), 0, 79, pnlTopBar.Width, 79);

            var lblGreet = new Label
            {
                Text = "Xin chào, " + currentUserName + "!",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(28, 14),
                AutoSize = true
            };
            var lblSub = new Label
            {
                Text = "Chào mừng bạn đến với hệ thống quản lý thi trắc nghiệm",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_MUTED,
                Location = new Point(30, 50),
                AutoSize = true
            };
            var lblTime = new Label
            {
                Text = DateTime.Now.ToString("HH:mm") + "   " + DateTime.Now.ToString("dd/MM/yyyy"),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_TEXT,
                BackColor = C_BG,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            pnlTopBar.Controls.AddRange(new Control[] { lblGreet, lblSub, lblTime });
            pnlTopBar.Resize += (s, e) =>
            {
                lblTime.Location = new Point(pnlTopBar.Width - lblTime.Width - 20, 28);
            };

            pnlMain.Controls.Add(pnlTopBar);
        }

        // =====================================================
        // PAGES
        // =====================================================
        private enum PageKind
        {
            Dashboard,
            History,
            RegisterExam,
            Notifications,
            Account
        }

        private void BuildDashboardPage()
        {
            pnlDashboardPage = CreatePagePanel();

            // Stats
            var pnlStats = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(1000, 130),
                BackColor = C_BG,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlDashboardPage.Controls.Add(pnlStats);

            pnlStats.Controls.Add(CreateStatCard("Tổng bài thi", "0", "", "", C_BLUE, C_BLUE_LIGHT, new Point(0, 0), out lblDashboardExamCount));
            pnlStats.Controls.Add(CreateStatCard("Điểm trung bình", "0/10", "", "", C_GREEN, C_GREEN_LIGHT, new Point(255, 0), out lblDashboardAverageScore));
            pnlStats.Controls.Add(CreateStatCard("Điểm cao nhất", "0/10", "", "", C_ORANGE, C_ORANGE_LIGHT, new Point(510, 0), out lblDashboardBestScore));
            pnlStats.Controls.Add(CreateStatCard("Xếp hạng", "Chưa có", "", "", C_PURPLE, C_PURPLE_LIGHT, new Point(765, 0), out lblDashboardRank));

            // Row 2 cards
            pnlDashboardScheduleCard = CreateSectionCard("Lịch thi sắp tới", new Point(20, 166), new Size(520, 300));
            pnlDashboardRecentResultCard = CreateSectionCard("Kết quả gần đây", new Point(560, 166), new Size(520, 300));
            pnlDashboardPage.Controls.Add(pnlDashboardScheduleCard);
            pnlDashboardPage.Controls.Add(pnlDashboardRecentResultCard);

            lnkSeeAllSchedule = new LinkLabel
            {
                Text = "Xem tất cả lịch thi",
                Font = new Font("Segoe UI", 9),
                LinkColor = C_BLUE,
                Location = new Point(180, 268),
                AutoSize = true
            };
            lnkSeeAllSchedule.LinkClicked += (s, e) => ShowPage(PageKind.RegisterExam);
            pnlDashboardScheduleCard.Controls.Add(lnkSeeAllSchedule);

            // Row 3 cards
            pnlDashboardProgressCard = CreateSectionCard("Tiến độ học tập", new Point(20, 486), new Size(520, 280));
            pnlDashboardChartCard = CreateSectionCard("Thống kê hiệu suất", new Point(560, 486), new Size(520, 280));
            pnlDashboardPage.Controls.Add(pnlDashboardProgressCard);
            pnlDashboardPage.Controls.Add(pnlDashboardChartCard);

            RebuildDashboardStaticContent();
        }

        private void RebuildDashboardStaticContent()
        {
            pnlDashboardScheduleCard.Controls.Clear();
            pnlDashboardRecentResultCard.Controls.Clear();
            pnlDashboardProgressCard.Controls.Clear();
            pnlDashboardChartCard.Controls.Clear();

            var hdr1 = CreateSectionHeader("Lịch thi sắp tới");
            pnlDashboardScheduleCard.Controls.Add(hdr1);
            pnlDashboardScheduleCard.Controls.Add(CreateSectionDivider(520));

            var hdr2 = CreateSectionHeader("Kết quả gần đây");
            pnlDashboardRecentResultCard.Controls.Add(hdr2);
            pnlDashboardRecentResultCard.Controls.Add(CreateSectionDivider(520));

            var hdr3 = CreateSectionHeader("Tiến độ học tập");
            pnlDashboardProgressCard.Controls.Add(hdr3);
            pnlDashboardProgressCard.Controls.Add(CreateSectionDivider(520));

            var hdr4 = CreateSectionHeader("Thống kê hiệu suất");
            pnlDashboardChartCard.Controls.Add(hdr4);
            pnlDashboardChartCard.Controls.Add(CreateSectionDivider(520));

            pnlDashboardScheduleCard.Controls.Add(lnkSeeAllSchedule);

            // Chart placeholder content
            var chart = new DonutChartPanel
            {
                Location = new Point(46, 70),
                Size = new Size(178, 178),
                BackColor = C_WHITE
            };
            pnlDashboardChartCard.Controls.Add(chart);

            var legendItems = new[]
            {
                new { Label="Xuất sắc (9-10)",  Pct="35%", Color=Color.FromArgb(22,163,74)  },
                new { Label="Tốt (7-8.9)",       Pct="40%", Color=Color.FromArgb(37,99,235)  },
                new { Label="Trung bình (5-6.9)",Pct="20%", Color=Color.FromArgb(217,119,6)  },
                new { Label="Yếu (0-4.9)",       Pct="5%",  Color=Color.FromArgb(220,38,38)   },
            };
            int lx = 270, ly = 96;
            foreach (var leg in legendItems)
            {
                var dot = new Panel { Location = new Point(lx, ly + 3), Size = new Size(10, 10), BackColor = leg.Color };
                DrawRoundedPanel(dot, 5);
                var lbl = new Label
                {
                    Text = leg.Label,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = C_MUTED,
                    Location = new Point(lx + 16, ly),
                    AutoSize = true
                };
                var pct = new Label
                {
                    Text = leg.Pct,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = C_TEXT,
                    Location = new Point(lx + 170, ly),
                    AutoSize = true
                };
                pnlDashboardChartCard.Controls.AddRange(new Control[] { dot, lbl, pct });
                ly += 28;
            }

            // Progress sample bars
            var progressItems = new[]
            {
                new { Name="Lập trình C#", Pct=85, Color=Color.FromArgb(22,163,74) },
                new { Name="Cơ sở dữ liệu", Pct=70, Color=Color.FromArgb(37,99,235) },
                new { Name="Mạng máy tính", Pct=60, Color=Color.FromArgb(217,119,6) },
                new { Name="Tin học cơ bản", Pct=90, Color=Color.FromArgb(123,94,167) },
            };
            int py = 58;
            foreach (var item in progressItems)
            {
                var row = CreateProgressRow(item.Name, item.Pct, item.Color, py);
                pnlDashboardProgressCard.Controls.Add(row);
                py += 52;
            }
        }

        private void BuildRegisterPage()
        {
            pnlRegisterPage = CreatePagePanel();

            var hdr = new Label
            {
                Text = "Danh sách bài thi",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(22, 18),
                AutoSize = true
            };
            var sub = new Label
            {
                Text = "Chọn bài thi phù hợp, xem trạng thái rồi vào làm bài hoặc dùng link/mã phòng nếu giáo viên gửi riêng.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_MUTED,
                Location = new Point(24, 50),
                AutoSize = true
            };
            pnlRegisterPage.Controls.AddRange(new Control[] { hdr, sub });

            tabRegister = new TabControl
            {
                Location = new Point(24, 86),
                Size = new Size(0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9.5f)
            };
            tabExamJoin = new TabPage("Làm bài");
            tabMyPracticeSets = new TabPage("Đề ôn của tôi");
            tabRegister.BackColor = UiTheme.SurfaceRaised;
            tabExamJoin.BackColor = UiTheme.SurfaceRaised;
            tabMyPracticeSets.BackColor = UiTheme.SurfaceRaised;
            tabRegister.TabPages.Add(tabExamJoin);
            tabRegister.TabPages.Add(tabMyPracticeSets);
            pnlRegisterPage.Controls.Add(tabRegister);

            txtExamSearch = new TextBox
            {
                Location = new Point(16, 16),
                Size = new Size(280, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Text = ""
            };
            txtExamSearch.TextChanged += (s, e) => LoadExamsGrid();
            UiTheme.ApplyTextBox(txtExamSearch);
            tabExamJoin.Controls.Add(txtExamSearch);
            SetPlaceholder(txtExamSearch, "Tìm kiếm bài thi...");

            var btnRefresh = CreateBlueButton("Làm mới", new Point(310, 14), new Size(110, 32));
            btnRefresh.Click += (s, e) => LoadExamsGrid();
            tabExamJoin.Controls.Add(btnRefresh);

            var btnJoinByLink = CreateBlueButton("Vào bằng link/mã", new Point(432, 14), new Size(150, 32));
            btnJoinByLink.Click += (s, e) => JoinExamByLinkOrRoom();
            tabExamJoin.Controls.Add(btnJoinByLink);

            var btnOpenPracticeManager = CreateBlueButton("Tạo đề ôn", new Point(594, 14), new Size(130, 32));
            btnOpenPracticeManager.Click += (s, e) => tabRegister.SelectedTab = tabMyPracticeSets;
            tabExamJoin.Controls.Add(btnOpenPracticeManager);

            lblExamStatusHint = new Label
            {
                Text = "Bạn có thể lọc nhanh theo tên bài, phòng thi hoặc trạng thái.",
                ForeColor = C_MUTED,
                AutoSize = true,
                Location = new Point(740, 20)
            };
            tabExamJoin.Controls.Add(lblExamStatusHint);

            dgvExams = CreateGrid();
            dgvExams.Location = new Point(16, 58);
            dgvExams.Size = new Size(0, 0);
            dgvExams.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvExams.CellContentClick += DgvExams_CellContentClick;
            dgvExams.CellDoubleClick += DgvExams_CellContentClick;
            tabExamJoin.Controls.Add(dgvExams);

            studentPracticeManager = new ucManageExam(userID);
            studentPracticeManager.ConfigureForStudentPractice();
            studentPracticeManager.Dock = DockStyle.Fill;
            tabMyPracticeSets.Controls.Add(studentPracticeManager);

            pnlRegisterPage.Resize += (s, e) =>
            {
                int tabWidth = Math.Max(980, pnlRegisterPage.ClientSize.Width - 48);
                int tabHeight = Math.Max(520, pnlRegisterPage.ClientSize.Height - 102);
                tabRegister.Location = new Point(24, 86);
                tabRegister.Size = new Size(tabWidth, tabHeight);

                int hintX = Math.Max(740, tabExamJoin.ClientSize.Width - lblExamStatusHint.Width - 20);
                lblExamStatusHint.Location = new Point(hintX, 20);

                dgvExams.Location = new Point(16, 58);
                int examGridWidth = Math.Max(640, tabExamJoin.ClientSize.Width - 32);
                int examGridHeight = Math.Min(Math.Max(160, GetExamGridPreferredHeight()), Math.Max(220, tabExamJoin.ClientSize.Height - 74));
                dgvExams.Size = new Size(examGridWidth, examGridHeight);
                ApplyExamGridLayout();
            };

            pnlRegisterPage.PerformLayout();
        }

        private void BuildNotificationsPage()
        {
            pnlNotificationsPage = CreatePagePanel();

            var hdr = new Label
            {
                Text = "Thông báo",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(22, 18),
                AutoSize = true
            };
            var sub = new Label
            {
                Text = "Thông báo chung và thông báo riêng cho tài khoản của bạn.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_MUTED,
                Location = new Point(24, 50),
                AutoSize = true
            };
            pnlNotificationsPage.Controls.AddRange(new Control[] { hdr, sub });

            dgvNotifications = CreateGrid();
            dgvNotifications.Location = new Point(24, 90);
            dgvNotifications.Size = new Size(620, 470);
            dgvNotifications.SelectionChanged += DgvNotifications_SelectionChanged;
            pnlNotificationsPage.Controls.Add(dgvNotifications);

            lblNotificationEmptyState = new Label
            {
                Text = "Bạn chưa có thông báo nào.\nKhi có thông báo mới từ giáo viên hoặc hệ thống, nội dung sẽ hiển thị tại đây.",
                Font = new Font("Segoe UI", 10f),
                ForeColor = C_MUTED,
                Size = new Size(520, 56),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(74, 280),
                Visible = false
            };
            pnlNotificationsPage.Controls.Add(lblNotificationEmptyState);

            var pnlDetail = new RoundedPanel
            {
                Location = new Point(660, 90),
                Size = new Size(384, 470),
                BackColor = C_WHITE
            };
            pnlNotificationsPage.Controls.Add(pnlDetail);

            lblNotificationDetailTitle = new Label
            {
                Text = "Chi tiết thông báo",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(18, 16),
                AutoSize = true
            };
            txtNotificationDetail = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(18, 48),
                Size = new Size(348, 360),
                Font = new Font("Segoe UI", 9.25f)
            };
            UiTheme.ApplyTextBox(txtNotificationDetail);
            var btnMarkRead = CreateBlueButton("Đánh dấu đã đọc", new Point(18, 420), new Size(150, 32));
            btnMarkRead.Click += (s, e) => MarkSelectedNotificationRead();

            pnlDetail.Controls.AddRange(new Control[] { lblNotificationDetailTitle, txtNotificationDetail, btnMarkRead });
        }

        private void BuildAccountPage()
        {
            pnlAccountPage = CreatePagePanel();

            var hdr = new Label
            {
                Text = "Tài khoản",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(22, 18),
                AutoSize = true
            };
            var sub = new Label
            {
                Text = "Cập nhật thông tin cá nhân và đổi mật khẩu.",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_MUTED,
                Location = new Point(24, 50),
                AutoSize = true
            };
            pnlAccountPage.Controls.AddRange(new Control[] { hdr, sub });

            var infoCard = new RoundedPanel
            {
                Location = new Point(24, 90),
                Size = new Size(700, 680),
                BackColor = C_WHITE
            };
            pnlAccountPage.Controls.Add(infoCard);

            infoCard.Controls.Add(CreateSectionHeader("Thông tin tài khoản"));

            accountAvatarBox = new RoundPictureBox
            {
                Location = new Point(390, 70),
                Size = new Size(112, 112),
                BackColor = C_BLUE_LIGHT,
                Radius = 24
            };
            var lblAvatar = new Label { Text = "Ảnh đại diện", Location = new Point(390, 190), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            var btnChooseAvatar = CreateBlueButton("Chọn ảnh", new Point(390, 218), new Size(100, 32));
            btnChooseAvatar.Click += (s, e) => ChooseAvatarImage();
            var btnRemoveAvatar = new Button
            {
                Text = "Xóa ảnh",
                Location = new Point(500, 218),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = C_WHITE,
                ForeColor = C_MUTED,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRemoveAvatar.FlatAppearance.BorderColor = C_BORDER;
            btnRemoveAvatar.Click += (s, e) => RemoveAvatarImage();
            infoCard.Controls.AddRange(new Control[] { accountAvatarBox, lblAvatar, btnChooseAvatar, btnRemoveAvatar });

            lblAccountId = new Label
            {
                Text = "User ID: -",
                Location = new Point(24, 60),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true
            };
            lblAccountRole = new Label
            {
                Text = "Vai trò: -",
                Location = new Point(24, 86),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true
            };
            lblAccountStatus = new Label
            {
                Text = "Trạng thái: -",
                Location = new Point(24, 110),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true
            };
            lblAccountCreated = new Label
            {
                Text = "Ngày tạo: -",
                Location = new Point(24, 134),
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = true
            };

            infoCard.Controls.AddRange(new Control[] { lblAccountId, lblAccountRole, lblAccountStatus, lblAccountCreated });

            var lblName = new Label { Text = "Họ tên", Location = new Point(24, 180), AutoSize = true };
            txtAccountFullName = new TextBox { Location = new Point(24, 205), Size = new Size(320, 28), BorderStyle = BorderStyle.FixedSingle };
            UiTheme.ApplyTextBox(txtAccountFullName);

            var lblClass = new Label { Text = "Lớp", Location = new Point(24, 250), AutoSize = true };
            txtAccountClassName = new TextBox { Location = new Point(24, 275), Size = new Size(320, 28), BorderStyle = BorderStyle.FixedSingle };
            UiTheme.ApplyTextBox(txtAccountClassName);

            var lblUser = new Label { Text = "Tên đăng nhập", Location = new Point(24, 320), AutoSize = true };
            txtAccountUserName = new TextBox { Location = new Point(24, 345), Size = new Size(320, 28), BorderStyle = BorderStyle.FixedSingle, ReadOnly = true };
            UiTheme.ApplyTextBox(txtAccountUserName);

            var lblCurPass = new Label { Text = "Mật khẩu hiện tại", Location = new Point(24, 390), AutoSize = true };
            txtAccountCurrentPass = new TextBox { Location = new Point(24, 415), Size = new Size(320, 28), BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
            UiTheme.ApplyTextBox(txtAccountCurrentPass);

            var lblNewPass = new Label { Text = "Mật khẩu mới", Location = new Point(24, 460), AutoSize = true };
            txtAccountNewPass = new TextBox { Location = new Point(24, 485), Size = new Size(320, 28), BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
            UiTheme.ApplyTextBox(txtAccountNewPass);

            var lblConfirm = new Label { Text = "Xác nhận mật khẩu mới", Location = new Point(24, 530), AutoSize = true };
            txtAccountConfirmPass = new TextBox { Location = new Point(24, 555), Size = new Size(320, 28), BorderStyle = BorderStyle.FixedSingle, UseSystemPasswordChar = true };
            UiTheme.ApplyTextBox(txtAccountConfirmPass);

            var btnSave = CreateBlueButton("Lưu thay đổi", new Point(24, 610), new Size(140, 34));
            btnSave.Click += (s, e) => SaveAccountChanges();

            infoCard.Controls.AddRange(new Control[]
            {
                lblName, txtAccountFullName,
                lblClass, txtAccountClassName,
                lblUser, txtAccountUserName,
                lblCurPass, txtAccountCurrentPass,
                lblNewPass, txtAccountNewPass,
                lblConfirm, txtAccountConfirmPass,
                btnSave
            });


        }

        // =====================================================
        // HISTORY PAGE
        // =====================================================
        private void BuildHistoryPage()
        {
            pnlHistoryPage = CreatePagePanel();

            var hdr = new Label
            {
                Text = "LỊCH SỬ THI",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(28, 20),
                AutoSize = true
            };
            var sub = new Label
            {
                Text = "Kết quả của " + currentUserName,
                Font = new Font("Segoe UI", 10),
                ForeColor = C_MUTED,
                Location = new Point(30, 58),
                AutoSize = true
            };
            pnlHistoryPage.Controls.AddRange(new Control[] { hdr, sub });

            // Summary stat cards
            Panel cardTotal = BuildHistoryStatCard("Tổng lần thi", "0", "", C_BLUE, C_BLUE_LIGHT, new Point(28, 96), out Label lblHistTotal);
            Panel cardAvg = BuildHistoryStatCard("Điểm TB", "0/10", "", C_GREEN, C_GREEN_LIGHT, new Point(248, 96), out Label lblHistAvg);
            Panel cardBest = BuildHistoryStatCard("Điểm cao nhất", "0/10", "", C_ORANGE, C_ORANGE_LIGHT, new Point(468, 96), out Label lblHistBest);
            Panel cardPass = BuildHistoryStatCard("Tỷ lệ đạt", "0%", "", C_PURPLE, C_PURPLE_LIGHT, new Point(688, 96), out Label lblHistPass);

            pnlHistoryPage.Controls.AddRange(new Control[] { cardTotal, cardAvg, cardBest, cardPass });

            // Grid wrapper card
            var gridCard = new RoundedPanel
            {
                Location = new Point(28, 230),
                Size = new Size(1200, 560),
                BackColor = C_WHITE,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            pnlHistoryPage.Controls.Add(gridCard);

            var gridHdr = new Label
            {
                Text = "DANH SÁCH KẾT QUẢ",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = C_BLUE,
                Location = new Point(20, 16),
                AutoSize = true
            };
            gridCard.Controls.Add(gridHdr);

            lblHistorySummary = new Label
            {
                Text = "Mỗi dòng là một lần nộp bài. Nhấn Xem để mở chi tiết bài làm.",
                Font = new Font("Segoe UI", 9.2f),
                ForeColor = C_MUTED,
                Location = new Point(20, 38),
                AutoSize = true
            };
            gridCard.Controls.Add(lblHistorySummary);

            // Divider
            var div = new Panel
            {
                Location = new Point(0, 66),
                Size = new Size(1200, 1),
                BackColor = C_BORDER
            };
            gridCard.Controls.Add(div);

            dgvHistory = new DataGridView
            {
                Location = new Point(0, 68),
                Size = new Size(1200, 480),
                BackgroundColor = C_WHITE,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 42 },
                RowHeadersVisible = false,
                ColumnHeadersHeight = 44,
                EnableHeadersVisualStyles = false,
                GridColor = C_BORDER,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(100, 110, 130);
            dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgvHistory.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f);
            dgvHistory.DefaultCellStyle.SelectionBackColor = C_BLUE_LIGHT;
            dgvHistory.DefaultCellStyle.SelectionForeColor = C_TEXT;
            dgvHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 253);
            dgvHistory.CellPainting += DgvHistory_CellPainting;
            dgvHistory.CellClick += DgvHistory_CellClick;
            gridCard.Controls.Add(dgvHistory);

            lblHistoryEmptyState = new Label
            {
                Text = "Bạn chưa có bài thi nào trong lịch sử.\nKhi hoàn thành bài đầu tiên, kết quả sẽ xuất hiện tại đây.",
                Font = new Font("Segoe UI", 11f),
                ForeColor = C_MUTED,
                Size = new Size(500, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            gridCard.Controls.Add(lblHistoryEmptyState);

            // Bind stat labels for refresh
            pnlHistoryPage.Tag = new Label[] { lblHistTotal, lblHistAvg, lblHistBest, lblHistPass };

            // Resize grid card when host resizes
            pnlHistoryPage.Resize += (sender2, args2) =>
            {
                int pw = pnlHistoryPage.ClientSize.Width;
                int ph = pnlHistoryPage.ClientSize.Height;
                gridCard.Width = Math.Max(600, pw - 56);
                gridCard.Height = Math.Max(300, ph - 250);
                dgvHistory.Size = new Size(gridCard.ClientSize.Width, gridCard.ClientSize.Height - 70);
                div.Width = gridCard.ClientSize.Width;
                lblHistoryEmptyState.Location = new Point((gridCard.ClientSize.Width - lblHistoryEmptyState.Width) / 2, Math.Max(110, (gridCard.ClientSize.Height - lblHistoryEmptyState.Height) / 2));
            };
        }

        private Panel BuildHistoryStatCard(string title, string value, string icon, Color accent, Color accentLight, Point loc, out Label valueLabel)
        {
            var card = new RoundedPanel { Location = loc, Size = new Size(210, 100), BackColor = C_WHITE };
            var badge = new RoundedPanel { Location = new Point(14, 14), Size = new Size(38, 38), BackColor = accentLight };
            var lblIcon = new Label { Text = icon, Font = new Font("Segoe UI", 16), ForeColor = accent, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            badge.Controls.Add(lblIcon);
            var lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 8.5f), ForeColor = C_MUTED, Location = new Point(62, 14), AutoSize = true };
            valueLabel = new Label { Text = value, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = C_TEXT, Location = new Point(62, 36), AutoSize = true };
            var divider = new Panel { Location = new Point(0, 98), Size = new Size(210, 1), BackColor = C_BORDER };
            card.Controls.AddRange(new Control[] { badge, lblTitle, valueLabel, divider });
            return card;
        }

        private void DgvHistory_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (dgvHistory.Columns[e.ColumnIndex].Name == "colHistLevel" && e.Value != null)
            {
                e.PaintBackground(e.ClipBounds, true);
                string val = e.Value.ToString();
                Color bg = Color.LightGray, fg = Color.Gray;
                if (val == "Xuất sắc") { bg = Color.FromArgb(210, 250, 230); fg = Color.FromArgb(22, 163, 74); }
                else if (val == "Tốt") { bg = Color.FromArgb(220, 235, 255); fg = Color.FromArgb(37, 99, 235); }
                else if (val == "Trung bình") { bg = Color.FromArgb(255, 243, 205); fg = Color.FromArgb(217, 119, 6); }
                else if (val == "Yếu") { bg = Color.FromArgb(255, 220, 220); fg = Color.FromArgb(220, 38, 38); }
                var r = new Rectangle(e.CellBounds.X + 8, e.CellBounds.Y + 7, e.CellBounds.Width - 16, e.CellBounds.Height - 14);
                using (var b = new SolidBrush(bg)) e.Graphics.FillRectangle(b, r);
                var sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
                using (var tb = new SolidBrush(fg)) e.Graphics.DrawString(val, new Font("Segoe UI", 8.5f, FontStyle.Bold), tb, r, sf);
                e.Handled = true;
            }
        }

        private void LoadHistoryGrid()
        {
            if (dgvHistory == null) return;

            DataTable dt = SafeQuery(
                $"SELECT r.ResultID, e.ExamName, r.Score, r.SubmitDate, ISNULL(e.ExamMode, 'Exam') AS ExamMode " +
                $"FROM Results r " +
                $"LEFT JOIN Exams e ON r.ExamID = e.ExamID " +
                $"WHERE r.UserID = {userID} " +
                $"ORDER BY r.SubmitDate DESC, r.ResultID DESC");

            dgvHistory.Columns.Clear();
            dgvHistory.Rows.Clear();

            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHistId", HeaderText = "ID", ReadOnly = true, Width = 80, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHistExam", HeaderText = "Bài thi", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHistScore", HeaderText = "Điểm", ReadOnly = true, Width = 100, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHistDate", HeaderText = "Ngày nộp", ReadOnly = true, Width = 180, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvHistory.Columns.Add(new DataGridViewTextBoxColumn { Name = "colHistLevel", HeaderText = "Xếp loại", ReadOnly = true, Width = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvHistory.Columns.Add(new DataGridViewButtonColumn { Name = "colHistDetail", HeaderText = "Chi tiết", Text = "Xem", UseColumnTextForButtonValue = true, Width = 90, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, FlatStyle = FlatStyle.Flat });

            double sum = 0, best = 0;
            int count = 0, pass = 0;

            foreach (DataRow row in dt.Rows)
            {
                int id = SafeToInt(GetAnyField(row, "ResultID", "Id", "ID"));
                string examName = GetAnyField(row, "ExamName", "TenBaiThi", "Title", "Name");
                if (string.IsNullOrWhiteSpace(examName)) examName = "Bài thi";
                double score = SafeToDouble(GetAnyField(row, "Score", "Diem"));
                string examMode = GetAnyField(row, "ExamMode");
                if (string.Equals(examMode, "Practice", StringComparison.OrdinalIgnoreCase))
                {
                    var review = ExamAttemptReviewService.LoadByResultId(id);
                    if (review != null && review.TotalQuestions > 0)
                        score = review.Score;
                }
                DateTime submitDate = GetDateField(row, "SubmitDate", "NgayNop", "CreatedAt");
                string level = GetLevel(score);

                dgvHistory.Rows.Add(id, examName, score.ToString("0.##") + "/10", submitDate.ToString("dd/MM/yyyy HH:mm"), level);
                sum += score;
                if (score > best) best = score;
                if (score >= 5) pass++;
                count++;
            }

            bool hasHistory = count > 0;
            dgvHistory.Visible = hasHistory;
            if (lblHistoryEmptyState != null)
            {
                lblHistoryEmptyState.Visible = !hasHistory;
                if (lblHistoryEmptyState.Parent != null)
                {
                    lblHistoryEmptyState.Location = new Point((lblHistoryEmptyState.Parent.ClientSize.Width - lblHistoryEmptyState.Width) / 2, Math.Max(110, (lblHistoryEmptyState.Parent.ClientSize.Height - lblHistoryEmptyState.Height) / 2));
                }
            }

            if (lblHistorySummary != null)
            {
                if (!hasHistory)
                {
                    lblHistorySummary.Text = "Lịch sử thi sẽ tự xuất hiện sau khi bạn nộp bài thành công.";
                }
                else
                {
                    string latestExam = dgvHistory.Rows.Count > 0 ? Convert.ToString(dgvHistory.Rows[0].Cells["colHistExam"].Value) : "bài thi gần nhất";
                    lblHistorySummary.Text = "Bạn đã có " + count + " lần nộp bài. Lần gần nhất là \"" + latestExam + "\". Nhấn Xem để mở chi tiết từng bài làm.";
                }
            }

            // Update stat labels stored in Tag
            if (pnlHistoryPage?.Tag is Label[] statLabels && statLabels.Length == 4)
            {
                statLabels[0].Text = count.ToString("N0");
                statLabels[1].Text = count > 0 ? (sum / count).ToString("0.##") + "/10" : "0/10";
                statLabels[2].Text = count > 0 ? best.ToString("0.##") + "/10" : "0/10";
                statLabels[3].Text = count > 0 ? ((pass * 100) / count) + "%" : "0%";
            }
        }

        private void DgvHistory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || dgvHistory.Columns[e.ColumnIndex].Name != "colHistDetail")
                return;

            int resultId = SafeToInt(dgvHistory.Rows[e.RowIndex].Cells["colHistId"].Value);
            if (resultId <= 0) return;

            var review = ExamAttemptReviewService.LoadByResultId(resultId);
            if (review == null || review.TotalQuestions <= 0 || review.ReviewRows == null || review.ReviewRows.Count == 0)
            {
                MessageBox.Show("Chưa có dữ liệu chi tiết cho bài làm này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
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

        // =====================================================
        // LOAD DATA
        // =====================================================
        private void LoadAllData()
        {
            Database.EnsureExamAccessColumns();
            Database.EnsureAdvancedExamColumns();
            LoadDashboardData();
            LoadExamsGrid();
            LoadNotificationsGrid();
            LoadAccountData();
        }

        private void LoadDashboardData()
        {
            int totalDone = SafeCount($"SELECT COUNT(*) FROM Results WHERE UserID={userID}");
            double avgScore = GetDouble($"SELECT AVG(Score) FROM Results WHERE UserID={userID}");
            double maxScore = GetDouble($"SELECT MAX(Score) FROM Results WHERE UserID={userID}");

            DataTable dtRank = SafeQuery(
                "SELECT COUNT(*) AS TotalStudents FROM Users WHERE RoleID=3;" +
                "SELECT UserID, AVG(Score) AS AvgScore FROM Results GROUP BY UserID");

            int totalStudents = SafeCount("SELECT COUNT(*) FROM Users WHERE RoleID=3");
            int betterStudents = 0;

            try
            {
                var dt = SafeQuery($"SELECT UserID, AVG(Score) AS AvgScore FROM Results GROUP BY UserID");
                if (dt.Rows.Count > 0 && avgScore > 0)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        double a = SafeToDouble(r["AvgScore"]);
                        if (a > avgScore) betterStudents++;
                    }
                }
            }
            catch
            {
                betterStudents = 0;
            }

            int rankPercent = totalStudents > 0 ? Math.Max(1, (int)Math.Round((betterStudents + 1) * 100.0 / totalStudents)) : 0;

            lblDashboardExamCount.Text = totalDone.ToString("N0");
            lblDashboardAverageScore.Text = avgScore <= 0 ? "0/10" : avgScore.ToString("0.##") + "/10";
            lblDashboardBestScore.Text = maxScore <= 0 ? "0/10" : maxScore.ToString("0.##") + "/10";
            lblDashboardRank.Text = totalDone > 0 ? "Top " + rankPercent + "%" : "Chưa có";

            LoadDashboardSchedule();
            LoadDashboardRecentResults();

            // C?p nh?t bi?u d? donut v?i d? li?u th?c t? DB
            if (pnlDashboardChartCard != null)
            {
                foreach (Control c in pnlDashboardChartCard.Controls)
                {
                    if (c is DonutChartPanel donut)
                    {
                        int passCount = SafeCount($"SELECT COUNT(*) FROM Results WHERE UserID={userID} AND Score >= 5");
                        donut.TotalExams = totalDone;
                        donut.PassRate = totalDone > 0 ? (float)passCount * 100f / totalDone : 0f;
                        donut.Invalidate();
                        break;
                    }
                }
            }
        }

        private void LoadDashboardSchedule()
        {
            pnlDashboardScheduleCard.Controls.Clear();
            pnlDashboardScheduleCard.Controls.Add(CreateSectionHeader("Lịch thi sắp tới"));
            pnlDashboardScheduleCard.Controls.Add(CreateSectionDivider(520));

            DataTable dt = LoadExamTable();
            int y = 60;
            int added = 0;
            foreach (DataRow row in dt.Rows)
            {
                if (added >= 4) break;

                int examId = GetIntField(row, "ExamID", "Id", "ID");
                string examName = GetStringField(row, "ExamName", "TenBaiThi", "Title", "Name");
            string room = GetStringField(row, "RoomCode", "ExamLink", "Room", "Phong", "ClassRoom");
                string status = GetStringField(row, "Status", "TrangThai");
                string timeText = GetExamTimeText(row);
                string day = GetExamDay(row);
                string mon = GetExamMon(row);
                bool active = IsExamActive(row);

                pnlDashboardScheduleCard.Controls.Add(CreateScheduleRow(day, mon, examName, timeText, room, string.IsNullOrWhiteSpace(status) ? (active ? "Sắp diễn ra" : "Sắp thi") : status, active, y, examId));
                y += 58;
                added++;
            }

            if (added == 0)
            {
                pnlDashboardScheduleCard.Controls.Add(new Label
                {
                    Text = "Chưa có lịch thi nào.",
                    ForeColor = C_MUTED,
                    Location = new Point(18, 90),
                    AutoSize = true
                });
            }

            lnkSeeAllSchedule.Location = new Point(18, 270);
            pnlDashboardScheduleCard.Controls.Add(lnkSeeAllSchedule);
        }

        private void LoadDashboardRecentResults()
        {
            pnlDashboardRecentResultCard.Controls.Clear();
            pnlDashboardRecentResultCard.Controls.Add(CreateSectionHeader("Kết quả gần đây"));
            pnlDashboardRecentResultCard.Controls.Add(CreateSectionDivider(520));

            DataTable resultItems = SafeQuery(
                $"SELECT TOP 5 r.ResultID, r.Score, r.SubmitDate, e.ExamName, ISNULL(e.ExamMode, 'Exam') AS ExamMode " +
                $"FROM Results r " +
                $"LEFT JOIN Exams e ON r.ExamID = e.ExamID " +
                $"WHERE r.UserID={userID} " +
                $"ORDER BY r.SubmitDate DESC, r.ResultID DESC");

            int ry = 60;
            if (resultItems.Rows.Count == 0)
            {
                pnlDashboardRecentResultCard.Controls.Add(new Label
                {
                    Text = "Tài khoản này chưa có lịch sử làm bài.",
                    Font = new Font("Segoe UI", 9.5f),
                    ForeColor = C_MUTED,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Location = new Point(14, 120),
                    Size = new Size(492, 32)
                });
                return;
            }

            foreach (DataRow item in resultItems.Rows)
            {
                double score = SafeToDouble(GetAnyField(item, "Score", "Diem"));
                string examMode = GetAnyField(item, "ExamMode");
                if (string.Equals(examMode, "Practice", StringComparison.OrdinalIgnoreCase))
                {
                    int resultId = SafeToInt(GetAnyField(item, "ResultID", "Id", "ID"));
                    var review = ExamAttemptReviewService.LoadByResultId(resultId);
                    if (review != null && review.TotalQuestions > 0)
                        score = review.Score;
                }
                string level = GetLevel(score);
                Color levelColor = GetLevelColor(score);
                DateTime submitDate = GetDateField(item, "SubmitDate", "NgayNop", "CreatedAt");
                string examName = GetAnyField(item, "ExamName", "TenBaiThi", "Title", "Name");
                if (string.IsNullOrWhiteSpace(examName))
                    examName = "Bài thi";

                pnlDashboardRecentResultCard.Controls.Add(CreateResultRow("", examName, submitDate.ToString("dd/MM/yyyy HH:mm"), score.ToString("0.##") + "/10", level, levelColor, ry));
                ry += 48;
            }
        }

        private void LoadExamsGrid()
        {
            DataTable dt = LoadExamTable();
            dgvExams.Columns.Clear();
            dgvExams.Rows.Clear();

            dgvExams.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvExams.Columns.Add(new DataGridViewTextBoxColumn { Name = "colExamId", Visible = false });
            dgvExams.Columns.Add(new DataGridViewTextBoxColumn { Name = "colExamName", HeaderText = "Bài thi", ReadOnly = true, MinimumWidth = 260, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvExams.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMode", HeaderText = "Loại", ReadOnly = true, MinimumWidth = 90, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvExams.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = "Thời gian", ReadOnly = true, MinimumWidth = 130, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvExams.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRoom", HeaderText = "Phòng", ReadOnly = true, MinimumWidth = 110, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvExams.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Trạng thái", ReadOnly = true, MinimumWidth = 150, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
            dgvExams.Columns.Add(new DataGridViewButtonColumn { Name = "colStart", HeaderText = "Vào bài", Text = "Vào bài", UseColumnTextForButtonValue = true, MinimumWidth = 120, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, FlatStyle = FlatStyle.Flat });
            ApplyExamGridLayout();

            string keyword = (txtExamSearch?.Text ?? "").Trim().ToLowerInvariant();

            foreach (DataRow row in dt.Rows)
            {
                int examId = GetIntField(row, "ExamID", "Id", "ID");
                string examName = GetStringField(row, "ExamName", "TenBaiThi", "Title", "Name");
                string room = GetStringField(row, "Room", "Phong", "ClassRoom");
                string status = GetExamAccessStatus(row);
                string modeText = GetExamModeText(row);
                string timeText = GetExamTimeText(row);

                if (!string.IsNullOrWhiteSpace(keyword) &&
                    !(examName ?? "").ToLowerInvariant().Contains(keyword) &&
                    !(room ?? "").ToLowerInvariant().Contains(keyword) &&
                    !(status ?? "").ToLowerInvariant().Contains(keyword))
                {
                    continue;
                }

                int idx = dgvExams.Rows.Add(examId, examName, modeText, timeText, room, status, "Vào bài");
                dgvExams.Rows[idx].Tag = row;
            }

            int readyCount = 0;
            foreach (DataGridViewRow row in dgvExams.Rows)
            {
                string status = SafeToString(row.Cells["colStatus"].Value);
                if (status == "Sẵn sàng" || status == "Cần mật khẩu")
                    readyCount++;
            }
            lblExamStatusHint.Text = $"Tìm thấy {dgvExams.Rows.Count} bài thi. Có {readyCount} bài bạn có thể vào làm ngay.";
            if (tabExamJoin != null)
            {
                int examGridWidth = Math.Max(640, tabExamJoin.ClientSize.Width - 32);
                int examGridHeight = Math.Min(Math.Max(160, GetExamGridPreferredHeight()), Math.Max(220, tabExamJoin.ClientSize.Height - 74));
                dgvExams.Size = new Size(examGridWidth, examGridHeight);
                ApplyExamGridLayout();
            }
        }

        private void ApplyExamGridLayout()
        {
            if (dgvExams == null || dgvExams.Columns.Count == 0)
                return;

            int modeWidth = 90;
            int timeWidth = 130;
            int roomWidth = 110;
            int statusWidth = 150;
            int startWidth = 120;
            int fixedWidth = modeWidth + timeWidth + roomWidth + statusWidth + startWidth;
            int availableWidth = Math.Max(260, dgvExams.ClientSize.Width - fixedWidth - 4);

            if (dgvExams.Columns.Contains("colExamName"))
                dgvExams.Columns["colExamName"].Width = availableWidth;
            if (dgvExams.Columns.Contains("colMode"))
                dgvExams.Columns["colMode"].Width = modeWidth;
            if (dgvExams.Columns.Contains("colTime"))
                dgvExams.Columns["colTime"].Width = timeWidth;
            if (dgvExams.Columns.Contains("colRoom"))
                dgvExams.Columns["colRoom"].Width = roomWidth;
            if (dgvExams.Columns.Contains("colStatus"))
                dgvExams.Columns["colStatus"].Width = statusWidth;
            if (dgvExams.Columns.Contains("colStart"))
                dgvExams.Columns["colStart"].Width = startWidth;
        }

        private int GetExamGridPreferredHeight()
        {
            if (dgvExams == null)
                return 220;

            int headerHeight = dgvExams.ColumnHeadersVisible ? dgvExams.ColumnHeadersHeight : 0;
            int rowHeight = dgvExams.RowTemplate != null && dgvExams.RowTemplate.Height > 0 ? dgvExams.RowTemplate.Height : 32;
            int rowCount = Math.Max(1, dgvExams.Rows.Count);
            int contentHeight = headerHeight + (rowCount * rowHeight) + 6;
            return Math.Max(160, Math.Min(320, contentHeight));
        }

        private void LoadNotificationsGrid()
        {
            DataTable dt = LoadNotificationsTable();
            dgvNotifications.Columns.Clear();
            dgvNotifications.Rows.Clear();

            dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", Visible = false });
            dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTitle", HeaderText = "Tiêu đề", ReadOnly = true, Width = 180 });
            dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMessage", HeaderText = "Nội dung", ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "Ngày", ReadOnly = true, Width = 120 });
            dgvNotifications.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRead", HeaderText = "Trạng thái", ReadOnly = true, Width = 100 });

            foreach (DataRow row in dt.Rows)
            {
                int id = GetIntField(row, "NotificationID", "ThongBaoID", "Id", "ID");
                string title = GetAnyField(row, "Title", "TieuDe", "Name");
                string message = GetAnyField(row, "Message", "NoiDung", "Content");
                DateTime date = GetDateField(row, "CreatedAt", "NgayTao", "SendDate", "Date");
                bool isRead = GetBoolField(row, "IsRead", "DaDoc", "Read");

                dgvNotifications.Rows.Add(id, title, message, date.ToString("dd/MM/yyyy HH:mm"), isRead ? "Đã đọc" : "Mới");
            }

            bool hasNotifications = dgvNotifications.Rows.Count > 0;
            dgvNotifications.Visible = hasNotifications;
            if (lblNotificationEmptyState != null)
                lblNotificationEmptyState.Visible = !hasNotifications;

            if (hasNotifications)
                dgvNotifications.Rows[0].Selected = true;
            else
            {
                if (txtNotificationDetail != null)
                    txtNotificationDetail.Text = "Chưa có thông báo để hiển thị.";
            }
        }

        private void LoadAccountData()
        {
            Database.EnsureStudentAssessmentColumns();
            DataTable dt = LoadUserTable();
            if (dt.Rows.Count == 0)
            {
                _currentUserRow = null;
                return;
            }

            _currentUserRow = dt.Rows[0];
            lblAccountId.Text = "User ID: " + userID;
            lblAccountRole.Text = "Vai trò: " + GetAnyField(_currentUserRow, "RoleName", "VaiTro", "Role");
            lblAccountStatus.Text = "Trạng thái: " + GetAnyField(_currentUserRow, "Status", "TrangThai");
            lblAccountCreated.Text = "Ngày tạo: " + GetDateField(_currentUserRow, "CreatedAt", "NgayTao", "CreateDate").ToString("dd/MM/yyyy HH:mm");

            txtAccountFullName.Text = GetAnyField(_currentUserRow, "FullName", "HoTen", "DisplayName", "Ten");
            currentStudentClassName = GetAnyField(_currentUserRow, "ClassName", "Class", "Lop", "TenLop");
            if (txtAccountClassName != null) txtAccountClassName.Text = currentStudentClassName;
            txtAccountUserName.Text = GetAnyField(_currentUserRow, "UserName", "Username", "TenDangNhap");
            currentAvatarPath = GetAnyField(_currentUserRow, "AvatarPath");
            RefreshAvatarViews();
            txtAccountCurrentPass.Text = "";
            txtAccountNewPass.Text = "";
            txtAccountConfirmPass.Text = "";
        }

        // =====================================================
        // GRID EVENTS
        // =====================================================
        private void DgvExams_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (e.ColumnIndex >= 0 && dgvExams.Columns[e.ColumnIndex].Name != "colStart")
                return;

            var row = dgvExams.Rows[e.RowIndex];
            int examId = SafeToInt(row.Cells["colExamId"].Value);
            string examName = SafeToString(row.Cells["colExamName"].Value);

            StartExam(examId, examName);
        }

        private void DgvNotifications_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvNotifications.SelectedRows.Count == 0) return;
            var r = dgvNotifications.SelectedRows[0];
            if (r == null || r.Index < 0) return;

            string title = SafeToString(r.Cells["colTitle"].Value);
            string message = SafeToString(r.Cells["colMessage"].Value);
            string date = SafeToString(r.Cells["colDate"].Value);
            string read = SafeToString(r.Cells["colRead"].Value);

            txtNotificationDetail.Text =
                "Tiêu đề: " + title + Environment.NewLine +
                "Ngày: " + date + Environment.NewLine +
                "Trạng thái: " + read + Environment.NewLine + Environment.NewLine +
                message;
        }

        // =====================================================
        // ACTIONS
        // =====================================================


        private void StartExam(int examId, string examName)
        {
            if (examId <= 0)
            {
                MessageBox.Show("Không xác định được bài thi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataTable examInfo = SafeQuery($"SELECT TOP 1 * FROM Exams WHERE ExamID = {examId}");
            if (examInfo.Rows.Count == 0)
            {
                MessageBox.Show("Không tìm thấy bài thi.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DataRow examRow = examInfo.Rows[0];
            if (!CanOpenExam(examRow, out string reason))
            {
                MessageBox.Show(reason, "Chưa thể làm bài", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string password = GetAnyField(examRow, "ExamPassword");
            if (!string.IsNullOrWhiteSpace(password) && !PromptExamPassword(password))
            {
                MessageBox.Show("Mật khẩu bộ đề không đúng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Kiểm tra đề thi có câu hỏi không trước khi mở
            try
            {
                int questionCount = 0;
                DataTable chkQ = SafeQuery(
                    $"SELECT COUNT(*) FROM ExamQuestions WHERE ExamID = {examId}");
                if (chkQ.Rows.Count > 0 && chkQ.Rows[0][0] != DBNull.Value)
                    questionCount = SafeToInt(chkQ.Rows[0][0]);

                if (questionCount == 0)
                {
                    MessageBox.Show("Đề thi chưa có câu hỏi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            catch { }

            try
            {
                bool requireClassBeforeStart = GetBoolField(examRow, true, "RequireClassBeforeStart");
                bool allowStudentEditClass = GetBoolField(examRow, true, "AllowStudentEditClass");
                using (var identityPrompt = new StudentExamIdentityPrompt(
                    examName,
                    txtAccountFullName != null ? txtAccountFullName.Text : currentUserName,
                    txtAccountClassName != null ? txtAccountClassName.Text : currentStudentClassName,
                    requireClassBeforeStart,
                    allowStudentEditClass))
                {
                    if (identityPrompt.ShowDialog(this) != DialogResult.OK) return;

                    SaveStudentClassProfile(identityPrompt.StudentFullName, identityPrompt.StudentClassName);

                    var examForm = new frmExam(examId, userID, identityPrompt.StudentFullName, identityPrompt.StudentClassName);
                    examForm.StartPosition = FormStartPosition.CenterParent;
                    examForm.ShowDialog(this);
                }

                // Sau khi nộp bài: chuyển sang trang Lịch sử thi và làm mới dữ liệu
                LoadAllData();
                ShowPage(PageKind.History);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể mở bài thi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FrmStudent_Shown(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(pendingExamLink)) return;
            OpenExamFromSharedLink(pendingExamLink, true);
        }

        private void JoinExamByLinkOrRoom()
        {
            Database.EnsureExamAccessColumns();
            using (var dlg = new Form())
            {
                dlg.Text = "Vào bài bằng link hoặc mã phòng";
                dlg.Size = new Size(460, 170);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = UiTheme.PageBg;
                dlg.Font = Font;

                var lbl = new Label { Text = "Dán link làm bài hoặc nhập mã phòng giáo viên đã gửi:", Location = new Point(18, 18), AutoSize = true };
                var txt = new TextBox { Location = new Point(18, 46), Width = 400 };
                var btnOk = new Button { Text = "Vào bài", DialogResult = DialogResult.OK, Location = new Point(250, 84), Size = new Size(80, 30) };
                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(340, 84), Size = new Size(80, 30) };
                UiTheme.ApplyTextBox(txt);
                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);
                dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                OpenExamFromSharedLink(txt.Text, false);
            }
        }

        private void OpenExamFromSharedLink(string input, bool fromProtocol)
        {
            string raw = (input ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw)) return;

            string room = SqlEscape(ExamShareLinkService.ExtractRoomCode(raw));
            string link = SqlEscape(raw);
            DataTable dt = SafeQuery(
                "SELECT TOP 1 ExamID, ExamName FROM Exams WHERE RoomCode=N'" + room + "' OR ExamLink=N'" + link + "' OR ExamLink=N'" + SqlEscape(ExamShareLinkService.BuildLegacyLink(room)) + "'");
            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("Không tìm thấy bài thi theo link hoặc mã phòng này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ShowPage(PageKind.RegisterExam);
            StartExam(SafeToInt(dt.Rows[0]["ExamID"]), SafeToString(dt.Rows[0]["ExamName"]));
        }

        private void MarkSelectedNotificationRead()
        {
            if (dgvNotifications.SelectedRows.Count == 0)
            {
                MessageBox.Show("Hãy chọn một thông báo.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var row = dgvNotifications.SelectedRows[0];
            int id = SafeToInt(row.Cells["colId"].Value);
            if (id <= 0)
            {
                MessageBox.Show("Không xác định được thông báo.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string sql = $"UPDATE Notifications SET IsRead = 1 WHERE NotificationID = {id}";
                if (!ExecNonQuery(sql))
                {
                    MessageBox.Show("Không cập nhật được thông báo. Kiểm tra lại tên bảng/cột trong CSDL.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LoadNotificationsGrid();
                MessageBox.Show("Đã đánh dấu là đã đọc.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể cập nhật thông báo: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAccountChanges()
        {
            if (_currentUserRow == null)
            {
                MessageBox.Show("Không tải được thông tin tài khoản.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string fullName = (txtAccountFullName.Text ?? "").Trim();
            string className = (txtAccountClassName.Text ?? "").Trim();
            string currentPass = txtAccountCurrentPass.Text ?? "";
            string newPass = txtAccountNewPass.Text ?? "";
            string confirmPass = txtAccountConfirmPass.Text ?? "";

            string userTableFullNameCol = FindExistingColumn(_currentUserRow, "FullName", "HoTen", "DisplayName", "Ten");
            string userTableClassCol = EnsureStudentClassColumn();
            string userTablePassCol = FindExistingColumn(_currentUserRow, "Password", "MatKhau", "PassWord", "Pwd");
            string userTableAvatarCol = EnsureStudentAvatarColumn();
            string userTableIdCol = FindExistingColumn(_currentUserRow, "UserID", "Id", "ID");

            if (string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show("Họ tên không được để trống.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(newPass) || !string.IsNullOrWhiteSpace(confirmPass) || !string.IsNullOrWhiteSpace(currentPass))
            {
                if (string.IsNullOrWhiteSpace(currentPass))
                {
                    MessageBox.Show("Vui lòng nhập mật khẩu hiện tại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(newPass))
                {
                    MessageBox.Show("Vui lòng nhập mật khẩu mới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (newPass != confirmPass)
                {
                    MessageBox.Show("Xác nhận mật khẩu không khớp.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!string.IsNullOrEmpty(userTablePassCol))
                {
                    string currentStoredPass = SafeToString(_currentUserRow[userTablePassCol]);
                    if (!string.Equals(currentStoredPass, currentPass, StringComparison.Ordinal))
                    {
                        MessageBox.Show("Mật khẩu hiện tại không đúng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            try
            {
                bool anyUpdated = false;

                if (!string.IsNullOrEmpty(userTableFullNameCol))
                {
                    string sqlUpdateName = $"UPDATE Users SET {userTableFullNameCol} = N'{SqlEscape(fullName)}' WHERE {userTableIdCol} = {userID}";
                    anyUpdated |= ExecNonQuery(sqlUpdateName);
                }

                if (!string.IsNullOrEmpty(userTableClassCol))
                {
                    string sqlUpdateClass = $"UPDATE Users SET {userTableClassCol} = N'{SqlEscape(className)}' WHERE {userTableIdCol} = {userID}";
                    anyUpdated |= ExecNonQuery(sqlUpdateClass);
                }

                if (!string.IsNullOrEmpty(userTableAvatarCol))
                {
                    string sqlUpdateAvatar = $"UPDATE Users SET {userTableAvatarCol} = N'{SqlEscape(currentAvatarPath)}' WHERE {userTableIdCol} = {userID}";
                    anyUpdated |= ExecNonQuery(sqlUpdateAvatar);
                }

                if (!string.IsNullOrWhiteSpace(newPass) && !string.IsNullOrEmpty(userTablePassCol))
                {
                    string sqlUpdatePass = $"UPDATE Users SET {userTablePassCol} = N'{SqlEscape(newPass)}' WHERE {userTableIdCol} = {userID}";
                    anyUpdated |= ExecNonQuery(sqlUpdatePass);
                }

                if (!anyUpdated)
                {
                    MessageBox.Show("Không thể cập nhật tài khoản. Hãy kiểm tra lại tên bảng/cột trong CSDL.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show("Đã cập nhật tài khoản thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                currentUserName = fullName;
                currentStudentClassName = className;
                UpdateSidebarUserName(fullName);
                LoadAccountData();
                LoadDashboardData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu thay đổi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string EnsureStudentAvatarColumn()
        {
            Database.EnsureStudentAssessmentColumns();
            DataTable refreshed = LoadUserTable();
            if (refreshed.Rows.Count > 0) _currentUserRow = refreshed.Rows[0];
            return FindExistingColumn(_currentUserRow, "AvatarPath");
        }

        private void ChooseAvatarImage()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn ảnh đại diện";
                ofd.Filter = "Tất cả định dạng ảnh|*.bmp;*.gif;*.jpg;*.jpeg;*.jfif;*.png;*.tif;*.tiff;*.ico;*.emf;*.wmf;*.webp;*.heic;*.heif;*.avif|Tất cả file (*.*)|*.*";
                ofd.FilterIndex = 1;
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                if (!CanLoadAvatarImage(ofd.FileName, out string error))
                {
                    MessageBox.Show(error, "Không đọc được ảnh", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                currentAvatarPath = ofd.FileName;
                RefreshAvatarViews();
            }
        }

        private bool CanLoadAvatarImage(string path, out string error)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
            {
                error = "File ảnh không tồn tại hoặc đã bị di chuyển.";
                return false;
            }

            try
            {
                using (var img = RoundPictureBox.LoadAvatarImage(path))
                {
                    if (img != null && img.Width > 0 && img.Height > 0) return true;
                }
                error = "File này không đọc được như ảnh hợp lệ.";
                return false;
            }
            catch (Exception ex)
            {
                error = "Không đọc được ảnh này. File có thể bị hỏng hoặc dùng codec mà Windows chưa hỗ trợ.\n\nChi tiết: " + ex.Message;
                return false;
            }
        }
        private void RemoveAvatarImage()
        {
            currentAvatarPath = "";
            RefreshAvatarViews();
        }

        private void RefreshAvatarViews()
        {
            string initials = BuildInitials(string.IsNullOrWhiteSpace(currentUserName) ? txtAccountFullName?.Text : currentUserName);
            ApplyAvatar(sidebarAvatarBox, currentAvatarPath, initials);
            ApplyAvatar(accountAvatarBox, currentAvatarPath, initials);
        }

        private void ApplyAvatar(RoundPictureBox box, string imagePath, string initials)
        {
            if (box == null) return;
            box.ImagePath = imagePath;
            box.Text = initials;
            box.Invalidate();
        }

        private string BuildInitials(string name)
        {
            string value = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return "?";
            string[] parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }
        private string EnsureStudentClassColumn()
        {
            Database.EnsureStudentAssessmentColumns();
            DataTable refreshed = LoadUserTable();
            if (refreshed.Rows.Count > 0) _currentUserRow = refreshed.Rows[0];
            return FindExistingColumn(_currentUserRow, "ClassName", "Class", "Lop", "TenLop");
        }

        private void SaveStudentClassProfile(string fullName, string className)
        {
            try
            {
                Database.EnsureStudentAssessmentColumns();
                string userTableIdCol = _currentUserRow == null ? "UserID" : FindExistingColumn(_currentUserRow, "UserID", "Id", "ID");
                string nameCol = _currentUserRow == null ? "FullName" : FindExistingColumn(_currentUserRow, "FullName", "HoTen", "DisplayName", "Ten");
                string classCol = EnsureStudentClassColumn();
                if (string.IsNullOrEmpty(userTableIdCol) || string.IsNullOrEmpty(nameCol) || string.IsNullOrEmpty(classCol)) return;

                string classUpdate = string.IsNullOrWhiteSpace(className) ? "" : $", {classCol}=N'{SqlEscape(className)}'";
                ExecNonQuery($"UPDATE Users SET {nameCol}=N'{SqlEscape(fullName)}'{classUpdate} WHERE {userTableIdCol}={userID}");
                if (!string.IsNullOrWhiteSpace(className)) currentStudentClassName = className;
            }
            catch
            {
            }
        }

        // =====================================================
        // HISTORY / LOGOUT
        // =====================================================
        private void BtnHistory_Click(object sender, EventArgs e)
        {
            ShowPage(PageKind.History);
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Bạn có muốn đăng xuất?", "Thông báo",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var f = new frmLogin();
                    f.Show();
                    Close();
                }
                catch
                {
                    Hide();
                }
            }
        }

        // =====================================================
        // HELPERS: PAGE
        // =====================================================
        private Panel CreatePagePanel()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG,
                Visible = false
            };
        }

        private void ShowPage(PageKind kind)
        {
            pnlDashboardPage.Visible = kind == PageKind.Dashboard;
            pnlHistoryPage.Visible = kind == PageKind.History;
            pnlRegisterPage.Visible = kind == PageKind.RegisterExam;
            pnlNotificationsPage.Visible = kind == PageKind.Notifications;
            pnlAccountPage.Visible = kind == PageKind.Account;

            SetNavActive(btnHome, kind == PageKind.Dashboard);
            SetNavActive(btnHistory, kind == PageKind.History);
            SetNavActive(btnRegisterExam, kind == PageKind.RegisterExam);
            SetNavActive(btnNotifications, kind == PageKind.Notifications);
            SetNavActive(btnAccount, kind == PageKind.Account);
            SetNavActive(btnLogout, false);

            switch (kind)
            {
                case PageKind.Dashboard:
                    LoadDashboardData();
                    break;
                case PageKind.History:
                    LoadHistoryGrid();
                    break;
                case PageKind.RegisterExam:
                    LoadExamsGrid();
                    break;
                case PageKind.Notifications:
                    LoadNotificationsGrid();
                    break;
                case PageKind.Account:
                    LoadAccountData();
                    break;
            }
        }

        private void SetNavActive(Button btn, bool active)
        {
            if (btn == null) return;
            btn.BackColor = active ? C_BLUE_LIGHT : C_WHITE;
            btn.ForeColor = active ? C_BLUE : C_MUTED;
            btn.Font = new Font("Segoe UI", 9.5f, active ? FontStyle.Bold : FontStyle.Regular);
        }

        private void RepositionPages()
        {
            foreach (Control c in pnlPageHost.Controls)
            {
                c.Location = new Point(0, 0);
                c.Size = pnlPageHost.ClientSize;
            }
        }

        // =====================================================
        // HELPERS: CONTROLS
        // =====================================================
        private Button CreateNavButton(string text, int top, bool active, EventHandler click)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(SidebarWidth, 50),
                Location = new Point(0, top),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, active ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = active ? C_BLUE : C_MUTED,
                BackColor = active ? C_BLUE_LIGHT : C_WHITE,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0)
            };
            UiTheme.ApplyButton(btn, UiButtonVariant.Ghost, true);
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = C_BLUE_LIGHT;
            btn.Click += click;

            btn.MouseEnter += (s, e) =>
            {
                if (!btn.BackColor.Equals(C_BLUE_LIGHT))
                {
                    btn.BackColor = C_BG;
                    btn.ForeColor = C_TEXT;
                }
            };
            btn.MouseLeave += (s, e) =>
            {
                if (!btn.BackColor.Equals(C_BLUE_LIGHT))
                {
                    btn.BackColor = C_WHITE;
                    btn.ForeColor = C_MUTED;
                }
            };
            return btn;
        }

        private Button CreateBlueButton(string text, Point loc, Size size)
        {
            var btn = new Button
            {
                Text = text,
                Location = loc,
                Size = size,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = C_BLUE,
                Cursor = Cursors.Hand
            };
            UiTheme.ApplyButton(btn, UiButtonVariant.Primary);
            return btn;
        }

        private Panel CreateStatCard(string title, string value, string subtitle, string icon, Color accent, Color accentLight, Point location, out Label valueLabel)
        {
            var card = new RoundedPanel
            {
                Location = location,
                Size = new Size(240, 110),
                BackColor = C_WHITE
            };

            var badge = new RoundedPanel
            {
                Location = new Point(14, 14),
                Size = new Size(38, 38),
                BackColor = accentLight
            };

            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 16),
                ForeColor = accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            badge.Controls.Add(lblIcon);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = C_MUTED,
                Location = new Point(62, 14),
                AutoSize = true
            };

            valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(62, 34),
                AutoSize = true
            };

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                var lblSubtitle = new Label
                {
                    Text = subtitle,
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = C_MUTED,
                    Location = new Point(62, 70),
                    AutoSize = true
                };
                card.Controls.Add(lblSubtitle);
            }

            card.Controls.AddRange(new Control[] { badge, lblTitle, valueLabel });
            return card;
        }

        private void DrawRoundedPanel(Control control, int radius)
        {
            if (control == null) return;

            Action apply = null;
            apply = () =>
            {
                if (control.Width <= 1 || control.Height <= 1) return;
                using (var path = BuildRoundPath(new Rectangle(0, 0, control.Width - 1, control.Height - 1), radius))
                {
                    control.Region = new Region(path);
                }
            };

            control.Resize += (s, e) => apply();
            control.VisibleChanged += (s, e) => apply();
            apply();
        }

        private void SetPlaceholder(TextBox box, string placeholder)
        {
            if (box == null) return;
            box.Text = "";
            if (!box.IsHandleCreated)
            {
                box.HandleCreated += (s, e) => ApplyCueBanner(box, placeholder);
            }
            else
            {
                ApplyCueBanner(box, placeholder);
            }
        }

        private void ApplyCueBanner(TextBox box, string placeholder)
        {
            try
            {
                SendMessage(box.Handle, EM_SETCUEBANNER, new IntPtr(0), placeholder);
            }
            catch
            {
            }
        }

        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                BackgroundColor = C_WHITE,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeight = 34,
                GridColor = C_BORDER,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoGenerateColumns = false,
                MultiSelect = false
            };
            UiTheme.ApplyGrid(grid);
            return grid;
        }

        private Panel CreateMiniStat(string value, string title, Point location, Color accent, Color bg)
        {
            var pnl = new RoundedPanel
            {
                Location = location,
                Size = new Size(150, 72),
                BackColor = C_WHITE
            };
            var bar = new Panel { Location = new Point(0, 0), Size = new Size(5, 72), BackColor = accent };
            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(16, 10),
                AutoSize = true
            };
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = C_MUTED,
                Location = new Point(16, 42),
                AutoSize = true
            };
            pnl.Controls.AddRange(new Control[] { bar, lblValue, lblTitle });
            return pnl;
        }

        private Panel CreateSectionCard(string title, Point loc, Size size)
        {
            var card = new RoundedPanel
            {
                Location = loc,
                Size = size,
                BackColor = C_WHITE
            };
            card.Controls.Add(CreateSectionHeader(title));
            card.Controls.Add(CreateSectionDivider(size.Width));
            return card;
        }

        private Label CreateSectionHeader(string title)
        {
            return new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(18, 16),
                AutoSize = true
            };
        }

        private Panel CreateSectionDivider(int width)
        {
            return new Panel
            {
                Location = new Point(0, 46),
                Size = new Size(width, 1),
                BackColor = C_BORDER
            };
        }

        private static GraphicsPath BuildRoundPath(Rectangle bounds, int radius)
        {
            int d = Math.Max(0, radius * 2);
            var path = new GraphicsPath();
            if (d <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int w = Math.Max(1, bounds.Width);
            int h = Math.Max(1, bounds.Height);
            int dx = Math.Min(d, w);
            int dy = Math.Min(d, h);

            path.AddArc(bounds.X, bounds.Y, dx, dy, 180, 90);
            path.AddArc(bounds.Right - dx, bounds.Y, dx, dy, 270, 90);
            path.AddArc(bounds.Right - dx, bounds.Bottom - dy, dx, dy, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - dy, dx, dy, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Panel CreateScheduleRow(string day, string mon, string name, string time, string room, string status, bool active, int top, int examId)
        {
            var row = new Panel
            {
                Location = new Point(14, top),
                Size = new Size(492, 50),
                BackColor = C_WHITE,
                Tag = examId
            };

            Color dateBg = active ? C_BLUE_LIGHT : C_BG;
            Color dateColor = active ? C_BLUE : C_MUTED;

            var pnlDate = new RoundedPanel { Location = new Point(0, 0), Size = new Size(44, 46), BackColor = dateBg };
            var lblDay = new Label
            {
                Text = day,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = dateColor,
                Location = new Point(2, 2),
                Size = new Size(40, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            var lblMon = new Label
            {
                Text = mon,
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = dateColor,
                Location = new Point(2, 24),
                Size = new Size(40, 14),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlDate.Controls.AddRange(new Control[] { lblDay, lblMon });

            var lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(56, 4),
                AutoSize = true
            };
            var lblMeta = new Label
            {
                Text = time + "   " + room,
                Font = new Font("Segoe UI", 8),
                ForeColor = C_MUTED,
                Location = new Point(56, 24),
                AutoSize = true
            };

            var btnGo = new Label
            {
                Text = status,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = active ? C_BLUE : C_MUTED,
                BackColor = active ? C_BLUE_LIGHT : C_BG,
                Location = new Point(360, 14),
                Size = new Size(120, 22),
                TextAlign = ContentAlignment.MiddleCenter
            };
            DrawRoundedPanel(btnGo, 6);

            var sep = new Panel { Location = new Point(0, 49), Size = new Size(492, 1), BackColor = C_BORDER };
            row.Controls.AddRange(new Control[] { pnlDate, lblName, lblMeta, btnGo, sep });
            return row;
        }

        private Panel CreateResultRow(string icon, string name, string date, string score, string level, Color levelColor, int top)
        {
            var row = new Panel { Location = new Point(14, top), Size = new Size(492, 42), BackColor = C_WHITE };
            var pnlIcon = new RoundedPanel { Location = new Point(0, 4), Size = new Size(34, 34), BackColor = C_BLUE_LIGHT };
            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 14),
                Location = new Point(4, 4),
                Size = new Size(26, 26),
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlIcon.Controls.Add(lblIcon);
            if (string.IsNullOrWhiteSpace(icon))
                pnlIcon.Visible = false;

            var lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = C_TEXT,
                Location = new Point(string.IsNullOrWhiteSpace(icon) ? 0 : 46, 4),
                AutoSize = true
            };
            var lblDate = new Label
            {
                Text = date,
                Font = new Font("Segoe UI", 8),
                ForeColor = C_MUTED,
                Location = new Point(string.IsNullOrWhiteSpace(icon) ? 0 : 46, 22),
                AutoSize = true
            };
            var lblScore = new Label
            {
                Text = score,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = levelColor,
                Location = new Point(390, 4),
                AutoSize = true
            };
            var lblLevel = new Label
            {
                Text = level,
                Font = new Font("Segoe UI", 8),
                ForeColor = levelColor,
                Location = new Point(398, 24),
                AutoSize = true
            };

            var sep = new Panel { Location = new Point(0, 41), Size = new Size(492, 1), BackColor = C_BORDER };
            row.Controls.AddRange(new Control[] { pnlIcon, lblName, lblDate, lblScore, lblLevel, sep });
            return row;
        }

        private Panel CreateProgressRow(string name, int pct, Color barColor, int top)
        {
            var row = new Panel { Location = new Point(14, top), Size = new Size(492, 42), BackColor = C_WHITE };
            var lblName = new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_TEXT,
                Location = new Point(0, 0),
                AutoSize = true
            };
            var lblPct = new Label
            {
                Text = pct + "%",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_MUTED,
                Location = new Point(440, 0),
                AutoSize = true
            };
            var track = new RoundedPanel { Location = new Point(0, 22), Size = new Size(492, 8), BackColor = C_BG };
            var fill = new RoundedPanel { Location = new Point(0, 0), Size = new Size((int)(492 * pct / 100.0), 8), BackColor = barColor };
            track.Controls.Add(fill);
            row.Controls.AddRange(new Control[] { lblName, lblPct, track });
            return row;
        }

        // =====================================================
        // CUSTOM CONTROLS
        // =====================================================
        public class RoundedPanel : Panel
        {
            private int _radius = 12;
            public int Radius { get => _radius; set { _radius = value; UiTheme.ApplyRoundedRegion(this, _radius); Invalidate(); } }

            protected override void OnPaint(PaintEventArgs e)
            {
                UiTheme.PaintSoftCard(this, e, _radius);
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                UiTheme.ApplyRoundedRegion(this, _radius);
                Invalidate();
            }
        }

        public class RoundPictureBox : Panel
        {
            public new string Text { get; set; } = "";
            public new Font Font { get; set; } = new Font("Segoe UI", 16, FontStyle.Bold);
            public string ImagePath { get; set; } = "";
            public int Radius { get; set; } = 16;

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = RoundedRect(rect, Radius))
                {
                    g.SetClip(path);
                    if (!string.IsNullOrWhiteSpace(ImagePath) && System.IO.File.Exists(ImagePath))
                    {
                        try
                        {
                            using (var img = LoadAvatarImage(ImagePath))
                            {
                                if (img == null) throw new InvalidOperationException("Cannot decode avatar image.");
                                g.DrawImage(img, CoverRect(img.Size, rect));
                            }
                        }
                        catch
                        {
                            PaintFallback(g, rect);
                        }
                    }
                    else
                    {
                        PaintFallback(g, rect);
                    }
                    g.ResetClip();
                    using (var pen = new Pen(Color.FromArgb(190, 205, 223), 1))
                        g.DrawPath(pen, path);
                }
            }

            public static Image LoadAvatarImage(string path)
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return null;

                try
                {
                    using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
                    using (var img = Image.FromStream(fs, true, false))
                    {
                        return new Bitmap(img);
                    }
                }
                catch
                {
                    return LoadAvatarImageWithWic(path);
                }
            }

            private static Image LoadAvatarImageWithWic(string path)
            {
                try
                {
                    var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(
                        new Uri(path, UriKind.Absolute),
                        System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreColorProfile,
                        System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
                    if (decoder.Frames.Count == 0) return null;

                    var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(decoder.Frames[0]));
                    using (var ms = new System.IO.MemoryStream())
                    {
                        encoder.Save(ms);
                        ms.Position = 0;
                        using (var img = Image.FromStream(ms, true, false))
                            return new Bitmap(img);
                    }
                }
                catch
                {
                    return null;
                }
            }
            private void PaintFallback(Graphics g, Rectangle rect)
            {
                using (var brush = new LinearGradientBrush(rect, Color.FromArgb(224, 231, 255), Color.FromArgb(219, 234, 254), 45f))
                    g.FillRectangle(brush, rect);
                if (string.IsNullOrWhiteSpace(Text)) return;
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var br = new SolidBrush(Color.FromArgb(37, 99, 235)))
                    g.DrawString(Text, Font, br, new RectangleF(0, 0, Width, Height), sf);
            }

            private Rectangle CoverRect(Size imageSize, Rectangle bounds)
            {
                if (imageSize.Width <= 0 || imageSize.Height <= 0) return bounds;
                float scale = Math.Max(bounds.Width / (float)imageSize.Width, bounds.Height / (float)imageSize.Height);
                int w = (int)Math.Ceiling(imageSize.Width * scale);
                int h = (int)Math.Ceiling(imageSize.Height * scale);
                return new Rectangle(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
            }

            private GraphicsPath RoundedRect(Rectangle bounds, int radius)
            {
                int d = Math.Max(2, radius * 2);
                var path = new GraphicsPath();
                path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
                path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
                path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
                path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
        public class StatusBadge : Label
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new SolidBrush(BackColor))
                using (var path = new GraphicsPath())
                {
                    int d = 10;
                    var r = new Rectangle(0, 0, Width - 1, Height - 1);
                    path.AddArc(r.X, r.Y, d, d, 180, 90);
                    path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                    path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                    path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                    path.CloseFigure();
                    g.FillPath(br, path);
                }
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var br = new SolidBrush(ForeColor))
                    g.DrawString(Text, Font, br, new RectangleF(0, 0, Width, Height), sf);
            }
        }

        public class DonutChartPanel : RoundedPanel
        {
            // D? li?u th?c du?c set t? LoadDashboardData()
            public float PassRate { get; set; } = 0f;
            public int TotalExams { get; set; } = 0;

            private (float Value, Color Color)[] BuildSegments()
            {
                float pass = Math.Max(0, Math.Min(100, PassRate));
                float fail = 100f - pass;
                return new[]
                {
                    (pass, Color.FromArgb(22, 163, 74)),
                    (fail > 0 ? fail : 0.001f, Color.FromArgb(230, 235, 245)),
                };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                int cx = Width / 2;
                int cy = Height / 2;
                int outer = Math.Max(34, Math.Min(Width, Height) / 2 - 16);
                int inner = Math.Max(20, outer - 30);
                var segs = BuildSegments();
                float total = 0; foreach (var sg in segs) total += sg.Value;

                float startAngle = -90f;
                foreach (var seg in segs)
                {
                    float sweep = (seg.Value / total) * 360f;
                    using (var br = new SolidBrush(seg.Color))
                    {
                        var outerRect = new RectangleF(cx - outer, cy - outer, outer * 2, outer * 2);
                        using (var path = new GraphicsPath())
                        {
                            path.AddPie(outerRect.X, outerRect.Y, outerRect.Width, outerRect.Height, startAngle, Math.Max(0.1f, sweep - 1.5f));
                            g.FillPath(br, path);
                        }
                    }
                    startAngle += sweep;
                }

                var innerRect = new RectangleF(cx - inner, cy - inner, inner * 2, inner * 2);
                using (var br = new SolidBrush(Color.White))
                    g.FillEllipse(br, innerRect);

                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using (var fnt = new Font("Segoe UI", 16, FontStyle.Bold))
                    g.DrawString(PassRate.ToString("0") + "%", fnt, Brushes.Black, new PointF(cx, cy - 8), sf);
                using (var fnt = new Font("Segoe UI", 7.5f))
                using (var br = new SolidBrush(Color.FromArgb(130, 135, 150)))
                    g.DrawString("Tỷ lệ đạt", fnt, br, new PointF(cx, cy + 14), sf);
            }
        }

        // =====================================================
        // DATABASE HELPERS
        // =====================================================
        private DataTable SafeQuery(string sql)
        {
            try { return db.ExecuteQuery(sql); }
            catch { return new DataTable(); }
        }

        private int SafeCount(string sql)
        {
            DataTable dt = SafeQuery(sql);
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
            return SafeToInt(dt.Rows[0][0]);
        }

        private double GetDouble(string sql)
        {
            DataTable dt = SafeQuery(sql);
            if (dt.Rows.Count == 0 || dt.Rows[0][0] == DBNull.Value) return 0;
            return SafeToDouble(dt.Rows[0][0]);
        }

        private bool ExecNonQuery(string sql)
        {
            try
            {
                int result = db.ExecuteNonQuery(sql);
                return result >= 0;
            }
            catch
            {
                return false;
            }
        }

        // =====================================================
        // LOAD TABLES WITH FALLBACK QUERIES
        // =====================================================
        private DataTable LoadExamTable()
        {
            string[] sqls =
            {
                $"SELECT * FROM Exams WHERE ISNULL(CreatedByUserID,0)={userID} OR ISNULL(IsPublishedToServer,0)=1 OR (ExamLink IS NOT NULL AND LTRIM(RTRIM(ExamLink))<>'') ORDER BY 1 DESC",
                "SELECT * FROM Exam ORDER BY 1 DESC",
                "SELECT * FROM DeThi ORDER BY 1 DESC"
            };

            foreach (string sql in sqls)
            {
                var dt = SafeQuery(sql);
                if (dt.Rows.Count > 0 || dt.Columns.Count > 0) return dt;
            }

            return new DataTable();
        }

        private string GetExamModeText(DataRow row)
        {
            string mode = GetAnyField(row, "ExamMode", "Mode", "Loai");
            return mode == "Practice" || mode.Equals("Ôn", StringComparison.OrdinalIgnoreCase) ? "Ôn" : "Thi";
        }

        private string GetExamAccessStatus(DataRow row)
        {
            if (!CanOpenExam(row, out string reason)) return reason;
            string password = GetAnyField(row, "ExamPassword");
            return string.IsNullOrWhiteSpace(password) ? "Sẵn sàng" : "Cần mật khẩu";
        }

        private bool CanOpenExam(DataRow row, out string reason)
        {
            reason = "";
            DateTime now = DateTime.Now;
            bool isPractice = string.Equals(GetAnyField(row, "ExamMode", "Mode", "Loai"), "Practice", StringComparison.OrdinalIgnoreCase);
            string startText = GetAnyField(row, "StartTime", "NgayThi", "DateTime", "ExamTime", "BeginTime", "TimeStart");
            string endText = GetAnyField(row, "EndTime", "TimeEnd", "FinishTime");

            if (DateTime.TryParse(startText, out DateTime start) && now < start)
            {
                reason = "Chưa tới giờ làm";
                return false;
            }

            if (DateTime.TryParse(endText, out DateTime end) && now > end)
            {
                reason = "Đã hết hạn";
                return false;
            }

            int maxAttempts = GetIntField(row, "MaxAttempts", "SoLanLam");
            if (!isPractice && maxAttempts > 0)
            {
                int examId = GetIntField(row, "ExamID", "Id", "ID");
                int done = SafeCount($"SELECT COUNT(*) FROM Results WHERE UserID={userID} AND ExamID={examId}");
                if (done >= maxAttempts)
                {
                    reason = "Đã hết lượt";
                    return false;
                }
            }

            reason = "Sẵn sàng";
            return true;
        }

        private bool PromptExamPassword(string expectedPassword)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Nhập mật khẩu bộ đề";
                dlg.Size = new Size(360, 170);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = UiTheme.PageBg;
                dlg.Font = Font;

                var lbl = new Label { Text = "Mật khẩu:", Location = new Point(20, 22), AutoSize = true };
                var txt = new TextBox { Location = new Point(20, 48), Width = 300, UseSystemPasswordChar = true };
                var btnOk = new Button { Text = "Vào bài", DialogResult = DialogResult.OK, Location = new Point(140, 88), Size = new Size(90, 30) };
                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(240, 88), Size = new Size(80, 30) };
                UiTheme.ApplyTextBox(txt);
                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);

                dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                return dlg.ShowDialog(this) == DialogResult.OK && txt.Text == expectedPassword;
            }
        }

        private DataTable LoadNotificationsTable()
        {
            string[] sqls =
            {
                $"SELECT * FROM Notifications WHERE UserID={userID} OR UserID IS NULL OR IsGlobal = 1 ORDER BY 1 DESC",
                $"SELECT * FROM ThongBao WHERE UserID={userID} OR UserID IS NULL ORDER BY 1 DESC",
                $"SELECT * FROM Notification ORDER BY 1 DESC"
            };

            foreach (string sql in sqls)
            {
                var dt = SafeQuery(sql);
                if (dt.Rows.Count > 0 || dt.Columns.Count > 0) return dt;
            }

            return new DataTable();
        }

        private DataTable LoadUserTable()
        {
            string[] sqls =
            {
                $"SELECT TOP 1 * FROM Users WHERE UserID={userID}",
                $"SELECT TOP 1 * FROM Users WHERE ID={userID}",
                $"SELECT TOP 1 * FROM NguoiDung WHERE UserID={userID}",
                $"SELECT TOP 1 * FROM UserTable WHERE UserID={userID}"
            };

            foreach (string sql in sqls)
            {
                var dt = SafeQuery(sql);
                if (dt.Rows.Count > 0 || dt.Columns.Count > 0) return dt;
            }

            return new DataTable();
        }

        // =====================================================
        // FIELD HELPERS
        // =====================================================
        private string GetAnyField(DataRow row, params string[] names)
        {
            if (row == null) return "";
            foreach (string n in names)
            {
                if (row.Table.Columns.Contains(n) && row[n] != DBNull.Value)
                    return row[n].ToString();
            }
            return "";
        }

        private int GetIntField(DataRow row, params string[] names)
        {
            return SafeToInt(GetAnyField(row, names));
        }

        private DateTime GetDateField(DataRow row, params string[] names)
        {
            string s = GetAnyField(row, names);
            if (DateTime.TryParse(s, out DateTime d)) return d;
            return DateTime.Now;
        }

        private bool GetBoolField(DataRow row, bool defaultValue, params string[] names)
        {
            string s = GetAnyField(row, names);
            if (string.IsNullOrWhiteSpace(s)) return defaultValue;
            if (bool.TryParse(s, out bool b)) return b;
            if (int.TryParse(s, out int i)) return i != 0;
            return s == "1" || s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private bool GetBoolField(DataRow row, params string[] names)
        {
            string s = GetAnyField(row, names);
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (bool.TryParse(s, out bool b)) return b;
            if (int.TryParse(s, out int i)) return i != 0;
            return s == "1" || s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private string GetStringField(DataRow row, params string[] names)
        {
            return GetAnyField(row, names);
        }

        private string FindExistingColumn(DataRow row, params string[] names)
        {
            if (row == null) return "";
            foreach (string n in names)
                if (row.Table.Columns.Contains(n))
                    return n;
            return "";
        }

        private int SafeToInt(object value)
        {
            try
            {
                if (value == null || value == DBNull.Value) return 0;
                if (value is int i) return i;
                if (value is long l) return (int)l;
                if (value is decimal d) return (int)d;
                if (value is double db) return (int)db;
                if (int.TryParse(value.ToString(), out int x)) return x;
            }
            catch { }
            return 0;
        }

        private double SafeToDouble(object value)
        {
            try
            {
                if (value == null || value == DBNull.Value) return 0;
                if (value is double d) return d;
                if (value is decimal dc) return (double)dc;
                if (value is int i) return i;
                if (double.TryParse(value.ToString(), out double x)) return x;
            }
            catch { }
            return 0;
        }

        private string SafeToString(object value)
        {
            return value == null || value == DBNull.Value ? "" : value.ToString();
        }

        private string SqlEscape(string input)
        {
            return (input ?? "").Replace("'", "''");
        }

        // =====================================================
        // EXAM TEXT HELPERS
        // =====================================================
        private string GetExamTimeText(DataRow row)
        {
            string start = GetAnyField(row, "StartTime", "NgayThi", "DateTime", "ExamTime", "BeginTime", "TimeStart");
            string end = GetAnyField(row, "EndTime", "TimeEnd", "FinishTime");
            string duration = GetAnyField(row, "Duration", "ThoiLuong", "DurationMinutes");

            if (!string.IsNullOrWhiteSpace(start) && !string.IsNullOrWhiteSpace(end))
                return $"{FormatDateTimeSafe(start)} - {FormatDateTimeSafe(end)}";

            if (!string.IsNullOrWhiteSpace(start))
                return FormatDateTimeSafe(start);

            if (!string.IsNullOrWhiteSpace(duration))
                return duration + " phút";

            return "Chưa cập nhật";
        }

        private string GetExamDay(DataRow row)
        {
            string start = GetAnyField(row, "StartTime", "NgayThi", "DateTime", "ExamTime", "BeginTime", "TimeStart");
            if (DateTime.TryParse(start, out DateTime dt))
                return dt.Day.ToString("00");
            return "--";
        }

        private string GetExamMon(DataRow row)
        {
            string start = GetAnyField(row, "StartTime", "NgayThi", "DateTime", "ExamTime", "BeginTime", "TimeStart");
            if (DateTime.TryParse(start, out DateTime dt))
                return dt.ToString("MMM").ToUpperInvariant();
            return "---";
        }

        private bool IsExamActive(DataRow row)
        {
            string st = GetAnyField(row, "Status", "TrangThai");
            if (!string.IsNullOrWhiteSpace(st))
            {
                string low = st.ToLowerInvariant();
                if (low.Contains("đã") || low.Contains("da") || low.Contains("inactive") || low.Contains("kết thúc"))
                    return false;
                return true;
            }

            string start = GetAnyField(row, "StartTime", "NgayThi", "DateTime", "ExamTime");
            if (DateTime.TryParse(start, out DateTime dt))
                return dt >= DateTime.Now.AddDays(-7);

            return true;
        }

        private string FormatDateTimeSafe(string s)
        {
            if (DateTime.TryParse(s, out DateTime dt))
                return dt.ToString("dd/MM HH:mm");
            return s;
        }

        private string GetLevel(double score)
        {
            if (score >= 9) return "Xuất sắc";
            if (score >= 7) return "Tốt";
            if (score >= 5) return "Trung bình";
            return "Yếu";
        }

        private Color GetLevelColor(double score)
        {
            if (score >= 9) return Color.FromArgb(22, 163, 74);
            if (score >= 7) return Color.FromArgb(37, 99, 235);
            if (score >= 5) return Color.FromArgb(217, 119, 6);
            return Color.FromArgb(220, 38, 38);
        }

        private void UpdateSidebarUserName(string fullName)
        {
            foreach (Control p in pnlSidebar.Controls)
            {
                foreach (Control c in p.Controls)
                {
                    if (c is Label lbl && lbl.Font.Size >= 11 && lbl.Font.Bold)
                    {
                        if (!string.IsNullOrWhiteSpace(fullName))
                            lbl.Text = fullName;
                        RefreshAvatarViews();
                        return;
                    }
                }
            }
        }

        // =====================================================
        // LAYOUT
        // =====================================================
        private void UpdateLayout()
        {
            int cw = ClientSize.Width;
            int ch = ClientSize.Height;

            if (pnlSidebar != null)
                pnlSidebar.Size = new Size(SidebarWidth, ch);

            if (pnlMain != null)
            {
                pnlMain.Location = new Point(SidebarWidth, 0);
                pnlMain.Size = new Size(Math.Max(0, cw - SidebarWidth), ch);
            }

            if (pnlTopBar != null)
                pnlTopBar.Width = pnlMain != null ? pnlMain.ClientSize.Width : Math.Max(0, cw - SidebarWidth);

            if (pnlPageHost != null)
            {
                pnlPageHost.Location = new Point(0, TopBarHeight);
                pnlPageHost.Size = new Size(pnlMain.ClientSize.Width, pnlMain.ClientSize.Height - TopBarHeight);
            }

            RepositionPages();
        }

        // =====================================================
        // EVENT UTILS
        // =====================================================
        private void LoadRefreshAllAndStay()
        {
            LoadAllData();
            if (pnlDashboardPage.Visible) ShowPage(PageKind.Dashboard);
            else if (pnlHistoryPage.Visible) ShowPage(PageKind.History);
            else if (pnlRegisterPage.Visible) ShowPage(PageKind.RegisterExam);
            else if (pnlNotificationsPage.Visible) ShowPage(PageKind.Notifications);
            else if (pnlAccountPage.Visible) ShowPage(PageKind.Account);
        }
    }
}























