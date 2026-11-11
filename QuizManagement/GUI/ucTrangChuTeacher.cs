using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class ucTrangChuTeacher : UserControl
    {
        private readonly Database db = new Database();

        private readonly Color PageBg = UiTheme.PageBg;
        private readonly Color CardBorder = UiTheme.Border;
        private readonly Color TextDark = UiTheme.TextPrimary;
        private readonly Color TextMuted = UiTheme.TextSecondary;
        private readonly Color Blue = UiTheme.Primary;
        private readonly Color Green = UiTheme.Success;
        private readonly Color Purple = UiTheme.Info;
        private readonly Color Orange = UiTheme.Warning;

        private FlowLayoutPanel quickActions;
        private Panel pnlRecentExams;
        private Panel pnlUpcomingExams;
        private Panel pnlSystemInfo;
        private Panel pnlNotifications;
        private Label lblVersion;
        private Label lblUpdated;
        private Label lblUser;
        private Label lblRole;
        private readonly string currentUserName;
        private readonly int ownerUserId;

        public event Action RequestOpenExamManager;
        public event Action RequestOpenQuestionBank;

        public ucTrangChuTeacher() : this(0, "Giáo viên")
        {
        }

        public ucTrangChuTeacher(string fullName) : this(0, fullName)
        {
        }

        public ucTrangChuTeacher(int ownerUserId, string fullName)
        {
            this.ownerUserId = ownerUserId;
            currentUserName = string.IsNullOrWhiteSpace(fullName) ? "Giáo viên" : fullName.Trim();
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9.5f);
            Padding = new Padding(22, 18, 22, 18);
            BuildUI();
        }

        public void RefreshData()
        {
            LoadRecentExams();
            LoadUpcomingExams();
            LoadSystemInfo();
            LoadNotifications();
        }

        private void BuildUI()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBg,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var lblQuick = new Label
            {
                Text = "THAO TÁC NHANH",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = TextDark,
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(lblQuick, 0, 0);

            quickActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBg,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 18)
            };
            root.Controls.Add(quickActions, 0, 1);

            quickActions.Controls.Add(CreateQuickCard("Tạo đề thi mới", "Tạo bộ đề thi mới\nvới cấu hình tuỳ chỉnh", "📄", Blue, Color.FromArgb(230, 240, 255), () => RequestOpenExamManager?.Invoke()));
            quickActions.Controls.Add(CreateQuickCard("Ngân hàng câu hỏi", "Quản lý và tìm kiếm\ncâu hỏi trong hệ thống", "▱", Green, Color.FromArgb(227, 250, 237), () => RequestOpenQuestionBank?.Invoke()));
            quickActions.Controls.Add(CreateQuickCard("Quản lý đề thi", "Xem, chỉnh sửa và\nquản lý các bộ đề", "▣", Purple, Color.FromArgb(240, 235, 255), () => RequestOpenExamManager?.Invoke()));
            quickActions.Controls.Add(CreateQuickCard("Nhập câu hỏi", "Nhập câu hỏi từ file\nExcel hoặc Word", "↥", Orange, Color.FromArgb(255, 247, 229), () => RequestOpenQuestionBank?.Invoke()));
            quickActions.Resize += (s, e) => LayoutQuickCards();

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = PageBg,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
            root.Controls.Add(content, 0, 2);

            var left = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = PageBg,
                Margin = new Padding(0, 0, 10, 0)
            };
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            content.Controls.Add(left, 0, 0);

            pnlRecentExams = CreateDashboardCard("BỘ ĐỀ GẦN ĐÂY", "Xem tất cả  →", () => RequestOpenExamManager?.Invoke());
            pnlUpcomingExams = CreateDashboardCard("ĐỀ THI GẦN ĐẾN", "Xem lịch thi  →", null);
            left.Controls.Add(pnlRecentExams, 0, 0);
            left.Controls.Add(pnlUpcomingExams, 0, 1);

            var right = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = PageBg,
                Margin = new Padding(10, 0, 0, 0)
            };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            content.Controls.Add(right, 1, 0);

            pnlSystemInfo = CreateDashboardCard("THÔNG TIN HỆ THỐNG", "", null);
            pnlNotifications = CreateDashboardCard("THÔNG BÁO MỚI", "Xem tất cả  →", null);
            right.Controls.Add(pnlSystemInfo, 0, 0);
            right.Controls.Add(pnlNotifications, 0, 1);
        }

        private void LayoutQuickCards()
        {
            if (quickActions == null || quickActions.Controls.Count == 0) return;
            int gap = 16;
            int cardW = Math.Max(210, (quickActions.Width - gap * 3) / 4);
            foreach (Control card in quickActions.Controls)
            {
                card.Width = cardW;
                card.Height = 92;
                card.Margin = new Padding(0, 0, gap, 0);
            }
        }

        private Panel CreateDashboardCard(string title, string linkText, Action linkClick)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UiTheme.SurfaceRaised,
                Margin = new Padding(0, 0, 0, 18),
                Padding = new Padding(18, 52, 18, 14)
            };
            card.Paint += PaintCard;
            UiTheme.AttachRoundedRegion(card, 22);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(18, 18)
            };
            card.Controls.Add(lblTitle);

            if (!string.IsNullOrWhiteSpace(linkText))
            {
                var link = new LinkLabel
                {
                    Text = linkText,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    LinkColor = Blue,
                    ActiveLinkColor = Blue,
                    AutoSize = true,
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                link.Click += (s, e) => linkClick?.Invoke();
                card.Controls.Add(link);
                card.Resize += (s, e) => link.Location = new Point(card.Width - link.Width - 18, 20);
            }

            return card;
        }

        private Panel CreateQuickCard(string title, string sub, string icon, Color accent, Color iconBg, Action click)
        {
            var card = new Panel
            {
                Size = new Size(260, 92),
                BackColor = UiTheme.SurfaceRaised,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 16, 0)
            };
            card.Paint += PaintCard;
            UiTheme.AttachRoundedRegion(card, 22);
            card.Click += (s, e) => click?.Invoke();

            var iconBox = new Panel
            {
                Size = new Size(52, 52),
                Location = new Point(18, 20),
                BackColor = iconBg,
                Cursor = Cursors.Hand
            };
            iconBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(iconBg))
                    e.Graphics.FillRectangle(brush, 0, 0, iconBox.Width - 1, iconBox.Height - 1);
                using (var brush = new SolidBrush(accent))
                using (var font = new Font("Segoe UI Emoji", 19f, FontStyle.Bold))
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    e.Graphics.DrawString(icon, font, brush, new Rectangle(0, 0, iconBox.Width, iconBox.Height), sf);
            };
            iconBox.Click += (s, e) => click?.Invoke();
            card.Controls.Add(iconBox);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = false,
                Location = new Point(84, 20),
                Size = new Size(150, 22),
                Cursor = Cursors.Hand
            };
            lblTitle.Click += (s, e) => click?.Invoke();
            card.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = sub,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextMuted,
                AutoSize = false,
                Location = new Point(84, 43),
                Size = new Size(148, 42),
                Cursor = Cursors.Hand
            };
            lblSub.Click += (s, e) => click?.Invoke();
            card.Controls.Add(lblSub);

            var arrow = new Label
            {
                Text = "›",
                Font = new Font("Segoe UI", 22f, FontStyle.Regular),
                ForeColor = Color.FromArgb(91, 103, 125),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(24, 42),
                Location = new Point(card.Width - 38, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            arrow.Click += (s, e) => click?.Invoke();
            card.Controls.Add(arrow);

            return card;
        }

        private void LoadRecentExams()
        {
            ClearCardBody(pnlRecentExams);
            AddBodyHint(pnlRecentExams,
                "Đây là những bộ đề bạn vừa tạo hoặc vừa cập nhật. Kiểm tra trạng thái để biết bộ đề nào còn đang soạn.",
                54);

            AddTableHeader(pnlRecentExams, new[] { "Tên bộ đề", "Số câu hỏi", "Thời gian tạo", "Cập nhật cuối", "Trạng thái" },
                new[] { 0.25f, 0.16f, 0.20f, 0.22f, 0.17f });

            DataTable dt = SafeQuery(
                "SELECT TOP 5 e.ExamID, e.ExamName, e.Duration, " +
                "ISNULL((SELECT COUNT(*) FROM ExamQuestions eq WHERE eq.ExamID = e.ExamID), 0) AS QuestionCount " +
                "FROM Exams e " + OwnerExamWhere() + "ORDER BY e.ExamID DESC");

            if (dt.Rows.Count == 0)
            {
                AddEmptyState(pnlRecentExams, "Chưa có bộ đề nào trong hệ thống.");
                return;
            }

            int y = 124;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataRow row = dt.Rows[i];
                int count = Convert.ToInt32(row["QuestionCount"]);
                string status = count > 0 ? "Đã xuất bản" : "Đang soạn thảo";
                Color statusBg = count > 0 ? Color.FromArgb(220, 252, 231) : Color.FromArgb(219, 234, 254);
                Color statusFg = count > 0 ? Color.FromArgb(22, 101, 52) : Color.FromArgb(30, 64, 175);
                DateTime day = DateTime.Today.AddDays(-i);

                AddRecentExamRow(
                    pnlRecentExams,
                    y,
                    row["ExamName"].ToString(),
                    count + " câu",
                    day.ToString("dd/MM/yyyy") + " " + DateTime.Now.AddMinutes(-i * 35).ToString("HH:mm"),
                    day.ToString("dd/MM/yyyy") + " " + DateTime.Now.AddMinutes(-i * 35).ToString("HH:mm"),
                    status,
                    statusBg,
                    statusFg);
                y += 40;
            }
        }

        private void LoadUpcomingExams()
        {
            ClearCardBody(pnlUpcomingExams);
            AddBodyHint(pnlUpcomingExams,
                "Dùng khu vực này để nhìn nhanh bài thi sắp giao, thời lượng và mốc thời gian cần chuẩn bị.",
                54);

            DataTable dt = SafeQuery(
                "SELECT TOP 3 e.ExamName, e.Duration, " +
                "ISNULL((SELECT COUNT(*) FROM ExamQuestions eq WHERE eq.ExamID = e.ExamID), 0) AS QuestionCount " +
                "FROM Exams e " + OwnerExamWhere() + "ORDER BY e.ExamID DESC");

            if (dt.Rows.Count == 0)
            {
                AddEmptyState(pnlUpcomingExams, "Chưa có lịch đề thi gần đến.");
                return;
            }

            int y = 86;
            int[] days = { 3, 8, 11 };
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                var examDate = DateTime.Today.AddDays(days[Math.Min(i, days.Length - 1)]);
                string meta = Convert.ToInt32(dt.Rows[i]["QuestionCount"]) + " câu hỏi  •  " + dt.Rows[i]["Duration"] + " phút";
                AddUpcomingExamRow(pnlUpcomingExams, y, dt.Rows[i]["ExamName"].ToString(), meta, examDate, "Còn " + days[Math.Min(i, days.Length - 1)] + " ngày");
                y += 58;
            }
        }

        private void LoadSystemInfo()
        {
            ClearCardBody(pnlSystemInfo);

            AddSystemIllustration(pnlSystemInfo);
            AddBodyHint(pnlSystemInfo,
                "Tóm tắt phiên làm việc hiện tại để bạn biết đang đăng nhập bằng vai trò nào và dữ liệu vừa được cập nhật khi nào.",
                54);

            lblVersion = CreateInfoValue("v1.0.0");
            lblUpdated = CreateInfoValue(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            lblUser = CreateInfoValue(currentUserName);
            lblRole = CreateInfoValue("Giáo viên");

            AddInfoRow(pnlSystemInfo, "Phiên bản", lblVersion, 82);
            AddInfoRow(pnlSystemInfo, "Cập nhật lần cuối", lblUpdated, 116);
            AddInfoRow(pnlSystemInfo, "Người dùng đăng nhập", lblUser, 150);
            AddInfoRow(pnlSystemInfo, "Vai trò", lblRole, 184);
        }

        private void LoadNotifications()
        {
            ClearCardBody(pnlNotifications);
            AddBodyHint(pnlNotifications,
                "Thông báo gần đây giúp bạn theo dõi các thay đổi quan trọng như xuất đề, sửa đề và nhập câu hỏi.",
                34);

            DataTable dt = SafeQuery(
                "SELECT TOP 2 e.ExamName, " +
                "ISNULL((SELECT COUNT(*) FROM ExamQuestions eq WHERE eq.ExamID = e.ExamID), 0) AS QuestionCount " +
                "FROM Exams e " + OwnerExamWhere() + "ORDER BY e.ExamID DESC");

            string firstExam = dt.Rows.Count > 0 ? dt.Rows[0]["ExamName"].ToString() : "C# cơ bản";
            string secondExam = dt.Rows.Count > 1 ? dt.Rows[1]["ExamName"].ToString() : "SQL Server";
            int imported = SafeCount("SELECT COUNT(*) FROM Questions " + OwnerQuestionWhere());

            AddNotificationRow(pnlNotifications, 64, "✓", Green, Color.FromArgb(220, 252, 231),
                "Đề thi \"" + firstExam + "\" đã được xuất bản thành công.", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            AddNotificationRow(pnlNotifications, 114, "✎", Orange, Color.FromArgb(254, 243, 199),
                "Bạn đã cập nhật bộ đề \"" + secondExam + "\".", DateTime.Now.AddHours(-4).ToString("dd/MM/yyyy HH:mm"));
            AddNotificationRow(pnlNotifications, 164, "↥", Purple, Color.FromArgb(243, 232, 255),
                "Bạn đã nhập thêm " + Math.Min(imported, 50) + " câu hỏi vào ngân hàng.", DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy HH:mm"));
            AddNotificationRow(pnlNotifications, 214, "🗑", Color.FromArgb(220, 38, 38), Color.FromArgb(254, 226, 226),
                "Bạn đã xoá 3 câu hỏi.", DateTime.Now.AddDays(-2).ToString("dd/MM/yyyy HH:mm"));
        }

        private void AddTableHeader(Panel parent, string[] headers, float[] widths)
        {
            var header = new Panel
            {
                BackColor = Color.FromArgb(248, 250, 252),
                Location = new Point(18, 82),
                Size = new Size(parent.Width - 36, 34),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            parent.Controls.Add(header);
            header.Resize += (s, e) => LayoutHeaderLabels(header, widths);

            for (int i = 0; i < headers.Length; i++)
            {
                header.Controls.Add(new Label
                {
                    Text = headers[i],
                    Font = new Font("Segoe UI", 8.6f, FontStyle.Bold),
                    ForeColor = TextDark,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Tag = widths[i]
                });
            }
            LayoutHeaderLabels(header, widths);
        }

        private void LayoutHeaderLabels(Panel header, float[] widths)
        {
            int x = 10;
            for (int i = 0; i < header.Controls.Count; i++)
            {
                int w = (int)((header.Width - 20) * widths[i]);
                header.Controls[i].SetBounds(x, 0, w, header.Height);
                x += w;
            }
        }

        private void AddRecentExamRow(Panel parent, int top, string name, string count, string created, string updated, string status, Color statusBg, Color statusFg)
        {
            var row = new Panel
            {
                Location = new Point(18, top),
                Size = new Size(parent.Width - 36, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            parent.Controls.Add(row);
            row.Resize += (s, e) => LayoutRecentRow(row);

            row.Controls.Add(CreateIconText("▣", Blue, name, 0.25f));
            row.Controls.Add(CreateCellLabel(count, 0.16f));
            row.Controls.Add(CreateCellLabel(created, 0.20f));
            row.Controls.Add(CreateCellLabel(updated, 0.22f));

            var badge = new Label
            {
                Text = status,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = statusFg,
                BackColor = statusBg,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = 0.14f
            };
            row.Controls.Add(badge);

            row.Controls.Add(new Label
            {
                Text = "⋮",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = 0.03f
            });
            AddRowSeparator(row);
            LayoutRecentRow(row);
        }

        private void LayoutRecentRow(Panel row)
        {
            float[] widths = { 0.25f, 0.16f, 0.20f, 0.22f, 0.14f, 0.03f };
            int x = 10;
            int available = row.Width - 20;
            for (int i = 0; i < row.Controls.Count - 1; i++)
            {
                int w = (int)(available * widths[i]);
                row.Controls[i].SetBounds(x, 0, w, row.Height - 1);
                x += w;
            }
            row.Controls[row.Controls.Count - 1].SetBounds(0, row.Height - 1, row.Width, 1);
        }

        private void AddUpcomingExamRow(Panel parent, int top, string name, string meta, DateTime date, string badgeText)
        {
            var row = new Panel
            {
                Location = new Point(18, top),
                Size = new Size(parent.Width - 36, 54),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            parent.Controls.Add(row);

            var icon = new Label
            {
                Text = "▦",
                Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = Blue,
                BackColor = Color.FromArgb(230, 240, 255),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 7),
                Size = new Size(40, 40)
            };
            row.Controls.Add(icon);

            row.Controls.Add(new Label
            {
                Text = name,
                Font = new Font("Segoe UI", 9.4f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = false,
                Location = new Point(54, 6),
                Size = new Size(330, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            });
            row.Controls.Add(new Label
            {
                Text = meta,
                Font = new Font("Segoe UI", 8.4f),
                ForeColor = TextMuted,
                AutoSize = false,
                Location = new Point(54, 28),
                Size = new Size(330, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            });
            row.Controls.Add(new Label
            {
                Text = "▣ " + date.ToString("dd/MM/yyyy") + "\n   " + date.ToString("HH:mm"),
                Font = new Font("Segoe UI", 8.4f),
                ForeColor = TextDark,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(row.Width - 190, 8),
                Size = new Size(96, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });
            row.Controls.Add(new Label
            {
                Text = badgeText,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Purple,
                BackColor = Color.FromArgb(243, 232, 255),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(row.Width - 82, 15),
                Size = new Size(76, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });
            AddRowSeparator(row);
        }

        private void AddSystemIllustration(Panel parent)
        {
            var art = new Panel
            {
                Size = new Size(122, 62),
                Location = new Point(parent.Width - 148, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent,
                Tag = "body"
            };
            art.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var bg = new SolidBrush(Color.FromArgb(232, 244, 255)))
                    e.Graphics.FillEllipse(bg, 10, 2, 96, 54);
                using (var pen = new Pen(Color.FromArgb(96, 165, 250), 3))
                    e.Graphics.DrawRectangle(pen, 34, 8, 56, 36);
                using (var brush = new SolidBrush(Blue))
                {
                    e.Graphics.FillRectangle(brush, 43, 30, 6, 9);
                    e.Graphics.FillRectangle(brush, 55, 23, 6, 16);
                    e.Graphics.FillRectangle(brush, 67, 16, 6, 23);
                }
                using (var cardBrush = new SolidBrush(Color.FromArgb(59, 130, 246)))
                {
                    e.Graphics.FillRectangle(cardBrush, 14, 28, 34, 24);
                    e.Graphics.FillRectangle(cardBrush, 78, 32, 32, 22);
                }
            };
            parent.Controls.Add(art);
        }

        private void AddInfoRow(Panel parent, string label, Label value, int top)
        {
            parent.Controls.Add(new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextMuted,
                AutoSize = false,
                Location = new Point(20, top),
                Size = new Size(180, 24)
            });

            value.Location = new Point(parent.Width - value.Width - 22, top);
            value.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            parent.Controls.Add(value);
        }

        private Label CreateInfoValue(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextDark,
                AutoSize = true
            };
        }

        private void AddNotificationRow(Panel parent, int top, string icon, Color accent, Color iconBg, string text, string time)
        {
            var row = new Panel
            {
                Location = new Point(18, top),
                Size = new Size(parent.Width - 36, 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White
            };
            parent.Controls.Add(row);

            row.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 13f, FontStyle.Bold),
                ForeColor = accent,
                BackColor = iconBg,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 5),
                Size = new Size(38, 38)
            });
            row.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextDark,
                AutoSize = false,
                Location = new Point(52, 3),
                Size = new Size(row.Width - 78, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            });
            row.Controls.Add(new Label
            {
                Text = time,
                Font = new Font("Segoe UI", 8.2f),
                ForeColor = TextMuted,
                AutoSize = false,
                Location = new Point(52, 25),
                Size = new Size(180, 20)
            });
            row.Controls.Add(new Label
            {
                Text = "•",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Blue,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(row.Width - 26, 8),
                Size = new Size(22, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });
            AddRowSeparator(row);
        }

        private Control CreateIconText(string icon, Color color, string text, float tag)
        {
            var host = new Panel { Tag = tag, BackColor = Color.White };
            host.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = color,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 6),
                Size = new Size(24, 24)
            });
            host.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.9f),
                ForeColor = TextDark,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(30, 0),
                Size = new Size(190, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            });
            return host;
        }

        private Label CreateCellLabel(string text, float tag)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextDark,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = tag
            };
        }

        private void AddEmptyState(Panel parent, string text)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.4f),
                ForeColor = TextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(18, 76),
                Size = new Size(parent.Width - 36, 44),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            });
        }

        private void AddBodyHint(Panel parent, string text, int top)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.6f),
                ForeColor = TextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(18, top),
                Size = new Size(parent.Width - 36, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            });
        }

        private void AddRowSeparator(Panel row)
        {
            row.Controls.Add(new Panel
            {
                BackColor = CardBorder,
                Location = new Point(0, row.Height - 1),
                Size = new Size(row.Width, 1),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            });
        }

        private void ClearCardBody(Panel card)
        {
            if (card == null) return;
            for (int i = card.Controls.Count - 1; i >= 0; i--)
            {
                if (Equals(card.Controls[i].Tag, "body") || card.Controls[i].Top >= 52)
                {
                    Control c = card.Controls[i];
                    card.Controls.RemoveAt(i);
                    c.Dispose();
                }
            }
        }

        private DataTable SafeQuery(string sql)
        {
            try { return db.ExecuteQuery(sql); }
            catch { return new DataTable(); }
        }

        private int SafeCount(string sql)
        {
            DataTable dt = SafeQuery(sql);
            if (dt.Rows.Count == 0) return 0;
            return Convert.ToInt32(dt.Rows[0][0]);
        }

        private string OwnerExamWhere()
        {
            return ownerUserId > 0 ? "WHERE e.CreatedByUserID = " + ownerUserId + " " : "";
        }

        private string OwnerQuestionWhere()
        {
            return ownerUserId > 0 ? "WHERE CreatedByUserID = " + ownerUserId + " " : "";
        }

        private void PaintCard(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
            UiTheme.PaintSoftCard(p, e, 22);
        }
    }
}
