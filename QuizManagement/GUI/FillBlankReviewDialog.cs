using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QuizManagement.GUI
{
    internal sealed class FillBlankReviewDialog : Form
    {
        private readonly DataGridView grid;
        private readonly List<ResultReviewRow> rows;

        public FillBlankReviewDialog(IReadOnlyList<ResultReviewRow> reviewRows)
        {
            rows = reviewRows == null
                ? new List<ResultReviewRow>()
                : reviewRows.Where(r => r.IsFillBlank).Select(CloneRow).ToList();

            Text = "Rà soát câu điền khuyết";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1180, 620);
            MinimumSize = new Size(980, 520);
            Font = new Font("Segoe UI", 9.5F);

            var title = new Label
            {
                Dock = DockStyle.Top,
                Height = 54,
                Text = "Rà soát câu điền khuyết và chỉnh điểm nếu cần",
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                Padding = new Padding(18, 16, 0, 0)
            };
            Controls.Add(title);

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Number", HeaderText = "Câu", FillWeight = 6, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Question", HeaderText = "Nội dung", FillWeight = 28, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UserAnswer", HeaderText = "Bài làm", FillWeight = 18, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CorrectAnswer", HeaderText = "Đáp án gốc", FillWeight = 18, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Suggestion", HeaderText = "Gợi ý chấm", FillWeight = 18, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "MaxPoints", HeaderText = "Điểm tối đa", FillWeight = 8, ReadOnly = true });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "AwardedPoints", HeaderText = "Điểm duyệt", FillWeight = 8 });
            Controls.Add(grid);

            var footer = new Panel { Dock = DockStyle.Bottom, Height = 62, Padding = new Padding(18, 12, 18, 12) };
            var hint = new Label
            {
                AutoSize = true,
                Text = "Giáo viên có thể duyệt lại các câu gần đúng rồi lưu trực tiếp.",
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(0, 18)
            };
            var btnCancel = new Button
            {
                Text = "Đóng",
                DialogResult = DialogResult.Cancel,
                Size = new Size(100, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(1040, 12)
            };
            var btnSave = new Button
            {
                Text = "Lưu điểm",
                DialogResult = DialogResult.OK,
                Size = new Size(110, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(920, 12),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            footer.Controls.Add(hint);
            footer.Controls.Add(btnSave);
            footer.Controls.Add(btnCancel);
            Controls.Add(footer);

            LoadRows();
        }

        public IReadOnlyList<ResultReviewRow> UpdatedRows => rows;

        private void LoadRows()
        {
            foreach (ResultReviewRow row in rows)
            {
                string suggestion = row.SuggestedScoreRatio >= 1M
                    ? "Khớp tốt, có thể cho trọn điểm"
                    : "Khớp " + (row.SuggestedScoreRatio * 100M).ToString("0") + "%" +
                      (string.IsNullOrWhiteSpace(row.MatchedKeywords) ? "" : " | từ khóa: " + row.MatchedKeywords);
                grid.Rows.Add(
                    row.Number,
                    row.QuestionContent,
                    row.UserAnswer,
                    row.CorrectAnswer,
                    suggestion,
                    row.MaxPoints.ToString("0.##"),
                    row.AwardedPoints.ToString("0.##"));
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                decimal awarded;
                string text = Convert.ToString(grid.Rows[i].Cells["AwardedPoints"].Value) ?? "0";
                if (!decimal.TryParse(text, out awarded))
                {
                    MessageBox.Show("Điểm duyệt phải là số hợp lệ.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                if (awarded < 0M || awarded > rows[i].MaxPoints)
                {
                    MessageBox.Show("Điểm duyệt phải nằm trong khoảng từ 0 đến điểm tối đa của câu.", "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.None;
                    return;
                }

                rows[i].AwardedPoints = awarded;
                rows[i].IsCorrect = awarded >= rows[i].MaxPoints && rows[i].MaxPoints > 0;
            }
        }

        private static ResultReviewRow CloneRow(ResultReviewRow source)
        {
            return new ResultReviewRow
            {
                AttemptAnswerId = source.AttemptAnswerId,
                QuestionId = source.QuestionId,
                Number = source.Number,
                QuestionContent = source.QuestionContent,
                UserAnswer = source.UserAnswer,
                CorrectAnswer = source.CorrectAnswer,
                IsCorrect = source.IsCorrect,
                IsFillBlank = source.IsFillBlank,
                MaxPoints = source.MaxPoints,
                AwardedPoints = source.AwardedPoints,
                SuggestedScoreRatio = source.SuggestedScoreRatio,
                MatchedKeywords = source.MatchedKeywords
            };
        }
    }
}
