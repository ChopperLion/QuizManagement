using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QuizManagement.BUS;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class frmExam : Form
    {
        private readonly Database db = new Database();

        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblTimer;
        private Label lblProgress;
        private Label lblQuestion;
        private Label lblSidebarTitle;
        private Label lblSidebarHint;
        private Label lblAnswered;
        private Label lblPracticeFeedback;
        private Label lblPracticeMeter;
        private Label lblReviewHint;

        private RadioButton rdA;
        private RadioButton rdB;
        private RadioButton rdC;
        private RadioButton rdD;
        private TextBox txtFillAnswer;
        private Button btnCheckFillAnswer;

        private Button btnNext;
        private Button btnPrev;
        private Button btnSubmit;
        private Button btnMarkReview;

        private FlowLayoutPanel pnlQuestionNumber;
        private GroupBox grpAnswer;
        private Panel pnlHeaderCard;
        private Panel pnlQuestionCard;
        private Panel pnlSidebarCard;
        private Panel pnlFooterCard;

        private Timer timer1;

        private DataTable dtQuestion;
        private int currentQuestion = 0;
        private int timeLeft = 1800;
        private string[] userAnswers;
        private decimal[] earnedPointsByQuestion;
        private bool[] markedForReview;
        private bool[] practiceHadWrongBeforeCorrect;
        private bool[] practiceLockedQuestions;
        private int[] practiceWrongAttempts;
        private bool submitted = false;
        public bool HasNoQuestions { get; private set; } = false;

        private readonly int examID;
        private readonly int userID;
        private readonly string studentFullName;
        private readonly string studentClassName;
        private bool isPracticeMode;
        private decimal scoreScale = 10M;
        private int practiceMaxWrongPerQuestion;
        private bool practiceShowAnswer = true;
        private bool practiceShowPointGain = true;
        private bool practiceAllowStudentOptions = true;
        private bool practiceAutoNextOnCorrect;
        private decimal practiceLiveScore;
        private decimal loadedQuestionTotalPoints;

        public frmExam(int examID, int userID, string studentFullName, string studentClassName)
        {
            this.examID = examID;
            this.userID = userID;
            this.studentFullName = (studentFullName ?? "").Trim();
            this.studentClassName = (studentClassName ?? "").Trim();

            InitUI();
            LoadQuestion();

            if (dtQuestion.Rows.Count == 0)
            {
                HasNoQuestions = true;
                return;
            }

            if (isPracticeMode && practiceAllowStudentOptions && !ShowPracticeOptionsDialog())
            {
                HasNoQuestions = true;
                BeginInvoke((Action)Close);
                return;
            }

            CreateQuestionButtons();
            LoadCurrentQuestion();
            StartTimer();
            FormClosing += FrmExam_FormClosing;
        }

        private void InitUI()
        {
            Text = "Thi tr\u1eafc nghi\u1ec7m";
            Size = new Size(1320, 820);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.PageBg;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            pnlHeaderCard = new Panel
            {
                Location = new Point(28, 20),
                Size = new Size(1246, 116),
                BackColor = UiTheme.SurfaceRaised
            };
            pnlHeaderCard.Paint += (s, e) => UiTheme.PaintSoftCard(pnlHeaderCard, e, 22);
            UiTheme.AttachRoundedRegion(pnlHeaderCard, 22);
            Controls.Add(pnlHeaderCard);

            lblTitle = new Label
            {
                Text = "B\u00c0I THI TR\u1eaeC NGHI\u1ec6M",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(28, 18)
            };
            pnlHeaderCard.Controls.Add(lblTitle);

            lblSubtitle = new Label
            {
                Text = BuildSubtitle(),
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Location = new Point(31, 62)
            };
            pnlHeaderCard.Controls.Add(lblSubtitle);

            lblTimer = new Label
            {
                Text = "30:00",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 38, 38),
                AutoSize = true,
                Location = new Point(1110, 24)
            };
            pnlHeaderCard.Controls.Add(lblTimer);

            var lblTimerHint = new Label
            {
                Text = "Th\u1eddi gian c\u00f2n l\u1ea1i",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoSize = true,
                Location = new Point(1080, 68)
            };
            pnlHeaderCard.Controls.Add(lblTimerHint);

            pnlQuestionCard = new Panel
            {
                Location = new Point(28, 154),
                Size = new Size(878, 546),
                BackColor = UiTheme.SurfaceRaised
            };
            pnlQuestionCard.Paint += (s, e) => UiTheme.PaintSoftCard(pnlQuestionCard, e, 22);
            UiTheme.AttachRoundedRegion(pnlQuestionCard, 22);
            Controls.Add(pnlQuestionCard);

            lblProgress = new Label
            {
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235),
                AutoSize = true,
                Location = new Point(28, 24)
            };
            pnlQuestionCard.Controls.Add(lblProgress);

            lblQuestion = new Label
            {
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                MaximumSize = new Size(820, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(28, 62)
            };
            pnlQuestionCard.Controls.Add(lblQuestion);

            lblPracticeFeedback = new Label
            {
                Font = new Font("Segoe UI", 10F),
                MaximumSize = new Size(820, 0),
                AutoSize = true,
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(28, 122),
                Visible = false
            };
            pnlQuestionCard.Controls.Add(lblPracticeFeedback);

            lblPracticeMeter = new Label
            {
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                AutoSize = true,
                Location = new Point(650, 24),
                Visible = false
            };
            pnlQuestionCard.Controls.Add(lblPracticeMeter);

            grpAnswer = new GroupBox
            {
                Text = "\u0110\u00c1P \u00c1N",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Size = new Size(822, 300),
                Location = new Point(28, 170),
                BackColor = UiTheme.Surface
            };
            pnlQuestionCard.Controls.Add(grpAnswer);

            rdA = CreateRadioButton(48);
            rdB = CreateRadioButton(102);
            rdC = CreateRadioButton(156);
            rdD = CreateRadioButton(210);
            rdA.CheckedChanged += PracticeAnswer_CheckedChanged;
            rdB.CheckedChanged += PracticeAnswer_CheckedChanged;
            rdC.CheckedChanged += PracticeAnswer_CheckedChanged;
            rdD.CheckedChanged += PracticeAnswer_CheckedChanged;
            grpAnswer.Controls.AddRange(new Control[] { rdA, rdB, rdC, rdD });

            txtFillAnswer = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                Location = new Point(22, 52),
                Size = new Size(760, 96),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                WordWrap = true,
                AcceptsReturn = true,
                Visible = false
            };
            txtFillAnswer.KeyDown += TxtFillAnswer_KeyDown;
            grpAnswer.Controls.Add(txtFillAnswer);

            btnCheckFillAnswer = new Button
            {
                Text = "Kiểm tra",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(648, 164),
                Size = new Size(134, 40),
                Visible = false
            };
            UiTheme.ApplyButton(btnCheckFillAnswer, UiButtonVariant.Primary);
            btnCheckFillAnswer.Click += BtnCheckFillAnswer_Click;
            grpAnswer.Controls.Add(btnCheckFillAnswer);

            pnlFooterCard = new Panel
            {
                Location = new Point(28, 716),
                Size = new Size(878, 62),
                BackColor = Color.Transparent
            };
            Controls.Add(pnlFooterCard);

            btnPrev = CreateActionButton("C\u00e2u tr\u01b0\u1edbc", new Point(0, 8), Color.FromArgb(51, 65, 85));
            btnNext = CreateActionButton("C\u00e2u ti\u1ebfp", new Point(168, 8), Color.FromArgb(37, 99, 235));
            btnMarkReview = CreateActionButton("Đánh dấu xem lại", new Point(336, 8), Color.FromArgb(217, 119, 6));
            btnSubmit = CreateActionButton("N\u1ed9p b\u00e0i", new Point(696, 8), Color.FromArgb(185, 28, 28));
            btnPrev.Click += BtnPrev_Click;
            btnNext.Click += BtnNext_Click;
            btnMarkReview.Click += BtnMarkReview_Click;
            btnSubmit.Click += BtnSubmit_Click;
            pnlFooterCard.Controls.AddRange(new Control[] { btnPrev, btnNext, btnMarkReview, btnSubmit });

            pnlSidebarCard = new Panel
            {
                Location = new Point(926, 154),
                Size = new Size(348, 624),
                BackColor = UiTheme.SurfaceRaised
            };
            pnlSidebarCard.Paint += (s, e) => UiTheme.PaintSoftCard(pnlSidebarCard, e, 22);
            UiTheme.AttachRoundedRegion(pnlSidebarCard, 22);
            Controls.Add(pnlSidebarCard);

            lblSidebarTitle = new Label
            {
                Text = "DANH S\u00c1CH C\u00c2U",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                AutoSize = true,
                Location = new Point(24, 24)
            };
            pnlSidebarCard.Controls.Add(lblSidebarTitle);

            lblSidebarHint = new Label
            {
                Text = "M\u00e0u xanh l\u00e0 c\u00e2u \u0111\u00e3 tr\u1ea3 l\u1eddi. B\u1ea5m v\u00e0o t\u1eebng s\u1ed1 \u0111\u1ec3 chuy\u1ec3n nhanh.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = false,
                Size = new Size(300, 40),
                Location = new Point(24, 54)
            };
            pnlSidebarCard.Controls.Add(lblSidebarHint);

            lblAnswered = new Label
            {
                Text = "\u0110\u00e3 tr\u1ea3 l\u1eddi: 0/0",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                AutoSize = true,
                Location = new Point(24, 104)
            };
            pnlSidebarCard.Controls.Add(lblAnswered);

            lblReviewHint = new Label
            {
                Text = "Đánh dấu xem lại: 0",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                AutoSize = true,
                Location = new Point(170, 104)
            };
            pnlSidebarCard.Controls.Add(lblReviewHint);

            pnlQuestionNumber = new FlowLayoutPanel
            {
                Size = new Size(300, 468),
                Location = new Point(24, 142),
                AutoScroll = true,
                BackColor = UiTheme.Surface,
                Padding = new Padding(10),
                WrapContents = true
            };
            pnlQuestionNumber.Paint += (s, e) => UiTheme.PaintInsetSurface(pnlQuestionNumber, e, 18);
            pnlSidebarCard.Controls.Add(pnlQuestionNumber);
        }

        private RadioButton CreateRadioButton(int top)
        {
            return new RadioButton
            {
                Font = new Font("Segoe UI", 11.5F),
                AutoSize = false,
                Size = new Size(760, 36),
                Location = new Point(22, top),
                ForeColor = UiTheme.TextPrimary
            };
        }

        private Button CreateActionButton(string text, Point location, Color bg)
        {
            Button button = new Button
            {
                Text = text,
                Size = new Size(150, 46),
                Location = location,
                BackColor = bg,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };
            UiTheme.ApplyButton(button,
                bg == Color.FromArgb(51, 65, 85) ? UiButtonVariant.Secondary :
                bg == Color.FromArgb(217, 119, 6) ? UiButtonVariant.Warning :
                bg == Color.FromArgb(185, 28, 28) ? UiButtonVariant.Danger :
                UiButtonVariant.Primary);
            return button;
        }

        private void LoadQuestion()
        {
            Database.EnsureExamAccessColumns();
            Database.EnsureAdvancedExamColumns();

            DataTable dtExam = db.ExecuteQuery(
                "SELECT Duration, IsRandomQuestion, RandomQuestionCount, ExamMode, ScoreScale, PracticeMaxWrongPerQuestion, PracticeShowAnswer, PracticeShowPointGain, PracticeAutoNextOnCorrect, PracticeAllowStudentOptions FROM Exams WHERE ExamID = " + examID
            );

            bool randomQuestion = false;
            int randomQuestionCount = 0;
            if (dtExam.Rows.Count > 0)
            {
                timeLeft = Convert.ToInt32(dtExam.Rows[0]["Duration"]) * 60;
                if (dtExam.Columns.Contains("IsRandomQuestion") && dtExam.Rows[0]["IsRandomQuestion"] != DBNull.Value)
                    randomQuestion = Convert.ToBoolean(dtExam.Rows[0]["IsRandomQuestion"]);
                if (dtExam.Columns.Contains("RandomQuestionCount") && dtExam.Rows[0]["RandomQuestionCount"] != DBNull.Value)
                    randomQuestionCount = Convert.ToInt32(dtExam.Rows[0]["RandomQuestionCount"]);
                if (dtExam.Columns.Contains("ExamMode") && dtExam.Rows[0]["ExamMode"] != DBNull.Value)
                    isPracticeMode = string.Equals(dtExam.Rows[0]["ExamMode"].ToString(), "Practice", StringComparison.OrdinalIgnoreCase);
                if (dtExam.Columns.Contains("ScoreScale") && dtExam.Rows[0]["ScoreScale"] != DBNull.Value)
                    scoreScale = Math.Max(1M, Convert.ToDecimal(dtExam.Rows[0]["ScoreScale"]));
                if (dtExam.Columns.Contains("PracticeMaxWrongPerQuestion") && dtExam.Rows[0]["PracticeMaxWrongPerQuestion"] != DBNull.Value)
                    practiceMaxWrongPerQuestion = Math.Max(0, Convert.ToInt32(dtExam.Rows[0]["PracticeMaxWrongPerQuestion"]));
                if (dtExam.Columns.Contains("PracticeShowAnswer") && dtExam.Rows[0]["PracticeShowAnswer"] != DBNull.Value)
                    practiceShowAnswer = Convert.ToBoolean(dtExam.Rows[0]["PracticeShowAnswer"]);
                if (dtExam.Columns.Contains("PracticeShowPointGain") && dtExam.Rows[0]["PracticeShowPointGain"] != DBNull.Value)
                    practiceShowPointGain = Convert.ToBoolean(dtExam.Rows[0]["PracticeShowPointGain"]);
                if (dtExam.Columns.Contains("PracticeAutoNextOnCorrect") && dtExam.Rows[0]["PracticeAutoNextOnCorrect"] != DBNull.Value)
                    practiceAutoNextOnCorrect = Convert.ToBoolean(dtExam.Rows[0]["PracticeAutoNextOnCorrect"]);
                if (dtExam.Columns.Contains("PracticeAllowStudentOptions") && dtExam.Rows[0]["PracticeAllowStudentOptions"] != DBNull.Value)
                    practiceAllowStudentOptions = Convert.ToBoolean(dtExam.Rows[0]["PracticeAllowStudentOptions"]);
            }

            string topClause = randomQuestion && randomQuestionCount > 0 ? "TOP " + randomQuestionCount + " " : "";
            string query =
                "SELECT " + topClause + "q.QuestionID, q.QuestionContent, q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.CorrectAnswer, ISNULL(eq.Points, q.Points) AS Points, " +
                "CASE WHEN COL_LENGTH('Questions','QuestionType') IS NULL THEN N'MultipleChoice' ELSE q.QuestionType END AS QuestionType " +
                "FROM ExamQuestions eq INNER JOIN Questions q ON eq.QuestionID = q.QuestionID " +
                "WHERE eq.ExamID = " + examID + " " +
                (randomQuestion ? "ORDER BY NEWID()" : "ORDER BY q.QuestionID");

            dtQuestion = db.ExecuteQuery(query);
            loadedQuestionTotalPoints = 0M;
            foreach (DataRow questionRow in dtQuestion.Rows)
                loadedQuestionTotalPoints += GetQuestionPoints(questionRow);
            userAnswers = new string[dtQuestion.Rows.Count];
            earnedPointsByQuestion = new decimal[dtQuestion.Rows.Count];
            markedForReview = new bool[dtQuestion.Rows.Count];
            practiceHadWrongBeforeCorrect = new bool[dtQuestion.Rows.Count];
            practiceLockedQuestions = new bool[dtQuestion.Rows.Count];
            practiceWrongAttempts = new int[dtQuestion.Rows.Count];
        }

        private void LoadCurrentQuestion()
        {
            DataRow row = dtQuestion.Rows[currentQuestion];

            lblProgress.Text = $"C\u00e2u {currentQuestion + 1}/{dtQuestion.Rows.Count}";
            lblQuestion.Text = row["QuestionContent"].ToString();
            bool fillBlank = IsFillBlank(row);
            lblPracticeFeedback.Visible = false;
            lblPracticeFeedback.Text = "";

            rdA.Visible = rdB.Visible = rdC.Visible = rdD.Visible = !fillBlank;
            txtFillAnswer.Visible = fillBlank;
            btnCheckFillAnswer.Visible = fillBlank && isPracticeMode;
            if (fillBlank)
            {
                grpAnswer.Text = "\u0110I\u1ec0N \u0110\u00c1P \u00c1N";
                txtFillAnswer.Text = userAnswers[currentQuestion] ?? "";
            }
            else
            {
                grpAnswer.Text = "CH\u1eccN M\u1ed8T \u0110\u00c1P \u00c1N";
                rdA.Text = "A. " + row["OptionA"];
                rdB.Text = "B. " + row["OptionB"];
                rdC.Text = "C. " + row["OptionC"];
                rdD.Text = "D. " + row["OptionD"];
            }

            LoadSelectedAnswer();
            UpdatePracticeQuestionState();
            UpdateQuestionButtonColor();
            HighlightCurrentQuestion();
            UpdateNavigationState();
            UpdateSidebarSummary();
            UpdatePracticeMeter();
            if (btnMarkReview != null)
                btnMarkReview.BackColor = markedForReview != null && markedForReview[currentQuestion]
                    ? Color.FromArgb(180, 83, 9)
                    : Color.FromArgb(217, 119, 6);
        }

        private void LoadSelectedAnswer()
        {
            rdA.Checked = rdB.Checked = rdC.Checked = rdD.Checked = false;
            if (IsFillBlank(dtQuestion.Rows[currentQuestion]))
            {
                txtFillAnswer.Text = userAnswers[currentQuestion] ?? "";
                return;
            }

            switch (userAnswers[currentQuestion])
            {
                case "A": rdA.Checked = true; break;
                case "B": rdB.Checked = true; break;
                case "C": rdC.Checked = true; break;
                case "D": rdD.Checked = true; break;
            }
        }

        private void SaveAnswer()
        {
            if (isPracticeMode && practiceLockedQuestions[currentQuestion])
            {
                UpdateSidebarSummary();
                return;
            }

            if (IsFillBlank(dtQuestion.Rows[currentQuestion]))
            {
                userAnswers[currentQuestion] = txtFillAnswer.Text.Trim();
            }
            else if (rdA.Checked) userAnswers[currentQuestion] = "A";
            else if (rdB.Checked) userAnswers[currentQuestion] = "B";
            else if (rdC.Checked) userAnswers[currentQuestion] = "C";
            else if (rdD.Checked) userAnswers[currentQuestion] = "D";

            UpdateQuestionButtonColor();
            UpdateSidebarSummary();
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            SaveAnswer();
            if (currentQuestion < dtQuestion.Rows.Count - 1)
            {
                currentQuestion++;
                LoadCurrentQuestion();
            }
        }

        private void BtnPrev_Click(object sender, EventArgs e)
        {
            SaveAnswer();
            if (currentQuestion > 0)
            {
                currentQuestion--;
                LoadCurrentQuestion();
            }
        }

        private void StartTimer()
        {
            timer1 = new Timer();
            timer1.Interval = 1000;
            timer1.Tick += Timer1_Tick;
            timer1.Start();
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            timeLeft--;

            int m = timeLeft / 60;
            int s = timeLeft % 60;
            lblTimer.Text = $"{m:00}:{s:00}";

            if (timeLeft <= 0)
            {
                timer1.Stop();
                SubmitExam();
            }
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (submitted) return;

            int unanswered = CountUnansweredQuestions();
            int reviewMarked = CountMarkedForReview();
            string message = "B\u1ea1n c\u00f3 ch\u1eafc mu\u1ed1n n\u1ed9p b\u00e0i ngay b\u00e2y gi\u1edd?";
            if (unanswered > 0 || reviewMarked > 0)
            {
                message += Environment.NewLine + Environment.NewLine +
                    "- Câu chưa trả lời: " + unanswered + Environment.NewLine +
                    "- Câu đã đánh dấu xem lại: " + reviewMarked;
            }

            if (MessageBox.Show(message, "X\u00e1c nh\u1eadn n\u1ed9p b\u00e0i",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                SubmitExam();
            }
        }

        private void FrmExam_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (submitted || HasNoQuestions || dtQuestion == null || dtQuestion.Rows.Count == 0)
                return;

            e.Cancel = MessageBox.Show(
                "Bạn đang làm bài. Nếu đóng cửa sổ lúc này, bài sẽ được nộp ngay với các đáp án hiện có.\nBạn có muốn nộp và đóng không?",
                "Xác nhận rời bài thi",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) != DialogResult.Yes;
            if (!e.Cancel)
                SubmitExam();
        }

        private void SubmitExam()
        {
            if (submitted) return;

            submitted = true;
            SaveAnswer();
            timer1?.Stop();

            int correct = 0;
            decimal totalPoints = 0M;
            decimal earnedPoints = 0M;

            for (int i = 0; i < dtQuestion.Rows.Count; i++)
            {
                DataRow row = dtQuestion.Rows[i];
                decimal questionPoints = GetQuestionPoints(row);
                totalPoints += questionPoints;

                bool ok = IsCorrectAnswer(userAnswers[i], row["CorrectAnswer"].ToString(), IsFillBlank(row));
                decimal awardedPoints = CalculateAwardedPoints(row, userAnswers[i], i);
                if (ok)
                {
                    correct++;
                }

                earnedPoints += awardedPoints;
            }

            double score = totalPoints <= 0 ? 0 : Math.Round((double)(earnedPoints / totalPoints * scoreScale), 2);

            db.ExecuteNonQuery(
                "INSERT INTO Results (UserID, ExamID, Score, SubmitDate, StudentFullName, StudentClass) VALUES (" +
                userID + "," + examID + "," + score.ToString().Replace(",", ".") + ",GETDATE()," +
                "N'" + SqlText(studentFullName) + "',N'" + SqlText(studentClassName) + "')"
            );
            int resultId = GetLatestResultId();
            SaveAttemptAnswers(resultId);

            new frmResult(correct, dtQuestion.Rows.Count, Math.Round(score, 2), earnedPoints, totalPoints, BuildReviewRows())
                .ShowDialog();

            Close();
        }

        private string BuildSubtitle()
        {
            string identity = "";
            if (!string.IsNullOrWhiteSpace(studentFullName))
                identity = " | Thí sinh: " + studentFullName;
            if (!string.IsNullOrWhiteSpace(studentClassName))
                identity += " - Lớp " + studentClassName;

            return "Theo dõi thời gian, chuyển câu linh hoạt và nộp bài ngay trong một màn hình." + identity;
        }

        private bool IsFillBlank(DataRow row)
        {
            return row.Table.Columns.Contains("QuestionType") && row["QuestionType"].ToString() == "FillBlank";
        }

        private bool IsCorrectAnswer(string userAnswer, string correctAnswer, bool fillBlank)
        {
            if (!fillBlank) return userAnswer == correctAnswer;
            return FillBlankScoringService.Evaluate(userAnswer, correctAnswer).IsCorrect;
        }

        private decimal GetQuestionPoints(DataRow row)
        {
            if (row != null && row.Table.Columns.Contains("Points") && row["Points"] != DBNull.Value)
            {
                decimal value = Convert.ToDecimal(row["Points"]);
                if (value > 0) return value;
            }
            return 1M;
        }

        private int GetLatestResultId()
        {
            try
            {
                DataTable dt = db.ExecuteQuery("SELECT TOP 1 ResultID FROM Results WHERE UserID=" + userID + " AND ExamID=" + examID + " ORDER BY SubmitDate DESC, ResultID DESC");
                if (dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["ResultID"]);
            }
            catch { }
            return 0;
        }

        private void SaveAttemptAnswers(int resultId)
        {
            try
            {
                for (int i = 0; i < dtQuestion.Rows.Count; i++)
                {
                    DataRow r = dtQuestion.Rows[i];
                    bool ok = IsCorrectAnswer(userAnswers[i], r["CorrectAnswer"].ToString(), IsFillBlank(r));
                    decimal questionPoints = GetQuestionPoints(r);
                    decimal awardedPoints = CalculateAwardedPoints(r, userAnswers[i], i);
                    db.ExecuteNonQuery(
                        "INSERT INTO ExamAttemptAnswers (ResultID,UserID,ExamID,QuestionID,QuestionOrder,QuestionContent,OptionA,OptionB,OptionC,OptionD,CorrectAnswer,UserAnswer,IsCorrect,QuestionPoints,AwardedPoints) VALUES (" +
                        (resultId > 0 ? resultId.ToString() : "NULL") + "," + userID + "," + examID + "," + r["QuestionID"] + "," + (i + 1) + "," +
                        "N'" + SqlText(r["QuestionContent"].ToString()) + "',N'" + SqlText(r["OptionA"].ToString()) + "',N'" + SqlText(r["OptionB"].ToString()) + "',N'" + SqlText(r["OptionC"].ToString()) + "',N'" + SqlText(r["OptionD"].ToString()) + "'," +
                        "N'" + SqlText(r["CorrectAnswer"].ToString()) + "',N'" + SqlText(userAnswers[i] ?? "") + "'," + (ok ? "1" : "0") + "," +
                        questionPoints.ToString().Replace(",", ".") + "," + awardedPoints.ToString().Replace(",", ".") + ")");
                }
            }
            catch { }
        }

        private System.Collections.Generic.IReadOnlyList<ResultReviewRow> BuildReviewRows()
        {
            var rows = new System.Collections.Generic.List<ResultReviewRow>();
            for (int i = 0; i < dtQuestion.Rows.Count; i++)
            {
                DataRow r = dtQuestion.Rows[i];
                bool fillBlank = IsFillBlank(r);
                bool ok = IsCorrectAnswer(userAnswers[i], r["CorrectAnswer"].ToString(), fillBlank);
                decimal questionPoints = GetQuestionPoints(r);
                decimal awardedPoints = CalculateAwardedPoints(r, userAnswers[i], i);
                var fillEvaluation = fillBlank ? FillBlankScoringService.Evaluate(userAnswers[i], r["CorrectAnswer"].ToString()) : null;
                string userAnswer = string.IsNullOrWhiteSpace(userAnswers[i]) ? "(\u1ecf tr\u1ed1ng)" : userAnswers[i];
                if (!fillBlank && !string.IsNullOrWhiteSpace(userAnswers[i]))
                {
                    string optionText = r["Option" + userAnswers[i]].ToString();
                    userAnswer = userAnswers[i] + ". " + optionText;
                }

                string correctAnswer = r["CorrectAnswer"].ToString();
                if (!fillBlank && !string.IsNullOrWhiteSpace(correctAnswer))
                {
                    string optionText = r["Option" + correctAnswer].ToString();
                    correctAnswer = correctAnswer + ". " + optionText;
                }

                rows.Add(new ResultReviewRow
                {
                    Number = i + 1,
                    QuestionContent = r["QuestionContent"].ToString(),
                    UserAnswer = userAnswer,
                    CorrectAnswer = correctAnswer,
                    IsCorrect = ok,
                    IsFillBlank = fillBlank,
                    MaxPoints = questionPoints,
                    AwardedPoints = awardedPoints,
                    SuggestedScoreRatio = fillEvaluation == null ? (ok ? 1M : 0M) : fillEvaluation.ScoreRatio,
                    MatchedKeywords = fillEvaluation == null ? "" : fillEvaluation.MatchedKeywords
                });
            }
            return rows;
        }

        private static string SqlText(string text) => (text ?? "").Replace("'", "''");

        private void CreateQuestionButtons()
        {
            pnlQuestionNumber.Controls.Clear();

            for (int i = 0; i < dtQuestion.Rows.Count; i++)
            {
                Button btn = new Button
                {
                    Size = new Size(58, 46),
                    Text = (i + 1).ToString(),
                    Tag = i,
                    Margin = new Padding(8),
                    BackColor = Color.FromArgb(226, 232, 240),
                    ForeColor = Color.FromArgb(30, 41, 59),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += QuestionButton_Click;
                pnlQuestionNumber.Controls.Add(btn);
            }
        }

        private void QuestionButton_Click(object sender, EventArgs e)
        {
            SaveAnswer();
            currentQuestion = (int)((Button)sender).Tag;
            LoadCurrentQuestion();
        }

        private void BtnMarkReview_Click(object sender, EventArgs e)
        {
            if (markedForReview == null || currentQuestion < 0 || currentQuestion >= markedForReview.Length)
                return;

            markedForReview[currentQuestion] = !markedForReview[currentQuestion];
            btnMarkReview.BackColor = markedForReview[currentQuestion]
                ? Color.FromArgb(180, 83, 9)
                : Color.FromArgb(217, 119, 6);
            UpdateQuestionButtonColor();
            UpdateSidebarSummary();
        }

        private bool ShowPracticeOptionsDialog()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "Tùy chọn luyện đề";
                dlg.Size = new Size(500, 370);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.Font = Font;
                dlg.BackColor = UiTheme.SurfaceRaised;

                var lblWrong = new Label { Text = "Được phép chọn sai tối đa mỗi câu:", Location = new Point(20, 24), AutoSize = true };
                var nudWrong = new NumericUpDown { Location = new Point(20, 50), Width = 100, Minimum = 0, Maximum = 5, Value = practiceMaxWrongPerQuestion };
                var chkShowAnswer = new CheckBox { Text = "Hiện đáp án sau khi câu hỏi được chốt", Location = new Point(20, 92), AutoSize = true, Checked = practiceShowAnswer };
                var chkShowPoint = new CheckBox { Text = "Hiện điểm cộng dần khi trả lời đúng ngay", Location = new Point(20, 124), AutoSize = true, Checked = practiceShowPointGain };
                var chkAutoNext = new CheckBox { Text = "Tự chuyển sang câu tiếp theo khi chọn đúng", Location = new Point(20, 156), AutoSize = true, Checked = practiceAutoNextOnCorrect };
                var lblScale = new Label { Text = "Thang điểm áp dụng: " + scoreScale.ToString("0.##"), Location = new Point(20, 194), AutoSize = true, ForeColor = Color.FromArgb(71, 85, 105) };
                var lblHint = new Label
                {
                    Text = "Câu điền khuyết có thể được chấm gần đúng. Nếu sửa lại đúng trong số lượt cho phép thì vẫn được tính điểm.",
                    Location = new Point(20, 220),
                    Size = new Size(400, 52),
                    ForeColor = Color.FromArgb(100, 116, 139)
                };
                var btnOk = new Button { Text = "Bắt đầu luyện", DialogResult = DialogResult.OK, Location = new Point(236, 294), Size = new Size(130, 36) };
                var btnCancel = new Button { Text = "Hủy", DialogResult = DialogResult.Cancel, Location = new Point(376, 294), Size = new Size(84, 36) };

                UiTheme.ApplyButton(btnOk, UiButtonVariant.Primary);
                UiTheme.ApplyButton(btnCancel, UiButtonVariant.Secondary);
                dlg.Controls.AddRange(new Control[] { lblWrong, nudWrong, chkShowAnswer, chkShowPoint, chkAutoNext, lblScale, lblHint, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return false;

                practiceMaxWrongPerQuestion = (int)nudWrong.Value;
                practiceShowAnswer = chkShowAnswer.Checked;
                practiceShowPointGain = chkShowPoint.Checked;
                practiceAutoNextOnCorrect = chkAutoNext.Checked;
                return true;
            }
        }

        private void PracticeAnswer_CheckedChanged(object sender, EventArgs e)
        {
            if (!isPracticeMode || submitted) return;
            if (!(sender is RadioButton radio) || !radio.Checked) return;
            if (currentQuestion < 0 || currentQuestion >= dtQuestion.Rows.Count) return;
            if (IsFillBlank(dtQuestion.Rows[currentQuestion])) return;
            if (practiceLockedQuestions[currentQuestion])
            {
                UpdatePracticeQuestionState();
                return;
            }

            string answer = radio == rdA ? "A" : radio == rdB ? "B" : radio == rdC ? "C" : "D";
            userAnswers[currentQuestion] = answer;
            HandlePracticeAnswerSelection(answer);
            UpdateQuestionButtonColor();
            UpdateSidebarSummary();
            UpdatePracticeMeter();
        }

        private void HandlePracticeAnswerSelection(string answer)
        {
            DataRow row = dtQuestion.Rows[currentQuestion];
            bool correct = IsCorrectAnswer(answer, row["CorrectAnswer"].ToString(), false);
            decimal questionPoints = GetQuestionPoints(row);

            if (correct)
            {
                bool correctedAfterRetry = practiceWrongAttempts[currentQuestion] > 0;
                earnedPointsByQuestion[currentQuestion] = questionPoints;
                practiceHadWrongBeforeCorrect[currentQuestion] = correctedAfterRetry;
                practiceLockedQuestions[currentQuestion] = true;
                if (!correctedAfterRetry && practiceShowPointGain)
                    ShowPracticeFeedback("Chọn đúng rồi! +" + questionPoints.ToString("0.##") + " điểm.", false);
                else if (correctedAfterRetry)
                    ShowPracticeFeedback(BuildPracticeRevealMessage("Bạn đã chọn đúng sau khi thử lại và vẫn được tính điểm.", row), false);
                else
                    ShowPracticeFeedback(BuildPracticeRevealMessage("Chọn đúng rồi.", row), false);
            }
            else
            {
                practiceWrongAttempts[currentQuestion]++;
                int remaining = practiceMaxWrongPerQuestion - practiceWrongAttempts[currentQuestion];
                practiceHadWrongBeforeCorrect[currentQuestion] = true;

                if (remaining > 0)
                {
                    ShowPracticeFeedback("Sai rùi, bạn còn tối đa " + remaining + " lần chọn lại.", true);
                }
                else
                {
                    earnedPointsByQuestion[currentQuestion] = 0M;
                    practiceLockedQuestions[currentQuestion] = true;
                    ShowPracticeFeedback(BuildPracticeRevealMessage("Bạn đã dùng hết số lần chọn lại, câu này không được tính điểm.", row), true);
                }
            }

            UpdatePracticeQuestionState();
            if (correct) AutoMoveToNextQuestionIfNeeded();
        }

        private void AutoMoveToNextQuestionIfNeeded()
        {
            if (!practiceAutoNextOnCorrect || submitted) return;
            if (currentQuestion >= dtQuestion.Rows.Count - 1) return;

            Timer moveTimer = new Timer { Interval = 450 };
            moveTimer.Tick += (s, e) =>
            {
                moveTimer.Stop();
                moveTimer.Dispose();
                if (submitted || currentQuestion >= dtQuestion.Rows.Count - 1) return;
                currentQuestion++;
                LoadCurrentQuestion();
            };
            moveTimer.Start();
        }

        private string BuildPracticeRevealMessage(string message, DataRow row)
        {
            if (practiceShowAnswer)
                return message + " Đáp án đúng: " + row["CorrectAnswer"];
            return message;
        }

        private void ShowPracticeFeedback(string text, bool danger)
        {
            lblPracticeFeedback.Text = text;
            lblPracticeFeedback.ForeColor = danger ? Color.FromArgb(220, 38, 38) : Color.FromArgb(22, 101, 52);
            lblPracticeFeedback.Visible = true;
        }

        private void UpdatePracticeQuestionState()
        {
            if (!isPracticeMode)
            {
                lblPracticeFeedback.Visible = false;
                lblPracticeMeter.Visible = false;
                txtFillAnswer.Enabled = true;
                btnCheckFillAnswer.Enabled = true;
                return;
            }

            bool fillBlank = IsFillBlank(dtQuestion.Rows[currentQuestion]);
            bool locked = practiceLockedQuestions[currentQuestion];
            rdA.Enabled = rdB.Enabled = rdC.Enabled = rdD.Enabled = !(locked && !fillBlank);
            txtFillAnswer.Enabled = !(locked && fillBlank);
            btnCheckFillAnswer.Enabled = !(locked && fillBlank);
        }

        private void UpdatePracticeMeter()
        {
            if (!isPracticeMode)
            {
                lblPracticeMeter.Visible = false;
                return;
            }

            practiceLiveScore = 0M;
            foreach (decimal point in earnedPointsByQuestion)
                practiceLiveScore += point;

            lblPracticeMeter.Visible = practiceShowPointGain;
            if (practiceShowPointGain)
                lblPracticeMeter.Text = "Điểm cộng dần: " + practiceLiveScore.ToString("0.##");
            if (practiceShowPointGain)
                lblPracticeMeter.Text = "Điểm cộng dần: " + ToDisplayScalePoints(practiceLiveScore).ToString("0.##");
        }

        private void UpdateQuestionButtonColor()
        {
            for (int i = 0; i < pnlQuestionNumber.Controls.Count; i++)
            {
                Button btn = (Button)pnlQuestionNumber.Controls[i];
                bool answered = !string.IsNullOrWhiteSpace(userAnswers[i]);
                if (isPracticeMode && practiceLockedQuestions[i])
                {
                    bool earned = earnedPointsByQuestion[i] > 0;
                    btn.BackColor = earned ? Color.FromArgb(187, 247, 208) : Color.FromArgb(254, 226, 226);
                    btn.ForeColor = earned ? Color.FromArgb(22, 101, 52) : Color.FromArgb(153, 27, 27);
                }
                else
                {
                    if (markedForReview != null && markedForReview[i])
                    {
                        btn.BackColor = Color.FromArgb(255, 237, 213);
                        btn.ForeColor = Color.FromArgb(154, 52, 18);
                    }
                    else
                    {
                        btn.BackColor = answered ? Color.FromArgb(187, 247, 208) : Color.FromArgb(226, 232, 240);
                        btn.ForeColor = answered ? Color.FromArgb(22, 101, 52) : Color.FromArgb(30, 41, 59);
                    }
                }
            }
        }

        private void HighlightCurrentQuestion()
        {
            for (int i = 0; i < pnlQuestionNumber.Controls.Count; i++)
            {
                Button btn = (Button)pnlQuestionNumber.Controls[i];
                bool active = i == currentQuestion;
                btn.FlatAppearance.BorderSize = active ? 2 : 0;
                btn.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
            }
        }

        private void UpdateNavigationState()
        {
            btnPrev.Enabled = currentQuestion > 0;
            btnNext.Enabled = currentQuestion < dtQuestion.Rows.Count - 1;
            btnPrev.BackColor = btnPrev.Enabled ? Color.FromArgb(51, 65, 85) : Color.FromArgb(148, 163, 184);
            btnNext.BackColor = btnNext.Enabled ? Color.FromArgb(37, 99, 235) : Color.FromArgb(148, 163, 184);
        }

        private void UpdateSidebarSummary()
        {
            int answered = 0;
            for (int i = 0; i < userAnswers.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(userAnswers[i]))
                    answered++;
            }
            lblAnswered.Text = $"\u0110\u00e3 tr\u1ea3 l\u1eddi: {answered}/{dtQuestion.Rows.Count}";
            if (lblReviewHint != null)
                lblReviewHint.Text = "Đánh dấu xem lại: " + CountMarkedForReview();
        }

        private int CountMarkedForReview()
        {
            if (markedForReview == null) return 0;
            int count = 0;
            foreach (bool item in markedForReview)
                if (item) count++;
            return count;
        }

        private int CountUnansweredQuestions()
        {
            if (userAnswers == null) return 0;
            int count = 0;
            foreach (string answer in userAnswers)
                if (string.IsNullOrWhiteSpace(answer)) count++;
            return count;
        }

        private decimal CalculateAwardedPoints(DataRow row, string answer, int questionIndex)
        {
            if (isPracticeMode)
                return earnedPointsByQuestion[questionIndex];

            decimal questionPoints = GetQuestionPoints(row);
            if (!IsFillBlank(row))
                return IsCorrectAnswer(answer, row["CorrectAnswer"].ToString(), false) ? questionPoints : 0M;

            var evaluation = FillBlankScoringService.Evaluate(answer, row["CorrectAnswer"].ToString());
            return Math.Round(questionPoints * evaluation.ScoreRatio, 2, MidpointRounding.AwayFromZero);
        }

        private void BtnCheckFillAnswer_Click(object sender, EventArgs e)
        {
            CheckFillBlankPracticeAnswer();
        }

        private void TxtFillAnswer_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isPracticeMode || !txtFillAnswer.Visible || !btnCheckFillAnswer.Visible) return;
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            CheckFillBlankPracticeAnswer();
        }

        private void CheckFillBlankPracticeAnswer()
        {
            if (!isPracticeMode || submitted || currentQuestion < 0 || currentQuestion >= dtQuestion.Rows.Count) return;
            DataRow row = dtQuestion.Rows[currentQuestion];
            if (!IsFillBlank(row) || practiceLockedQuestions[currentQuestion]) return;

            string answer = (txtFillAnswer.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(answer))
            {
                ShowPracticeFeedback("Hãy nhập đáp án trước khi kiểm tra.", true);
                return;
            }

            userAnswers[currentQuestion] = answer;
            var evaluation = FillBlankScoringService.Evaluate(answer, row["CorrectAnswer"].ToString());
            decimal questionPoints = GetQuestionPoints(row);
            if (evaluation.ScoreRatio > 0M)
            {
                earnedPointsByQuestion[currentQuestion] = Math.Round(questionPoints * evaluation.ScoreRatio, 2, MidpointRounding.AwayFromZero);
                practiceLockedQuestions[currentQuestion] = true;
                if (evaluation.IsCorrect)
                {
                    string prefix = practiceShowPointGain
                        ? "Đúng rồi! +" + earnedPointsByQuestion[currentQuestion].ToString("0.##") + " điểm."
                        : "Đúng rồi.";
                    if (practiceShowPointGain)
                        prefix = "Đúng rồi! +" + ToDisplayScalePoints(earnedPointsByQuestion[currentQuestion]).ToString("0.##") + " điểm.";
                    ShowPracticeFeedback(BuildPracticeRevealMessage(prefix, row), false);
                }
                else
                {
                    ShowPracticeFeedback(
                        BuildPracticeRevealMessage(
                            "Câu trả lời gần đúng, tạm cộng " + earnedPointsByQuestion[currentQuestion].ToString("0.##") +
                            "/" + questionPoints.ToString("0.##") + " điểm.", row),
                        false);
                    ShowPracticeFeedback(
                        BuildPracticeRevealMessage(
                            "Câu trả lời gần đúng, tạm cộng " + ToDisplayScalePoints(earnedPointsByQuestion[currentQuestion]).ToString("0.##") +
                            "/" + ToDisplayScalePoints(questionPoints).ToString("0.##") + " điểm.", row),
                        false);
                }
            }
            else
            {
                practiceWrongAttempts[currentQuestion]++;
                int remaining = practiceMaxWrongPerQuestion - practiceWrongAttempts[currentQuestion];
                if (remaining > 0)
                {
                    ShowPracticeFeedback("Chưa khớp lắm, bạn còn tối đa " + remaining + " lần nhập lại.", true);
                }
                else
                {
                    earnedPointsByQuestion[currentQuestion] = 0M;
                    practiceLockedQuestions[currentQuestion] = true;
                    ShowPracticeFeedback(BuildPracticeRevealMessage("Bạn đã dùng hết số lần nhập lại, câu này không được tính điểm.", row), true);
                }
            }

            UpdatePracticeQuestionState();
            UpdateQuestionButtonColor();
            UpdateSidebarSummary();
            UpdatePracticeMeter();
            if (evaluation.IsCorrect) AutoMoveToNextQuestionIfNeeded();
        }

        private decimal ToDisplayScalePoints(decimal rawPoints)
        {
            if (loadedQuestionTotalPoints <= 0 || scoreScale <= 0)
                return Math.Round(rawPoints, 2, MidpointRounding.AwayFromZero);

            return Math.Round((rawPoints / loadedQuestionTotalPoints) * scoreScale, 2, MidpointRounding.AwayFromZero);
        }
    }
}






