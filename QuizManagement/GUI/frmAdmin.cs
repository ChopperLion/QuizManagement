using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class frmAdmin : Form
    {
        private readonly Database db = new Database();

        private Panel pnlSidebar;
        private Panel pnlHeader;
        private Panel pnlContent;
        private Panel pnlStats;
        private Panel pnlAccountCard;
        private Panel pnlGridCard;
        private Panel pnlActions;

        private Label lblTotalUser;
        private Label lblTotalExam;
        private Label lblTotalQuestion;
        private Label lblTotalResult;

        private TextBox txtUsername;
        private TextBox txtPassword;
        private TextBox txtFullName;
        private TextBox txtSearch;
        private ComboBox cboRoleID;
        private DataGridView dgvUser;
        private Label lblAccountHint;
        private Label lblGridSummary;
        private Label lblGridEmptyState;
        private Label lblSystemHint;

        private Button btnAdd;
        private Button btnUpdate;
        private Button btnDelete;
        private Button btnResetPass;
        private Button btnRefresh;
        private Button btnLogout;

        // Sidebar nav buttons
        private Button btnSideOverview;
        private Button btnSideAccount;
        private Button btnSideSystem;

        private int selectedID = -1;
        private readonly string currentUserName;

        private const int SidebarWidth = 210;
        private const int HeaderHeight = 96;

        private readonly Color PageBg = UiTheme.PageBg;
        private readonly Color SidebarBg = UiTheme.DarkSurface;
        private readonly Color SidebarPanel = Color.FromArgb(38, 49, 83);
        private readonly Color CardBorder = UiTheme.Border;
        private readonly Color TextDark = UiTheme.TextPrimary;
        private readonly Color TextMuted = UiTheme.TextSecondary;
        private readonly Color Blue = UiTheme.Primary;
        private readonly Color Green = UiTheme.Success;
        private readonly Color Orange = UiTheme.Warning;
        private readonly Color Purple = UiTheme.Info;
        private readonly Color Red = UiTheme.Danger;

        public frmAdmin() : this("Admin")
        {
        }

        public frmAdmin(string fullName)
        {
            currentUserName = string.IsNullOrWhiteSpace(fullName) ? "Admin" : fullName.Trim();
            InitUI();
            DoubleBuffered = true;
            Load += Frm_Load;
            Resize += (s, e) => UpdateLayout();
        }

        private void Frm_Load(object sender, EventArgs e)
        {
            Database.EnsureQuestionPointsColumn();
            Database.EnsureTeacherOwnershipColumns();
            UpdateLayout();
            LoadUser();
            LoadStatistics();
        }

        private void InitUI()
        {
            Text = "Admin Panel";
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(1280, 760);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9.5f);

            BuildSidebar();
            BuildHeader();
            BuildContent();
        }

        private void BuildSidebar()
        {
            pnlSidebar = new Panel
            {
                BackColor = SidebarBg,
                Location = new Point(0, 0),
                Size = new Size(SidebarWidth, ClientSize.Height)
            };
            Controls.Add(pnlSidebar);

            var logo = new Panel
            {
                BackColor = SidebarPanel,
                Location = new Point(0, 0),
                Size = new Size(SidebarWidth, 78)
            };
            pnlSidebar.Controls.Add(logo);

            logo.Controls.Add(new Label
            {
                Text = "ADMIN",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            });

            pnlSidebar.Controls.Add(CreateSidebarLabel("QUẢN TRỊ", 106));

            btnSideOverview = CreateSidebarButton("  ▣  Tổng quan", 136, true);
            btnSideOverview.Click += (s2, e2) => ShowAdminView(AdminView.Overview);
            pnlSidebar.Controls.Add(btnSideOverview);

            btnSideAccount = CreateSidebarButton("  ▤  Tài khoản", 184, false);
            btnSideAccount.Click += (s2, e2) => ShowAdminView(AdminView.Account);
            pnlSidebar.Controls.Add(btnSideAccount);

            btnSideSystem = CreateSidebarButton("  ▥  Hệ thống", 232, false);
            btnSideSystem.Click += (s2, e2) => ShowAdminView(AdminView.System);
            pnlSidebar.Controls.Add(btnSideSystem);

            btnLogout = new Button
            {
                Text = "  ⎋  Đăng Xuất",
                BackColor = Color.FromArgb(200, 35, 35),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand,
                Size = new Size(SidebarWidth, 50)
            };
            UiTheme.ApplyButton(btnLogout, UiButtonVariant.Danger, true);
            btnLogout.Click += BtnLogout_Click;
            pnlSidebar.Controls.Add(btnLogout);
        }

        private Label CreateSidebarLabel(string text, int top)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = false,
                Location = new Point(18, top),
                Size = new Size(SidebarWidth - 36, 22)
            };
        }

        private Button CreateSidebarButton(string text, int top, bool active)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(SidebarWidth, 46),
                Location = new Point(0, top),
                BackColor = active ? Blue : SidebarBg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void BuildHeader()
        {
            pnlHeader = new Panel
            {
                BackColor = Color.White,
                Location = new Point(SidebarWidth, 0),
                Size = new Size(ClientSize.Width - SidebarWidth, HeaderHeight)
            };
            Controls.Add(pnlHeader);

            pnlHeader.Controls.Add(new Label
            {
                Text = "QUẢN TRỊ HỆ THỐNG",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(28, 22)
            });

            pnlHeader.Controls.Add(new Label
            {
                Text = "Quản lý tài khoản người dùng và theo dõi dữ liệu hệ thống.",
                Font = new Font("Segoe UI", 10f),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(30, 58)
            });

            pnlHeader.Controls.Add(new Label
            {
                Text = currentUserName + "\nQuản trị viên",
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlHeader.Width - 330, 25),
                Size = new Size(130, 42)
            });

            btnRefresh = CreateOutlineButton("↻  Làm mới dữ liệu");
            btnRefresh.Click += BtnRefresh_Click;
            pnlHeader.Controls.Add(btnRefresh);
        }

        private void BuildContent()
        {
            pnlContent = new Panel
            {
                BackColor = PageBg,
                Location = new Point(SidebarWidth, HeaderHeight),
                Size = new Size(ClientSize.Width - SidebarWidth, ClientSize.Height - HeaderHeight)
            };
            Controls.Add(pnlContent);

            pnlStats = new Panel { BackColor = PageBg };
            pnlContent.Controls.Add(pnlStats);

            lblSystemHint = new Label
            {
                Text = "Khu vuc nay tom tat du lieu he thong va giu man hinh thong thoang hon so voi viec mo hop thoai thong tin.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                AutoSize = false,
                Visible = false
            };
            pnlContent.Controls.Add(lblSystemHint);

            pnlAccountCard = CreateCard("THÔNG TIN TÀI KHOẢN");
            pnlContent.Controls.Add(pnlAccountCard);

            AddField(pnlAccountCard, "Tên đăng nhập", 58, out txtUsername);
            AddField(pnlAccountCard, "Mật khẩu", 126, out txtPassword);
            AddField(pnlAccountCard, "Họ tên", 194, out txtFullName);

            pnlAccountCard.Controls.Add(new Label
            {
                Text = "Vai trò",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(20, 262),
                AutoSize = true
            });
            cboRoleID = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10f),
                Location = new Point(20, 286),
                Size = new Size(330, 30)
            };
            UiTheme.ApplyComboBox(cboRoleID);
            cboRoleID.Items.AddRange(new object[] { "Admin", "Teacher", "Student" });
            pnlAccountCard.Controls.Add(cboRoleID);

            lblAccountHint = new Label
            {
                Text = "Nhap thong tin de tao tai khoan moi, hoac chon mot tai khoan trong danh sach de cap nhat.",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextMuted,
                Location = new Point(20, 332),
                Size = new Size(330, 48)
            };
            pnlAccountCard.Controls.Add(lblAccountHint);

            pnlActions = CreateCard("THAO TÁC");
            pnlContent.Controls.Add(pnlActions);

            btnAdd = CreateActionButton("+  Thêm tài khoản", Green);
            btnUpdate = CreateActionButton("✎  Cập nhật", Orange);
            btnDelete = CreateActionButton("🗑  Xóa", Red);
            btnResetPass = CreateActionButton("↺  Reset mật khẩu", Purple);
            pnlActions.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnResetPass });

            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnResetPass.Click += BtnResetPass_Click;

            pnlGridCard = CreateCard("DANH SÁCH TÀI KHOẢN");
            pnlContent.Controls.Add(pnlGridCard);

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(20, 52),
                Size = new Size(360, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            UiTheme.ApplyTextBox(txtSearch);
            txtSearch.TextChanged += TxtSearch_TextChanged;
            pnlGridCard.Controls.Add(txtSearch);

            lblGridSummary = new Label
            {
                Text = "Danh sach tai khoan se cap nhat ngay khi ban tim kiem hoac thay doi du lieu.",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextMuted,
                Location = new Point(20, 52),
                AutoSize = true
            };
            pnlGridCard.Controls.Add(lblGridSummary);

            pnlGridCard.Controls.Add(new Label
            {
                Text = "Tìm theo username",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextMuted,
                Location = new Point(24, 86),
                AutoSize = true
            });

            dgvUser = new DataGridView
            {
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 42,
                RowTemplate = { Height = 38 },
                GridColor = CardBorder
            };
            UiTheme.ApplyGrid(dgvUser);
            dgvUser.CellClick += DgvUser_CellClick;
            pnlGridCard.Controls.Add(dgvUser);

            lblGridEmptyState = new Label
            {
                Text = "Chua co tai khoan nao phu hop voi bo loc hien tai.\r\nHay doi tu khoa tim kiem hoac tao tai khoan moi.",
                Font = new Font("Segoe UI", 10f),
                ForeColor = TextMuted,
                Size = new Size(420, 56),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            pnlGridCard.Controls.Add(lblGridEmptyState);
        }

        private void AddField(Panel parent, string label, int top, out TextBox textBox)
        {
            parent.Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(20, top),
                AutoSize = true
            });

            textBox = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(20, top + 24),
                Size = new Size(330, 30)
            };
            UiTheme.ApplyTextBox(textBox);
            parent.Controls.Add(textBox);
        }

        private Panel CreateCard(string title)
        {
            var panel = new Panel
            {
                BackColor = UiTheme.SurfaceRaised,
                Padding = new Padding(18)
            };
            panel.Paint += PaintCard;
            panel.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.2f, FontStyle.Bold),
                ForeColor = TextDark,
                Location = new Point(20, 18),
                AutoSize = true
            });
            return panel;
        }

        private Button CreateActionButton(string text, Color color)
        {
            var btn = new Button
            {
                Text = text,
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Height = 42
            };
            UiTheme.ApplyButton(btn,
                color == Green ? UiButtonVariant.Success :
                color == Orange ? UiButtonVariant.Warning :
                color == Purple ? UiButtonVariant.Info :
                color == Red ? UiButtonVariant.Danger :
                UiButtonVariant.Primary);
            return btn;
        }

        private Button CreateOutlineButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(160, 36),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.3f),
                Cursor = Cursors.Hand
            };
            UiTheme.ApplyButton(btn, UiButtonVariant.Secondary);
            return btn;
        }

        private void UpdateLayout()
        {
            if (pnlSidebar == null || pnlHeader == null || pnlContent == null) return;

            int cw = ClientSize.Width - SidebarWidth;
            int ch = ClientSize.Height;
            pnlSidebar.Size = new Size(SidebarWidth, ch);
            btnLogout.Location = new Point(0, ch - 56);

            pnlHeader.SetBounds(SidebarWidth, 0, cw, HeaderHeight);
            btnRefresh.Location = new Point(pnlHeader.Width - btnRefresh.Width - 28, 30);

            pnlContent.SetBounds(SidebarWidth, HeaderHeight, cw, ch - HeaderHeight);

            int margin = 22;
            int gap = 14;
            int contentW = pnlContent.Width - margin * 2;
            int statsH = 96;
            int topCardsY = margin + statsH + gap;
            int leftW = Math.Max(360, (int)(contentW * 0.30));
            int actionW = 250;
            int gridW = contentW - leftW - actionW - gap * 2;
            int lowerH = pnlContent.Height - topCardsY - margin;

            pnlStats.SetBounds(margin, margin, contentW, statsH);
            if (lblSystemHint != null)
                lblSystemHint.SetBounds(margin, margin + statsH + 6, contentW, 34);
            LayoutStatsCards();

            pnlAccountCard.SetBounds(margin, topCardsY, leftW, Math.Max(360, lowerH));
            pnlActions.SetBounds(margin + leftW + gap, topCardsY, actionW, Math.Max(360, lowerH));
            pnlGridCard.SetBounds(margin + leftW + gap + actionW + gap, topCardsY, gridW, Math.Max(360, lowerH));

            int actionTop = 58;
            foreach (Control control in pnlActions.Controls)
            {
                if (control is Button)
                {
                    control.SetBounds(20, actionTop, pnlActions.Width - 40, 42);
                    actionTop += 54;
                }
            }

            txtSearch.Width = Math.Min(420, pnlGridCard.Width - 40);
            dgvUser.SetBounds(20, 116, pnlGridCard.Width - 40, pnlGridCard.Height - 136);
            if (lblGridSummary != null)
                lblGridSummary.Location = new Point(20, 52);
            if (lblGridEmptyState != null)
            {
                lblGridEmptyState.Location = new Point(
                    Math.Max(20, (pnlGridCard.Width - lblGridEmptyState.Width) / 2),
                    Math.Max(140, (pnlGridCard.Height - lblGridEmptyState.Height) / 2));
            }
        }

        private void LayoutStatsCards()
        {
            if (pnlStats == null || pnlStats.Controls.Count == 0) return;
            int gap = 12;
            int w = (pnlStats.Width - gap * 3) / 4;
            for (int i = 0; i < pnlStats.Controls.Count; i++)
                pnlStats.Controls[i].SetBounds(i * (w + gap), 0, w, pnlStats.Height);
        }

        private Panel CreateStatCard(string icon, string title, Color accent, Color bg, out Label valueLabel)
        {
            var card = new Panel { BackColor = Color.White };
            card.Paint += PaintCard;

            card.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 18f, FontStyle.Bold),
                ForeColor = accent,
                BackColor = bg,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(18, 24),
                Size = new Size(48, 48)
            });

            valueLabel = new Label
            {
                Text = "0",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = false,
                Location = new Point(82, 18),
                Size = new Size(130, 30)
            };
            card.Controls.Add(valueLabel);

            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = false,
                Location = new Point(82, 51),
                Size = new Size(170, 22)
            });

            return card;
        }

        private void EnsureStatsCards()
        {
            if (pnlStats.Controls.Count > 0) return;
            pnlStats.Controls.Add(CreateStatCard("👤", "Tài khoản", Blue, Color.FromArgb(219, 234, 254), out lblTotalUser));
            pnlStats.Controls.Add(CreateStatCard("▣", "Bộ đề", Green, Color.FromArgb(220, 252, 231), out lblTotalExam));
            pnlStats.Controls.Add(CreateStatCard("📖", "Câu hỏi", Orange, Color.FromArgb(255, 237, 213), out lblTotalQuestion));
            pnlStats.Controls.Add(CreateStatCard("▥", "Kết quả", Purple, Color.FromArgb(237, 233, 254), out lblTotalResult));
        }

        private void LoadUser()
        {
            dgvUser.DataSource = db.ExecuteQuery(BuildUserListSql(""));
            FormatGrid();
            dgvUser.ClearSelection();
            UpdateUserGridState();
        }

        private string BuildUserListSql(string whereClause)
        {
            return
                "SELECT UserID, Username, Password, FullName, " +
                "CASE RoleID WHEN 1 THEN N'Admin' WHEN 2 THEN N'Teacher' WHEN 3 THEN N'Student' ELSE N'Không rõ' END AS RoleName, " +
                "RoleID FROM Users " + whereClause + " ORDER BY UserID DESC";
        }

        private void FormatGrid()
        {
            if (dgvUser.Columns.Contains("UserID")) dgvUser.Columns["UserID"].HeaderText = "Mã";
            if (dgvUser.Columns.Contains("Username")) dgvUser.Columns["Username"].HeaderText = "Tên đăng nhập";
            if (dgvUser.Columns.Contains("Password")) dgvUser.Columns["Password"].HeaderText = "Mật khẩu";
            if (dgvUser.Columns.Contains("FullName")) dgvUser.Columns["FullName"].HeaderText = "Họ tên";
            if (dgvUser.Columns.Contains("RoleName")) dgvUser.Columns["RoleName"].HeaderText = "Vai trò";
            if (dgvUser.Columns.Contains("RoleID")) dgvUser.Columns["RoleID"].Visible = false;

            if (dgvUser.Columns.Contains("UserID")) dgvUser.Columns["UserID"].FillWeight = 45;
            if (dgvUser.Columns.Contains("Username")) dgvUser.Columns["Username"].FillWeight = 110;
            if (dgvUser.Columns.Contains("Password")) dgvUser.Columns["Password"].FillWeight = 100;
            if (dgvUser.Columns.Contains("FullName")) dgvUser.Columns["FullName"].FillWeight = 150;
            if (dgvUser.Columns.Contains("RoleName")) dgvUser.Columns["RoleName"].FillWeight = 80;
        }

        private void LoadStatistics()
        {
            EnsureStatsCards();
            lblTotalUser.Text = SafeCount("SELECT COUNT(*) FROM Users").ToString("N0");
            lblTotalExam.Text = SafeCount("SELECT COUNT(*) FROM Exams").ToString("N0");
            lblTotalQuestion.Text = SafeCount("SELECT COUNT(*) FROM Questions").ToString("N0");
            lblTotalResult.Text = SafeCount("SELECT COUNT(*) FROM Results").ToString("N0");
            LayoutStatsCards();
            UpdateAccountHint();
        }

        private int SafeCount(string sql)
        {
            try
            {
                DataTable dt = db.ExecuteQuery(sql);
                return dt.Rows.Count == 0 ? 0 : Convert.ToInt32(dt.Rows[0][0]);
            }
            catch { return 0; }
        }

        private string SqlEscape(string input)
        {
            if (input == null) return string.Empty;
            return input.Replace("'", "''");
        }

        private int GetRoleID()
        {
            if (cboRoleID.Text == "Admin") return 1;
            if (cboRoleID.Text == "Teacher") return 2;
            if (cboRoleID.Text == "Student") return 3;
            return -1;
        }

        private void SetRoleById(int roleId)
        {
            if (roleId == 1) cboRoleID.SelectedIndex = 0;
            else if (roleId == 2) cboRoleID.SelectedIndex = 1;
            else if (roleId == 3) cboRoleID.SelectedIndex = 2;
            else cboRoleID.SelectedIndex = -1;
        }

        private bool ValidateInput()
        {
            if (txtUsername.Text.Trim() == "" || txtPassword.Text.Trim() == "" ||
                txtFullName.Text.Trim() == "" || cboRoleID.Text.Trim() == "")
            {
                MessageBox.Show("Vui lòng nhập đầy đủ dữ liệu!");
                return false;
            }
            return true;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            string username = SqlEscape(txtUsername.Text.Trim());
            string password = SqlEscape(txtPassword.Text.Trim());
            string fullName = SqlEscape(txtFullName.Text.Trim());
            int roleID = GetRoleID();

            if (roleID == -1) { MessageBox.Show("Role không hợp lệ!"); return; }

            DataTable dt = db.ExecuteQuery("SELECT * FROM Users WHERE Username = N'" + username + "'");
            if (dt.Rows.Count > 0) { MessageBox.Show("Username đã tồn tại!"); return; }

            db.ExecuteNonQuery(
                "INSERT INTO Users (Username, Password, FullName, RoleID) VALUES (" +
                "N'" + username + "',N'" + password + "',N'" + fullName + "'," + roleID + ")");

            MessageBox.Show("Thêm tài khoản thành công!");
            LoadUser();
            LoadStatistics();
            ClearData();
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedID == -1) { MessageBox.Show("Chọn tài khoản cần sửa!"); return; }
            if (!ValidateInput()) return;

            string username = SqlEscape(txtUsername.Text.Trim());
            string password = SqlEscape(txtPassword.Text.Trim());
            string fullName = SqlEscape(txtFullName.Text.Trim());
            int roleID = GetRoleID();
            if (roleID == -1) { MessageBox.Show("Role không hợp lệ!"); return; }

            db.ExecuteNonQuery(
                "UPDATE Users SET Username=N'" + username + "', Password=N'" + password +
                "', FullName=N'" + fullName + "', RoleID=" + roleID +
                " WHERE UserID=" + selectedID);

            MessageBox.Show("Cập nhật thành công!");
            LoadUser();
            LoadStatistics();
            ClearData();
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedID == -1) { MessageBox.Show("Chọn tài khoản cần xóa!"); return; }

            if (MessageBox.Show("Bạn có chắc muốn xóa?", "Thông báo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            db.ExecuteNonQuery("DELETE FROM Users WHERE UserID = " + selectedID);
            MessageBox.Show("Xóa thành công!");
            LoadUser();
            LoadStatistics();
            ClearData();
        }

        private void BtnResetPass_Click(object sender, EventArgs e)
        {
            if (selectedID == -1) { MessageBox.Show("Chọn tài khoản!"); return; }

            db.ExecuteNonQuery("UPDATE Users SET Password = '123456' WHERE UserID = " + selectedID);
            MessageBox.Show("Đã reset password về 123456");
            LoadUser();
            ClearData();
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            ClearData();
            LoadUser();
            LoadStatistics();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string keyword = SqlEscape(txtSearch.Text.Trim());
            dgvUser.DataSource = db.ExecuteQuery(BuildUserListSql(
                "WHERE Username LIKE N'%" + keyword + "%' OR FullName LIKE N'%" + keyword + "%'"));
            FormatGrid();
            selectedID = -1;
            dgvUser.ClearSelection();
            UpdateUserGridState();
            UpdateAccountHint();
        }

        private void DgvUser_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvUser.Rows[e.RowIndex];
            selectedID = Convert.ToInt32(row.Cells["UserID"].Value);
            txtUsername.Text = row.Cells["Username"].Value?.ToString();
            txtPassword.Text = row.Cells["Password"].Value?.ToString();
            txtFullName.Text = row.Cells["FullName"].Value?.ToString();

            int roleID = 0;
            if (dgvUser.Columns.Contains("RoleID") && row.Cells["RoleID"].Value != DBNull.Value && row.Cells["RoleID"].Value != null)
                int.TryParse(row.Cells["RoleID"].Value.ToString(), out roleID);
            SetRoleById(roleID);
            UpdateAccountHint();
        }

        private void ClearData()
        {
            txtUsername.Clear();
            txtPassword.Clear();
            txtFullName.Clear();
            cboRoleID.SelectedIndex = -1;
            selectedID = -1;
            dgvUser.ClearSelection();
            UpdateAccountHint();
        }

        private void UpdateUserGridState()
        {
            int rowCount = dgvUser?.Rows?.Count ?? 0;
            bool hasRows = rowCount > 0;

            if (dgvUser != null) dgvUser.Visible = hasRows;
            if (lblGridEmptyState != null) lblGridEmptyState.Visible = !hasRows;

            if (lblGridSummary == null) return;

            string keyword = txtSearch == null ? string.Empty : txtSearch.Text.Trim();
            if (!hasRows)
            {
                lblGridSummary.Text = string.IsNullOrWhiteSpace(keyword)
                    ? "He thong chua co tai khoan nao. Ban co the tao tai khoan moi o khung ben trai."
                    : "Khong tim thay tai khoan nao khop voi tu khoa \"" + keyword + "\".";
                return;
            }

            lblGridSummary.Text = string.IsNullOrWhiteSpace(keyword)
                ? "Dang hien thi " + rowCount + " tai khoan. Chon mot dong de cap nhat hoac reset mat khau."
                : "Tim thay " + rowCount + " tai khoan khop voi tu khoa \"" + keyword + "\".";
        }

        private void UpdateAccountHint()
        {
            if (lblAccountHint == null) return;

            if (selectedID > 0)
            {
                string roleName = cboRoleID.SelectedItem == null ? "tai khoan" : cboRoleID.SelectedItem.ToString();
                lblAccountHint.Text = "Dang chon tai khoan ID " + selectedID + ". Ban co the cap nhat thong tin, doi vai tro hoac reset mat khau cho " + roleName + ".";
                return;
            }

            lblAccountHint.Text = "Nhap thong tin de tao tai khoan moi. Neu muon chinh sua tai khoan da co, hay chon mot dong trong danh sach ben phai.";
        }

        // ==============================================
        // ADMIN VIEW NAVIGATION
        // ==============================================
        private enum AdminView { Overview, Account, System }

        private void ShowAdminView(AdminView view)
        {
            // Update sidebar active state
            btnSideOverview.BackColor = view == AdminView.Overview ? Blue : SidebarBg;
            btnSideAccount.BackColor = view == AdminView.Account ? Blue : SidebarBg;
            btnSideSystem.BackColor = view == AdminView.System ? Blue : SidebarBg;

            switch (view)
            {
                case AdminView.Overview:
                    // Tổng quan: hiển thị thống kê + grid người dùng
                    pnlAccountCard.Visible = true;
                    pnlActions.Visible = true;
                    pnlGridCard.Visible = true;
                    pnlStats.Visible = true;
                    if (lblSystemHint != null) lblSystemHint.Visible = false;
                    LoadUser();
                    LoadStatistics();
                    var hdrLbl = pnlHeader.Controls.OfType<Label>().FirstOrDefault(l => l.Text.Contains("QUẢN TRỊ"));
                    if (hdrLbl != null) hdrLbl.Text = "QUẢN TRỊ HỆ THỐNG";
                    break;

                case AdminView.Account:
                    // Tài khoản: chỉ hiện form tạo/sửa tài khoản + grid
                    pnlAccountCard.Visible = true;
                    pnlActions.Visible = true;
                    pnlGridCard.Visible = true;
                    pnlStats.Visible = false;
                    if (lblSystemHint != null) lblSystemHint.Visible = false;
                    ClearData();
                    break;

                case AdminView.System:
                    // Hệ thống: ẩn tất cả form, chỉ hiện thống kê
                    pnlAccountCard.Visible = false;
                    pnlActions.Visible = false;
                    pnlGridCard.Visible = false;
                    pnlStats.Visible = true;
                    if (lblSystemHint != null)
                    {
                        lblSystemHint.Visible = true;
                        lblSystemHint.Text = "Thong tin he thong da duoc dua len ngay tren man hinh nay. Ban co the doi chieu tong tai khoan, de thi, cau hoi va ket qua ma khong can dong hop thoai.";
                    }
                    LoadStatistics();
                    bool showSystemDialog = false;
                    if (showSystemDialog) MessageBox.Show(
                        "Thông tin hệ thống:\n\n" +
                        "• Database: QuizManagement (SQL Server)\n" +
                        "• Phiên bản: 1.0\n" +
                        "• Tổng người dùng: " + SafeCount("SELECT COUNT(*) FROM Users") + "\n" +
                        "• Tổng đề thi: " + SafeCount("SELECT COUNT(*) FROM Exams") + "\n" +
                        "• Tổng câu hỏi: " + SafeCount("SELECT COUNT(*) FROM Questions") + "\n" +
                        "• Tổng kết quả: " + SafeCount("SELECT COUNT(*) FROM Results"),
                        "Thông tin hệ thống",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    break;
            }
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var f = new frmLogin();
            f.Show();
            Hide();
        }

        private void PaintCard(object sender, PaintEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null) return;
            UiTheme.PaintSoftCard(panel, e, 22);
        }
    }
}
