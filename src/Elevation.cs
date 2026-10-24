// Quench - elevation and single-instance helpers.
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;

namespace Quench
{
    internal static class Elevation
    {
        public static bool IsAdministrator()
        {
            try
            {
                using (WindowsIdentity id = WindowsIdentity.GetCurrent())
                {
                    var p = new WindowsPrincipal(id);
                    return p.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Relaunches this executable elevated with the given arguments and waits for it.
        /// Returns true when the elevated instance reported success (exit code 0).
        /// </summary>
        public static bool RunElevated(string arguments, out string error)
        {
            error = null;
            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
                psi.Arguments = arguments;
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                using (Process p = Process.Start(psi))
                {
                    if (p == null)
                    {
                        error = "The elevated process could not be started.";
                        return false;
                    }
                    p.WaitForExit();
                    if (p.ExitCode != 0)
                    {
                        error = "The elevated process failed (exit code " + p.ExitCode + ").";
                        return false;
                    }
                    return true;
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223) error = "The elevation request was cancelled.";
                else error = "Elevation failed: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = "Elevation failed: " + ex.Message;
                return false;
            }
        }
    }

    internal static class SingleInstance
    {
        private static Mutex _mutex;

        /// <summary>Returns false when another Quench process already owns the UI.</summary>
        public static bool Acquire()
        {
            try
            {
                bool created;
                _mutex = new Mutex(true, @"Local\Quench.4E1C7A2F.SingleInstance", out created);
                return created;
            }
            catch
            {
                return true;
            }
        }
    }
}
