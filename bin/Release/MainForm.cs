using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Web;
using System.Drawing.Text;

namespace ScanAgent
{
    public partial class MainForm : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private FileWatcher fileWatcher;
        private WebSocketServer webSocketServer;
        private SettingsForm settingsForm;
        private LogsForm logsForm;

        public MainForm()
        {
            InitializeComponent();
            InitializeTrayIcon();
            InitializeFileWatcher();
            InitializeWebSocketServer();

            // Check for protocol URL in command line arguments
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && args[1].StartsWith("scan-agent://"))
            {
                HandleProtocolURL(args[1]);
            }

            // Hide the form - we only show the tray icon
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
        }

        private void InitializeTrayIcon()
        {
            // Create tray menu
            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("Settings", null, OnSettings);
            trayMenu.Items.Add("Show Logs", null, OnShowLogs);
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Launch Scanner Programs", null, OnLaunchScannerPrograms);
            trayMenu.Items.Add("-");

            // Protocol handler submenu
            // ToolStripMenuItem protocolMenu = new ToolStripMenuItem("Protocol Handler (scan-agent://)");
            // protocolMenu.DropDownItems.Add("Register Protocol", null, OnRegisterProtocol);
            // protocolMenu.DropDownItems.Add("Unregister Protocol", null, OnUnregisterProtocol);
            // protocolMenu.DropDownItems.Add("Check Status", null, OnCheckProtocolStatus);
            // trayMenu.Items.Add(protocolMenu); 
            // trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, OnExit);

            // Create tray icon
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Scan Agent";
            trayIcon.Icon = this.Icon; // SystemIcons.Application; // TODO: Replace with custom icon
            trayIcon.ContextMenuStrip = trayMenu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += OnSettings;
        }

        private void InitializeFileWatcher()
        {
            string scanFolder = SettingsManager.Instance.ScanFolder;
            
            fileWatcher = new FileWatcher(scanFolder);
            fileWatcher.FileDetected += OnFileDetected;
            fileWatcher.Start();
            
            Logger.Instance.Log("File watcher started for: " + scanFolder);
        }

        private void InitializeWebSocketServer()
        {
            int port = SettingsManager.Instance.WebSocketPort;
            
            webSocketServer = new WebSocketServer(port);
            webSocketServer.Start();
            
            Logger.Instance.Log("WebSocket server started on port: " + port);
        }

        private void OnFileDetected(object sender, FileDetectedEventArgs e)
        {
            string filename = System.IO.Path.GetFileName(e.FilePath);
            Logger.Instance.Log("File detected: " + filename);

            // Broadcast file detected via WebSocket
            webSocketServer.BroadcastFileDetected(e.FilePath, filename);

            // Determine webhook URL
            string webhookUrl = webSocketServer.CurrentSessionWebhookUrl;
            if (string.IsNullOrEmpty(webhookUrl))
            {
                webhookUrl = SettingsManager.Instance.WebHookUrl;
            }

            if (string.IsNullOrEmpty(webhookUrl))
            {
                Logger.Instance.Log("No webhook URL configured");
                return;
            }

            // Upload file
            WebhookUploader.UploadFile(e.FilePath, webhookUrl, (success, response, error) =>
            {
                if (success)
                {
                    Logger.Instance.Log("File uploaded successfully: " + filename);
                    
                    // Extract s3BucketUrl from response if available
                    string s3BucketUrl = ExtractS3BucketUrl(response);
                    
                    if (!string.IsNullOrEmpty(s3BucketUrl))
                    {
                        Logger.Instance.Log("S3 Bucket URL: " + s3BucketUrl);
                    }

                    webSocketServer.BroadcastFileUploaded(e.FilePath, filename, s3BucketUrl);
                    ShowNotification("File Uploaded", "Successfully uploaded " + filename);
                }
                else
                {
                    Logger.Instance.Log("Upload failed: " + filename + " - " + error);
                    ShowNotification("Upload Failed", "Failed to upload " + filename);
                }
            });
        }

        private string ExtractS3BucketUrl(string jsonResponse)
        {
            // Simple JSON parsing for .NET 3.5 (no JSON.NET available)
            if (string.IsNullOrEmpty(jsonResponse)) return string.Empty;
            
            try
            {
                int index = jsonResponse.IndexOf("\"s3BucketUrl\"");
                if (index >= 0)
                {
                    int colonIndex = jsonResponse.IndexOf(":", index);
                    int startQuote = jsonResponse.IndexOf("\"", colonIndex);
                    int endQuote = jsonResponse.IndexOf("\"", startQuote + 1);
                    
                    if (startQuote >= 0 && endQuote > startQuote)
                    {
                        return jsonResponse.Substring(startQuote + 1, endQuote - startQuote - 1);
                    }
                }
            }
            catch { }
            
            return string.Empty;
        }

        private void ShowNotification(string title, string message)
        {
            if (trayIcon != null)
            {
                trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
            }
        }

        private void OnShowLogs(object sender, EventArgs e)
        {
            if (logsForm == null || logsForm.IsDisposed)
            {
                logsForm = new LogsForm();
            }
            logsForm.ShowDialog();
        }

        private void OnSettings(object sender, EventArgs e)
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new SettingsForm();
                settingsForm.SettingsSaved += OnSettingsSaved;
            }
            settingsForm.ShowDialog();
        }

        private void OnSettingsSaved(object sender, EventArgs e)
        {
            // Restart file watcher with new settings
            if (fileWatcher != null)
            {
                fileWatcher.Stop();
                fileWatcher.SetFolder(SettingsManager.Instance.ScanFolder);
                fileWatcher.Start();
            }
        }

        private void OnLaunchScannerPrograms(object sender, EventArgs e)
        {
            LaunchScannerPrograms();
        }

        private void OnRegisterProtocol(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "This will register the scan-agent:// protocol handler.\n\n" +
                "Administrator privileges are required.\n\n" +
                "Do you want to continue?",
                "Register Protocol Handler",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                bool success = ProtocolRegistration.RegisterProtocol();
                if (success)
                {
                    MessageBox.Show(
                        "Protocol handler registered successfully!\n\n" +
                        "You can now use URLs like:\n" +
                        "scan-agent://scan?token=xxx&webHookUrl=https://example.com",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to register protocol handler.\n\n" +
                        "Please run the application as Administrator or use install-protocol.bat",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void OnUnregisterProtocol(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "This will unregister the scan-agent:// protocol handler.\n\n" +
                "Administrator privileges are required.\n\n" +
                "Do you want to continue?",
                "Unregister Protocol Handler",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                bool success = ProtocolRegistration.UnregisterProtocol();
                if (success)
                {
                    MessageBox.Show(
                        "Protocol handler unregistered successfully!",
                        "Success",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Failed to unregister protocol handler.\n\n" +
                        "Please run the application as Administrator or use uninstall-protocol.bat",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void OnCheckProtocolStatus(object sender, EventArgs e)
        {
            string status = ProtocolRegistration.GetRegistrationStatus();
            MessageBox.Show(
                "Protocol Handler Status:\n\n" + status,
                "Protocol Handler Status",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void LaunchScannerPrograms()
        {
            var defaultScanSnapProgram = @"C:\Program Files (x86)\PFU\ScanSnap\Home\PfuSshMain.exe";
            string[] scannerPrograms = SettingsManager.Instance.ScannerPrograms;

            // Use default scanner program if none configured
            if (scannerPrograms == null || scannerPrograms.Length == 0)
            {
               scannerPrograms = new string[] { defaultScanSnapProgram };
            }

            var launchedCtr = 0;
            foreach (string programPath in scannerPrograms)
            {
                if (System.IO.File.Exists(programPath))
                {
                    var isLaunched = TryLaunchProgram(programPath);
                    if (isLaunched)
                    {
                        launchedCtr++;
                    }
                }
            } 
        }
        private bool TryLaunchProgram(string programPath)
        {
            try
            {
                Process.Start(programPath);
                Logger.Instance.Log("Launched scanner program: " + programPath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Failed to launch scanner program: " + programPath + " - " + ex.Message);
            }
            return false;
        }

        private void OnExit(object sender, EventArgs e)
        {
            // Clean up
            if (fileWatcher != null)
            {
                fileWatcher.Stop();
            }

            if (webSocketServer != null)
            {
                webSocketServer.Stop();
            }

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            Application.Exit();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Launch scanner programs on startup
            LaunchScannerPrograms();
        }

        private void HandleProtocolURL(string urlString)
        {
            try
            {
                Logger.Instance.Log("Received protocol URL: " + urlString);

                // Parse URL: scan-agent://scan?token=xxx&dealId=yyy&transactionId=100&formId=200&formName=abc&webHookUrl=https://example.com
                if (!urlString.StartsWith("scan-agent://"))
                {
                    Logger.Instance.Log("Invalid protocol URL: " + urlString);
                    return;
                }

                // Extract query string
                int queryIndex = urlString.IndexOf('?');
                if (queryIndex < 0)
                {
                    Logger.Instance.Log("No query parameters in protocol URL");
                    return;
                }

                string queryString = urlString.Substring(queryIndex + 1);
                NameValueCollection queryParams = HttpUtility.ParseQueryString(queryString);

                // Log parameters
                Logger.Instance.Log("Protocol URL params: " + queryParams.ToString());

                // Update webhook URL if provided
                string webhookUrl = queryParams["webHookUrl"];
                if (!string.IsNullOrEmpty(webhookUrl))
                {
                    SettingsManager.Instance.WebHookUrl = webhookUrl;
                    Logger.Instance.Log("Updated webhook URL: " + webhookUrl);
                }

                // Start scan session via WebSocket server
                if (webSocketServer != null)
                {
                    System.Collections.Generic.Dictionary<string, object> scanData =
                        new System.Collections.Generic.Dictionary<string, object>();

                    scanData["token"] = queryParams["token"] ?? "";
                    scanData["dealId"] = queryParams["dealId"] ?? "";
                    scanData["transactionId"] = queryParams["transactionId"] ?? "";
                    scanData["formId"] = queryParams["formId"] ?? "";
                    scanData["formName"] = queryParams["formName"] ?? "";
                    scanData["webHookUrl"] = webhookUrl ?? "";
                    scanData["scanFolder"] = SettingsManager.Instance.ScanFolder;

                    // Broadcast scan_started event
                    System.Collections.Generic.Dictionary<string, object> message =
                        new System.Collections.Generic.Dictionary<string, object>();
                    message["type"] = "scan_started";
                    message["data"] = scanData;

                    webSocketServer.BroadcastMessage(message);

                    Logger.Instance.Log("Scan session started via protocol URL");
                    ShowNotification("Scan Session Started", "Ready to scan documents");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Log("Error handling protocol URL: " + ex.Message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (trayIcon != null)
                {
                    trayIcon.Dispose();
                }
                if (trayMenu != null)
                {
                    trayMenu.Dispose();
                }
                if (fileWatcher != null)
                {
                    fileWatcher.Dispose();
                }
                if (webSocketServer != null)
                {
                    webSocketServer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }
}

