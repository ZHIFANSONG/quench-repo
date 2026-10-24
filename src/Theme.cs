// Quench - visual theme: palette, fonts and DPI scaling helpers.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Quench
{
    internal static class Theme
    {
        public static float Scale = 1f;

        public static readonly Color Background = Color.FromArgb(14, 17, 22);
        public static readonly Color Card = Color.FromArgb(22, 27, 34);
        public static readonly Color CardHover = Color.FromArgb(26, 32, 40);
        public static readonly Color Border = Color.FromArgb(35, 42, 52);
        public static readonly Color BorderStrong = Color.FromArgb(48, 58, 70);
        public static readonly Color TextPrimary = Color.FromArgb(230, 237, 243);
        public static readonly Color TextSecondary = Color.FromArgb(139, 148, 158);
        public static readonly Color TextMuted = Color.FromArgb(100, 110, 122);
        public static readonly Color Accent = Color.FromArgb(45, 212, 191);
        public static readonly Color AccentDark = Color.FromArgb(23, 132, 120);
        public static readonly Color Amber = Color.FromArgb(240, 180, 41);
        public static readonly Color Red = Color.FromArgb(248, 81, 73);
        public static readonly Color Green = Color.FromArgb(63, 185, 80);
        public static readonly Color Nvidia = Color.FromArgb(118, 185, 0);
        public static readonly Color Amd = Color.FromArgb(237, 28, 36);
        public static readonly Color Intel = Color.FromArgb(0, 113, 197);

        private static string _family = "Segoe UI";
        private static bool _resolved;

        public static int S(float value)
        {
            return (int)Math.Round(value * Scale);
        }

        public static float SF(float value)
        {
            return value * Scale;
        }

        private static string Family
        {
            get
            {
                if (_resolved) return _family;
                _resolved = true;
                try
                {
                    using (var fonts = new InstalledFontCollection())
                    {
                        bool has = false;
                        foreach (FontFamily f in fonts.Families)
                        {
                            if (string.Equals(f.Name, "Segoe UI", StringComparison.OrdinalIgnoreCase)) { has = true; break; }
                        }
                        if (!has) _family = "Tahoma";
                    }
                }
                catch
                {
                    _family = "Tahoma";
                }
                return _family;
            }
        }

        public static Font Font(float size, FontStyle style)
        {
            return new Font(Family, SF(size), style, GraphicsUnit.Pixel);
        }

        public static Font Regular(float size) { return Font(size, FontStyle.Regular); }
        public static Font Bold(float size) { return Font(size, FontStyle.Bold); }
        public static Font SemiBold(float size) { return Font(size, FontStyle.Bold); }

        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0) { path.AddRectangle(r); return path; }
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static Color VendorColor(GpuVendor v)
        {
            switch (v)
            {
                case GpuVendor.Nvidia: return Nvidia;
                case GpuVendor.Amd: return Amd;
                case GpuVendor.Intel: return Intel;
                case GpuVendor.Virtual: return Color.FromArgb(130, 120, 190);
                default: return TextSecondary;
            }
        }

        public static Color Blend(Color a, Color b, double t)
        {
            if (t < 0) t = 0; if (t > 1) t = 1;
            return Color.FromArgb(
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }
    }
}
