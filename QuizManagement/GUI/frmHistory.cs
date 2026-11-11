using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class frmHistory : Form
    {
        private readonly Database db = new Database();
        private readonly int? userID;
        private readonly string userName;

        private Panel pnlMain;
        private Panel pnlCard;
        private Label lblTitle;
        private Label lblSubTitle;
        private DataGridView dgvHistory;
        private Label lblEmptyState;

        public frmHistory(int? userID, string userName)
        {
            this.userID = userID;
            this.userName = string.IsNullOrWhiteSpace(userName)
                ? "Tất cả tài khoản"
                : userName;

            InitUI();
            LoadHistory();
        }

        private void InitUI()
        {
            Text = "Lịch sử thi";
            Size = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiTheme.PageBg;
            Font = new Font("Segoe UI", 9.5f);

            pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = UiTheme.PageBg
            };
            Controls.Add(pnlMain);

            lblTitle = new Label
            {
                Text = "📜 LỊCH SỬ THI",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true
            };

            lblSubTitle = new Label
            {
                Text = userID.HasValue
                    ? "Kết quả của " + userName
                    : "Tất cả tài khoản",
                Font = new Font("Segoe UI", 10),
                ForeColor = UiTheme.TextSecondary,
                AutoSize = true,
                Top = 40
            };

            pnlCard = new Panel
            {
                Top = 80,
                Width = 920,
                Height = 480,
                BackColor = UiTheme.SurfaceRaised,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            pnlCard.Paint += (s, e) => UiTheme.PaintSoftCard(pnlCard, e, 22);

            dgvHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false
            };
            UiTheme.ApplyGrid(dgvHistory);

            lblEmptyState = new Label
            {
                Text = "Chưa có lịch sử thi phù hợp.\nKết quả nộp bài sẽ xuất hiện ở đây.",
                Size = new Size(420, 56),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = UiTheme.TextSecondary,
                Visible = false
            };

            pnlCard.Controls.Add(dgvHistory);
            pnlCard.Controls.Add(lblEmptyState);
            pnlMain.Controls.Add(lblTitle);
            pnlMain.Controls.Add(lblSubTitle);
            pnlMain.Controls.Add(pnlCard);

            pnlCard.Resize += (s, e) =>
            {
                if (lblEmptyState != null)
                {
                    lblEmptyState.Location = new Point(
                        Math.Max(0, (pnlCard.ClientSize.Width - lblEmptyState.Width) / 2),
                        Math.Max(90, (pnlCard.ClientSize.Height - lblEmptyState.Height) / 2));
                }
            };
        }

        private void LoadHistory()
        {
            string where = userID.HasValue
                ? "WHERE r.UserID=" + userID
                : "";

            string query =
                "SELECT r.ResultID, e.ExamName, r.Score, r.SubmitDate " +
                "FROM Results r " +
                "LEFT JOIN Exams e ON r.ExamID=e.ExamID " +
                where +
                " ORDER BY r.SubmitDate DESC";

            DataTable table = db.ExecuteQuery(query);
            dgvHistory.DataSource = table;
            if (lblEmptyState != null) lblEmptyState.Visible = table.Rows.Count == 0;
        }
    }
}
