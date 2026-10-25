// Quench - deep sleep: stop the vendor control-centre services that keep waking the dGPU.
//
// A vendor control centre (the Uniwill "GCU" stack, and similar tools) polls the
// discrete GPU every few seconds to draw its own dashboard. Each poll drags the GPU
// out of D3, so a hybrid laptop never gets to the "off" state even though nothing is
// using the card. Stopping that service stack is what actually lets the GPU sleep.
//
// This is deliberately opt-in and only offered when a known vendor service exists.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace Quench
{
    internal sealed class QuietState
    {
        public bool Supported;
        public bool ServiceRunning;
        public string ServiceStatus = "—";
        public string StartType = "—";
        public bool ChildrenRunning;
        public bool Active;
    }

    internal static class PowerSave
    {
        // Vendor service stacks known to poll the discrete GPU.
        // First entry is the Windows service; the rest are its helper processes.
        private static readonly string[][] KnownStacks =
        {
            new string[] { "GCUBridge", "GCUService", "OSDTpDetect" }, // Uniwill "GCU" control centre
        };

        private const string StoreKey = @"SOFTWARE\Quench";

        private static string _service;
        private static string[] _children;
        private static bool _resolved;

        public static bool Supported
        {
            get { Resolve(); return _service != null; }
        }

        public static string ServiceName
        {
            get { Resolve(); return _service; }
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;
            foreach (string[] stack in KnownStacks)
            {
                if (!ServiceExists(stack[0])) continue;
                _service = stack[0];
                _children = new string[stack.Length - 1];
                Array.Copy(stack, 1, _children, 0, _children.Length);
                return;
            }
        }

        private static bool ServiceExists(string name)
        {
            try
            {
                using (var sc = new ServiceController(name))
                {
                    // Reading Status throws InvalidOperationException when the service
                    // is not installed on this machine, which is exactly the probe we want.
                    ServiceControllerStatus status = sc.Status;
                    GC.KeepAlive(status);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // ---------------- probing ----------------

        public static QuietState Probe()
        {
            var st = new QuietState();
            Resolve();
            if (_service == null) return st;

            st.Supported = true;
            try
            {
                using (var sc = new ServiceController(_service))
                {
                    ServiceControllerStatus s = sc.Status;
                    st.ServiceStatus = StatusText(s);
                    st.ServiceRunning = s == ServiceControllerStatus.Running ||
                                        s == ServiceControllerStatus.StartPending;
                }
            }
            catch (Exception ex)
            {
                st.ServiceStatus = ex.GetType().Name;
            }

            st.StartType = StartTypeText(ReadStartValue(_service));
            st.ChildrenRunning = !st.ServiceRunning && AnyChildRunning();
            st.Active = !st.ServiceRunning && !st.ChildrenRunning;
            return st;
        }

        private static string StatusText(ServiceControllerStatus s)
        {
            switch (s)
            {
                case ServiceControllerStatus.Running: return "Running";
                case ServiceControllerStatus.Stopped: return "Stopped";
                case ServiceControllerStatus.StartPending: return "StartPending";
                case ServiceControllerStatus.StopPending: return "StopPending";
                case ServiceControllerStatus.Paused: return "Paused";
                default: return s.ToString();
            }
        }

        private static string StartTypeText(int value)
        {
            switch (value)
            {
                case 0: return "Boot";
                case 1: return "System";
                case 2: return "Automatic";
                case 3: return "Manual";
                case 4: return "Disabled";
                default: return "—";
            }
        }

        private static bool AnyChildRunning()
        {
            if (_children == null) return false;
            for (int i = 0; i < _children.Length; i++)
            {
                try
                {
                    Process[] ps = Process.GetProcessesByName(_children[i]);
                    bool any = ps.Length > 0;
                    for (int j = 0; j < ps.Length; j++) ps[j].Dispose();
                    if (any) return true;
                }
                catch
                {
                }
            }
            return false;
        }

        // ---------------- registry (start type) ----------------

        private static int ReadStartValue(string name)
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\" + name, false))
                {
                    if (k == null) return -1;
                    object v = k.GetValue("Start");
                    if (v == null) return -1;
                    return Convert.ToInt32(v, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                return -1;
            }
        }

        private static bool WriteStartValue(string name, int value)
        {
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Services\" + name, true))
                {
                    if (k == null) return false;
                    k.SetValue("Start", value, RegistryValueKind.DWord);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        // Remember the stock start type so restoring puts it back exactly.
        private static void RememberStart(int value)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(StoreKey))
                {
                    if (k != null) k.SetValue("QuietOriginalStart", value, RegistryValueKind.DWord);
                }
            }
            catch
            {
            }
        }

        private static int RecalledStart()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(StoreKey, false))
                {
                    if (k == null) return 2;
                    object v = k.GetValue("QuietOriginalStart");
                    if (v == null) return 2;
                    int n = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                    if (n < 2 || n > 4) return 2;
                    return n;
                }
            }
            catch
            {
                return 2;
            }
        }

        // ---------------- actions ----------------

        public static bool SetQuiet(bool on, out string error)
        {
            error = null;
            Resolve();
            if (_service == null)
            {
                error = "No vendor control-centre service was found on this machine.";
                return false;
            }

            try
            {
                if (on)
                {
                    int current = ReadStartValue(_service);
                    if (current >= 0) RememberStart(current);

                    StopService(_service);
                    WriteStartValue(_service, 3); // manual, so it stays down across a reboot
                    KillChildren(3);
                    Thread.Sleep(600);
                    KillChildren(2);
                }
                else
                {
                    WriteStartValue(_service, RecalledStart());
                    StartService(_service);
                    Thread.Sleep(1200);
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static void StopService(string name)
        {
            using (var sc = new ServiceController(name))
            {
                if (sc.Status == ServiceControllerStatus.Stopped) return;
                try { sc.Stop(); }
                catch (InvalidOperationException) { }
                try { sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20)); }
                catch { }
            }
        }

        private static void StartService(string name)
        {
            using (var sc = new ServiceController(name))
            {
                if (sc.Status == ServiceControllerStatus.Running) return;
                try { sc.Start(); }
                catch (InvalidOperationException) { }
                try { sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(25)); }
                catch { }
            }
        }

        private static void KillChildren(int passes)
        {
            if (_children == null) return;
            for (int p = 0; p < passes; p++)
            {
                for (int i = 0; i < _children.Length; i++)
                {
                    Process[] ps;
                    try { ps = Process.GetProcessesByName(_children[i]); }
                    catch { continue; }
                    for (int j = 0; j < ps.Length; j++)
                    {
                        Process pr = ps[j];
                        try { pr.Kill(); pr.WaitForExit(3000); }
                        catch { }
                        finally { pr.Dispose(); }
                    }
                }
                if (p < passes - 1) Thread.Sleep(1500);
            }
        }

        // ---------------- headless entry ----------------

        public static bool IsQuietVerb(string verb)
        {
            return verb == "quiet-on" || verb == "quiet-off" || verb == "quiet-status";
        }

        public static int Run(string verb, StringBuilder log)
        {
            if (!Elevation.IsAdministrator())
            {
                log.AppendLine("error=This helper must run elevated.");
                return 3;
            }

            if (verb == "quiet-status")
            {
                QuietState st = Probe();
                log.AppendLine("supported=" + st.Supported);
                log.AppendLine("active=" + st.Active);
                log.AppendLine("service=" + st.ServiceStatus);
                log.AppendLine("starttype=" + st.StartType);
                log.AppendLine("children=" + st.ChildrenRunning);
                return st.Supported ? 0 : 4;
            }

            bool on = verb == "quiet-on";
            string error;
            bool ok = SetQuiet(on, out error);
            log.AppendLine("changed=" + ok);
            if (!ok) log.AppendLine("error=" + (error ?? "unknown"));

            QuietState after = Probe();
            log.AppendLine("active=" + after.Active);
            log.AppendLine("service=" + after.ServiceStatus);
            log.AppendLine("starttype=" + after.StartType);
            log.AppendLine("children=" + after.ChildrenRunning);
            return ok ? 0 : 5;
        }
    }
}
