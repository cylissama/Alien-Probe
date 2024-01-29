using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace AlphaScan
{
    public partial class BlacklistSettingsControl : UserControl

    {
        BlacklistedTagFunction _BlacklistFunction;
         const char padChar = '0';

        public BlacklistSettingsControl(BlacklistedTagFunction blacklistFunction)
        {
            InitializeComponent();
            _BlacklistFunction = blacklistFunction;
            try
            {
                StreamReader Import = new StreamReader(Convert.ToString(Properties.Settings.Default.BlacklistFile));
                _BlacklistFunction.Blacklist.Clear();

                while (Import.Peek() >= 0) _BlacklistFunction.Blacklist.Add(Convert.ToString(Import.ReadLine()));

                Import.Close();
                listBoxBlacklist.Items.AddRange(_BlacklistFunction.Blacklist.ToArray());
            }
            catch  { MessageBox.Show("Blacklist Not Loaded"); }
        }
        private void Button2_Click(object sender, EventArgs e)
        {
            bool t = int.TryParse(textBox1.Text, out int x);
            if (!t) return;
            if (!_BlacklistFunction.Blacklist.Contains(textBox1.Text))
            {
                _BlacklistFunction.Blacklist.Add(textBox1.Text);
            }
            listBoxBlacklist.Items.Clear();
            listBoxBlacklist.Items.AddRange(_BlacklistFunction.Blacklist.ToArray());
            textBox1.Text = "";
            _BlacklistFunction.IsListDirty = true;

        }
        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            if (listBoxBlacklist.SelectedIndex < 0) return;
            while (_BlacklistFunction.Blacklist.Contains(listBoxBlacklist.SelectedItem.ToString())) _BlacklistFunction.Blacklist.Remove(listBoxBlacklist.SelectedItem.ToString());
            listBoxBlacklist.Items.Clear();
            listBoxBlacklist.Items.AddRange(_BlacklistFunction.Blacklist.ToArray());
            _BlacklistFunction.IsListDirty = true;
        }

        private void ButtonLoadBlacklist_Click(object sender, EventArgs e)
        {
            this.listBoxBlacklist.Items.Clear();
            OpenFileDialog Open = new OpenFileDialog()
            {
                FileName = _BlacklistFunction.BlaclistFileName,
                Filter = "XML Document|*.xml|All Files|*.*"
            };
            try
            {
                DialogResult dr = Open.ShowDialog();
                if (dr == System.Windows.Forms.DialogResult.Cancel) return;
                StreamReader Import = new StreamReader(Convert.ToString(Open.FileName));
                _BlacklistFunction.Blacklist.Clear();

                while (Import.Peek() >= 0) _BlacklistFunction.Blacklist.Add(Convert.ToString(Import.ReadLine()));

                Import.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }
            _BlacklistFunction.IsListDirty = false;
            _BlacklistFunction.BlaclistFileName = Open.FileName;
            listBoxBlacklist.Items.AddRange(_BlacklistFunction.Blacklist.ToArray());



        }

        private void ButtonSaveBlacklist_Click(object sender, EventArgs e)
        {
            StreamWriter Write;
            SaveFileDialog Open = new SaveFileDialog()
            {
                FileName = _BlacklistFunction.BlaclistFileName
            };
            try
            {
                Open.Filter = ("XML Document|*.xml|All Files|*.*");
                Open.ShowDialog();
                Write = new StreamWriter(Open.FileName);
                for (int I = 0; I < listBoxBlacklist.Items.Count; I++)
                {
                    Write.WriteLine(Convert.ToString(listBoxBlacklist.Items[I]));
                }
                Write.Close();
                _BlacklistFunction.BlaclistFileName = Open.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }
            _BlacklistFunction.IsListDirty = false;

        }
        public void SaveBlacklist(object sender, EventArgs e)
        {
            ButtonSaveBlacklist_Click(sender, e);
        }
            
    }
}

