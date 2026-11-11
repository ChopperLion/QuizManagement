using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class QuestionEditDialog : Form
    {
        private readonly Database db = new Database();
        private readonly int? questionId;
        private readonly int? linkExamId;
        private readonly int ownerUserId;

        private TextBox txtQuestion;
        private TextBox txtA;
        private TextBox txtB;
        private TextBox txtC;
        private TextBox txtD;
        private ComboBox cboCorrect;
        private ComboBox cboQuestionType;
        private NumericUpDown nudPoints;
        private CheckBox chkHasPoints;
        private Button btnTogglePoints;

        public int SavedQuestionId { get; private set; }

        public QuestionEditDialog(int? editQuestionId = null, int? autoLinkExamId = null, int ownerUserId = 0)
        {
            questionId = editQuestionId;
            linkExamId = autoLinkExamId;
            this.ownerUserId = ownerUserId;
            Database.EnsureAdvancedExamColumns();
            BuildUI();
            if (questionId.HasValue && questionId.Value > 0)
                LoadQuestion(questionId.Value);
        }

        private void BuildUI()
        {
            Text = questionId.HasValue && questionId.Value > 0 ? "Ch\u1ec9nh s\u1eeda c\u00e2u h\u1ecfi" : "T\u1ea1o c\u00e2u h\u1ecfi m\u1edbi";
            Size = new Size(610, 680);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            AutoScroll = true;

            int y = 18;
            Controls.Add(Lbl("Lo\u1ea1i c\u00e2u h\u1ecfi *", 20, y));
            y += 24;

            cboQuestionType = new ComboBox
            {
                Location = new Point(20, y),
                Size = new Size(240, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboQuestionType.Items.AddRange(new object[] { "Tr\u1eafc nghi\u1ec7m", "\u0110i\u1ec1n khuy\u1ebft" });
            cboQuestionType.SelectedIndex = 0;
            cboQuestionType.SelectedIndexChanged += (s, e) => SyncQuestionType();
            Controls.Add(cboQuestionType);
            y += 48;

            Controls.Add(Lbl("N\u1ed9i dung c\u00e2u h\u1ecfi *", 20, y));
            y += 24;

            txtQuestion = new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(548, 86),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(txtQuestion);
            y += 98;

            Controls.Add(Lbl("\u0110\u00e1p \u00e1n A *", 20, y));
            y += 24;
            txtA = new TextBox { Location = new Point(20, y), Size = new Size(548, 30) };
            Controls.Add(txtA);
            y += 42;

            Controls.Add(Lbl("\u0110\u00e1p \u00e1n B *", 20, y));
            y += 24;
            txtB = new TextBox { Location = new Point(20, y), Size = new Size(548, 30) };
            Controls.Add(txtB);
            y += 42;

            Controls.Add(Lbl("\u0110\u00e1p \u00e1n C *", 20, y));
            y += 24;
            txtC = new TextBox { Location = new Point(20, y), Size = new Size(548, 30) };
            Controls.Add(txtC);
            y += 42;

            Controls.Add(Lbl("\u0110\u00e1p \u00e1n D *", 20, y));
            y += 24;
            txtD = new TextBox { Location = new Point(20, y), Size = new Size(548, 30) };
            Controls.Add(txtD);
            y += 42;

            Controls.Add(Lbl("\u0110\u00e1p \u00e1n \u0111\u00fang *", 20, y));
            y += 24;

            cboCorrect = new ComboBox
            {
                Location = new Point(20, y),
                Size = new Size(170, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboCorrect.Items.AddRange(new object[] { "A", "B", "C", "D" });
            cboCorrect.SelectedIndex = 0;
            Controls.Add(cboCorrect);

            chkHasPoints = new CheckBox
            {
                Text = "G\u00e1n s\u1ed1 \u0111i\u1ec3m",
                Location = new Point(206, y + 4),
                AutoSize = true,
                Visible = false
            };
            chkHasPoints.CheckedChanged += (s, e) => SyncPointsControls();
            Controls.Add(chkHasPoints);

            btnTogglePoints = new Button
            {
                Text = "+ Th\u00eam \u0111i\u1ec3m",
                Location = new Point(206, y - 1),
                Size = new Size(128, 32),
                BackColor = Color.FromArgb(241, 245, 249),
                ForeColor = Color.FromArgb(37, 99, 235),
                FlatStyle = FlatStyle.Flat
            };
            btnTogglePoints.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btnTogglePoints.Click += (s, e) =>
            {
                chkHasPoints.Checked = !chkHasPoints.Checked;
                SyncPointsControls();
            };
            Controls.Add(btnTogglePoints);

            nudPoints = new NumericUpDown
            {
                Location = new Point(348, y),
                Size = new Size(92, 30),
                Minimum = 0.25M,
                Maximum = 100M,
                DecimalPlaces = 2,
                Increment = 0.25M,
                Value = 1M,
                Enabled = false
            };
            Controls.Add(nudPoints);

            Controls.Add(Lbl("(t\u00f9y ch\u1ecdn)", 450, y + 4, Color.Gray));
            SyncPointsControls();
            y += 58;

            var btnSave = new Button
            {
                Text = "L\u01b0u",
                Location = new Point(384, y),
                Size = new Size(86, 38),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            var btnCancel = new Button
            {
                Text = "H\u1ee7y",
                Location = new Point(482, y),
                Size = new Size(86, 38),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);
            CancelButton = btnCancel;

            if (linkExamId.HasValue && linkExamId.Value > 0)
                chkHasPoints.Checked = true;

            SyncQuestionType();
        }

        private static Label Lbl(string text, int x, int y, Color? fg = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = fg ?? Color.FromArgb(31, 41, 55)
            };
        }

        private void SyncPointsControls()
        {
            if (nudPoints != null)
                nudPoints.Enabled = chkHasPoints.Checked;

            if (btnTogglePoints != null)
            {
                btnTogglePoints.Text = chkHasPoints.Checked ? "B\u1ecf \u0111i\u1ec3m" : "+ Th\u00eam \u0111i\u1ec3m";
                btnTogglePoints.BackColor = chkHasPoints.Checked ? Color.FromArgb(219, 234, 254) : Color.FromArgb(241, 245, 249);
            }
        }

        private void SyncQuestionType()
        {
            bool fillBlank = cboQuestionType != null && cboQuestionType.SelectedIndex == 1;
            if (txtA != null) txtA.Enabled = !fillBlank;
            if (txtB != null) txtB.Enabled = !fillBlank;
            if (txtC != null) txtC.Enabled = !fillBlank;
            if (txtD != null) txtD.Enabled = !fillBlank;
            if (cboCorrect == null) return;

            cboCorrect.DropDownStyle = fillBlank ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList;
            if (fillBlank)
            {
                cboCorrect.Items.Clear();
            }
            else if (cboCorrect.Items.Count == 0)
            {
                cboCorrect.Items.AddRange(new object[] { "A", "B", "C", "D" });
                cboCorrect.SelectedIndex = 0;
            }
        }

        private void LoadQuestion(int id)
        {
            DataTable dt = db.ExecuteQuery(
                "SELECT QuestionContent, OptionA, OptionB, OptionC, OptionD, CorrectAnswer, Points, " +
                "CASE WHEN COL_LENGTH('Questions','QuestionType') IS NULL THEN N'MultipleChoice' ELSE QuestionType END AS QuestionType " +
                "FROM Questions WHERE QuestionID=" + id);
            if (dt.Rows.Count == 0) return;

            DataRow r = dt.Rows[0];
            string type = r.Table.Columns.Contains("QuestionType") ? r["QuestionType"].ToString() : "MultipleChoice";
            cboQuestionType.SelectedIndex = type == "FillBlank" ? 1 : 0;
            txtQuestion.Text = r["QuestionContent"].ToString();
            txtA.Text = r["OptionA"].ToString();
            txtB.Text = r["OptionB"].ToString();
            txtC.Text = r["OptionC"].ToString();
            txtD.Text = r["OptionD"].ToString();

            string ans = r["CorrectAnswer"].ToString().Trim().ToUpperInvariant();
            if (cboQuestionType.SelectedIndex == 1)
                cboCorrect.Text = r["CorrectAnswer"].ToString();
            else if (ans == "A" || ans == "B" || ans == "C" || ans == "D")
                cboCorrect.SelectedItem = ans;

            if (r["Points"] != DBNull.Value && r["Points"] != null)
            {
                chkHasPoints.Checked = true;
                decimal p = Convert.ToDecimal(r["Points"]);
                nudPoints.Value = Math.Max(nudPoints.Minimum, Math.Min(nudPoints.Maximum, p));
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            string q = Esc(txtQuestion.Text.Trim());
            string a = Esc(txtA.Text.Trim());
            string b = Esc(txtB.Text.Trim());
            string c = Esc(txtC.Text.Trim());
            string d = Esc(txtD.Text.Trim());
            bool fillBlank = cboQuestionType.SelectedIndex == 1;
            string correct = fillBlank ? Esc(cboCorrect.Text.Trim()) : cboCorrect.Text;
            string questionType = fillBlank ? "FillBlank" : "MultipleChoice";
            string pointsSql = chkHasPoints.Checked ? nudPoints.Value.ToString(CultureInfo.InvariantCulture) : "NULL";

            if (questionId.HasValue && questionId.Value > 0)
            {
                db.ExecuteNonQuery(
                    "UPDATE Questions SET QuestionContent=N'" + q + "',OptionA=N'" + a + "',OptionB=N'" + b +
                    "',OptionC=N'" + c + "',OptionD=N'" + d + "',CorrectAnswer=N'" + correct + "',Points=" + pointsSql +
                    ",QuestionType=N'" + questionType + "'" +
                    " WHERE QuestionID=" + questionId.Value);
                SavedQuestionId = questionId.Value;
            }
            else
            {
                string ownerColumn = ownerUserId > 0 ? ",CreatedByUserID" : "";
                string ownerValue = ownerUserId > 0 ? "," + ownerUserId : "";
                db.ExecuteNonQuery(
                    "INSERT INTO Questions (QuestionContent,OptionA,OptionB,OptionC,OptionD,CorrectAnswer,Points,QuestionType" + ownerColumn + ") VALUES " +
                    "(N'" + q + "',N'" + a + "',N'" + b + "',N'" + c + "',N'" + d + "',N'" + correct + "'," + pointsSql + ",N'" + questionType + "'" + ownerValue + ")");
                DataTable dt = db.ExecuteQuery("SELECT TOP 1 QuestionID FROM Questions ORDER BY QuestionID DESC");
                SavedQuestionId = Convert.ToInt32(dt.Rows[0]["QuestionID"]);
            }

            if (linkExamId.HasValue && linkExamId.Value > 0)
            {
                DataTable chk = db.ExecuteQuery(
                    "SELECT COUNT(*) AS C FROM ExamQuestions WHERE ExamID=" + linkExamId.Value + " AND QuestionID=" + SavedQuestionId);
                if (chk.Rows.Count > 0 && Convert.ToInt32(chk.Rows[0]["C"]) == 0)
                    db.ExecuteNonQuery("INSERT INTO ExamQuestions (ExamID, QuestionID, Points) VALUES (" + linkExamId.Value + "," + SavedQuestionId + "," + pointsSql + ")");
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtQuestion.Text))
            {
                MessageBox.Show("Nh\u1eadp n\u1ed9i dung c\u00e2u h\u1ecfi.", "Th\u00f4ng b\u00e1o", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (cboQuestionType.SelectedIndex == 1)
            {
                if (string.IsNullOrWhiteSpace(cboCorrect.Text))
                {
                    MessageBox.Show("Nh\u1eadp \u0111\u00e1p \u00e1n \u0111\u00fang cho c\u00e2u \u0111i\u1ec1n khuy\u1ebft.", "Th\u00f4ng b\u00e1o", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                return true;
            }

            if (string.IsNullOrWhiteSpace(txtA.Text) || string.IsNullOrWhiteSpace(txtB.Text) ||
                string.IsNullOrWhiteSpace(txtC.Text) || string.IsNullOrWhiteSpace(txtD.Text))
            {
                MessageBox.Show("Nh\u1eadp \u0111\u1ea7y \u0111\u1ee7 4 \u0111\u00e1p \u00e1n.", "Th\u00f4ng b\u00e1o", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private static string Esc(string t)
        {
            return (t ?? "").Replace("'", "''");
        }
    }
}
