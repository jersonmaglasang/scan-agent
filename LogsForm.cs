using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScanAgent
{
    public partial class LogsForm : Form
    {
        public LogsForm()
        {
            InitializeComponent();
        }

        private void LogsForm_Load(object sender, EventArgs e)
        { 
            LoadTodaysLogs();
        }

        private void LoadTodaysLogs() 
        {
            string logs = Logger.Instance.GetTodayLogs();
            this.LogsTextBox.Text = logs;
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            LoadTodaysLogs();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            this.LogsTextBox?.Clear();
            Logger.Instance.ClearTodayLogs();
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(this.LogsTextBox.Text);
            MessageBox.Show("Logs has been copied to Clipboard", "Scan Agent - Copy Logs",MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
