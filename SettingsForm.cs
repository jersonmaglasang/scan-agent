using System;
using System.Windows.Forms;

namespace ScanAgent
{
    public partial class SettingsForm : Form
    {
        public event EventHandler SettingsSaved;

        public SettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            txtScanFolder.Text = SettingsManager.Instance.ScanFolder;
            txtWebhookUrl.Text = SettingsManager.Instance.WebHookUrl;
            chkAutoStart.Checked = SettingsManager.Instance.AutoStart;
            txtWebSocketPort.Text = SettingsManager.Instance.WebSocketPort.ToString();
            
            string[] programs = SettingsManager.Instance.ScannerPrograms;
            if (programs != null && programs.Length > 0)
            {
                txtScannerPrograms.Text = string.Join(Environment.NewLine, programs);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select scan folder";
                dialog.SelectedPath = txtScanFolder.Text;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtScanFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(txtScanFolder.Text))
            {
                MessageBox.Show("Please select a scan folder.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int port;
            if (!int.TryParse(txtWebSocketPort.Text, out port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Please enter a valid port number (1-65535).", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Save settings
            SettingsManager.Instance.ScanFolder = txtScanFolder.Text;
            SettingsManager.Instance.WebHookUrl = txtWebhookUrl.Text;
            SettingsManager.Instance.AutoStart = chkAutoStart.Checked;
            SettingsManager.Instance.WebSocketPort = port;

            // Parse scanner programs
            string[] programs = txtScannerPrograms.Text.Split(
                new string[] { Environment.NewLine }, 
                StringSplitOptions.RemoveEmptyEntries
            );
            SettingsManager.Instance.ScannerPrograms = programs;

            // Notify that settings were saved
            if (SettingsSaved != null)
            {
                SettingsSaved(this, EventArgs.Empty);
            }

            MessageBox.Show("Settings saved successfully!", "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

