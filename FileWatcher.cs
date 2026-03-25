using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ScanAgent
{
    public class FileDetectedEventArgs : EventArgs
    {
        public string FilePath { get; set; }
    }

    public class FileWatcher : IDisposable
    {
        private string folderPath;
        private FileSystemWatcher watcher;
        private HashSet<string> processedFiles;
        private HashSet<string> pendingFiles;
        private Timer stabilityTimer;
        private Dictionary<string, FileStabilityCheck> stabilityChecks;

        public event EventHandler<FileDetectedEventArgs> FileDetected;

        public FileWatcher(string path)
        {
            folderPath = path;
            processedFiles = new HashSet<string>();
            pendingFiles = new HashSet<string>();
            stabilityChecks = new Dictionary<string, FileStabilityCheck>();
        }

        public void Start()
        {
            // Ensure folder exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Mark existing files as processed (ignore old files)
            MarkExistingFilesAsProcessed();

            // Create file system watcher
            watcher = new FileSystemWatcher(folderPath);
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size;
            watcher.Created += OnFileCreated;
            watcher.Changed += OnFileChanged;
            watcher.EnableRaisingEvents = true;

            Logger.Instance.Log("File watcher started for: " + folderPath);
        }

        public void Stop()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }

            // Cancel all pending stability checks
            lock (stabilityChecks)
            {
                stabilityChecks.Clear();
            }

            Logger.Instance.Log("File watcher stopped");
        }

        public void SetFolder(string newPath)
        {
            Stop();
            folderPath = newPath;
            processedFiles.Clear();
            pendingFiles.Clear();
            Start();
        }

        private void MarkExistingFilesAsProcessed()
        {
            try
            {
                string[] existingFiles = Directory.GetFiles(folderPath);
                foreach (string file in existingFiles)
                {
                    processedFiles.Add(file);
                }

                if (existingFiles.Length > 0)
                {
                    Logger.Instance.Log("Ignoring " + existingFiles.Length + " existing file(s) in scan folder");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Error marking existing files: " + ex.Message);
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            HandleFileEvent(e.FullPath);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            HandleFileEvent(e.FullPath);
        }

        private void HandleFileEvent(string filePath)
        {
            // Ignore if already processed or pending
            if (processedFiles.Contains(filePath) || pendingFiles.Contains(filePath))
            {
                return;
            }

            // Add to pending and start stability check
            pendingFiles.Add(filePath);
            CheckFileStability(filePath);
        }

        private void CheckFileStability(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    pendingFiles.Remove(filePath);
                    return;
                }

                FileInfo fileInfo = new FileInfo(filePath);
                long initialSize = fileInfo.Length;

                if (initialSize == 0)
                {
                    pendingFiles.Remove(filePath);
                    return;
                }

                Logger.Instance.Log("Checking stability for: " + Path.GetFileName(filePath) + " (size: " + initialSize + " bytes)");

                // Wait 1 second and check again
                Thread.Sleep(1000);

                if (!File.Exists(filePath))
                {
                    pendingFiles.Remove(filePath);
                    return;
                }

                FileInfo currentInfo = new FileInfo(filePath);
                long currentSize = currentInfo.Length;

                if (currentSize == initialSize && !processedFiles.Contains(filePath))
                {
                    // File is stable
                    Logger.Instance.Log("File stable and ready: " + Path.GetFileName(filePath));
                    processedFiles.Add(filePath);
                    pendingFiles.Remove(filePath);

                    // Trigger file detected event
                    OnFileDetectedEvent(filePath);
                }
                else if (currentSize != initialSize)
                {
                    // File still being written, check again
                    Logger.Instance.Log("File still being written: " + Path.GetFileName(filePath));
                    CheckFileStability(filePath);
                }
                else
                {
                    pendingFiles.Remove(filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Error checking file stability: " + ex.Message);
                pendingFiles.Remove(filePath);
            }
        }

        private void OnFileDetectedEvent(string filePath)
        {
            if (FileDetected != null)
            {
                FileDetected(this, new FileDetectedEventArgs { FilePath = filePath });
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private class FileStabilityCheck
        {
            public string FilePath { get; set; }
            public long InitialSize { get; set; }
            public DateTime CheckTime { get; set; }
        }
    }
}

