// Quench - NVML telemetry for the NVIDIA GPU (readable without administrator rights).
using System;
using System.Text;

namespace Quench
{
    internal sealed class GpuTelemetry
    {
        public bool Available;
        public string Name = "";
        public double PowerWatts = -1;
        public double PowerLimitWatts = -1;
        public int Temperature = -1;
        public string PerfState = "";
        public int PcieGeneration = -1;
        public int PcieWidth = -1;
        public int ClockGraphics = -1;
        public int ClockMemory = -1;
        public double MemoryUsedMb = -1;
        public double MemoryTotalMb = -1;
        public string Error;
    }

    /// <summary>Thin, fail-soft wrapper over NVML.</summary>
    internal sealed class Nvml : IDisposable
    {
        private bool _initialized;
        private IntPtr _device = IntPtr.Zero;
        private bool _hasDevice;
        private string _name = "";
        private int _lastTemperature = -1;

        public bool IsReady { get { return _initialized && _hasDevice; } }
        public string Name { get { return _name; } }

        public bool Initialize(out string error)
        {
            error = null;
            if (_initialized) return _hasDevice;
            try
            {
                int rc = Native.nvmlInit();
                if (rc != 0)
                {
                    error = "NVML init failed (code " + rc + "). The NVIDIA driver is probably not loaded.";
                    return false;
                }
                _initialized = true;
                int count;
                if (Native.nvmlDeviceGetCount(out count) != 0 || count <= 0)
                {
                    error = "No NVIDIA GPU was reported by the driver.";
                    return false;
                }
                if (Native.nvmlDeviceGetHandleByIndex(0, out _device) != 0)
                {
                    error = "The NVIDIA GPU handle could not be opened.";
                    return false;
                }
                var sb = new StringBuilder(96);
                if (Native.nvmlDeviceGetName(_device, sb, sb.Capacity) == 0) _name = sb.ToString();
                _hasDevice = true;
                return true;
            }
            catch (DllNotFoundException)
            {
                error = "nvml.dll was not found. Install (or reinstall) the NVIDIA driver.";
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                error = "The installed nvml.dll is too old for this build of Quench.";
                return false;
            }
            catch (Exception ex)
            {
                error = "NVML error: " + ex.Message;
                return false;
            }
        }

        public GpuTelemetry Read()
        {
            var t = new GpuTelemetry();
            if (!IsReady)
            {
                t.Error = "NVML is not available.";
                return t;
            }
            t.Available = true;
            t.Name = _name;
            try
            {
                int mw;
                if (Native.nvmlDeviceGetPowerUsage(_device, out mw) == 0) t.PowerWatts = mw / 1000.0;
                if (Native.nvmlDeviceGetEnforcedPowerLimit(_device, out mw) == 0) t.PowerLimitWatts = mw / 1000.0;

                int v;
                if (Native.nvmlDeviceGetTemperature(_device, 0, out v) == 0)
                {
                    // Some drivers report the temperature only once per session; keep the last good value.
                    if (v > 0) _lastTemperature = v;
                    if (_lastTemperature > 0) t.Temperature = _lastTemperature;
                }
                if (Native.nvmlDeviceGetPowerState(_device, out v) == 0) t.PerfState = "P" + v;
                if (Native.nvmlDeviceGetCurrPcieLinkGeneration(_device, out v) == 0) t.PcieGeneration = v;
                if (Native.nvmlDeviceGetCurrPcieLinkWidth(_device, out v) == 0) t.PcieWidth = v;
                if (Native.nvmlDeviceGetClockInfo(_device, 0, out v) == 0) t.ClockGraphics = v;
                if (Native.nvmlDeviceGetClockInfo(_device, 1, out v) == 0) t.ClockMemory = v;

                Native.NvmlMemory mem;
                if (Native.nvmlDeviceGetMemoryInfo(_device, out mem) == 0)
                {
                    t.MemoryUsedMb = mem.used / 1048576.0;
                    t.MemoryTotalMb = mem.total / 1048576.0;
                }
            }
            catch (Exception ex)
            {
                t.Error = ex.Message;
            }
            return t;
        }

        public void Dispose()
        {
            if (_initialized)
            {
                try { Native.nvmlShutdown(); }
                catch { }
                _initialized = false;
                _hasDevice = false;
            }
        }
    }
}
