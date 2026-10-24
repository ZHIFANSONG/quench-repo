// Quench - entry point.
using System;
using System.Windows.Forms;

namespace Quench
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            try { Native.SetProcessDPIAware(); }
            catch { }

            // Headless mode used by the elevated helper instance.
            string verb = CommandLine.Verb(args);
            if (verb != null)
                return Headless.Run(verb);

            if (!SingleInstance.Acquire())
            {
                MessageBox.Show("Quench is already running.", "Quench",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }
    }

    internal static class CommandLine
    {
        /// <summary>Returns "enable", "disable" or "status" when Quench was started headless.</summary>
        public static string Verb(string[] args)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length; i++)
            {
                string a = (args[i] ?? "").Trim().ToLowerInvariant();
                if (a == "--disable" || a == "-disable") return "disable";
                if (a == "--enable" || a == "-enable") return "enable";
                if (a == "--status" || a == "-status") return "status";
            }
            return null;
        }
    }
}
