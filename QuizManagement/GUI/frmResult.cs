using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace QuizManagement.GUI
{
    public class frmResult : Form
    {
        private readonly IReadOnlyList<ResultReviewRow> reviewRows;
        private Label lblResultHeadline;
        private Label lblResultHint;

        public frmResult(
            int correct,
            int total,
            double score,
            decimal earnedPoints,
            decimal totalPoints,
            IReadOnlyList<ResultReviewRow> reviewRows
        )
        {
            this.reviewRows = reviewRows ?? new List<ResultReviewRow>();
            InitUI(correct, total, score, earnedPoints, totalPoints);
        }

        private void InitUI(
            int correct,
            int total,
            double score,
            decimal earnedPoints,
            decimal totalPoints
        )
        {
            Text = "K\u1ebft qu\u1ea3 b\u00e0i thi";
            Size = new Size(1040, 760);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UiTheme.PageBg;
            Font = new Font("Segoe UI", 10F);

            var card = new Panel
            {
                Location = new Point(22, 18),
                Size = new Size(980, 684),
                BackColor = UiTheme.SurfaceRaised,
                Padding = new Padding(28, 24, 28, 24)
            };
            card.Paint += (s, e) => UiTheme.PaintSoftCard(card, e, 24);
            Controls.Add(card);

            var lblChip = new Label
            {
                Text = "B\u00e0i thi \u0111\u00e3 n\u1ed9p th\u00e0nh c\u00f4ng",
                AutoSize = false,
                Size = new Size(220, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(220, 252, 231),
                ForeColor = Color.FromArgb(21, 128, 61),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Location = new Point(24, 18)
            };
            card.Controls.Add(lblChip);

            var lblTitle = new Label
            {
                Text = "K\u1ebeT QU\u1ea2 B\u00c0I THI",
                AutoSize = true,
                Font = new Font("Segoe UI", 25F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(22, 68)
            };
            card.Controls.Add(lblTitle);

            card.Controls.Add(CreateSummaryBox("\u0110i\u1ec3m ch\u00ednh th\u1ee9c", score.ToString("0.00") + "/10", Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235), new Point(24, 154)));
            card.Controls.Add(CreateSummaryBox("S\u1ed1 c\u00e2u \u0111\u00fang", correct + "/" + total, Color.FromArgb(240, 253, 244), Color.FromArgb(22, 163, 74), new Point(258, 154)));
            card.Controls.Add(CreateSummaryBox("\u0110i\u1ec3m theo t\u1eebng c\u00e2u", earnedPoints.ToString("0.##") + "/" + totalPoints.ToString("0.##"), Color.FromArgb(255, 247, 237), Color.FromArgb(234, 88, 12), new Point(492, 154)));
            card.Controls.Add(CreateSummaryBox("C\u00e2u \u0111\u00e3 tr\u1ea3 l\u1eddi", CountAnswered() + "/" + total, Color.FromArgb(245, 243, 255), Color.FromArgb(124, 58, 237), new Point(726, 154)));

            var infoPanel = new Panel
            {
                Location = new Point(24, 274),
                Size = new Size(928, 98),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            card.Controls.Add(infoPanel);

            lblResultHeadline = new Label
            {
                Text = BuildResultHeadline(score),
                Location = new Point(18, 14),
                Size = new Size(892, 22),
                Font = new Font("Segoe UI", 10.2F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
            infoPanel.Controls.Add(lblResultHeadline);

            lblResultHint = new Label
            {
                Text = BuildResultHint(score, correct, total),
                Location = new Point(18, 42),
                Size = new Size(892, 42),
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            infoPanel.Controls.Add(lblResultHint);

            var lblReview = new Label
            {
                Text = "CHI TI\u1ebeT B\u00c0I L\u00c0M",
                AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(24, 390)
            };
            card.Controls.Add(lblReview);

            var lblReviewHint = new Label
            {
                Text = "M\u1ed7i d\u00f2ng cho bi\u1ebft n\u1ed9i dung c\u00e2u h\u1ecfi, \u0111\u00e1p \u00e1n b\u1ea1n \u0111\u00e3 ch\u1ecdn, \u0111\u00e1p \u00e1n \u0111\u00fang v\u00e0 k\u1ebft qu\u1ea3.",
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(24, 416)
            };
            card.Controls.Add(lblReviewHint);

            var grid = BuildReviewGrid();
            grid.Location = new Point(24, 448);
            card.Controls.Add(grid);

            var btnClose = new Button
            {
                Text = "\u0110\u00f3ng",
                DialogResult = DialogResult.OK,
                Location = new Point(852, 624),
                Size = new Size(100, 38),
                BackColor = UiTheme.Primary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            UiTheme.ApplyButton(btnClose, UiButtonVariant.Primary);
            card.Controls.Add(btnClose);

            AcceptButton = btnClose;
        }

        private Panel CreateSummaryBox(string title, string value, Color bg, Color valueColor, Point location)
        {
            var panel = new Panel
            {
                Location = location,
                Size = new Size(210, 100),
                BackColor = bg
            };

            var lblTitle = new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(14, 14)
            };
            panel.Controls.Add(lblTitle);

            var lblValue = new Label
            {
                Text = value,
                AutoSize = true,
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = valueColor,
                Location = new Point(14, 42)
            };
            panel.Controls.Add(lblValue);

            return panel;
        }

        private DataGridView BuildReviewGrid()
        {
            var grid = new DataGridView
            {
                Size = new Size(928, 188),
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                MultiSelect = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UiTheme.ApplyGrid(grid);
            grid.ColumnHeadersHeight = 40;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "No", HeaderText = "C\u00e2u", FillWeight = 10 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Question", HeaderText = "N\u1ed9i dung", FillWeight = 36 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserAnswer", HeaderText = "B\u1ea1n tr\u1ea3 l\u1eddi", FillWeight = 19 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Correct", HeaderText = "\u0110\u00e1p \u00e1n \u0111\u00fang", FillWeight = 19 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Result", HeaderText = "K\u1ebft qu\u1ea3", FillWeight = 11 });

            foreach (ResultReviewRow row in reviewRows)
            {
                string resultText = row.IsCorrect ? "\u0110\u00fang" : (row.AwardedPoints > 0 ? "G\u1ea7n \u0111\u00fang" : "Sai");
                int idx = grid.Rows.Add(row.Number, row.QuestionContent, row.UserAnswer, row.CorrectAnswer, resultText);
                grid.Rows[idx].DefaultCellStyle.BackColor = row.IsCorrect
                    ? Color.FromArgb(240, 253, 244)
                    : row.AwardedPoints > 0
                        ? Color.FromArgb(255, 247, 237)
                        : Color.FromArgb(254, 242, 242);
            }

            return grid;
        }

        private int CountAnswered()
        {
            int answered = 0;
            foreach (ResultReviewRow row in reviewRows)
            {
                if (!string.IsNullOrWhiteSpace(row.UserAnswer) && row.UserAnswer != "(\u1ecf tr\u1ed1ng)")
                    answered++;
            }
            return answered;
        }

        private string BuildResultHeadline(double score)
        {
            if (score >= 8)
                return "B\u1ea1n \u0111\u00e3 ho\u00e0n th\u00e0nh b\u00e0i thi r\u1ea5t t\u1ed1t.";
            if (score >= 5)
                return "B\u1ea1n \u0111\u00e3 ho\u00e0n th\u00e0nh b\u00e0i thi \u0111\u1ea1t y\u00eau c\u1ea7u.";
            return "B\u00e0i l\u00e0m \u0111\u00e3 \u0111\u01b0\u1ee3c l\u01b0u, b\u1ea1n n\u00ean xem l\u1ea1i c\u00e1c c\u00e2u ch\u01b0a \u0111\u00fang.";
        }

        private string BuildResultHint(double score, int correct, int total)
        {
            int unanswered = Math.Max(0, total - CountAnswered());
            string accuracy = total > 0
                ? "T\u1ef7 l\u1ec7 \u0111\u00fang hi\u1ec7n t\u1ea1i l\u00e0 " + Math.Round(correct * 100.0 / total, 0).ToString("0") + "%."
                : "B\u00e0i l\u00e0m hi\u1ec7n ch\u01b0a c\u00f3 \u0111\u1ee7 d\u1eef li\u1ec7u \u0111\u1ec3 t\u00ednh t\u1ef7 l\u1ec7 \u0111\u00fang.";

            string unansweredHint = unanswered > 0
                ? " B\u1ea1n c\u00f2n b\u1ecf tr\u1ed1ng " + unanswered + " c\u00e2u."
                : " B\u1ea1n \u0111\u00e3 tr\u1ea3 l\u1eddi to\u00e0n b\u1ed9 c\u00e2u h\u1ecfi.";

            if (score >= 8)
                return accuracy + unansweredHint + " B\u1ea1n c\u00f3 th\u1ec3 xem l\u1ea1i chi ti\u1ebft b\u00ean d\u01b0\u1edbi \u0111\u1ec3 ghi nh\u1edb c\u00e1c c\u00e2u l\u00e0m t\u1ed1t.";
            if (score >= 5)
                return accuracy + unansweredHint + " H\u00e3y xem l\u1ea1i m\u1ed9t v\u00e0i c\u00e2u sai \u0111\u1ec3 c\u1ee7ng c\u1ed1 ph\u1ea7n ki\u1ebfn th\u1ee9c c\u00f2n thi\u1ebfu.";
            return accuracy + unansweredHint + " N\u00ean xem k\u1ef9 ph\u1ea7n \u0111\u00e1p \u00e1n \u0111\u00fang b\u00ean d\u01b0\u1edbi \u0111\u1ec3 bi\u1ebft n\u1ed9i dung c\u1ea7n \u00f4n t\u1eadp th\u00eam.";
        }
    }

    public class ResultReviewRow
    {
        public int AttemptAnswerId { get; set; }
        public int QuestionId { get; set; }
        public int Number { get; set; }
        public string QuestionContent { get; set; }
        public string UserAnswer { get; set; }
        public string CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsFillBlank { get; set; }
        public decimal MaxPoints { get; set; }
        public decimal AwardedPoints { get; set; }
        public decimal SuggestedScoreRatio { get; set; }
        public string MatchedKeywords { get; set; }
    }
}
