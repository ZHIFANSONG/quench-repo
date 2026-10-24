// Quench - display routing: which GPU is each monitor attached to?
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace Quench
{
    internal enum GpuVendor
    {
        Unknown,
        Amd,
        Nvidia,
        Intel,
        Virtual
    }

    internal sealed class MonitorInfo
    {
        public string AdapterDevice;      // \\.\DISPLAY18
        public string AdapterName;        // AMD Radeon(TM) Graphics
        public string AdapterDeviceId;    // PCI\VEN_1002&...
        public string MonitorDevice;      // \\.\DISPLAY18\Monitor0
        public string MonitorInterface;   // \\?\DISPLAY#PANEL01#5&...
        public string MonitorCode;        // PANEL01
        public string MonitorName;        // parsed from EDID, else code
        public int Width;
        public int Height;
        public int Refresh;
        public int BitsPerPixel;
        public bool Primary;
        public bool Attached;
        public GpuVendor AdapterVendor;
    }

    internal sealed class AdapterInfo
    {
        public string DeviceName;
        public string Description;
        public string DeviceId;
        public string DeviceKey;
        public int StateFlags;
        public GpuVendor Vendor;
        public readonly List<MonitorInfo> Monitors = new List<MonitorInfo>();

        public bool Attached { get { return (StateFlags & Native.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0; } }
        public bool Primary { get { return (StateFlags & Native.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0; } }
    }

    internal static class DisplayTopology
    {
        public static List<AdapterInfo> Query()
        {
            var adapters = new List<AdapterInfo>();

            for (uint i = 0; ; i++)
            {
                var dd = new Native.DISPLAY_DEVICE();
                dd.cb = Marshal.SizeOf(typeof(Native.DISPLAY_DEVICE));
                if (!Native.EnumDisplayDevicesW(null, i, ref dd, 0)) break;
                if (string.IsNullOrEmpty(dd.DeviceName)) continue;

                var a = new AdapterInfo();
                a.DeviceName = dd.DeviceName;
                a.Description = (dd.DeviceString ?? "").Trim();
                a.DeviceId = (dd.DeviceID ?? "").Trim();
                a.DeviceKey = (dd.DeviceKey ?? "").Trim();
                a.StateFlags = dd.StateFlags;
                a.Vendor = ClassifyVendor(a.Description, a.DeviceId);

                // Child monitors of this adapter.
                for (uint j = 0; ; j++)
                {
                    var md = new Native.DISPLAY_DEVICE();
                    md.cb = Marshal.SizeOf(typeof(Native.DISPLAY_DEVICE));
                    if (!Native.EnumDisplayDevicesW(dd.DeviceName, j, ref md, Native.EDD_GET_DEVICE_INTERFACE_NAME)) break;
                    if (string.IsNullOrEmpty(md.DeviceName) && string.IsNullOrEmpty(md.DeviceID)) continue;

                    var m = new MonitorInfo();
                    m.AdapterDevice = dd.DeviceName;
                    m.AdapterName = a.Description;
                    m.AdapterDeviceId = a.DeviceId;
                    m.AdapterVendor = a.Vendor;
                    m.MonitorDevice = md.DeviceName;
                    m.MonitorInterface = (md.DeviceID ?? "").Trim();
                    m.MonitorCode = ParseMonitorCode(m.MonitorInterface);
                    m.MonitorName = ResolveMonitorName(m.MonitorCode, m.MonitorInterface);
                    m.Attached = (md.StateFlags & Native.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0
                                 || (a.StateFlags & Native.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0;
                    m.Primary = a.Primary;

                    var dm = new Native.DEVMODE();
                    dm.dmSize = (short)Marshal.SizeOf(typeof(Native.DEVMODE));
                    if (Native.EnumDisplaySettingsW(dd.DeviceName, Native.ENUM_CURRENT_SETTINGS, ref dm))
                    {
                        m.Width = dm.dmPelsWidth;
                        m.Height = dm.dmPelsHeight;
                        m.Refresh = dm.dmDisplayFrequency;
                        m.BitsPerPixel = dm.dmBitsPerPel;
                    }
                    a.Monitors.Add(m);
                }
                adapters.Add(a);
            }
            return adapters;
        }

        public static GpuVendor ClassifyVendor(string name, string deviceId)
        {
            string n = (name ?? "").ToUpperInvariant();
            string id = (deviceId ?? "").ToUpperInvariant();

            if (id.StartsWith("ROOT\\", StringComparison.Ordinal)) return GpuVendor.Virtual;
            if (n.Contains("VIRTUAL") || n.Contains("IDD") || n.Contains("REMOTE") || n.Contains("MIRROR"))
                return GpuVendor.Virtual;
            if (id.Contains("VEN_10DE") || n.Contains("NVIDIA") || n.Contains("GEFORCE") || n.Contains("QUADRO") || n.Contains("RTX") || n.Contains("GTX"))
                return GpuVendor.Nvidia;
            if (id.Contains("VEN_1002") || n.Contains("AMD") || n.Contains("RADEON") || n.Contains("ATI "))
                return GpuVendor.Amd;
            if (id.Contains("VEN_8086") || n.Contains("INTEL") || n.Contains("ARC ") || n.Contains("UHD") || n.Contains("IRIS"))
                return GpuVendor.Intel;
            return GpuVendor.Unknown;
        }

        // \\?\DISPLAY#PANEL01#5&2529796&0&UID768#{e6f07b5f-...}  ->  PANEL01
        public static string ParseMonitorCode(string iface)
        {
            if (string.IsNullOrEmpty(iface)) return "";
            string[] parts = iface.Split('#');
            if (parts.Length >= 2 && parts[1].Length > 0 && !parts[1].StartsWith("{", StringComparison.Ordinal))
                return parts[1];
            return "";
        }

        private static string ParseInstance(string iface)
        {
            if (string.IsNullOrEmpty(iface)) return "";
            string[] parts = iface.Split('#');
            return parts.Length >= 3 ? parts[2] : "";
        }

        private static readonly Dictionary<string, string> NameCache = new Dictionary<string, string>();

        public static string ResolveMonitorName(string code, string iface)
        {
            if (string.IsNullOrEmpty(code)) return "Unknown display";
            string key = code + "|" + ParseInstance(iface);
            if (NameCache.ContainsKey(key)) return NameCache[key];

            string result = null;
            try
            {
                string inst = ParseInstance(iface);
                if (inst.Length > 0)
                {
                    using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Enum\DISPLAY\" + code + @"\" + inst + @"\Device Parameters"))
                    {
                        if (k != null)
                        {
                            byte[] edid = k.GetValue("EDID") as byte[];
                            result = EdidName(edid);
                        }
                    }
                }
            }
            catch
            {
                result = null;
            }
            if (string.IsNullOrEmpty(result)) result = code;
            else result = result + " (" + code + ")";
            NameCache[key] = result;
            return result;
        }

        private static string EdidName(byte[] edid)
        {
            if (edid == null || edid.Length < 128) return null;
            if (edid[0] != 0x00 || edid[1] != 0xFF || edid[2] != 0xFF || edid[3] != 0xFF ||
                edid[4] != 0xFF || edid[5] != 0xFF || edid[6] != 0xFF || edid[7] != 0x00)
                return null;

            for (int block = 54; block + 18 <= 126; block += 18)
            {
                if (edid[block] != 0x00 || edid[block + 1] != 0x00 || edid[block + 2] != 0x00) continue;
                int tag = edid[block + 3];
                if (tag != 0xFC && tag != 0xFE) continue;
                var sb = new StringBuilder(14);
                for (int c = block + 5; c < block + 18; c++)
                {
                    byte b = edid[c];
                    if (b == 0x0A || b == 0x00) break;
                    if (b >= 0x20 && b < 0x7F) sb.Append((char)b);
                }
                string s = sb.ToString().Trim();
                if (s.Length > 0) return s;
            }
            return null;
        }
    }
}
