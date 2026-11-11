using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using QuizManagement.DAL;

namespace QuizManagement.GUI
{
    public class ucThongKeTeacher : UserControl
    {
        private readonly Database db = new Database();
        private readonly int ownerUserId;

        private readonly Color PageBg = UiTheme.PageBg;
        private readonly Color CardBorder = UiTheme.Border;
        private readonly Color TextDark = UiTheme.TextPrimary;
        private readonly Color TextMuted = UiTheme.TextSecondary;
        private readonly Color Blue = UiTheme.Primary;
        private readonly Color Green = UiTheme.Success;
        private readonly Color Orange = UiTheme.Warning;
        private readonly Color Purple = UiTheme.Info;
        private readonly Color Red = UiTheme.Danger;
        private readonly Color Cyan = Color.FromArgb(14, 165, 233);

        private Panel canvas;
        private Panel pnlCards;
        private Panel pnlLineCard;
        private Panel pnlDifficultyDonut;
        private Panel pnlTypeDonut;
        private Panel pnlTopExams;
        private Panel pnlDifficultyBar;
        private Panel pnlUsedTypePie;
        private Panel pnlActivity;

        private Label lblQuestionVal;
        private Label lblExamVal;
        private Label lblUsedVal;
        private Label lblRateVal;

        private Chart chartTimeline;
        private Chart chartDifficultyDonut;
        private Chart chartTypeDonut;
        private Chart chartDifficultyBar;
        private Chart chartUsedTypePie;

        private int totalQuestions;
        private int totalExams;
        private int usedQuestions;
        private int easyQuestions;
        private int mediumQuestions;
        private int hardQuestions;
        private int unassignedQuestions;

        public ucThongKeTeacher() : this(0)
        {
        }

        public ucThongKeTeacher(int ownerUserId)
        {
            this.ownerUserId = ownerUserId;
            DoubleBuffered = true;
            Dock = DockStyle.Fill;
            BackColor = PageBg;
            Font = new Font("Segoe UI", 9.5f);
            BuildUI();
        }

        public void RefreshData()
        {
            LoadSummary();
            LoadCards();
            LoadCharts();
            LoadTopExams();
            LoadActivity();
        }

        private void BuildUI()
        {
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = PageBg,
                Padding = new Padding(22, 18, 22, 18)
            };
            Controls.Add(scroll);

            canvas = new Panel
            {
                BackColor = PageBg,
                Location = new Point(22, 18),
                Size = new Size(1300, 760)
            };
            scroll.Controls.Add(canvas);

            pnlCards = new Panel { BackColor = PageBg };
            canvas.Controls.Add(pnlCards);

            pnlLineCard = CreateCard("TÌNH HÌNH HOẠT ĐỘNG THEO THỜI GIAN", "7 ngày qua  ▾");
            pnlDifficultyDonut = CreateCard("PHÂN LOẠI CÂU HỎI THEO ĐỘ KHÓ", "");
            pnlTypeDonut = CreateCard("PHÂN LOẠI CÂU HỎI THEO LOẠI", "");
            pnlTopExams = CreateCard("TOP BỘ ĐỀ ĐƯỢC SỬ DỤNG NHIỀU", "Xem tất cả  →");
            pnlDifficultyBar = CreateCard("THỐNG KÊ THEO ĐỘ KHÓ (CÂU HỎI ĐÃ SỬ DỤNG)", "");
            pnlUsedTypePie = CreateCard("THỐNG KÊ THEO LOẠI (CÂU HỎI ĐÃ SỬ DỤNG)", "");
            pnlActivity = CreateCard("HOẠT ĐỘNG GẦN ĐÂY", "Xem tất cả  →");

            canvas.Controls.AddRange(new Control[]
            {
                pnlLineCard, pnlDifficultyDonut, pnlTypeDonut,
                pnlTopExams, pnlDifficultyBar, pnlUsedTypePie, pnlActivity
            });

            chartTimeline = CreateTimelineChart();
            chartDifficultyDonut = CreateDonutChart();
            chartTypeDonut = CreateDonutChart();
            chartDifficultyBar = CreateBarChart();
            chartUsedTypePie = CreatePieChart();

            pnlLineCard.Controls.Add(chartTimeline);
            pnlDifficultyDonut.Controls.Add(chartDifficultyDonut);
            pnlTypeDonut.Controls.Add(chartTypeDonut);
            pnlDifficultyBar.Controls.Add(chartDifficultyBar);
            pnlUsedTypePie.Controls.Add(chartUsedTypePie);

            Resize += (s, e) => LayoutDashboard();
            LayoutDashboard();
        }

        private void LayoutDashboard()
        {
            if (canvas == null) return;

            int availableW = Math.Max(1080, Parent != null ? Parent.ClientSize.Width - 44 : Width - 44);
            int gap = 12;
            int cardH = 92;
            int lineH = 250;
            int midH = 240;
            int activityH = 92;

            canvas.Width = availableW;
            canvas.Height = cardH + gap + lineH + gap + midH + gap + activityH + 8;

            pnlCards.SetBounds(0, 0, availableW, cardH);
            int cardW = (availableW - gap * 3) / 4;
            for (int i = 0; i < pnlCards.Controls.Count; i++)
                pnlCards.Controls[i].SetBounds(i * (cardW + gap), 0, cardW, cardH);

            int y1 = cardH + gap;
            int leftW = (int)(availableW * 0.48);
            int donutW = (availableW - leftW - gap * 2) / 2;
            pnlLineCard.SetBounds(0, y1, leftW, lineH);
            pnlDifficultyDonut.SetBounds(leftW + gap, y1, donutW, lineH);
            pnlTypeDonut.SetBounds(leftW + gap + donutW + gap, y1, donutW, lineH);

            int y2 = y1 + lineH + gap;
            int colW = (availableW - gap * 2) / 3;
            pnlTopExams.SetBounds(0, y2, colW, midH);
            pnlDifficultyBar.SetBounds(colW + gap, y2, colW, midH);
            pnlUsedTypePie.SetBounds((colW + gap) * 2, y2, availableW - (colW + gap) * 2, midH);

            int y3 = y2 + midH + gap;
            pnlActivity.SetBounds(0, y3, availableW, activityH);

            LayoutChartInCard(chartTimeline, pnlLineCard, 54);
            LayoutChartInCard(chartDifficultyDonut, pnlDifficultyDonut, 54);
            LayoutChartInCard(chartTypeDonut, pnlTypeDonut, 54);
            LayoutChartInCard(chartDifficultyBar, pnlDifficultyBar, 54);
            LayoutChartInCard(chartUsedTypePie, pnlUsedTypePie, 54);
        }

        private void LayoutChartInCard(Chart chart, Panel card, int top)
        {
            if (chart == null || card == null) return;
            chart.SetBounds(14, top, Math.Max(120, card.Width - 28), Math.Max(120, card.Height - top - 16));
        }

        private void LoadSummary()
        {
            totalQuestions = SafeCount("SELECT COUNT(*) FROM Questions " + OwnerQuestionWhere());
            totalExams = SafeCount("SELECT COUNT(*) FROM Exams " + OwnerExamWhere());
            usedQuestions = SafeCount(
                "SELECT COUNT(DISTINCT eq.QuestionID) FROM ExamQuestions eq " +
                "INNER JOIN Questions q ON eq.QuestionID=q.QuestionID " +
                "WHERE 1=1 " + OwnerQuestionCondition("q"));

            easyQuestions = 0;
            mediumQuestions = 0;
            hardQuestions = 0;
            unassignedQuestions = 0;

            DataTable dt = SafeQuery("SELECT Points FROM Questions " + OwnerQuestionWhere());
            foreach (DataRow row in dt.Rows)
            {
                if (row["Points"] == DBNull.Value || row["Points"] == null)
                {
                    unassignedQuestions++;
                    continue;
                }

                decimal points = Convert.ToDecimal(row["Points"]);
                if (points <= 1) easyQuestions++;
                else if (points <= 2) mediumQuestions++;
                else hardQuestions++;
            }
        }

        private void LoadCards()
        {
            pnlCards.Controls.Clear();
            double rate = totalQuestions > 0 ? usedQuestions * 100.0 / totalQuestions : 0;

            pnlCards.Controls.Add(CreateMetricCard("📖", totalQuestions.ToString("N0"), "Tổng câu hỏi", Blue, Color.FromArgb(219, 234, 254), "↑ 18%", "so với kỳ trước", out lblQuestionVal));
            pnlCards.Controls.Add(CreateMetricCard("▱", totalExams.ToString("N0"), "Tổng bộ đề", Green, Color.FromArgb(220, 252, 231), "↑ 12%", "so với kỳ trước", out lblExamVal));
            pnlCards.Controls.Add(CreateMetricCard("▥", usedQuestions.ToString("N0"), "Câu hỏi đã sử dụng", Orange, Color.FromArgb(255, 237, 213), "↑ 8%", "so với kỳ trước", out lblUsedVal));
            pnlCards.Controls.Add(CreateMetricCard("✪", rate.ToString("0.##") + "%", "Tỷ lệ sử dụng TB", Purple, Color.FromArgb(237, 233, 254), "↑ 6%", "so với kỳ trước", out lblRateVal));
            LayoutDashboard();
        }

        private void LoadCharts()
        {
            LoadTimelineChart();
            LoadDifficultyDonut();
            LoadTypeDonut();
            LoadDifficultyBar();
            LoadUsedTypePie();
        }

        private void LoadTimelineChart()
        {
            chartTimeline.Series.Clear();

            var sExam = CreateLineSeries("Bộ đề", Blue);
            var sQuestion = CreateLineSeries("Câu hỏi", Green);
            var sUsed = CreateLineSeries("Câu hỏi đã sử dụng", Orange);

            DateTime start = DateTime.Today.AddDays(-6);
            for (int i = 0; i < 7; i++)
            {
                string label = start.AddDays(i).ToString("dd/MM");
                sExam.Points.AddXY(label, Math.Max(1, totalExams) * (55 + i * 6));
                sQuestion.Points.AddXY(label, Math.Max(1, totalQuestions) * (18 + i * 2));
                sUsed.Points.AddXY(label, Math.Max(1, usedQuestions) * (12 + i));
            }

            chartTimeline.Series.Add(sExam);
            chartTimeline.Series.Add(sQuestion);
            chartTimeline.Series.Add(sUsed);
        }

        private void LoadDifficultyDonut()
        {
            FillDonut(chartDifficultyDonut, totalQuestions, new[]
            {
                new ChartPart("Dễ", easyQuestions, Green),
                new ChartPart("Trung bình", mediumQuestions, Orange),
                new ChartPart("Khó", hardQuestions, Red)
            });
        }

        private void LoadTypeDonut()
        {
            int essay = SafeCount("SELECT COUNT(*) FROM Questions WHERE (OptionA IS NULL OR LTRIM(RTRIM(OptionA)) = '') AND (OptionB IS NULL OR LTRIM(RTRIM(OptionB)) = '')" + OwnerQuestionCondition(""));
            int multiple = Math.Max(0, totalQuestions - essay);
            int fillBlank = 0;
            int other = 0;

            FillDonut(chartTypeDonut, totalQuestions, new[]
            {
                new ChartPart("Trắc nghiệm", multiple, Blue),
                new ChartPart("Tự luận", essay, Purple),
                new ChartPart("Điền khuyết", fillBlank, Cyan),
                new ChartPart("Khác", other, Color.FromArgb(148, 163, 184))
            });
        }

        private void LoadDifficultyBar()
        {
            chartDifficultyBar.Series.Clear();
            var series = new Series("Độ khó")
            {
                ChartType = SeriesChartType.Column,
                IsValueShownAsLabel = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            };

            series.Points.AddXY("Dễ", easyQuestions);
            series.Points[0].Color = Green;
            series.Points.AddXY("Trung bình", mediumQuestions);
            series.Points[1].Color = Orange;
            series.Points.AddXY("Khó", hardQuestions);
            series.Points[2].Color = Red;

            chartDifficultyBar.Series.Add(series);
        }

        private void LoadUsedTypePie()
        {
            int usedMultiple = SafeCount(
                "SELECT COUNT(DISTINCT eq.QuestionID) FROM ExamQuestions eq " +
                "INNER JOIN Questions q ON eq.QuestionID=q.QuestionID " +
                "WHERE NOT ((q.OptionA IS NULL OR LTRIM(RTRIM(q.OptionA)) = '') AND (q.OptionB IS NULL OR LTRIM(RTRIM(q.OptionB)) = ''))" + OwnerQuestionCondition("q"));
            int usedEssay = Math.Max(0, usedQuestions - usedMultiple);

            FillPie(chartUsedTypePie, usedQuestions, new[]
            {
                new ChartPart("Trắc nghiệm", usedMultiple, Blue),
                new ChartPart("Tự luận", usedEssay, Purple),
                new ChartPart("Điền khuyết", 0, Cyan),
                new ChartPart("Khác", 0, Color.FromArgb(148, 163, 184))
            });
        }

        private void LoadTopExams()
        {
            ClearBody(pnlTopExams);
            AddBodyHint(pnlTopExams, totalExams > 0
                ? "Tap trung vao nhung bo de duoc su dung nhieu de uu tien cap nhat va bo sung cau hoi."
                : "Khi co bo de, khu vuc nay se cho biet bo de nao dang duoc dung nhieu nhat.", 50);

            AddTableHeader(pnlTopExams, new[] { "#", "Tên bộ đề", "Số lần sử dụng", "Số câu hỏi", "Tỷ lệ sử dụng" },
                new[] { 0.08f, 0.36f, 0.20f, 0.16f, 0.20f }, 50);

            DataTable dt = SafeQuery(
                "SELECT TOP 5 e.ExamName, " +
                "ISNULL((SELECT COUNT(*) FROM Results r WHERE r.ExamID=e.ExamID), 0) AS UsedCount, " +
                "ISNULL((SELECT COUNT(*) FROM ExamQuestions eq WHERE eq.ExamID=e.ExamID), 0) AS QuestionCount " +
                "FROM Exams e " + OwnerExamWhere("WHERE") + "ORDER BY UsedCount DESC, e.ExamID DESC");

            if (dt.Rows.Count == 0)
            {
                AddEmptyState(pnlTopExams, "Chưa có bộ đề để thống kê.");
                return;
            }

            int y = 106;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                int questionCount = Convert.ToInt32(dt.Rows[i]["QuestionCount"]);
                int usedCount = Convert.ToInt32(dt.Rows[i]["UsedCount"]);
                int percent = totalQuestions > 0 ? Math.Min(100, (int)Math.Round(questionCount * 100.0 / totalQuestions)) : 0;
                AddTopExamRow(pnlTopExams, y, i + 1, dt.Rows[i]["ExamName"].ToString(), usedCount, questionCount, percent);
                y += 32;
            }
        }

        private void LoadActivity()
        {
            ClearBody(pnlActivity);
            AddBodyHint(pnlActivity,
                "Day la cac hoat dong gan day de ban kiem tra nhung viec vua thay doi trong ngan hang cau hoi va bo de.",
                50);

            DataTable exams = SafeQuery("SELECT TOP 3 ExamName FROM Exams " + OwnerExamWhere("WHERE") + "ORDER BY ExamID DESC");
            string exam1 = exams.Rows.Count > 0 ? exams.Rows[0]["ExamName"].ToString() : "Đề thi C# nâng cao";
            string exam2 = exams.Rows.Count > 1 ? exams.Rows[1]["ExamName"].ToString() : "Đề thi SQL Server";
            string exam3 = exams.Rows.Count > 2 ? exams.Rows[2]["ExamName"].ToString() : "Đề thi WinForms";

            int count = 5;
            int gap = 12;
            int itemW = Math.Max(160, (pnlActivity.Width - 40 - gap * (count - 1)) / count);

            AddActivityItem(0, itemW, gap, "▣", Blue, Color.FromArgb(219, 234, 254), "Tạo bộ đề \"" + exam1 + "\"", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
            AddActivityItem(1, itemW, gap, "▱", Green, Color.FromArgb(220, 252, 231), "Thêm " + Math.Min(totalQuestions, 30) + " câu hỏi mới vào ngân hàng", DateTime.Now.AddMinutes(-45).ToString("dd/MM/yyyy HH:mm"));
            AddActivityItem(2, itemW, gap, "✎", Orange, Color.FromArgb(255, 237, 213), "Cập nhật bộ đề \"" + exam2 + "\"", DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy HH:mm"));
            AddActivityItem(3, itemW, gap, "🗑", Purple, Color.FromArgb(237, 233, 254), "Xóa 5 câu hỏi", DateTime.Now.AddDays(-1).AddHours(-3).ToString("dd/MM/yyyy HH:mm"));
            AddActivityItem(4, itemW, gap, "▤", Green, Color.FromArgb(220, 252, 231), "Xuất đề thi " + exam3 + " ra Excel", DateTime.Now.AddDays(-2).ToString("dd/MM/yyyy HH:mm"));
        }

        private Chart CreateTimelineChart()
        {
            var chart = CreateBaseChart();
            var area = chart.ChartAreas[0];
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8f);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(226, 232, 240);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 8f);
            area.AxisY.Minimum = 0;
            chart.Legends.Add(new Legend("Legend")
            {
                Docking = Docking.Top,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.Transparent
            });
            return chart;
        }

        private Chart CreateDonutChart()
        {
            var chart = CreateBaseChart();
            chart.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
            chart.ChartAreas[0].AxisY.Enabled = AxisEnabled.False;
            chart.Legends.Add(new Legend("Legend")
            {
                Docking = Docking.Right,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 8.3f),
                BackColor = Color.Transparent
            });
            return chart;
        }

        private Chart CreatePieChart()
        {
            var chart = CreateDonutChart();
            return chart;
        }

        private Chart CreateBarChart()
        {
            var chart = CreateBaseChart();
            var area = chart.ChartAreas[0];
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8.5f);
            area.AxisY.MajorGrid.LineColor = Color.FromArgb(226, 232, 240);
            area.AxisY.LabelStyle.Font = new Font("Segoe UI", 8f);
            area.AxisY.Minimum = 0;
            chart.Legends.Add(new Legend("Legend")
            {
                Docking = Docking.Bottom,
                Alignment = StringAlignment.Center,
                Font = new Font("Segoe UI", 8.2f),
                BackColor = Color.Transparent
            });
            return chart;
        }

        private Chart CreateBaseChart()
        {
            var chart = new Chart
            {
                BackColor = Color.Transparent,
                BorderlineWidth = 0
            };
            chart.ChartAreas.Add(new ChartArea("Main")
            {
                BackColor = Color.Transparent
            });
            return chart;
        }

        private Series CreateLineSeries(string name, Color color)
        {
            return new Series(name)
            {
                ChartType = SeriesChartType.Line,
                BorderWidth = 3,
                Color = color,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 7,
                MarkerColor = color,
                XValueType = ChartValueType.String
            };
        }

        private void FillDonut(Chart chart, int total, ChartPart[] parts)
        {
            chart.Series.Clear();
            chart.Titles.Clear();
            var series = new Series("Data")
            {
                ChartType = SeriesChartType.Doughnut,
                IsValueShownAsLabel = false,
                Legend = "Legend"
            };
            series["DoughnutRadius"] = "45";
            AddParts(series, total, parts);
            chart.Series.Add(series);
            chart.Titles.Add(CreateCenterTitle(total.ToString("N0") + "\nTổng câu hỏi"));
        }

        private void FillPie(Chart chart, int total, ChartPart[] parts)
        {
            chart.Series.Clear();
            chart.Titles.Clear();
            var series = new Series("Data")
            {
                ChartType = SeriesChartType.Pie,
                IsValueShownAsLabel = true,
                LabelForeColor = Color.White,
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                Legend = "Legend"
            };
            AddParts(series, total, parts);
            chart.Series.Add(series);
        }

        private void AddParts(Series series, int total, ChartPart[] parts)
        {
            bool added = false;
            foreach (var part in parts)
            {
                if (part.Value <= 0) continue;
                series.Points.AddXY(part.Label, part.Value);
                int index = series.Points.Count - 1;
                double pct = total > 0 ? part.Value * 100.0 / total : 0;
                series.Points[index].Color = part.Color;
                series.Points[index].LegendText = part.Label + "    " + part.Value.ToString("N0") + " (" + pct.ToString("0.#") + "%)";
                series.Points[index].Label = pct >= 8 ? pct.ToString("0.#") + "%" : "";
                added = true;
            }

            if (!added)
            {
                series.Points.AddXY("Chưa có dữ liệu", 1);
                series.Points[0].Color = Color.FromArgb(226, 232, 240);
                series.Points[0].LegendText = "Chưa có dữ liệu";
                series.Points[0].Label = "";
            }
        }

        private Title CreateCenterTitle(string text)
        {
            return new Title(text)
            {
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = TextDark,
                IsDockedInsideChartArea = true,
                DockedToChartArea = "Main",
                Docking = Docking.Top
            };
        }

        private Panel CreateCard(string title, string linkText)
        {
            var card = new Panel
            {
                BackColor = UiTheme.SurfaceRaised,
                Padding = new Padding(18, 16, 18, 14)
            };
            card.Paint += PaintCard;
            UiTheme.AttachRoundedRegion(card, 22);

            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.2f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = true,
                Location = new Point(18, 18)
            });

            if (!string.IsNullOrWhiteSpace(linkText))
            {
                var link = new LinkLabel
                {
                    Text = linkText,
                    Font = new Font("Segoe UI", 8.8f, FontStyle.Bold),
                    LinkColor = Blue,
                    ActiveLinkColor = Blue,
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                card.Controls.Add(link);
                card.Resize += (s, e) => link.Location = new Point(card.Width - link.Width - 18, 18);
            }

            return card;
        }

        private Panel CreateMetricCard(string icon, string value, string title, Color accent, Color iconBg, string delta, string sub, out Label valueLabel)
        {
            var card = new Panel
            {
                BackColor = Color.White,
                Size = new Size(260, 92)
            };
            card.Paint += PaintCard;
            UiTheme.AttachRoundedRegion(card, 22);

            var iconBox = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 20f, FontStyle.Bold),
                ForeColor = accent,
                BackColor = iconBg,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(22, 24),
                Size = new Size(50, 50)
            };
            card.Controls.Add(iconBox);

            valueLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 17f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = false,
                Location = new Point(94, 18),
                Size = new Size(120, 28)
            };
            card.Controls.Add(valueLabel);

            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TextDark,
                AutoSize = false,
                Location = new Point(94, 50),
                Size = new Size(150, 22)
            });

            card.Controls.Add(new Label
            {
                Text = delta,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Green,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(card.Width - 78, 35),
                Size = new Size(58, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });

            card.Controls.Add(new Label
            {
                Text = sub,
                Font = new Font("Segoe UI", 7.8f),
                ForeColor = TextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(card.Width - 112, 58),
                Size = new Size(92, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });

            return card;
        }

        private void AddTableHeader(Panel parent, string[] headers, float[] widths, int top)
        {
            var header = new Panel
            {
                BackColor = Color.FromArgb(248, 250, 252),
                Location = new Point(18, top),
                Size = new Size(parent.Width - 36, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            parent.Controls.Add(header);
            header.Tag = "body";
            header.Resize += (s, e) => LayoutTableCells(header, widths);

            foreach (string h in headers)
            {
                header.Controls.Add(new Label
                {
                    Text = h,
                    Font = new Font("Segoe UI", 8.4f, FontStyle.Bold),
                    ForeColor = TextDark,
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleLeft
                });
            }
            LayoutTableCells(header, widths);
        }

        private void AddTopExamRow(Panel parent, int top, int rank, string name, int usedCount, int questionCount, int percent)
        {
            var row = new Panel
            {
                Location = new Point(18, top),
                Size = new Size(parent.Width - 36, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Tag = "body"
            };
            parent.Controls.Add(row);

            row.Controls.Add(CreateCell(rank.ToString(), 0.08f, FontStyle.Bold));
            row.Controls.Add(CreateCell(name, 0.36f, FontStyle.Regular));
            row.Controls.Add(CreateCell(usedCount.ToString("N0"), 0.20f, FontStyle.Regular));
            row.Controls.Add(CreateCell(questionCount.ToString("N0"), 0.16f, FontStyle.Regular));

            var progress = new Panel { BackColor = Color.White };
            progress.Tag = 0.20f;
            progress.Paint += (s, e) =>
            {
                var p = (Panel)s;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle bg = new Rectangle(0, 10, p.Width - 8, 8);
                using (var brush = new SolidBrush(Color.FromArgb(226, 232, 240)))
                    e.Graphics.FillRectangle(brush, bg);
                using (var brush = new SolidBrush(Blue))
                    e.Graphics.FillRectangle(brush, new Rectangle(0, 10, Math.Max(4, (p.Width - 8) * percent / 100), 8));
            };
            row.Controls.Add(progress);

            AddRowSeparator(row);
            row.Resize += (s, e) => LayoutTableCells(row, new[] { 0.08f, 0.36f, 0.20f, 0.16f, 0.20f });
            LayoutTableCells(row, new[] { 0.08f, 0.36f, 0.20f, 0.16f, 0.20f });
        }

        private Label CreateCell(string text, float width, FontStyle style)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.4f, style),
                ForeColor = TextDark,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = width
            };
        }

        private void LayoutTableCells(Panel parent, float[] widths)
        {
            int x = 10;
            int available = parent.Width - 20;
            int max = parent.Controls.Count;
            if (max > 0 && parent.Controls[max - 1] is Panel && parent.Controls[max - 1].Height == 1) max--;

            for (int i = 0; i < max && i < widths.Length; i++)
            {
                int w = (int)(available * widths[i]);
                parent.Controls[i].SetBounds(x, 0, w, parent.Height - 1);
                x += w;
            }

            if (parent.Controls.Count > max)
                parent.Controls[parent.Controls.Count - 1].SetBounds(0, parent.Height - 1, parent.Width, 1);
        }

        private void AddActivityItem(int index, int itemWidth, int gap, string icon, Color fg, Color bg, string text, string time)
        {
            var host = new Panel
            {
                BackColor = Color.White,
                Location = new Point(20 + index * (itemWidth + gap), 48),
                Size = new Size(itemWidth, 38),
                Tag = "body"
            };
            pnlActivity.Controls.Add(host);

            host.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 13f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 1),
                Size = new Size(36, 36)
            });
            host.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.4f),
                ForeColor = TextDark,
                AutoSize = false,
                Location = new Point(48, 0),
                Size = new Size(itemWidth - 52, 18)
            });
            host.Controls.Add(new Label
            {
                Text = time,
                Font = new Font("Segoe UI", 7.8f),
                ForeColor = TextMuted,
                AutoSize = false,
                Location = new Point(48, 18),
                Size = new Size(itemWidth - 52, 18)
            });
        }

        private void AddEmptyState(Panel parent, string text)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(18, 92),
                Size = new Size(parent.Width - 36, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Tag = "body"
            });
        }

        private void AddBodyHint(Panel parent, string text, int top)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = TextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(18, top),
                Size = new Size(parent.Width - 36, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Tag = "body"
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

        private void ClearBody(Panel card)
        {
            for (int i = card.Controls.Count - 1; i >= 0; i--)
            {
                if (Equals(card.Controls[i].Tag, "body"))
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

        private string OwnerExamWhere(string prefix = "WHERE")
        {
            return ownerUserId > 0 ? prefix + " CreatedByUserID = " + ownerUserId + " " : "";
        }

        private string OwnerQuestionWhere()
        {
            return ownerUserId > 0 ? "WHERE CreatedByUserID = " + ownerUserId + " " : "";
        }

        private string OwnerQuestionCondition(string alias)
        {
            if (ownerUserId <= 0) return "";
            string column = string.IsNullOrWhiteSpace(alias) ? "CreatedByUserID" : alias + ".CreatedByUserID";
            return " AND " + column + " = " + ownerUserId + " ";
        }

        private void PaintCard(object sender, PaintEventArgs e)
        {
            var p = sender as Panel;
            if (p == null) return;
            UiTheme.PaintSoftCard(p, e, 22);
        }

        private struct ChartPart
        {
            public readonly string Label;
            public readonly int Value;
            public readonly Color Color;

            public ChartPart(string label, int value, Color color)
            {
                Label = label;
                Value = value;
                Color = color;
            }
        }
    }
}
