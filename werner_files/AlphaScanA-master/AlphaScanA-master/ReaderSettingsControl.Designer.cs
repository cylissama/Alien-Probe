using System;

namespace AlphaScan
{
    partial class ReaderSettingsControl
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.checkBoxUseSimulator = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(4, 25);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(217, 20);
            this.textBox1.TabIndex = 0;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(258, 25);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 20);
            this.button2.TabIndex = 4;
            this.button2.Text = "Save";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // checkBoxUseSimulator
            // 
            this.checkBoxUseSimulator.AutoSize = true;
            this.checkBoxUseSimulator.Location = new System.Drawing.Point(13, 4);
            this.checkBoxUseSimulator.Name = "checkBoxUseSimulator";
            this.checkBoxUseSimulator.Size = new System.Drawing.Size(80, 17);
            this.checkBoxUseSimulator.TabIndex = 5;
            this.checkBoxUseSimulator.Text = "checkBox1";
            this.checkBoxUseSimulator.UseVisualStyleBackColor = true;
            
            // 
            // ReaderSettingsControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBoxUseSimulator);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.textBox1);
            this.Name = "ReaderSettingsControl";
            this.Size = new System.Drawing.Size(686, 385);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void Button2_Click(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBoxUseSimulator;
    }
}
