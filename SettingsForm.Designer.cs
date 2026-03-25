namespace ScanAgent
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblScanFolder;
        private System.Windows.Forms.TextBox txtScanFolder;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblWebhookUrl;
        private System.Windows.Forms.TextBox txtWebhookUrl;
        private System.Windows.Forms.Label lblWebSocketPort;
        private System.Windows.Forms.TextBox txtWebSocketPort;
        private System.Windows.Forms.CheckBox chkAutoStart;
        private System.Windows.Forms.Label lblScannerPrograms;
        private System.Windows.Forms.TextBox txtScannerPrograms;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblScanFolder = new System.Windows.Forms.Label();
            this.txtScanFolder = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lblWebhookUrl = new System.Windows.Forms.Label();
            this.txtWebhookUrl = new System.Windows.Forms.TextBox();
            this.lblWebSocketPort = new System.Windows.Forms.Label();
            this.txtWebSocketPort = new System.Windows.Forms.TextBox();
            this.chkAutoStart = new System.Windows.Forms.CheckBox();
            this.lblScannerPrograms = new System.Windows.Forms.Label();
            this.txtScannerPrograms = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblScanFolder
            // 
            this.lblScanFolder.AutoSize = true;
            this.lblScanFolder.Location = new System.Drawing.Point(12, 15);
            this.lblScanFolder.Name = "lblScanFolder";
            this.lblScanFolder.Size = new System.Drawing.Size(67, 13);
            this.lblScanFolder.TabIndex = 0;
            this.lblScanFolder.Text = "Scan Folder:";
            // 
            // txtScanFolder
            // 
            this.txtScanFolder.Location = new System.Drawing.Point(15, 31);
            this.txtScanFolder.Name = "txtScanFolder";
            this.txtScanFolder.Size = new System.Drawing.Size(350, 20);
            this.txtScanFolder.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(371, 29);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // lblWebhookUrl
            // 
            this.lblWebhookUrl.AutoSize = true;
            this.lblWebhookUrl.Location = new System.Drawing.Point(12, 65);
            this.lblWebhookUrl.Name = "lblWebhookUrl";
            this.lblWebhookUrl.Size = new System.Drawing.Size(77, 13);
            this.lblWebhookUrl.TabIndex = 3;
            this.lblWebhookUrl.Text = "Webhook URL:";
            // 
            // txtWebhookUrl
            // 
            this.txtWebhookUrl.Location = new System.Drawing.Point(15, 81);
            this.txtWebhookUrl.Name = "txtWebhookUrl";
            this.txtWebhookUrl.Size = new System.Drawing.Size(431, 20);
            this.txtWebhookUrl.TabIndex = 4;
            // 
            // lblWebSocketPort
            // 
            this.lblWebSocketPort.AutoSize = true;
            this.lblWebSocketPort.Location = new System.Drawing.Point(12, 115);
            this.lblWebSocketPort.Name = "lblWebSocketPort";
            this.lblWebSocketPort.Size = new System.Drawing.Size(89, 13);
            this.lblWebSocketPort.TabIndex = 5;
            this.lblWebSocketPort.Text = "WebSocket Port:";
            // 
            // txtWebSocketPort
            // 
            this.txtWebSocketPort.Location = new System.Drawing.Point(15, 131);
            this.txtWebSocketPort.Name = "txtWebSocketPort";
            this.txtWebSocketPort.Size = new System.Drawing.Size(100, 20);
            this.txtWebSocketPort.TabIndex = 6;
            // 
            // chkAutoStart
            // 
            this.chkAutoStart.AutoSize = true;
            this.chkAutoStart.Location = new System.Drawing.Point(15, 165);
            this.chkAutoStart.Name = "chkAutoStart";
            this.chkAutoStart.Size = new System.Drawing.Size(151, 17);
            this.chkAutoStart.TabIndex = 7;
            this.chkAutoStart.Text = "Start automatically on login";
            this.chkAutoStart.UseVisualStyleBackColor = true;
            // 
            // lblScannerPrograms
            // 
            this.lblScannerPrograms.AutoSize = true;
            this.lblScannerPrograms.Location = new System.Drawing.Point(12, 195);
            this.lblScannerPrograms.Name = "lblScannerPrograms";
            this.lblScannerPrograms.Size = new System.Drawing.Size(200, 13);
            this.lblScannerPrograms.TabIndex = 8;
            this.lblScannerPrograms.Text = "Scanner Programs (one per line):";
            // 
            // txtScannerPrograms
            // 
            this.txtScannerPrograms.Location = new System.Drawing.Point(15, 211);
            this.txtScannerPrograms.Multiline = true;
            this.txtScannerPrograms.Name = "txtScannerPrograms";
            this.txtScannerPrograms.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtScannerPrograms.Size = new System.Drawing.Size(431, 80);
            this.txtScannerPrograms.TabIndex = 9;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(290, 310);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 10;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(371, 310);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 11;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 351);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.txtScannerPrograms);
            this.Controls.Add(this.lblScannerPrograms);
            this.Controls.Add(this.chkAutoStart);
            this.Controls.Add(this.txtWebSocketPort);
            this.Controls.Add(this.lblWebSocketPort);
            this.Controls.Add(this.txtWebhookUrl);
            this.Controls.Add(this.lblWebhookUrl);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtScanFolder);
            this.Controls.Add(this.lblScanFolder);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Scan Agent Settings";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}

