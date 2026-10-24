// Quench - Win32 interop layer
// Targets .NET Framework 4.x / C# 5 (compiled with the in-box csc.exe).
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Quench
{
    internal static class Native
    {
        // ---------- user32 : display topology ----------
        public const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        public const int DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;
        public const int DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008;
        public const int EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;
        public const int ENUM_CURRENT_SETTINGS = -1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplayDevicesW(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplaySettingsW(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        // ---------- cfgmgr32 : device nodes ----------
        public const int CR_SUCCESS = 0;
        public const int CM_REENUMERATE_NORMAL = 0;
        public const int CM_REENUMERATE_SYNCHRONOUS = 1;

        public const uint DN_HAS_PROBLEM = 0x00000400;
        public const uint CM_PROB_DISABLED = 22;
        public const uint CM_PROB_PHANTOM = 45;
        public const uint CM_PROB_NEED_RESTART = 14;

        [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
        public static extern int CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, int ulFlags);

        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Get_DevNode_Status(out uint pulStatus, out uint pulProblemNumber, uint dnDevInst, int ulFlags);

        [DllImport("cfgmgr32.dll")]
        public static extern int CM_Reenumerate_DevNode(uint dnDevInst, int ulFlags);

        // ---------- setupapi : class enumeration + enable/disable ----------
        public const int DIGCF_PRESENT = 0x00000002;
        public const int DIGCF_ALLCLASSES = 0x00000004;

        public const int SPDRP_DEVICEDESC = 0x00000000;
        public const int SPDRP_CLASS = 0x00000007;
        public const int SPDRP_CLASSGUID = 0x00000008;
        public const int SPDRP_CONFIGFLAGS = 0x0000000A;
        public const int SPDRP_DRIVER = 0x00000009;
        public const int SPDRP_MFG = 0x0000000B;

        public const int CONFIGFLAG_DISABLED = 0x00000001;

        public const int DIF_PROPERTYCHANGE = 0x00000012;
        public const int DICS_ENABLE = 0x00000001;
        public const int DICS_DISABLE = 0x00000002;
        public const int DICS_FLAG_GLOBAL = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public int cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_CLASSINSTALL_HEADER
        {
            public int cbSize;
            public int InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public int StateChange;
            public int Scope;
            public int HwProfile;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVPROPKEY
        {
            public Guid fmtid;
            public int pid;
        }

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SetupDiGetClassDevsW(ref Guid ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        public static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, int MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiGetDeviceInstanceIdW(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            StringBuilder DeviceInstanceId, int DeviceInstanceIdSize, out int RequiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            int Property, out int PropertyRegDataType, byte[] PropertyBuffer, int PropertyBufferSize, out int RequiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiSetClassInstallParamsW(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_CLASSINSTALL_HEADER ClassInstallParams, int ClassInstallParamsSize);

        // The buffer Windows reads is a whole SP_PROPCHANGE_PARAMS, so pass the full struct and
        // let it alias the leading header. Passing a bare 8-byte header fails with error 87.
        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiSetClassInstallParamsW(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_PROPCHANGE_PARAMS ClassInstallParams, int ClassInstallParamsSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiCallClassInstaller(int InstallFunction, IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiGetDevicePropertyW(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            ref DEVPROPKEY PropertyKey, out int PropertyType, byte[] PropertyBuffer, int PropertyBufferSize,
            out int RequiredSize, int Flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool SetupDiRestartDevices(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);

        // ---------- nvml : NVIDIA telemetry (works without administrator) ----------
        [DllImport("nvml.dll", EntryPoint = "nvmlInit_v2")]
        public static extern int nvmlInit();

        [DllImport("nvml.dll", EntryPoint = "nvmlShutdown")]
        public static extern int nvmlShutdown();

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCount_v2")]
        public static extern int nvmlDeviceGetCount(out int count);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetHandleByIndex_v2")]
        public static extern int nvmlDeviceGetHandleByIndex(int index, out IntPtr device);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetName")]
        public static extern int nvmlDeviceGetName(IntPtr device, StringBuilder name, int length);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerUsage")]
        public static extern int nvmlDeviceGetPowerUsage(IntPtr device, out int milliwatts);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetEnforcedPowerLimit")]
        public static extern int nvmlDeviceGetEnforcedPowerLimit(IntPtr device, out int milliwatts);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetTemperature")]
        public static extern int nvmlDeviceGetTemperature(IntPtr device, int sensorType, out int celsius);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetPowerState")]
        public static extern int nvmlDeviceGetPowerState(IntPtr device, out int pstate);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCurrPcieLinkGeneration")]
        public static extern int nvmlDeviceGetCurrPcieLinkGeneration(IntPtr device, out int generation);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetCurrPcieLinkWidth")]
        public static extern int nvmlDeviceGetCurrPcieLinkWidth(IntPtr device, out int width);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetClockInfo")]
        public static extern int nvmlDeviceGetClockInfo(IntPtr device, int type, out int mhz);

        [DllImport("nvml.dll", EntryPoint = "nvmlDeviceGetMemoryInfo")]
        public static extern int nvmlDeviceGetMemoryInfo(IntPtr device, out NvmlMemory memory);

        [StructLayout(LayoutKind.Sequential)]
        public struct NvmlMemory
        {
            public ulong total;
            public ulong free;
            public ulong used;
        }

        // ---------- misc ----------
        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static readonly Guid GUID_DEVCLASS_DISPLAY = new Guid("4d36e968-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_MONITOR = new Guid("4d36e96e-e325-11ce-bfc1-08002be10318");

        // DEVPKEY_Device_PowerData
        public static readonly Guid DEVPKEY_DEVICE_FMTID = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0");
        public const int DEVPKEY_PID_POWER_DATA = 32;

        public static int LastError()
        {
            return Marshal.GetLastWin32Error();
        }
    }
}
