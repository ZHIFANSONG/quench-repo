// Quench - headless helper used by the elevated instance.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Quench
{
    internal static class Headless
    {
        public const string ResultFileName = "quench-result.txt";

        public static int Run(string verb)
        {
            var log = new StringBuilder();
            int exitCode = 1;
            try
            {
                log.AppendLine("verb=" + verb);
                log.AppendLine("elevated=" + Elevation.IsAdministrator());

                if (PowerSave.IsQuietVerb(verb))
                {
                    exitCode = PowerSave.Run(verb, log);
                }
                else if (!Elevation.IsAdministrator())
                {
                    log.AppendLine("error=This helper must run elevated.");
                    exitCode = 3;
                }
                else
                {
                    List<GpuDevice> all = GpuDevices.QueryAll();
                    GpuDevice dgpu = null;
                    for (int i = 0; i < all.Count; i++)
                    {
                        if (all[i].Vendor == GpuVendor.Nvidia) { dgpu = all[i]; break; }
                    }
                    if (dgpu == null)
                    {
                        log.AppendLine("error=No NVIDIA display adapter was found.");
                        exitCode = 4;
                    }
                    else
                    {
                        log.AppendLine("instance=" + dgpu.InstanceId);
                        log.AppendLine("before.problem=" + dgpu.Problem);

                        if (verb == "status")
                        {
                            log.AppendLine("problem=" + dgpu.Problem);
                            log.AppendLine("power=" + (dgpu.PowerDataValid ? dgpu.PowerState : -1));
                            exitCode = 0;
                        }
                        else
                        {
                            bool enable = verb == "enable";
                            string error;
                            bool ok = GpuDevices.SetEnabled(dgpu, enable, out error);
                            log.AppendLine("changed=" + ok);
                            if (!ok) log.AppendLine("error=" + error);

                            // Re-read the node so the caller sees the settled state.
                            GpuDevices.ReadStatus(dgpu);
                            GpuDevices.ReadPower(dgpu);
                            log.AppendLine("after.problem=" + dgpu.Problem);
                            log.AppendLine("after.power=" + dgpu.PowerState);
                            exitCode = ok ? 0 : 5;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.AppendLine("exception=" + ex.GetType().Name + ": " + ex.Message);
                exitCode = 9;
            }

            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ResultFileName);
                File.WriteAllText(path, log.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
            return exitCode;
        }

        /// <summary>Reads and deletes the helper's result file, if any.</summary>
        public static string TakeResult()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ResultFileName);
                if (!File.Exists(path)) return null;
                string text = File.ReadAllText(path);
                try { File.Delete(path); }
                catch { }
                return text;
            }
            catch
            {
                return null;
            }
        }

        public static string Field(string result, string key)
        {
            if (string.IsNullOrEmpty(result)) return null;
            string[] lines = result.Replace("\r\n", "\n").Split('\n');
            string prefix = key + "=";
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                if (lines[i].StartsWith(prefix, StringComparison.Ordinal))
                    return lines[i].Substring(prefix.Length).Trim();
            }
            return null;
        }
    }
}
