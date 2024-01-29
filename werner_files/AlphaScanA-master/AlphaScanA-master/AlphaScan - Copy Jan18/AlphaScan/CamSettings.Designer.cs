namespace AlphaScan
{
    partial class CamSettings
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.videoSourcePlayer = new AForge.Controls.VideoSourcePlayer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.disconnectButton = new System.Windows.Forms.Button();
            this.connectButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.snapshotResolutionsCombo = new System.Windows.Forms.ComboBox();
            this.videoResolutionsCombo = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.devicesCombo = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.triggerButton = new System.Windows.Forms.Button();
            this.checkBoxAnt1 = new System.Windows.Forms.CheckBox();
            this.checkBoxAnt2 = new System.Windows.Forms.CheckBox();
            this.checkBoxAnt3 = new System.Windows.Forms.CheckBox();
            this.checkBoxAnt4 = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // videoSourcePlayer
            // 
            this.videoSourcePlayer.AutoSizeControl = true;
            this.videoSourcePlayer.BackColor = System.Drawing.SystemColors.ControlDark;
            this.videoSourcePlayer.ForeColor = System.Drawing.Color.DarkRed;
            this.videoSourcePlayer.Location = new System.Drawing.Point(99, 6);
            this.videoSourcePlayer.Name = "videoSourcePlayer";
            this.videoSourcePlayer.Size = new System.Drawing.Size(322, 242);
            this.videoSourcePlayer.TabIndex = 0;
            this.videoSourcePlayer.VideoSource = null;
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Controls.Add(this.videoSourcePlayer);
            this.panel1.Location = new System.Drawing.Point(25, 110);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(520, 254);
            this.panel1.TabIndex = 18;
            // 
            // disconnectButton
            // 
            this.disconnectButton.Location = new System.Drawing.Point(455, 51);
            this.disconnectButton.Name = "disconnectButton";
            this.disconnectButton.Size = new System.Drawing.Size(75, 23);
            this.disconnectButton.TabIndex = 17;
            this.disconnectButton.Text = "&Disconnect";
            this.disconnectButton.UseVisualStyleBackColor = true;
            this.disconnectButton.Click += new System.EventHandler(this.DisconnectButton_Click);
            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(455, 21);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(75, 23);
            this.connectButton.TabIndex = 16;
            this.connectButton.Text = "&Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.ConnectButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(230, 54);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(101, 13);
            this.label3.TabIndex = 15;
            this.label3.Text = "Snapshot resoluton:";
            this.toolTip.SetToolTip(this.label3, "Press shutter button on your camera to make snapshot");
            // 
            // snapshotResolutionsCombo
            // 
            this.snapshotResolutionsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.snapshotResolutionsCombo.FormattingEnabled = true;
            this.snapshotResolutionsCombo.Location = new System.Drawing.Point(335, 51);
            this.snapshotResolutionsCombo.Name = "snapshotResolutionsCombo";
            this.snapshotResolutionsCombo.Size = new System.Drawing.Size(100, 21);
            this.snapshotResolutionsCombo.TabIndex = 14;
            this.toolTip.SetToolTip(this.snapshotResolutionsCombo, "Press shutter button on your camera to make snapshot");
            // 
            // videoResolutionsCombo
            // 
            this.videoResolutionsCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.videoResolutionsCombo.FormattingEnabled = true;
            this.videoResolutionsCombo.Location = new System.Drawing.Point(120, 51);
            this.videoResolutionsCombo.Name = "videoResolutionsCombo";
            this.videoResolutionsCombo.Size = new System.Drawing.Size(100, 21);
            this.videoResolutionsCombo.TabIndex = 13;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(35, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 13);
            this.label2.TabIndex = 12;
            this.label2.Text = "Video resoluton:";
            // 
            // devicesCombo
            // 
            this.devicesCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.devicesCombo.FormattingEnabled = true;
            this.devicesCombo.Location = new System.Drawing.Point(120, 21);
            this.devicesCombo.Name = "devicesCombo";
            this.devicesCombo.Size = new System.Drawing.Size(315, 21);
            this.devicesCombo.TabIndex = 11;
            this.devicesCombo.SelectedIndexChanged += new System.EventHandler(this.DevicesCombo_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(35, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(77, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Video devices:";
            // 
            // toolTip
            // 
            this.toolTip.AutoPopDelay = 5000;
            this.toolTip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.toolTip.InitialDelay = 100;
            this.toolTip.ReshowDelay = 100;
            // 
            // triggerButton
            // 
            this.triggerButton.Location = new System.Drawing.Point(455, 81);
            this.triggerButton.Name = "triggerButton";
            this.triggerButton.Size = new System.Drawing.Size(75, 23);
            this.triggerButton.TabIndex = 19;
            this.triggerButton.Text = "&Trigger";
            this.triggerButton.UseVisualStyleBackColor = true;
            this.triggerButton.Click += new System.EventHandler(this.TriggerButton_Click);
            // 
            // checkBoxAnt1
            // 
            this.checkBoxAnt1.AutoSize = true;
            this.checkBoxAnt1.Location = new System.Drawing.Point(25, 380);
            this.checkBoxAnt1.Name = "checkBoxAnt1";
            this.checkBoxAnt1.Size = new System.Drawing.Size(75, 17);
            this.checkBoxAnt1.TabIndex = 20;
            this.checkBoxAnt1.Text = "Antenna 1";
            this.checkBoxAnt1.UseVisualStyleBackColor = true;
            // 
            // checkBoxAnt2
            // 
            this.checkBoxAnt2.AutoSize = true;
            this.checkBoxAnt2.Location = new System.Drawing.Point(106, 380);
            this.checkBoxAnt2.Name = "checkBoxAnt2";
            this.checkBoxAnt2.Size = new System.Drawing.Size(75, 17);
            this.checkBoxAnt2.TabIndex = 21;
            this.checkBoxAnt2.Text = "Antenna 2";
            this.checkBoxAnt2.UseVisualStyleBackColor = true;
            // 
            // checkBoxAnt3
            // 
            this.checkBoxAnt3.AutoSize = true;
            this.checkBoxAnt3.Location = new System.Drawing.Point(187, 380);
            this.checkBoxAnt3.Name = "checkBoxAnt3";
            this.checkBoxAnt3.Size = new System.Drawing.Size(75, 17);
            this.checkBoxAnt3.TabIndex = 22;
            this.checkBoxAnt3.Text = "Antenna 3";
            this.checkBoxAnt3.UseVisualStyleBackColor = true;
            // 
            // checkBoxAnt4
            // 
            this.checkBoxAnt4.AutoSize = true;
            this.checkBoxAnt4.Location = new System.Drawing.Point(268, 380);
            this.checkBoxAnt4.Name = "checkBoxAnt4";
            this.checkBoxAnt4.Size = new System.Drawing.Size(75, 17);
            this.checkBoxAnt4.TabIndex = 23;
            this.checkBoxAnt4.Text = "Antenna 4";
            this.checkBoxAnt4.UseVisualStyleBackColor = true;
            // 
            // CamSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBoxAnt4);
            this.Controls.Add(this.checkBoxAnt3);
            this.Controls.Add(this.checkBoxAnt2);
            this.Controls.Add(this.checkBoxAnt1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.disconnectButton);
            this.Controls.Add(this.connectButton);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.snapshotResolutionsCombo);
            this.Controls.Add(this.videoResolutionsCombo);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.devicesCombo);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.triggerButton);
            this.Name = "CamSettings";
            this.Size = new System.Drawing.Size(570, 499);
            this.Load += new System.EventHandler(this.CamSettings_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private AForge.Controls.VideoSourcePlayer videoSourcePlayer;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button disconnectButton;
        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.ComboBox snapshotResolutionsCombo;
        private System.Windows.Forms.ComboBox videoResolutionsCombo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox devicesCombo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button triggerButton;
        private System.Windows.Forms.CheckBox checkBoxAnt1;
        private System.Windows.Forms.CheckBox checkBoxAnt2;
        private System.Windows.Forms.CheckBox checkBoxAnt3;
        private System.Windows.Forms.CheckBox checkBoxAnt4;
    }
}
