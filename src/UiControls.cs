// Quench - custom-drawn controls for the dark UI.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace Quench
{
    /// <summary>A rounded, dark card with an optional title.</summary>
    internal class Card : Panel
    {
        public string Title = "";
        public string Caption = "";
        public int Radius = 10;

        public Card()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.Background);

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(Radius)))
            using (var fill = new SolidBrush(Theme.Card))
            using (var pen = new Pen(Theme.Border))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            int x = Theme.S(20);
            int y = Theme.S(15);
            if (!string.IsNullOrEmpty(Title))
            {
                using (Font f = Theme.SemiBold(13f))
                using (var b = new SolidBrush(Theme.TextSecondary))
                {
                    g.DrawString(Title.ToUpperInvariant(), f, b, x, y);
                }
                y += Theme.S(20);
            }
            if (!string.IsNullOrEmpty(Caption))
            {
                using (Font f = Theme.Regular(11.5f))
                using (var b = new SolidBrush(Theme.TextMuted))
                {
                    g.DrawString(Caption, f, b, x, y);
                }
            }
        }
    }

    /// <summary>A small rounded status chip with optional coloured dot.</summary>
    internal class Pill : Control
    {
        public Color Accent = Theme.TextSecondary;
        public bool ShowDot = true;
        public bool Filled = false;

        public Pill()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Background;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.Background);

            int padX = Theme.S(9);
            int dot = Theme.S(6);
            using (Font f = Theme.SemiBold(11f))
            {
                SizeF sz = g.MeasureString(Text, f, int.MaxValue, StringFormat.GenericTypographic);
                int w = (int)Math.Ceiling(sz.Width) + padX * 2 + Theme.S(2);
                if (ShowDot) w += dot + Theme.S(6);
                var r = new Rectangle(0, (Height - Theme.S(22)) / 2, Math.Min(w, Width), Theme.S(22));

                using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(11)))
                {
                    if (Filled)
                    {
                        using (var b = new SolidBrush(Color.FromArgb(34, Accent)))
                            g.FillPath(b, path);
                    }
                    using (var pen = new Pen(Color.FromArgb(Filled ? 90 : 60, Accent)))
                        g.DrawPath(pen, path);
                }

                int tx = r.X + padX;
                if (ShowDot)
                {
                    using (var b = new SolidBrush(Accent))
                        g.FillEllipse(b, tx, r.Y + (r.Height - dot) / 2, dot, dot);
                    tx += dot + Theme.S(6);
                }
                using (var b = new SolidBrush(Accent))
                    g.DrawString(Text, f, b, tx, r.Y + (r.Height - sz.Height) / 2f, StringFormat.GenericTypographic);
            }
        }
    }

    /// <summary>A filled or outlined action button.</summary>
    internal class ActionButton : Control
    {
        public bool Primary = true;
        public Color Accent = Theme.Accent;
        public string SubText = "";
        private bool _hover;
        private bool _down;

        public ActionButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _down = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color accent = Enabled ? Accent : Theme.BorderStrong;

            using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(8)))
            {
                if (Primary)
                {
                    Color top = Enabled ? Theme.Blend(accent, Color.White, _down ? 0.0 : (_hover ? 0.12 : 0.05)) : Theme.CardHover;
                    Color bottom = Enabled ? Theme.Blend(accent, Color.Black, _down ? 0.18 : (_hover ? 0.05 : 0.14)) : Theme.Card;
                    using (var b = new LinearGradientBrush(r, top, bottom, 90f))
                        g.FillPath(b, path);
                }
                else
                {
                    using (var b = new SolidBrush(_hover && Enabled ? Theme.CardHover : Theme.Card))
                        g.FillPath(b, path);
                    using (var pen = new Pen(accent))
                        g.DrawPath(pen, path);
                }
            }

            Color fg;
            if (Primary) fg = Enabled ? Color.FromArgb(8, 15, 15) : Theme.TextMuted;
            else fg = Enabled ? Theme.TextPrimary : Theme.TextMuted;

            using (Font f = Theme.Bold(13.5f))
            using (var b = new SolidBrush(fg))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                if (string.IsNullOrEmpty(SubText))
                {
                    g.DrawString(Text, f, b, new RectangleF(0, 0, Width, Height), sf);
                }
                else
                {
                    var top = new RectangleF(0, Height * 0.22f, Width, Height * 0.38f);
                    g.DrawString(Text, f, b, top, sf);
                    using (Font f2 = Theme.Regular(10.5f))
                    using (var b2 = new SolidBrush(Primary ? Color.FromArgb(175, fg) : Theme.TextMuted))
                    {
                        var bot = new RectangleF(0, Height * 0.56f, Width, Height * 0.32f);
                        g.DrawString(SubText, f2, b2, bot, sf);
                    }
                }
            }
        }
    }

    /// <summary>The headline routing diagram: monitor box -> link -> GPU box.</summary>
    internal class RouteDiagram : Control
    {
        public string MonitorCode = "—";
        public string MonitorDetail = "";
        public string GpuName = "—";
        public string GpuKind = "";
        public string Note = "";
        public Color GpuAccent = Theme.TextSecondary;
        public bool IsDirect;
        public bool HasRoute;

        public RouteDiagram()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Theme.Card);

            int linkW = Theme.S(56);
            int boxH = Theme.S(94);
            int monW = (int)((Width - linkW) * 0.43f);
            int gpuW = Width - linkW - monW;

            // Centre the whole group vertically so a tall card does not look top-heavy.
            int blockH = boxH;
            if (!string.IsNullOrEmpty(Note)) blockH += Theme.S(18);
            using (Font fn = Theme.Regular(11.5f))
            {
                if (!string.IsNullOrEmpty(Note))
                    blockH += (int)Math.Ceiling(g.MeasureString(Note, fn, Math.Max(Theme.S(120), Width)).Height);
            }
            int top = (Height - blockH) / 2;
            if (top < 0) top = 0;

            var monitorRect = new Rectangle(0, top, monW, boxH);
            var gpuRect = new Rectangle(Width - gpuW, top, gpuW, boxH);

            DrawMonitor(g, monitorRect);
            DrawGpu(g, gpuRect);

            int x1 = monitorRect.Right;
            int x2 = gpuRect.Left;
            int yMid = monitorRect.Top + boxH / 2;
            Color linkColor = HasRoute ? GpuAccent : Theme.BorderStrong;
            using (var pen = new Pen(Color.FromArgb(170, linkColor), Theme.SF(1.8f)))
            {
                pen.DashStyle = DashStyle.Dot;
                pen.DashPattern = new float[] { 1.4f, 2.2f };
                g.DrawLine(pen, x1 + Theme.S(7), yMid, x2 - Theme.S(10), yMid);
            }
            if (HasRoute)
            {
                using (var b = new SolidBrush(linkColor))
                {
                    int a = Theme.S(5);
                    var pts = new PointF[] {
                        new PointF(x2 - Theme.S(9), yMid),
                        new PointF(x2 - Theme.S(9) - a * 1.5f, yMid - a),
                        new PointF(x2 - Theme.S(9) - a * 1.5f, yMid + a)
                    };
                    g.FillPolygon(b, pts);
                }
            }

            if (!string.IsNullOrEmpty(Note))
            {
                using (Font f = Theme.Regular(11.5f))
                using (var b = new SolidBrush(Theme.TextSecondary))
                {
                    var area = new RectangleF(0, monitorRect.Bottom + Theme.S(20), Width, Height - monitorRect.Bottom - Theme.S(20));
                    g.DrawString(Note, f, b, area);
                }
            }
        }

        private void DrawMonitor(Graphics g, Rectangle r)
        {
            using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(8)))
            using (var fill = new SolidBrush(Theme.CardHover))
            using (var pen = new Pen(Theme.BorderStrong))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            int pad = Theme.S(13);
            var screen = new Rectangle(r.X + pad, r.Y + pad + Theme.S(4), Theme.S(24), Theme.S(16));
            using (var pen = new Pen(Theme.TextMuted, Theme.SF(1.3f)))
            {
                g.DrawRectangle(pen, screen);
                g.DrawLine(pen, screen.X + screen.Width / 2, screen.Bottom, screen.X + screen.Width / 2, screen.Bottom + Theme.S(3));
                g.DrawLine(pen, screen.X + Theme.S(5), screen.Bottom + Theme.S(3), screen.Right - Theme.S(5), screen.Bottom + Theme.S(3));
            }

            int tx = r.X + pad + Theme.S(32);
            int avail = r.Right - tx - pad;
            using (Font f = Theme.SemiBold(11.5f))
            using (var b = new SolidBrush(Theme.TextPrimary))
                g.DrawString(Fit(g, MonitorCode, f, avail), f, b, tx, r.Y + pad);

            using (Font f = Theme.Regular(10.5f))
            using (var b = new SolidBrush(Theme.TextSecondary))
                g.DrawString(Fit(g, MonitorDetail, f, avail), f, b, tx, r.Y + pad + Theme.S(20));

            string tag = IsDirect ? Loc.WiredToDgpu : Loc.WiredToIgpu;
            if (!HasRoute) tag = Loc.NoDisplayTag;
            using (Font f = Theme.Regular(10f))
            using (var b = new SolidBrush(Theme.TextMuted))
                g.DrawString(Fit(g, tag, f, r.Width - pad * 2), f, b, r.X + pad, r.Bottom - Theme.S(22));
        }

        private void DrawGpu(Graphics g, Rectangle r)
        {
            Color accent = HasRoute ? GpuAccent : Theme.BorderStrong;
            using (GraphicsPath path = Theme.RoundedRect(r, Theme.S(8)))
            using (var fill = new SolidBrush(Theme.CardHover))
            using (var pen = new Pen(HasRoute ? Color.FromArgb(95, accent) : Theme.BorderStrong))
            {
                g.FillPath(fill, path);
                g.DrawPath(pen, path);
            }

            int pad = Theme.S(13);
            var chip = new Rectangle(r.X + pad, r.Y + pad + Theme.S(5), Theme.S(18), Theme.S(18));
            using (var pen = new Pen(accent, Theme.SF(1.4f)))
                g.DrawRectangle(pen, chip);
            using (var pen = new Pen(Color.FromArgb(150, accent), Theme.SF(1.1f)))
            {
                for (int i = 0; i < 2; i++)
                {
                    int yy = chip.Y + Theme.S(5) + i * Theme.S(5);
                    g.DrawLine(pen, chip.X - Theme.S(3), yy, chip.X, yy);
                    g.DrawLine(pen, chip.Right, yy, chip.Right + Theme.S(3), yy);
                }
            }

            int tx = r.X + pad + Theme.S(26);
            int avail = r.Right - tx - pad;
            using (Font f = Theme.SemiBold(11.5f))
            using (var b = new SolidBrush(Theme.TextPrimary))
                g.DrawString(Fit(g, GpuName, f, avail), f, b, tx, r.Y + pad);

            using (Font f = Theme.Regular(10.5f))
            using (var b = new SolidBrush(HasRoute ? accent : Theme.TextMuted))
                g.DrawString(Fit(g, GpuKind, f, avail), f, b, tx, r.Y + pad + Theme.S(20));

            using (Font f = Theme.Regular(10f))
            using (var b = new SolidBrush(Theme.TextMuted))
                g.DrawString(Fit(g, HasRoute ? Loc.DrivesDisplay : Loc.DrivesNothing, f, r.Width - pad * 2),
                    f, b, r.X + pad, r.Bottom - Theme.S(22));
        }

        private static string Fit(Graphics g, string text, Font f, int width)
        {
            if (string.IsNullOrEmpty(text)) return "";
            SizeF sz = g.MeasureString(text, f, int.MaxValue, StringFormat.GenericTypographic);
            if (sz.Width <= width) return text;
            string s = text;
            while (s.Length > 3 && g.MeasureString(s + "…", f, int.MaxValue, StringFormat.GenericTypographic).Width > width)
                s = s.Substring(0, s.Length - 1);
            return s + "…";
        }
    }

    /// <summary>A compact row: label on the left, value on the right.</summary>
    internal class StatRow : Control
    {
        public string Label = "";
        public string Value = "";
        public Color ValueColor = Theme.TextPrimary;

        public StatRow()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using (var typo = StringFormat.GenericTypographic)
            {
                int y = (Height - Theme.S(15)) / 2;
                if (y < 0) y = 0;

                using (Font f = Theme.Regular(11.5f))
                using (var b = new SolidBrush(Theme.TextMuted))
                    g.DrawString(Label, f, b, 0, y, typo);

                using (Font f = Theme.SemiBold(11.5f))
                using (var b = new SolidBrush(ValueColor))
                using (var sf = new StringFormat { Alignment = StringAlignment.Far })
                {
                    sf.FormatFlags = StringFormatFlags.NoWrap;
                    g.DrawString(Value, f, b, new RectangleF(0, y, Width, Height), sf);
                }
            }
        }
    }

    /// <summary>Big numeric readout used for the dGPU wattage.</summary>
    internal class BigValue : Control
    {
        public string Value = "—";
        public string Unit = "";
        public string Caption = "";
        public Color Accent = Theme.Accent;

        public BigValue()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Card;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var typo = StringFormat.GenericTypographic)
            using (Font f = Theme.Font(40f, FontStyle.Bold))
            {
                SizeF sz = g.MeasureString(Value, f, int.MaxValue, typo);
                using (var b = new SolidBrush(Accent))
                    g.DrawString(Value, f, b, -Theme.S(2), 0, typo);

                if (!string.IsNullOrEmpty(Unit))
                {
                    using (Font fu = Theme.SemiBold(14f))
                    using (var b = new SolidBrush(Theme.TextSecondary))
                    {
                        SizeF su = g.MeasureString(Unit, fu, int.MaxValue, typo);
                        g.DrawString(Unit, fu, b, sz.Width + Theme.S(2), sz.Height - su.Height - Theme.S(8), typo);
                    }
                }
            }
            if (!string.IsNullOrEmpty(Caption))
            {
                using (Font f = Theme.Regular(11f))
                using (var b = new SolidBrush(Theme.TextMuted))
                    g.DrawString(Caption, f, b, 0, Theme.S(50));
            }
        }
    }
}
