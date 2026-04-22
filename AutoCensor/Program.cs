using System;
using System.Windows.Forms;

namespace AutoCensor
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Logger.Instance.Error("UnhandledException",
                    (Exception)e.ExceptionObject);
            };

            Application.Run(new MainForm());

            Logger.Instance.Dispose();
        }
    }
}