// Quench - tiny two-language string table (Chinese / English).
using System;
using System.Globalization;

namespace Quench
{
    internal static class Loc
    {
        private static bool _english = DetectEnglish();

        public static bool English
        {
            get { return _english; }
            set { _english = value; }
        }

        private static bool DetectEnglish()
        {
            try
            {
                // Chinese (any script/region) gets Chinese; everything else gets English.
                CultureInfo c = CultureInfo.CurrentUICulture;
                if (c.TwoLetterISOLanguageName == "zh") return false;
                if (c.TextInfo != null && c.TextInfo.ANSICodePage == 936) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string P(string zh, string en) { return _english ? en : zh; }

        // ---- chrome ----
        public static string Tagline { get { return P("看清显示器接在哪块显卡上，并把独立显卡彻底断电。",
                                                     "See which GPU your display is wired to — and cut power to the discrete one."); } }
        public static string CardRouting { get { return P("显示路由", "Display routing"); } }
        public static string CardGpu { get { return P("独立显卡", "Discrete GPU"); } }
        public static string CardDisplays { get { return P("显示器", "Displays"); } }
        public static string DisplaysCaption { get { return P("仅列出已接入的显示器。", "Attached displays only."); } }

        // ---- routing diagram ----
        public static string NoActiveDisplay { get { return P("无活动显示器", "No active display"); } }
        public static string WiredToDgpu { get { return P("直连独立显卡", "wired directly to the dGPU"); } }
        public static string WiredToIgpu { get { return P("接在核显上", "wired to the integrated GPU"); } }
        public static string NoDisplayTag { get { return P("当前没有显示器", "no active display"); } }
        public static string DrivesDisplay { get { return P("正在驱动这块屏", "drives this display"); } }
        public static string DrivesNothing { get { return P("没有接任何显示器", "not driving any display"); } }

        public static string NoteDirect
        {
            get
            {
                return P("内屏是直连独立显卡的，所以独显必须一直供电。屏幕接在哪块显卡由固件（MUX）决定，切换只能回 BIOS 或厂商控制中心——先切回混合模式，才能关掉独显。",
                         "Your panel is wired straight to the discrete GPU, so that GPU has to stay powered. Only the firmware (MUX) can move a display between GPUs — switch back to hybrid mode first if you want to turn the dGPU off.");
            }
        }

        public static string NoteHybrid
        {
            get
            {
                return P("内屏由核显驱动，独立显卡没有接任何显示器，它现在只是为了待命在耗电。",
                         "Your panel is driven by the integrated graphics. The discrete GPU has no display attached, so it is only drawing power to sit idle.");
            }
        }

        public static string NoteNone
        {
            get { return P("当前没有接显示器。", "No display is currently attached."); }
        }

        public static string NoteDisabled
        {
            get
            {
                return P("独立显卡已被禁用，完全不再耗电。内屏照常由核显驱动；需要玩游戏时点下面的按钮即可恢复。",
                         "The discrete GPU is disabled and draws no power at all. The internal panel still runs on the integrated graphics — use the button below whenever you need the dGPU again.");
            }
        }

        // ---- stat labels ----
        public static string LblDeviceState { get { return P("设备状态", "Device state"); } }
        public static string LblPowerState { get { return P("电源状态", "Power state"); } }
        public static string LblTemperature { get { return P("温度", "Temperature"); } }
        public static string LblClocks { get { return P("核心/显存频率", "Clocks"); } }
        public static string LblPcie { get { return P("PCIe 链路", "PCIe link"); } }
        public static string LblMemory { get { return P("显存", "Memory"); } }

        // ---- device / power state values ----
        public static string StateEnabled { get { return P("已启用", "enabled"); } }
        public static string StateDisabled { get { return P("已禁用", "disabled"); } }
        public static string StateNoRestart { get { return P("需重启", "restart required"); } }
        public static string StateNotPresent { get { return P("不在位", "not present"); } }
        public static string StateNotFound { get { return P("未检测到", "not detected"); } }
        public static string StateProblem { get { return P("异常 ", "problem "); } }

        public static string PowerUnknown { get { return P("未知", "unknown"); } }
        public static string PowerD0 { get { return P("D0（全开）", "D0 (powered)"); } }
        public static string PowerD1 { get { return P("D1（低功耗）", "D1 (low power)"); } }
        public static string PowerD2 { get { return P("D2（睡眠）", "D2 (sleep)"); } }
        public static string PowerD3 { get { return P("D3（已断电）", "D3 (off)"); } }

        // ---- big value captions ----
        public static string CapLive { get { return P("实时板卡功耗", "Live board power draw"); } }
        public static string CapJustMeasured { get { return P("刚刚测得 · 板卡功耗", "Just measured · board power draw"); } }
        public static string CapAgo(int s) { return _english ? "Board power draw · " + s + "s ago" : "板卡功耗 · " + s + " 秒前测得"; }
        public static string CapDisabled { get { return P("设备已禁用 — 完全不耗电", "Device disabled — draws no power at all"); } }
        public static string CapAsleep { get { return P("独显已进入 D3 休眠，为保持休眠暂不采样", "GPU is in D3 sleep — not sampling it, so it stays asleep"); } }
        public static string CapWaiting { get { return P("等待采样…", "Waiting for a sample…"); } }
        public static string CapNoDgpu { get { return P("未检测到独立显卡", "No discrete GPU detected"); } }
        public static string CapNoTelemetry { get { return P("设备已禁用，无法读取遥测", "Device is disabled, so it reports no telemetry."); } }

        // ---- buttons ----
        public static string BtnDisable { get { return P("禁用独立显卡", "Disable discrete GPU"); } }
        public static string BtnEnable { get { return P("启用独立显卡", "Enable discrete GPU"); } }
        public static string BtnNoGpu { get { return P("未找到独立显卡", "No discrete GPU found"); } }
        public static string BtnRescan { get { return P("重新扫描", "Rescan"); } }
        public static string SubDisable { get { return P("功耗直接归零，随时可恢复", "Cuts it to 0 W until you turn it back on"); } }
        public static string SubEnable { get { return P("恢复独显，游戏才能提速", "Restores the dGPU for gaming"); } }

        // ---- pills ----
        public static string PillAdmin { get { return P("管理员", "Administrator"); } }
        public static string PillUser { get { return P("标准用户", "Standard user"); } }
        public static string PillDirect { get { return P("直连独显", "Direct to dGPU"); } }
        public static string PillViaIgpu { get { return P("经核显输出", "Routed via iGPU"); } }
        public static string PillNoDisplay { get { return P("无显示器", "No display"); } }

        // ---- status messages ----
        public static string StDisabling { get { return P("正在禁用独立显卡…", "Disabling the discrete GPU…"); } }
        public static string StEnabling { get { return P("正在启用独立显卡…", "Enabling the discrete GPU…"); } }
        public static string StDisabled { get { return P("独立显卡已禁用，现在完全不耗电。", "Discrete GPU disabled — it now draws no power."); } }
        public static string StEnabled { get { return P("独立显卡已启用。", "Discrete GPU enabled."); } }
        public static string StFailed { get { return P("操作失败。", "The change failed."); } }
        public static string StNoHelper { get { return P("提权助手没有返回结果。", "The elevated helper did not report a result."); } }
        public static string StError { get { return P("错误：", "Error: "); } }

        // ---- vendor labels ----
        public static string VendorNvidia { get { return P("NVIDIA 独立显卡", "NVIDIA discrete GPU"); } }
        public static string VendorAmd { get { return P("AMD 核显", "AMD integrated graphics"); } }
        public static string VendorIntel { get { return P("Intel 核显", "Intel integrated graphics"); } }
        public static string VendorVirtual { get { return P("虚拟显示器", "virtual display"); } }
        public static string VendorOther { get { return P("显示适配器", "display adapter"); } }

        // ---- dialogs ----
        public static string ConfirmTitle { get { return P("确认禁用独立显卡", "Quench"); } }

        public static string ConfirmBody
        {
            get
            {
                return P(
                    "禁用后独立显卡的功耗会直接降到 0。\r\n\r\n" +
                    "禁用期间：\r\n" +
                    "  ·  笔记本内屏照常显示（它接在核显上）。\r\n" +
                    "  ·  直接由独显驱动的外接显示器 / HDMI / DP 接口会黑屏。\r\n" +
                    "  ·  游戏和 GPU 计算会改用核显，速度变慢。\r\n" +
                    "  ·  随时可以在这里重新启用。\r\n\r\n" +
                    "现在禁用独立显卡吗？",
                    "Disabling the discrete GPU cuts its power draw to zero.\r\n\r\n" +
                    "While it is disabled:\r\n" +
                    "  •  The internal laptop screen keeps working (it is wired to the iGPU).\r\n" +
                    "  •  Any external monitor or HDMI/DisplayPort output wired directly to the dGPU goes dark.\r\n" +
                    "  •  Games and GPU compute run on the integrated graphics and will be slower.\r\n" +
                    "  •  You can enable it again at any time from this window.\r\n\r\n" +
                    "Disable the discrete GPU now?");
            }
        }

        // ---- monitor list ----
        public static string PrimaryBadge { get { return P("主屏", "PRIMARY"); } }
        public static string NoMonitors { get { return P("系统没有报告任何显示器。", "No displays were reported by the operating system."); } }
        public static string NoMonitorAttached { get { return P("适配器在位，但没有接显示器", "adapter present, no monitor attached"); } }
    }
}
