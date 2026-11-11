using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QuizManagement.BUS;

namespace QuizManagement.GUI
{
    public class frmLogin : Form
    {
        private readonly UserBUS bus = new UserBUS();
        private readonly string pendingExamLink;

        private Panel pnlCard;
        private Label lblStatus;
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;

        private readonly Color PageBg = UiTheme.PageBg;
        private readonly Color CardBorder = UiTheme.Border;
        private readonly Color TextDark = UiTheme.TextPrimary;
        private readonly Color TextMuted = UiTheme.TextSecondary;
        private readonly Color Blue = UiTheme.Primary;
        private readonly Color Red = UiTheme.Danger;
        private readonly Color Green = UiTheme.Success;

        public frmLogin() : this("")
        {
        }

        public frmLogin(string pendingExamLink)
        {
            this.pendingExamLink = pendingExamLink ?? "";
            InitUI();
        }

        private void InitUI()
        {
            Text = "Đăng nhập";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(920, 560);
            MinimumSize = new Size(760, 500);
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9.5f);
            DoubleBuffered = true;

            var leftPanel = new Panel
            {
                BackColor = UiTheme.DarkSurface,
                Dock = DockStyle.Left,
                Width = 330
            };
            leftPanel.Paint += PaintBrandPanel;
            Controls.Add(leftPanel);

            leftPanel.Controls.Add(new Label
            {
                Text = "QUIZ\nMANAGEMENT",
                Font = new Font("Segoe UI", 25f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Location = new Point(42, 58),
                Size = new Size(250, 88)
            });

            leftPanel.Controls.Add(new Label
            {
                Text = "Hệ thống quản lý đề thi và ngân hàng câu hỏi",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(203, 213, 225),
                AutoSize = false,
                Location = new Point(44, 158),
                Size = new Size(230, 54)
            });

            leftPanel.Controls.Add(CreateBrandBadge("✓", "Quản lý bộ đề", 260));
            leftPanel.Controls.Add(CreateBrandBadge("◱", "Ngân hàng câu hỏi", 314));
            leftPanel.Controls.Add(CreateBrandBadge("▥", "Báo cáo thống kê", 368));

            pnlCard = new Panel
            {
                BackColor = Color.White,
                Size = new Size(380, 390)
            };
            pnlCard.Paint += PaintCard;
            UiTheme.AttachRoundedRegion(pnlCard, 28);
            Controls.Add(pnlCard);

            pnlCard.Controls.Add(new Label
            {
                Text = "Đăng nhập",
                Font = new Font("Segoe UI", 22f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(34, 32)
            });

            pnlCard.Controls.Add(new Label
            {
                Text = "Nhập tài khoản để tiếp tục vào hệ thống.",
                Font = new Font("Segoe UI", 9.6f),
                ForeColor = TextMuted,
                AutoSize = false,
                Location = new Point(38, 78),
                Size = new Size(300, 24)
            });

            AddInputLabel("Tên đăng nhập", 122);
            txtUsername = CreateTextBox(150, false);
            pnlCard.Controls.Add(txtUsername);

            AddInputLabel("Mật khẩu", 204);
            txtPassword = CreateTextBox(232, true);
            pnlCard.Controls.Add(txtPassword);

            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Red,
                AutoSize = false,
                Location = new Point(38, 282),
                Size = new Size(304, 22),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlCard.Controls.Add(lblStatus);

            btnLogin = new Button
            {
                Text = "Đăng nhập",
                BackColor = Blue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(38, 310),
                Size = new Size(304, 44)
            };
            UiTheme.ApplyButton(btnLogin, UiButtonVariant.Primary);
            btnLogin.Click += BtnLogin_Click;
            pnlCard.Controls.Add(btnLogin);

            UiTheme.ApplyTextBox(txtUsername);
            UiTheme.ApplyTextBox(txtPassword);

            AcceptButton = btnLogin;
            Resize += (s, e) => LayoutLogin();
            LayoutLogin();
        }

        private Label CreateBrandBadge(string icon, string text, int top)
        {
            return new Label
            {
                Text = icon + "   " + text,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                AutoSize = false,
                Location = new Point(44, top),
                Size = new Size(230, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private void AddInputLabel(string text, int top)
        {
            pnlCard.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.2f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(38, top)
            });
        }

        private TextBox CreateTextBox(int top, bool password)
        {
            var box = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                Font = UiTheme.BodyFont(11f),
                Location = new Point(38, top),
                Size = new Size(304, 32),
                PasswordChar = password ? '•' : '\0'
            };
            box.Enter += (s, e) => SetStatus("", TextMuted);
            return box;
        }

        private void LayoutLogin()
        {
            if (pnlCard == null) return;
            int leftWidth = Controls.Count > 0 && Controls[0].Dock == DockStyle.Left ? Controls[0].Width : 330;
            int areaW = ClientSize.Width - leftWidth;
            pnlCard.Location = new Point(leftWidth + Math.Max(0, (areaW - pnlCard.Width) / 2), Math.Max(24, (ClientSize.Height - pnlCard.Height) / 2));
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", Red);
                return;
            }

            btnLogin.Enabled = false;
            SetStatus("Đang kiểm tra thông tin đăng nhập...", TextMuted);

            try
            {
                DataTable dt = bus.Login(username, password);

                if (dt.Rows.Count == 0)
                {
                    SetStatus("Sai tài khoản hoặc mật khẩu.", Red);
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                    return;
                }

                SetStatus("Đăng nhập thành công, đang mở hệ thống...", Green);
                OpenMainForm(dt.Rows[0], username);
            }
            catch (Exception ex)
            {
                SetStatus("Không thể đăng nhập: " + ex.Message, Red);
            }
            finally
            {
                btnLogin.Enabled = true;
            }
        }

        private void OpenMainForm(DataRow userRow, string username)
        {
            string roleID = userRow["RoleID"].ToString();

            if (roleID == "1")
            {
                string fullName = userRow["FullName"] == DBNull.Value || string.IsNullOrWhiteSpace(userRow["FullName"].ToString())
                    ? username
                    : userRow["FullName"].ToString();

                var f = new frmAdmin(fullName);
                f.Show();
            }
            else if (roleID == "2")
            {
                string fullName = userRow["FullName"] == DBNull.Value || string.IsNullOrWhiteSpace(userRow["FullName"].ToString())
                    ? username
                    : userRow["FullName"].ToString();

                int userID = Convert.ToInt32(userRow["UserID"]);
                var f = new frmTeacher(userID, fullName);
                f.Show();
            }
            else
            {
                int userID = Convert.ToInt32(userRow["UserID"]);
                string fullName = userRow["FullName"] == DBNull.Value || string.IsNullOrWhiteSpace(userRow["FullName"].ToString())
                    ? username
                    : userRow["FullName"].ToString();

                var f = new frmStudent(userID, fullName, pendingExamLink);
                f.Show();
            }

            Hide();
        }

        private void SetStatus(string text, Color color)
        {
            if (lblStatus == null) return;
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
        }

        private void PaintCard(object sender, PaintEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null) return;

            UiTheme.PaintSoftCard(panel, e, 28);
        }

        private void PaintBrandPanel(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(38, 49, 83), Color.FromArgb(90, 109, 191), LinearGradientMode.ForwardDiagonal))
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
        }
    }
}
