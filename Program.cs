using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace ScanAgent
{
    static class Program
    {
        private static Mutex mutex = null;
        private const string APP_GUID = "A1B2C3D4-E5F6-7890-ABCD-EF1234567890";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Create a mutex to ensure single instance
            bool createdNew;
            mutex = new Mutex(true, "Global\\" + APP_GUID, out createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                // If we have a protocol URL argument, we could send it to the running instance
                // For now, just exit
                Logger.Instance.Log("Another instance is already running");
                return;
            }

            try
            {
                //if (!ProtocolRegistration.IsProtocolRegistered())
                //{
                    ProtocolRegistration.RegisterProtocol();
                //}
                
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            finally
            {
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
            }
        }
    }
}

