using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScanAgent
{
    public class Logger
    {
        private static Logger instance;
        private List<string> todayLogs;
        private string logFilePath;
        private object lockObject = new object();

        public static Logger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Logger();
                }
                return instance;
            }
        }

        private Logger()
        {
            todayLogs = new List<string>();
            
            // Create logs directory
            string logsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ScanAgent"
            );
            logsDir = Path.Combine(logsDir, "Logs");
            
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            logFilePath = Path.Combine(logsDir, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
        }

        public void Log(string message)
        {
            lock (lockObject)
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = string.Format("[{0}] {1}", timestamp, message);

                // Add to in-memory list
                todayLogs.Add(logEntry);

                // Write to file
                try
                {
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Ignore file write errors
                }

                // Also write to console for debugging
                Console.WriteLine(logEntry);
            }
        }

        public string GetTodayLogs()
        {
            lock (lockObject)
            {
                StringBuilder sb = new StringBuilder();
                foreach (string log in todayLogs)
                {
                    sb.AppendLine(log);
                }
                return sb.ToString();
            }
        }

        public void ClearTodayLogs()
        {
            lock (lockObject)
            {
                todayLogs.Clear();
            }
        }
    }
}

