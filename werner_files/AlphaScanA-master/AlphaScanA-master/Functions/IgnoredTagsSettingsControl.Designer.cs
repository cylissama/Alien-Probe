namespace AlphaScan.Functions
{
    partial class IgnoredTagsSettingsControl
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
            this.buttonLoadIgnorelist = new System.Windows.Forms.Button();
            this.buttonSaveBlacklist = new System.Windows.Forms.Button();
            this.buttonRemove = new System.Windows.Forms.Button();
            this.listBoxIgnoreList = new System.Windows.Forms.ListBox();
            this.buttonAdd = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonLoadIgnorelist
            // 
            this.buttonLoadIgnorelist.Location = new System.Drawing.Point(3, 3);
            this.buttonLoadIgnorelist.Name = "buttonLoadIgnorelist";
            this.buttonLoadIgnorelist.Size = new System.Drawing.Size(125, 23);
            this.buttonLoadIgnorelist.TabIndex = 29;
            this.buttonLoadIgnorelist.Text = "Load Ignorelist";
            this.buttonLoadIgnorelist.UseVisualStyleBackColor = true;
            this.buttonLoadIgnorelist.Click += new System.EventHandler(this.ButtonLoadIgnorelist_Click);
            // 
            // buttonSaveBlacklist
            // 
            this.buttonSaveBlacklist.Location = new System.Drawing.Point(3, 88);
            this.buttonSaveBlacklist.Name = "buttonSaveBlacklist";
            this.buttonSaveBlacklist.Size = new System.Drawing.Size(125, 23);
            this.buttonSaveBlacklist.TabIndex = 28;
            this.buttonSaveBlacklist.Text = "Save IgnoreList";
            this.buttonSaveBlacklist.UseVisualStyleBackColor = true;
            this.buttonSaveBlacklist.Click += new System.EventHandler(this.ButtonSaveBlacklist_Click);
            // 
            // buttonRemove
            // 
            this.buttonRemove.Location = new System.Drawing.Point(3, 117);
            this.buttonRemove.Name = "buttonRemove";
            this.buttonRemove.Size = new System.Drawing.Size(125, 23);
            this.buttonRemove.TabIndex = 27;
            this.buttonRemove.Text = "Remove Selected";
            this.buttonRemove.UseVisualStyleBackColor = true;
            this.buttonRemove.Click += new System.EventHandler(this.ButtonRemove_Click);
            // 
            // listBoxIgnoreList
            // 
            this.listBoxIgnoreList.FormattingEnabled = true;
            this.listBoxIgnoreList.Location = new System.Drawing.Point(134, 3);
            this.listBoxIgnoreList.Name = "listBoxIgnoreList";
            this.listBoxIgnoreList.Size = new System.Drawing.Size(218, 95);
            this.listBoxIgnoreList.TabIndex = 26;
            // 
            // buttonAdd
            // 
            this.buttonAdd.Location = new System.Drawing.Point(3, 58);
            this.buttonAdd.Name = "buttonAdd";
            this.buttonAdd.Size = new System.Drawing.Size(125, 23);
            this.buttonAdd.TabIndex = 25;
            this.buttonAdd.Text = "Add To Ignore";
            this.buttonAdd.UseVisualStyleBackColor = true;
            this.buttonAdd.Click += new System.EventHandler(this.ButtonAdd_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(3, 32);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(114, 20);
            this.textBox1.TabIndex = 24;
            // 
            // IgnoredTagsSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonLoadIgnorelist);
            this.Controls.Add(this.buttonSaveBlacklist);
            this.Controls.Add(this.buttonRemove);
            this.Controls.Add(this.listBoxIgnoreList);
            this.Controls.Add(this.buttonAdd);
            this.Controls.Add(this.textBox1);
            this.Name = "IgnoredTagsSettingsControl";
            this.Size = new System.Drawing.Size(360, 146);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonLoadIgnorelist;
        private System.Windows.Forms.Button buttonSaveBlacklist;
        private System.Windows.Forms.Button buttonRemove;
        private System.Windows.Forms.ListBox listBoxIgnoreList;
        private System.Windows.Forms.Button buttonAdd;
        private System.Windows.Forms.TextBox textBox1;
    }
}
