// Quench - a monitor list drawn as rows (avoids one control per row).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Quench
{
    internal sealed class MonitorRow
    {
        public string Code;
        public string Detail;
        public string Adapter;
        public GpuVendor Vendor;
        public bool Active;
        public bool Primary;
    }

    internal class MonitorList : Control
    {
        public readonly List<MonitorRow> Rows = new List<MonitorRow>();

        public MonitorList()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }

        public void SetRows(IEnumerable<MonitorRow> rows)
        {
            Rows.Clear();
            Rows.AddRange(rows);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.Card);

            if (Rows.Count == 0)
            {
                using (Font f = Theme.Regular(12f))
                using (var b = new SolidBrush(Theme.TextMuted))
                    g.DrawString(Loc.NoMonitors, f, b, 0, Theme.S(6));
                return;
            }

            int rowH = Theme.S(46);
            int y = 0;
            using (var typo = StringFormat.GenericTypographic)
            {
                for (int i = 0; i < Rows.Count; i++)
                {
                    if (y + rowH > Height) break;
                    var row = Rows[i];
                    var r = new Rectangle(0, y, Width, rowH);

                    if (i > 0)
                    {
                        using (var pen = new Pen(Theme.Border))
                            g.DrawLine(pen, Theme.S(2), y, Width - Theme.S(2), y);
                    }

                    Color accent = Theme.VendorColor(row.Vendor);
                    using (var b = new SolidBrush(Color.FromArgb(row.Active ? 220 : 70, accent)))
                        g.FillRectangle(b, 0, y + Theme.S(13), Theme.S(3), rowH - Theme.S(26));

                    // right-aligned adapter badge, measured first so the text column can be clipped.
                    int textRight = Width - Theme.S(12);
                    using (Font f = Theme.Regular(10.5f))
                    using (var b = new SolidBrush(Color.FromArgb(row.Active ? 235 : 140, accent)))
                    {
                        SizeF sz = g.MeasureString(row.Adapter, f, int.MaxValue, typo);
                        float bx = Width - sz.Width - Theme.S(12);
                        g.DrawString(row.Adapter, f, b, bx, y + (rowH - sz.Height) / 2f, typo);
                        textRight = (int)bx - Theme.S(14);
                    }

                    if (row.Primary)
                    {
                        using (Font f = Theme.SemiBold(9f))
                        using (var b = new SolidBrush(Theme.Accent))
                        {
                            SizeF sz = g.MeasureString(Loc.PrimaryBadge, f, int.MaxValue, typo);
                            float pw = sz.Width + Theme.S(14);
                            float ph = Theme.S(18);
                            float px = textRight - pw;
                            float py = y + (rowH - ph) / 2f;
                            var pr = new RectangleF(px, py, pw, ph);
                            using (GraphicsPath path = Theme.RoundedRect(Rectangle.Round(pr), Theme.S(9)))
                            using (var fill = new SolidBrush(Color.FromArgb(30, Theme.Accent)))
                            using (var pen = new Pen(Color.FromArgb(85, Theme.Accent)))
                            {
                                g.FillPath(fill, path);
                                g.DrawPath(pen, path);
                            }
                            g.DrawString(Loc.PrimaryBadge, f, b, px + Theme.S(7), py + (ph - sz.Height) / 2f, typo);
                            textRight = (int)px - Theme.S(12);
                        }
                    }

                    int textW = Math.Max(Theme.S(80), textRight - Theme.S(14));
                    using (Font f = Theme.SemiBold(12f))
                    using (var b = new SolidBrush(row.Active ? Theme.TextPrimary : Theme.TextSecondary))
                        g.DrawString(Fit(g, row.Code, f, textW), f, b, Theme.S(14), y + Theme.S(8), typo);

                    using (Font f = Theme.Regular(10.5f))
                    using (var b = new SolidBrush(Theme.TextMuted))
                        g.DrawString(Fit(g, row.Detail, f, textW), f, b, Theme.S(14), y + Theme.S(25), typo);

                    y += rowH;
                }
            }
        }

        private static string Fit(Graphics g, string text, Font f, int width)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (g.MeasureString(text, f, int.MaxValue, StringFormat.GenericTypographic).Width <= width) return text;
            string s = text;
            while (s.Length > 3 && g.MeasureString(s + "…", f, int.MaxValue, StringFormat.GenericTypographic).Width > width)
                s = s.Substring(0, s.Length - 1);
            return s + "…";
        }
    }
}
