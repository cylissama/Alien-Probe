namespace AlphaScan
{
    partial class BlacklistSettingsControl
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
            this.buttonLoadBlacklist = new System.Windows.Forms.Button();
            this.buttonSaveBlacklist = new System.Windows.Forms.Button();
            this.buttonRemove = new System.Windows.Forms.Button();
            this.listBoxBlacklist = new System.Windows.Forms.ListBox();
            this.buttonAdd = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonLoadBlacklist
            // 
            this.buttonLoadBlacklist.Location = new System.Drawing.Point(0, 3);
            this.buttonLoadBlacklist.Name = "buttonLoadBlacklist";
            this.buttonLoadBlacklist.Size = new System.Drawing.Size(125, 23);
            this.buttonLoadBlacklist.TabIndex = 23;
            this.buttonLoadBlacklist.Text = "Load Blacklist";
            this.buttonLoadBlacklist.UseVisualStyleBackColor = true;
            this.buttonLoadBlacklist.Click += new System.EventHandler(this.ButtonLoadBlacklist_Click);
            // 
            // buttonSaveBlacklist
            // 
            this.buttonSaveBlacklist.Location = new System.Drawing.Point(0, 88);
            this.buttonSaveBlacklist.Name = "buttonSaveBlacklist";
            this.buttonSaveBlacklist.Size = new System.Drawing.Size(125, 23);
            this.buttonSaveBlacklist.TabIndex = 22;
            this.buttonSaveBlacklist.Text = "Save Blacklist";
            this.buttonSaveBlacklist.UseVisualStyleBackColor = true;
            this.buttonSaveBlacklist.Click += new System.EventHandler(this.ButtonSaveBlacklist_Click);
            // 
            // buttonRemove
            // 
            this.buttonRemove.Location = new System.Drawing.Point(0, 117);
            this.buttonRemove.Name = "buttonRemove";
            this.buttonRemove.Size = new System.Drawing.Size(125, 23);
            this.buttonRemove.TabIndex = 21;
            this.buttonRemove.Text = "Remove Selected";
            this.buttonRemove.UseVisualStyleBackColor = true;
            this.buttonRemove.Click += new System.EventHandler(this.ButtonRemove_Click);
            // 
            // listBoxBlacklist
            // 
            this.listBoxBlacklist.FormattingEnabled = true;
            this.listBoxBlacklist.Location = new System.Drawing.Point(131, 3);
            this.listBoxBlacklist.Name = "listBoxBlacklist";
            this.listBoxBlacklist.Size = new System.Drawing.Size(218, 95);
            this.listBoxBlacklist.TabIndex = 20;
            // 
            // buttonAdd
            // 
            this.buttonAdd.Location = new System.Drawing.Point(0, 58);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(125, 23);
            this.buttonAdd.TabIndex = 19;
            this.buttonAdd.Text = "Add To Blacklist";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.Button2_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(0, 32);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(114, 20);
            this.textBox1.TabIndex = 18;
            // 
            // BlacklistSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonLoadBlacklist);
            this.Controls.Add(this.buttonSaveBlacklist);
            this.Controls.Add(this.buttonRemove);
            this.Controls.Add(this.listBoxBlacklist);
            this.Controls.Add(this.buttonAdd);
            this.Controls.Add(this.textBox1);
            this.Name = "BlacklistSettingsControl";
            this.Size = new System.Drawing.Size(356, 150);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonLoadBlacklist;
        private System.Windows.Forms.Button buttonSaveBlacklist;
        private System.Windows.Forms.Button buttonRemove;
        private System.Windows.Forms.ListBox listBoxBlacklist;
        private System.Windows.Forms.Button buttonAdd;
        private System.Windows.Forms.TextBox textBox1;
    }
}
