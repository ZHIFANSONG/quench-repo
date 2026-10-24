// Quench - main window.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Windows.Forms;

namespace Quench
{
    internal sealed class MainForm : Form
    {
        // Reading NVML wakes the GPU out of D3, so telemetry is sampled only when the
        // device is already awake, and never more often than this.
        private const int NvmlMinAgeSeconds = 6;
        private const int TelemetryFreshSeconds = 14;

        private readonly Card _routeCard = new Card();
        private readonly Card _gpuCard = new Card();
        private readonly Card _displayCard = new Card();

        private readonly RouteDiagram _route = new RouteDiagram();
        private readonly BigValue _watts = new BigValue();
        private readonly StatRow _stateRow = new StatRow();
        private readonly StatRow _powerRow = new StatRow();
        private readonly StatRow _tempRow = new StatRow();
        private readonly StatRow _clockRow = new StatRow();
        private readonly StatRow _pcieRow = new StatRow();
        private readonly StatRow _memRow = new StatRow();
        private readonly MonitorList _monitors = new MonitorList();

        private readonly ActionButton _toggle = new ActionButton();
        private readonly ActionButton _refresh = new ActionButton();
        private readonly ActionButton _lang = new ActionButton();
        private readonly Pill _adminPill = new Pill();
        private readonly Pill _routePill = new Pill();

        private readonly Timer _timer = new Timer();

        private List<AdapterInfo> _adapters = new List<AdapterInfo>();
        private GpuDevice _dgpu;
        private GpuTelemetry _telemetry = new GpuTelemetry();
        private Nvml _nvml;
        private string _nvmlError;
        private int _telemetryAge = -1;
        private bool _busy;
        private int _ticks;
        private string _status = "";

        public MainForm()
        {
            Text = "Quench";
            BackColor = Theme.Background;
            ForeColor = Theme.TextPrimary;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Theme.Regular(12f);
            DoubleBuffered = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            SuspendLayout();
            Controls.Add(_routeCard);
            Controls.Add(_gpuCard);
            Controls.Add(_displayCard);

            _routeCard.Controls.Add(_route);
            _gpuCard.Controls.Add(_watts);
            _gpuCard.Controls.Add(_stateRow);
            _gpuCard.Controls.Add(_powerRow);
            _gpuCard.Controls.Add(_tempRow);
            _gpuCard.Controls.Add(_clockRow);
            _gpuCard.Controls.Add(_pcieRow);
            _gpuCard.Controls.Add(_memRow);
            _displayCard.Controls.Add(_monitors);

            _toggle.Cursor = Cursors.Hand;
            _toggle.Click += delegate { ToggleDgpu(); };

            _refresh.Primary = false;
            _refresh.Accent = Theme.BorderStrong;
            _refresh.Click += delegate { RefreshAll(true); };

            _lang.Primary = false;
            _lang.Accent = Theme.BorderStrong;
            _lang.Click += delegate
            {
                Loc.English = !Loc.English;
                ApplyStaticText();
                ApplyData();
                Invalidate(true);
            };

            _adminPill.ShowDot = true;
            _adminPill.Filled = true;
            _routePill.ShowDot = true;
            _routePill.Filled = true;

            Controls.Add(_toggle);
            Controls.Add(_refresh);
            Controls.Add(_lang);
            Controls.Add(_adminPill);
            Controls.Add(_routePill);

            ResumeLayout(false);

            Theme.Scale = ComputeScale();
            ClientSize = new Size(Theme.S(950), Theme.S(604));
            MinimumSize = new Size(Theme.S(880), Theme.S(600));

            ApplyStaticText();

            _timer.Interval = 2000;
            _timer.Tick += delegate { RefreshAll(false); };
            _timer.Start();

            Shown += delegate
            {
                RefreshAll(true);
                _timer.Start();
            };
            Resize += delegate { LayoutChildren(); };
            FormClosed += delegate { if (_nvml != null) _nvml.Dispose(); };

            LayoutChildren();
        }

        private void ApplyStaticText()
        {
            _routeCard.Title = Loc.CardRouting;
            _gpuCard.Title = Loc.CardGpu;
            _displayCard.Title = Loc.CardDisplays;
            _displayCard.Caption = Loc.DisplaysCaption;

            _stateRow.Label = Loc.LblDeviceState;
            _powerRow.Label = Loc.LblPowerState;
            _tempRow.Label = Loc.LblTemperature;
            _clockRow.Label = Loc.LblClocks;
            _pcieRow.Label = Loc.LblPcie;
            _memRow.Label = Loc.LblMemory;

            _refresh.Text = Loc.BtnRescan;
            _lang.Text = Loc.English ? "中文" : "EN";

            Text = "Quench";
            Invalidate(true);
        }

        private static float ComputeScale()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    float s = g.DpiX / 96f;
                    if (s < 1f) s = 1f;
                    if (s > 2.5f) s = 2.5f;
                    return s;
                }
            }
            catch
            {
                return 1f;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int pad = Theme.S(24);
            int titleY = Theme.S(14);
            int nameW;
            using (Font f = Theme.Bold(25f))
            using (var b = new SolidBrush(Theme.TextPrimary))
            {
                g.DrawString("Quench", f, b, pad, titleY);
                nameW = (int)Math.Ceiling(g.MeasureString("Quench", f).Width);
            }

            using (var b = new SolidBrush(Theme.Accent))
                g.FillRectangle(b, pad + nameW + Theme.S(10), titleY + Theme.S(8), Theme.S(4), Theme.S(21));

            using (Font f = Theme.Regular(12f))
            using (var b = new SolidBrush(Theme.TextMuted))
                g.DrawString(Loc.Tagline, f, b, pad + nameW + Theme.S(24), titleY + Theme.S(10));

            if (!string.IsNullOrEmpty(_status))
            {
                using (Font f = Theme.Regular(11f))
                using (var b = new SolidBrush(Theme.TextSecondary))
                using (var sf = new StringFormat { Alignment = StringAlignment.Far })
                    g.DrawString(_status, f, b, new RectangleF(pad, titleY + Theme.S(44), Width - pad * 2f, Theme.S(20)), sf);
            }
        }

        private void LayoutChildren()
        {
            if (_routeCard == null) return;
            int pad = Theme.S(24);
            int headerH = Theme.S(72);
            int gap = Theme.S(14);
            int footerH = Theme.S(82);

            int w = ClientSize.Width - pad * 2;
            int h = ClientSize.Height - headerH - footerH;
            if (w < Theme.S(400) || h < Theme.S(220)) return;

            int routeW = (int)(w * 0.55f);
            int gpuW = w - routeW - gap;

            // The display list gets exactly the height its rows need; the rest goes to the top row.
            int rows = _monitors.Rows.Count;
            if (rows < 1) rows = 1;
            if (rows > 3) rows = 3;
            int listH = Theme.S(48) * rows + Theme.S(80);
            int maxList = (int)(h * 0.52f);
            if (listH > maxList) listH = maxList;
            int topRowH = h - listH - gap;
            if (topRowH < Theme.S(250)) { topRowH = Theme.S(250); listH = h - topRowH - gap; }

            _routeCard.Bounds = new Rectangle(pad, headerH, routeW, topRowH);
            _gpuCard.Bounds = new Rectangle(pad + routeW + gap, headerH, gpuW, topRowH);
            _displayCard.Bounds = new Rectangle(pad, headerH + topRowH + gap, w, listH);

            int innerPad = Theme.S(20);
            _route.Bounds = new Rectangle(innerPad, Theme.S(46), routeW - innerPad * 2, topRowH - Theme.S(62));

            int gInner = Theme.S(20);
            int gW = gpuW - gInner * 2;
            _watts.Bounds = new Rectangle(gInner, Theme.S(42), gW, Theme.S(74));

            int y = Theme.S(124);
            int rowH = Theme.S(26);
            _stateRow.Bounds = new Rectangle(gInner, y, gW, rowH); y += rowH;
            _powerRow.Bounds = new Rectangle(gInner, y, gW, rowH); y += rowH;
            _tempRow.Bounds = new Rectangle(gInner, y, gW, rowH); y += rowH;
            _clockRow.Bounds = new Rectangle(gInner, y, gW, rowH); y += rowH;
            _pcieRow.Bounds = new Rectangle(gInner, y, gW, rowH); y += rowH;
            _memRow.Bounds = new Rectangle(gInner, y, gW, rowH);

            _monitors.Bounds = new Rectangle(Theme.S(20), Theme.S(58), w - Theme.S(40), Math.Max(Theme.S(48), listH - Theme.S(74)));

            int footerY = ClientSize.Height - footerH + Theme.S(10);
            _toggle.Bounds = new Rectangle(pad, footerY, Theme.S(300), Theme.S(52));
            _refresh.Bounds = new Rectangle(pad + Theme.S(312), footerY, Theme.S(104), Theme.S(52));
            _lang.Bounds = new Rectangle(pad + Theme.S(428), footerY + Theme.S(6), Theme.S(56), Theme.S(40));

            int pillY = footerY + (Theme.S(52) - Theme.S(24)) / 2;
            _adminPill.Bounds = new Rectangle(pad + Theme.S(508), pillY, Theme.S(126), Theme.S(24));
            _routePill.Bounds = new Rectangle(pad + Theme.S(646), pillY, Theme.S(190), Theme.S(24));

            _routePill.Text = _route.IsDirect ? Loc.PillDirect : Loc.PillViaIgpu;
            _routePill.Accent = _route.IsDirect ? Theme.Amber : Theme.Accent;
        }

        // ---------------- data ----------------

        private void RefreshAll(bool full)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                _ticks++;
                bool rescan = full || _ticks % 5 == 1;
                if (rescan)
                {
                    _adapters = DisplayTopology.Query();
                    List<GpuDevice> devices = GpuDevices.QueryAll();
                    _dgpu = null;
                    for (int i = 0; i < devices.Count; i++)
                    {
                        if (devices[i].Vendor == GpuVendor.Nvidia) { _dgpu = devices[i]; break; }
                    }
                }

                // Cheap and passive: reading the device power state wakes nothing.
                if (_dgpu != null) GpuDevices.ReadPower(_dgpu);

                SampleTelemetry(full || rescan);

                // Reading NVML pulls the device back to D0. Re-read the power state so the
                // "power state" row cannot claim D3 while the wattage row shows 27 W.
                if (_telemetryAge == 0 && _dgpu != null) GpuDevices.ReadPower(_dgpu);

                ApplyData();
                Invalidate();
            }
            catch (Exception ex)
            {
                _status = Loc.StError + ex.Message;
            }
            finally
            {
                _busy = false;
            }
        }

        /// <summary>Sample NVML only when it cannot be the thing that woke the GPU.</summary>
        private void SampleTelemetry(bool force)
        {
            if (_telemetryAge >= 0) _telemetryAge += 2;

            if (_dgpu != null && !_dgpu.EnabledOnMachine)
            {
                // Nothing to read: the driver is not bound to the device at all.
                if (_nvml != null) { _nvml.Dispose(); _nvml = null; }
                _telemetry = new GpuTelemetry();
                _telemetryAge = -1;
                _nvmlError = Loc.CapNoTelemetry;
                return;
            }

            bool asleep = _dgpu != null && _dgpu.PowerDataValid && _dgpu.PowerState >= 3;
            bool due = _telemetryAge < 0 || _telemetryAge >= NvmlMinAgeSeconds;
            if (!force && (asleep || !due)) return;

            if (_nvml == null) _nvml = new Nvml();
            if (!_nvml.IsReady)
            {
                string err;
                if (!_nvml.Initialize(out err)) { _nvmlError = err; _telemetry = new GpuTelemetry(); _telemetry.Error = err; _telemetryAge = -1; return; }
                _nvmlError = null;
            }
            GpuTelemetry t = _nvml.Read();
            if (!t.Available) { _nvml.Dispose(); _nvml = null; _telemetryAge = -1; return; }
            _telemetry = t;
            _telemetryAge = 0;
        }

        private bool TelemetryFresh { get { return _telemetry.Available && _telemetryAge >= 0 && _telemetryAge <= TelemetryFreshSeconds; } }

        private void ApplyData()
        {
            bool dgpuFound = _dgpu != null;
            bool dgpuEnabled = _dgpu != null && _dgpu.EnabledOnMachine;
            bool asleep = _dgpu != null && _dgpu.PowerDataValid && _dgpu.PowerState >= 3;

            // ---- routing ----
            MonitorInfo primary = null;
            AdapterInfo primaryAdapter = null;
            foreach (AdapterInfo a in _adapters)
            {
                if (!a.Attached) continue;
                foreach (MonitorInfo m in a.Monitors)
                {
                    if (m.Attached && m.Primary) { primary = m; primaryAdapter = a; break; }
                }
                if (primary != null) break;
            }
            if (primary == null)
            {
                foreach (AdapterInfo a in _adapters)
                {
                    if (!a.Attached) continue;
                    foreach (MonitorInfo m in a.Monitors)
                    {
                        if (m.Attached) { primary = m; primaryAdapter = a; break; }
                    }
                    if (primary != null) break;
                }
            }

            bool hasRoute = primary != null && primaryAdapter != null;
            _route.HasRoute = hasRoute;
            if (hasRoute)
            {
                _route.MonitorCode = primary.MonitorName;
                _route.MonitorDetail = primary.Width + " x " + primary.Height + " @ " + primary.Refresh + " Hz";
                _route.GpuName = primaryAdapter.Description;
                _route.GpuKind = VendorLabel(primaryAdapter.Vendor);
                _route.GpuAccent = Theme.VendorColor(primaryAdapter.Vendor);
                _route.IsDirect = primaryAdapter.Vendor == GpuVendor.Nvidia;
                _route.Note = !dgpuEnabled ? Loc.NoteDisabled : (_route.IsDirect ? Loc.NoteDirect : Loc.NoteHybrid);
            }
            else
            {
                _route.MonitorCode = Loc.NoActiveDisplay;
                _route.MonitorDetail = "—";
                _route.GpuName = "—";
                _route.GpuKind = "";
                _route.GpuAccent = Theme.TextSecondary;
                _route.IsDirect = false;
                _route.Note = Loc.NoteNone;
            }
            _route.Invalidate();

            _routePill.Text = hasRoute ? (_route.IsDirect ? Loc.PillDirect : Loc.PillViaIgpu) : Loc.PillNoDisplay;
            _routePill.Accent = !hasRoute ? Theme.TextMuted : (_route.IsDirect ? Theme.Amber : Theme.Accent);
            _routePill.Invalidate();

            // ---- monitors list ----
            var rows = new List<MonitorRow>();
            foreach (AdapterInfo a in _adapters)
            {
                foreach (MonitorInfo m in a.Monitors)
                {
                    if (!m.Attached) continue;
                    var row = new MonitorRow();
                    row.Code = m.MonitorName;
                    row.Detail = m.Width + " × " + m.Height + " @ " + m.Refresh + "Hz";
                    row.Adapter = a.Description;
                    row.Vendor = a.Vendor;
                    row.Active = true;
                    row.Primary = m.Primary;
                    rows.Add(row);
                }
            }
            if (rows.Count == 0)
            {
                foreach (AdapterInfo a in _adapters)
                {
                    if (!a.Attached || a.Vendor == GpuVendor.Virtual) continue;
                    var row = new MonitorRow();
                    row.Code = a.Description;
                    row.Detail = Loc.NoMonitorAttached;
                    row.Adapter = a.Description;
                    row.Vendor = a.Vendor;
                    row.Active = false;
                    rows.Add(row);
                }
            }
            _monitors.SetRows(rows);
            LayoutChildren();

            // ---- gpu card ----
            if (!dgpuEnabled)
            {
                _watts.Value = dgpuFound ? "0" : "—";
                _watts.Unit = dgpuFound ? " W" : "";
                _watts.Caption = dgpuFound ? Loc.CapDisabled : Loc.CapNoDgpu;
                _watts.Accent = dgpuFound ? Theme.Green : Theme.TextMuted;
            }
            else if (asleep)
            {
                _watts.Value = "0";
                _watts.Unit = " W";
                _watts.Caption = Loc.CapAsleep;
                _watts.Accent = Theme.Green;
            }
            else if (TelemetryFresh && _telemetry.PowerWatts >= 0)
            {
                _watts.Value = _telemetry.PowerWatts.ToString("0.0", CultureInfo.InvariantCulture);
                _watts.Unit = " W";
                _watts.Caption = _telemetryAge <= 2 ? Loc.CapJustMeasured : Loc.CapAgo(_telemetryAge);
                _watts.Accent = _telemetry.PowerWatts >= 20 ? Theme.Amber : Theme.Accent;
            }
            else
            {
                _watts.Value = "—";
                _watts.Unit = "";
                _watts.Caption = _nvmlError != null ? _nvmlError : Loc.CapWaiting;
                _watts.Accent = Theme.TextMuted;
            }
            _watts.Invalidate();

            _stateRow.Value = dgpuFound ? _dgpu.StateText : Loc.StateNotFound;
            _stateRow.ValueColor = !dgpuFound ? Theme.TextMuted : (dgpuEnabled ? Theme.TextPrimary : Theme.Green);
            _stateRow.Invalidate();

            _powerRow.Value = dgpuFound ? _dgpu.PowerStateText : "—";
            _powerRow.ValueColor = !dgpuFound ? Theme.TextMuted
                : (_dgpu.PowerDataValid && _dgpu.PowerState >= 3 ? Theme.Green : Theme.TextPrimary);
            _powerRow.Invalidate();

            // Grey = last known value from an older sample, not a live reading.
            Color staleColor = TelemetryFresh ? Theme.TextPrimary : Theme.TextMuted;

            _tempRow.Value = _telemetry.Available && _telemetry.Temperature >= 0 ? _telemetry.Temperature + " °C" : "—";
            _tempRow.ValueColor = staleColor;
            _tempRow.Invalidate();

            _clockRow.Value = _telemetry.Available && (_telemetry.ClockGraphics > 0 || _telemetry.ClockMemory > 0)
                ? _telemetry.ClockGraphics + " / " + _telemetry.ClockMemory + " MHz"
                : "—";
            _clockRow.ValueColor = staleColor;
            _clockRow.Invalidate();

            _pcieRow.Value = _telemetry.Available && _telemetry.PcieGeneration > 0
                ? "Gen" + _telemetry.PcieGeneration + " × " + _telemetry.PcieWidth
                : "—";
            _pcieRow.ValueColor = staleColor;
            _pcieRow.Invalidate();

            _memRow.Value = _telemetry.Available && _telemetry.MemoryTotalMb > 0
                ? _telemetry.MemoryUsedMb.ToString("0", CultureInfo.InvariantCulture) + " / " +
                  _telemetry.MemoryTotalMb.ToString("0", CultureInfo.InvariantCulture) + " MB"
                : "—";
            _memRow.ValueColor = staleColor;
            _memRow.Invalidate();

            // ---- action button ----
            if (!dgpuFound)
            {
                _toggle.Text = Loc.BtnNoGpu;
                _toggle.SubText = "";
                _toggle.Primary = false;
                _toggle.Accent = Theme.BorderStrong;
                _toggle.Enabled = false;
            }
            else if (dgpuEnabled)
            {
                _toggle.Text = Loc.BtnDisable;
                _toggle.SubText = Loc.SubDisable;
                _toggle.Primary = true;
                _toggle.Accent = Theme.Accent;
                _toggle.Enabled = true;
            }
            else
            {
                _toggle.Text = Loc.BtnEnable;
                _toggle.SubText = Loc.SubEnable;
                _toggle.Primary = false;
                _toggle.Accent = Theme.Green;
                _toggle.Enabled = true;
            }
            _toggle.Invalidate();

            bool admin = Elevation.IsAdministrator();
            _adminPill.Text = admin ? Loc.PillAdmin : Loc.PillUser;
            _adminPill.Accent = admin ? Theme.Accent : Theme.TextSecondary;
            _adminPill.Invalidate();
        }

        private static string VendorLabel(GpuVendor v)
        {
            switch (v)
            {
                case GpuVendor.Nvidia: return Loc.VendorNvidia;
                case GpuVendor.Amd: return Loc.VendorAmd;
                case GpuVendor.Intel: return Loc.VendorIntel;
                case GpuVendor.Virtual: return Loc.VendorVirtual;
                default: return Loc.VendorOther;
            }
        }

        // ---------------- actions ----------------

        private void ToggleDgpu()
        {
            if (_dgpu == null) return;
            bool enable = !_dgpu.EnabledOnMachine;

            if (!enable)
            {
                DialogResult r = MessageBox.Show(this, Loc.ConfirmBody, Loc.ConfirmTitle,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
                if (r != DialogResult.Yes) return;
            }

            RunChange(enable);
        }

        private void RunChange(bool enable)
        {
            string verb = enable ? "--enable" : "--disable";
            _toggle.Enabled = false;
            _status = enable ? Loc.StEnabling : Loc.StDisabling;
            Invalidate();
            Application.DoEvents();

            try
            {
                if (Elevation.IsAdministrator())
                {
                    string error;
                    bool ok = GpuDevices.SetEnabled(_dgpu, enable, out error);
                    _status = ok ? (enable ? Loc.StEnabled : Loc.StDisabled) : (error ?? Loc.StFailed);
                }
                else
                {
                    Headless.TakeResult();
                    string error;
                    bool started = Elevation.RunElevated(verb, out error);
                    string result = Headless.TakeResult();
                    string changed = Headless.Field(result, "changed");
                    if (started && changed == "True")
                    {
                        _status = enable ? Loc.StEnabled : Loc.StDisabled;
                    }
                    else
                    {
                        string detail = Headless.Field(result, "error");
                        if (string.IsNullOrEmpty(detail)) detail = error;
                        if (string.IsNullOrEmpty(detail)) detail = Loc.StNoHelper;
                        _status = detail;
                    }
                }
            }
            catch (Exception ex)
            {
                _status = Loc.StError + ex.Message;
            }
            finally
            {
                _toggle.Enabled = true;
                if (_nvml != null) { _nvml.Dispose(); _nvml = null; }
                _telemetryAge = -1;
                System.Threading.Thread.Sleep(500);
                RefreshAll(true);
            }
        }
    }
}
