using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuizManagement.GUI
{
    public sealed class StudentExamIdentityPrompt : Form
    {
        private readonly TextBox txtFullName;
        private readonly TextBox txtClassName;
        private readonly bool requireClassBeforeStart;
        private readonly bool allowStudentEditClass;

        public string StudentFullName => (txtFullName.Text ?? "").Trim();
        public string StudentClassName => (txtClassName.Text ?? "").Trim();

        public StudentExamIdentityPrompt(string examName, string defaultFullName, string defaultClassName)
            : this(examName, defaultFullName, defaultClassName, true, true)
        {
        }

        public StudentExamIdentityPrompt(string examName, string defaultFullName, string defaultClassName, bool requireClassBeforeStart, bool allowStudentEditClass)
        {
            this.requireClassBeforeStart = requireClassBeforeStart;
            this.allowStudentEditClass = allowStudentEditClass;
            Text = "Thông tin trước khi làm bài";
            Size = new Size(520, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(247, 250, 252);
            Font = new Font("Segoe UI", 10F);

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            Controls.Add(panel);

            var lblTitle = new Label
            {
                Text = "Xác nhận thông tin dự thi",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(0, 0)
            };
            panel.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = "Giáo viên sẽ dùng các thông tin này để quản lý điểm và thống kê theo lớp.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(2, 34)
            };
            panel.Controls.Add(lblSub);

            var lblExam = new Label
            {
                Text = "Bài thi: " + (string.IsNullOrWhiteSpace(examName) ? "Chưa rõ tên bài thi" : examName),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                AutoSize = true,
                Location = new Point(2, 68)
            };
            panel.Controls.Add(lblExam);

            var lblFullName = new Label { Text = "Họ tên", AutoSize = true, Location = new Point(2, 108) };
            txtFullName = new TextBox
            {
                Location = new Point(2, 132),
                Size = new Size(460, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Text = defaultFullName ?? ""
            };

            var lblClass = new Label { Text = "Lớp", AutoSize = true, Location = new Point(2, 172) };
            txtClassName = new TextBox
            {
                Location = new Point(2, 196),
                Size = new Size(180, 30),
                BorderStyle = BorderStyle.FixedSingle,
                Text = defaultClassName ?? "",
                ReadOnly = !allowStudentEditClass
            };

            var btnStart = new Button
            {
                Text = "Bắt đầu làm bài",
                Size = new Size(150, 36),
                Location = new Point(312, 192),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnStart.FlatAppearance.BorderSize = 0;
            btnStart.Click += BtnStart_Click;

            var btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(90, 36),
                Location = new Point(194, 192),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };

            panel.Controls.AddRange(new Control[] { lblFullName, txtFullName, lblClass, txtClassName, btnCancel, btnStart });
            AcceptButton = btnStart;
            CancelButton = btnCancel;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StudentFullName))
            {
                MessageBox.Show("Vui lòng nhập họ tên trước khi vào bài thi.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtFullName.Focus();
                DialogResult = DialogResult.None;
                return;
            }

            if (requireClassBeforeStart && string.IsNullOrWhiteSpace(StudentClassName))
            {
                string message = allowStudentEditClass
                    ? "Vui lòng nhập lớp để giáo viên có thể tổng hợp điểm theo lớp."
                    : "Bài thi yêu cầu thông tin lớp, nhưng lớp trong hồ sơ của bạn đang trống. Vui lòng cập nhật tài khoản hoặc liên hệ giáo viên.";
                MessageBox.Show(message, "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (allowStudentEditClass)
                    txtClassName.Focus();
                else
                    txtFullName.Focus();
                DialogResult = DialogResult.None;
            }
        }
    }
}



