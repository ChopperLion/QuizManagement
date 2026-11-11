using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace QuizManagement.GUI
{
    internal enum UiButtonVariant
    {
        Primary,
        Secondary,
        Success,
        Warning,
        Danger,
        Info,
        Ghost
    }

    internal static class UiTheme
    {
        public static readonly Color PageBg = Color.FromArgb(230, 236, 248);
        public static readonly Color Surface = Color.FromArgb(236, 241, 250);
        public static readonly Color SurfaceRaised = Color.FromArgb(246, 248, 253);
        public static readonly Color SurfaceInset = Color.FromArgb(218, 226, 242);
        public static readonly Color Border = Color.FromArgb(196, 206, 226);
        public static readonly Color TextPrimary = Color.FromArgb(27, 36, 62);
        public static readonly Color TextSecondary = Color.FromArgb(98, 112, 145);
        public static readonly Color Primary = Color.FromArgb(98, 119, 255);
        public static readonly Color PrimaryHover = Color.FromArgb(84, 104, 238);
        public static readonly Color Success = Color.FromArgb(52, 176, 123);
        public static readonly Color Warning = Color.FromArgb(235, 151, 63);
        public static readonly Color Danger = Color.FromArgb(212, 82, 103);
        public static readonly Color Info = Color.FromArgb(89, 154, 255);
        public static readonly Color DarkSurface = Color.FromArgb(45, 57, 96);

        public static Font TitleFont(float size) => new Font("Segoe UI Semibold", size, FontStyle.Bold);
        public static Font BodyFont(float size) => new Font("Segoe UI", size, FontStyle.Regular);

        public static void ApplyButton(Button button, UiButtonVariant variant, bool widePadding = false)
        {
            if (button == null) return;

            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = variant == UiButtonVariant.Secondary || variant == UiButtonVariant.Ghost ? 1 : 0;
            button.FlatAppearance.MouseDownBackColor = Color.Empty;
            button.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.Padding = widePadding ? new Padding(16, 0, 16, 0) : button.Padding;

            switch (variant)
            {
                case UiButtonVariant.Primary:
                    button.BackColor = Primary;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.MouseOverBackColor = PrimaryHover;
                    break;
                case UiButtonVariant.Success:
                    button.BackColor = Success;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(Success, 0.05f);
                    break;
                case UiButtonVariant.Warning:
                    button.BackColor = Warning;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(Warning, 0.05f);
                    break;
                case UiButtonVariant.Danger:
                    button.BackColor = Danger;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(Danger, 0.05f);
                    break;
                case UiButtonVariant.Info:
                    button.BackColor = Info;
                    button.ForeColor = Color.White;
                    button.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(Info, 0.05f);
                    break;
                case UiButtonVariant.Secondary:
                    button.BackColor = SurfaceRaised;
                    button.ForeColor = TextPrimary;
                    button.FlatAppearance.BorderColor = Border;
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(242, 245, 252);
                    break;
                default:
                    button.BackColor = Surface;
                    button.ForeColor = TextSecondary;
                    button.FlatAppearance.BorderColor = Surface;
                    button.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 232, 246);
                    break;
            }
        }

        public static void ApplyTextBox(TextBox textBox)
        {
            if (textBox == null) return;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = SurfaceRaised;
            textBox.ForeColor = TextPrimary;
            textBox.Font = BodyFont(10.5f);
        }

        public static void ApplyComboBox(ComboBox comboBox)
        {
            if (comboBox == null) return;
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.BackColor = SurfaceRaised;
            comboBox.ForeColor = TextPrimary;
            comboBox.Font = BodyFont(10f);
        }

        public static void ApplyGrid(DataGridView grid)
        {
            if (grid == null) return;
            grid.BackgroundColor = SurfaceRaised;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Border;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Surface;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold);
            grid.DefaultCellStyle.BackColor = SurfaceRaised;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 229, 255);
            grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(242, 245, 252);
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        }

        public static void PaintSoftCard(Control control, PaintEventArgs e, int radius = 18)
        {
            if (control == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
            ApplyRoundedRegion(control, radius);
            using (GraphicsPath path = BuildRoundRect(rect, radius))
            {
                using (SolidBrush brush = new SolidBrush(control.BackColor))
                    e.Graphics.FillPath(brush, path);

                using (Pen lightPen = new Pen(Color.FromArgb(255, 255, 255), 1.4f))
                    e.Graphics.DrawPath(lightPen, path);

                Rectangle shadowRect = new Rectangle(1, 1, control.Width - 3, control.Height - 3);
                using (GraphicsPath shadowPath = BuildRoundRect(shadowRect, Math.Max(4, radius - 2)))
                using (Pen shadowPen = new Pen(Color.FromArgb(176, 188, 214), 1f))
                    e.Graphics.DrawPath(shadowPen, shadowPath);
            }
        }

        public static void PaintInsetSurface(Control control, PaintEventArgs e, int radius = 16)
        {
            if (control == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, control.Width - 1, control.Height - 1);
            ApplyRoundedRegion(control, radius);
            using (GraphicsPath path = BuildRoundRect(rect, radius))
            {
                using (SolidBrush brush = new SolidBrush(control.BackColor))
                    e.Graphics.FillPath(brush, path);

                using (Pen darkPen = new Pen(Color.FromArgb(196, 206, 226), 1.1f))
                    e.Graphics.DrawPath(darkPen, path);
            }
        }

        public static void ApplyRoundedRegion(Control control, int radius)
        {
            if (control == null || control.Width <= 1 || control.Height <= 1) return;

            Rectangle rect = new Rectangle(0, 0, control.Width, control.Height);
            using (GraphicsPath path = BuildRoundRect(rect, radius))
            {
                Region oldRegion = control.Region;
                control.Region = new Region(path);
                oldRegion?.Dispose();
            }
        }

        public static void AttachRoundedRegion(Control control, int radius)
        {
            if (control == null) return;
            control.Resize += (s, e) => ApplyRoundedRegion(control, radius);
            ApplyRoundedRegion(control, radius);
        }

        private static GraphicsPath BuildRoundRect(Rectangle rect, int radius)
        {
            int diameter = Math.Max(2, radius * 2);
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
