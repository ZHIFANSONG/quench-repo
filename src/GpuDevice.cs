// Quench - GPU device model: enumeration, status, power state, enable/disable.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Quench
{
    internal sealed class GpuDevice
    {
        public string InstanceId;
        public string Description;
        public GpuVendor Vendor;
        public Guid ClassGuid;
        public string Service;

        public bool Present;
        public uint CmStatus;
        public uint Problem;

        public bool PowerDataValid;
        public int PowerState;        // 1 = D0 (on), 2..5 = D1..D3, 4 = D3
        public bool EnabledOnMachine; // present && problem != CM_PROB_DISABLED

        public bool IsNvidia { get { return Vendor == GpuVendor.Nvidia; } }

        public string StateText
        {
            get
            {
                if (!Present) return Loc.StateNotPresent;
                if (Problem == Native.CM_PROB_DISABLED) return Loc.StateDisabled;
                if (Problem == Native.CM_PROB_NEED_RESTART) return Loc.StateNoRestart;
                if (Problem != 0) return Loc.StateProblem + Problem;
                return Loc.StateEnabled;
            }
        }

        public string PowerStateText
        {
            get
            {
                if (!PowerDataValid) return Loc.PowerUnknown;
                switch (PowerState)
                {
                    case 1: return Loc.PowerD0;
                    case 2: return Loc.PowerD1;
                    case 3: return Loc.PowerD2;
                    case 4: return Loc.PowerD3;
                    case 5: return Loc.PowerD3;
                    default: return "D" + PowerState;
                }
            }
        }
    }

    internal static class GpuDevices
    {
        public static List<GpuDevice> QueryAll()
        {
            var list = new List<GpuDevice>();
            QueryInto(list, new Guid[] { Native.GUID_DEVCLASS_DISPLAY });
            return list;
        }

        private static void QueryInto(List<GpuDevice> list, Guid[] classes)
        {
            foreach (Guid cls in classes)
            {
                IntPtr set = IntPtr.Zero;
                try
                {
                    Guid g = cls;
                    set = Native.SetupDiGetClassDevsW(ref g, null, IntPtr.Zero, Native.DIGCF_PRESENT);
                    if (set == IntPtr.Zero || set == new IntPtr(-1)) continue;

                    var data = new Native.SP_DEVINFO_DATA();
                    data.cbSize = Marshal.SizeOf(typeof(Native.SP_DEVINFO_DATA));
                    for (int i = 0; Native.SetupDiEnumDeviceInfo(set, i, ref data); i++)
                    {
                        data.cbSize = Marshal.SizeOf(typeof(Native.SP_DEVINFO_DATA));
                        var sb = new StringBuilder(512);
                        int req;
                        if (!Native.SetupDiGetDeviceInstanceIdW(set, ref data, sb, sb.Capacity, out req)) continue;

                        var d = new GpuDevice();
                        d.InstanceId = sb.ToString();
                        d.Description = ReadStringProperty(set, ref data, Native.SPDRP_DEVICEDESC);
                        d.Service = ReadStringProperty(set, ref data, Native.SPDRP_DRIVER);
                        if (d.Service.StartsWith("{", StringComparison.Ordinal)) d.Service = "";
                        string clsName = ReadStringProperty(set, ref data, Native.SPDRP_CLASS);
                        d.ClassGuid = cls;
                        d.Vendor = DisplayTopology.ClassifyVendor(d.Description, d.InstanceId);
                        if (d.Vendor == GpuVendor.Unknown && clsName.Length == 0) continue;

                        ReadStatus(d);
                        ReadPower(d);
                        list.Add(d);
                    }
                }
                finally
                {
                    if (set != IntPtr.Zero && set != new IntPtr(-1)) Native.SetupDiDestroyDeviceInfoList(set);
                }
            }
        }

        private static string ReadStringProperty(IntPtr set, ref Native.SP_DEVINFO_DATA data, int prop)
        {
            int type, req;
            byte[] buf = new byte[1024];
            if (!Native.SetupDiGetDeviceRegistryPropertyW(set, ref data, prop, out type, buf, buf.Length, out req))
                return "";
            if (req <= 2) return "";
            return Encoding.Unicode.GetString(buf, 0, req - 2);
        }

        public static void ReadStatus(GpuDevice d)
        {
            uint dn;
            int lr = Native.CM_Locate_DevNodeW(out dn, d.InstanceId, 0);
            if (lr != Native.CR_SUCCESS)
            {
                d.Present = false;
                d.CmStatus = 0;
                d.Problem = Native.CM_PROB_PHANTOM;
                return;
            }
            uint st, prob;
            int sr = Native.CM_Get_DevNode_Status(out st, out prob, dn, 0);
            d.CmStatus = st;
            if (sr != Native.CR_SUCCESS)
            {
                d.Present = false;
                d.Problem = prob;
                return;
            }
            d.Present = (st & 0x00000400) == 0x00000400 ? prob != Native.CM_PROB_PHANTOM : true;
            d.Present = prob != Native.CM_PROB_PHANTOM;
            d.Problem = prob;
            d.EnabledOnMachine = d.Present && prob != Native.CM_PROB_DISABLED;
        }

        public static void ReadPower(GpuDevice d)
        {
            d.PowerDataValid = false;
            try
            {
                Guid g = Native.GUID_DEVCLASS_DISPLAY;
                IntPtr set = Native.SetupDiGetClassDevsW(ref g, null, IntPtr.Zero, Native.DIGCF_PRESENT);
                if (set == IntPtr.Zero || set == new IntPtr(-1)) return;
                try
                {
                    var data = new Native.SP_DEVINFO_DATA();
                    data.cbSize = Marshal.SizeOf(typeof(Native.SP_DEVINFO_DATA));
                    for (int i = 0; Native.SetupDiEnumDeviceInfo(set, i, ref data); i++)
                    {
                        data.cbSize = Marshal.SizeOf(typeof(Native.SP_DEVINFO_DATA));
                        var sb = new StringBuilder(512);
                        int req;
                        if (!Native.SetupDiGetDeviceInstanceIdW(set, ref data, sb, sb.Capacity, out req)) continue;
                        if (string.Compare(sb.ToString(), d.InstanceId, true) != 0) continue;

                        var key = new Native.DEVPROPKEY();
                        key.fmtid = Native.DEVPKEY_DEVICE_FMTID;
                        key.pid = Native.DEVPKEY_PID_POWER_DATA;
                        int type, need;
                        byte[] buf = new byte[512];
                        if (Native.SetupDiGetDevicePropertyW(set, ref data, ref key, out type, buf, buf.Length, out need, 0))
                        {
                            if (need >= 8)
                            {
                                d.PowerDataValid = true;
                                d.PowerState = BitConverter.ToInt32(buf, 4);
                            }
                        }
                        return;
                    }
                }
                finally
                {
                    Native.SetupDiDestroyDeviceInfoList(set);
                }
            }
            catch
            {
            }
        }

        /// <summary>Enable or disable a device node. Requires elevation.</summary>
        public static bool SetEnabled(GpuDevice d, bool enable, out string error)
        {
            error = null;
            IntPtr set = IntPtr.Zero;
            try
            {
                set = OpenDeviceSet(d, true);
                if (set == IntPtr.Zero || set == new IntPtr(-1))
                {
                    error = "Could not open the device information set (error " + Marshal.GetLastWin32Error() + ").";
                    return false;
                }

                var data = new Native.SP_DEVINFO_DATA();
                data.cbSize = Marshal.SizeOf(typeof(Native.SP_DEVINFO_DATA));
                bool found = false;
                for (int i = 0; Native.SetupDiEnumDeviceInfo(set, i, ref data); i++)
                {
                    data.cbSize = Marshal.SizeOf(typeof(Native.SP_DEVINFO_DATA));
                    var sb = new StringBuilder(512);
                    int req;
                    if (!Native.SetupDiGetDeviceInstanceIdW(set, ref data, sb, sb.Capacity, out req)) continue;
                    if (string.Compare(sb.ToString(), d.InstanceId, true) == 0) { found = true; break; }
                }
                if (!found)
                {
                    error = "The device node could not be located in the device tree.";
                    return false;
                }

                var p = new Native.SP_PROPCHANGE_PARAMS();
                p.ClassInstallHeader.cbSize = Marshal.SizeOf(typeof(Native.SP_CLASSINSTALL_HEADER));
                p.ClassInstallHeader.InstallFunction = Native.DIF_PROPERTYCHANGE;
                p.StateChange = enable ? Native.DICS_ENABLE : Native.DICS_DISABLE;
                p.Scope = Native.DICS_FLAG_GLOBAL;
                p.HwProfile = 0;

                bool okSet = Native.SetupDiSetClassInstallParamsW(set, ref data, ref p, Marshal.SizeOf(typeof(Native.SP_PROPCHANGE_PARAMS)));
                if (!okSet)
                {
                    int e = Marshal.GetLastWin32Error();
                    if (e == 5) error = "Access denied. Quench needs administrator rights to change GPU state.";
                    else error = "SetupDiSetClassInstallParams failed (error " + e + ").";
                    return false;
                }

                bool ok = Native.SetupDiCallClassInstaller(Native.DIF_PROPERTYCHANGE, set, ref data);
                if (!ok)
                {
                    int e = Marshal.GetLastWin32Error();
                    if (e == 5) error = "Access denied. Quench needs administrator rights to change GPU state.";
                    else error = "The driver refused the request (error " + e + ").";
                    return false;
                }

                // Make the change observable immediately.
                uint dn;
                if (Native.CM_Locate_DevNodeW(out dn, d.InstanceId, 0) == Native.CR_SUCCESS)
                    Native.CM_Reenumerate_DevNode(dn, Native.CM_REENUMERATE_SYNCHRONOUS);

                return true;
            }
            finally
            {
                if (set != IntPtr.Zero && set != new IntPtr(-1)) Native.SetupDiDestroyDeviceInfoList(set);
            }
        }

        private static IntPtr OpenDeviceSet(GpuDevice d, bool allClasses)
        {
            Guid g = d.ClassGuid;
            IntPtr set = Native.SetupDiGetClassDevsW(ref g, null, IntPtr.Zero, Native.DIGCF_PRESENT);
            if (set != IntPtr.Zero && set != new IntPtr(-1)) return set;
            return Native.SetupDiGetClassDevsW(ref g, null, IntPtr.Zero, 0);
        }
    }
}
