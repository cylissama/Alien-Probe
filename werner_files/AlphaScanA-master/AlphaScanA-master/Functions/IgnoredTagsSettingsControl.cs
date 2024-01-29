using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace AlphaScan.Functions
{
    public partial class IgnoredTagsSettingsControl : UserControl
    {
        IgnoredTagsFunction _IgnoredTagsFunction;
        const char padChar = '0';
        public const int _tagLength = 8;

        public IgnoredTagsSettingsControl(IgnoredTagsFunction IgnoredTagsFunc)
        {
            InitializeComponent();
            _IgnoredTagsFunction = IgnoredTagsFunc;
            try
            {
                StreamReader Import = new StreamReader(Convert.ToString(Properties.Settings.Default.IgnoreListFile));
                _IgnoredTagsFunction.Ignorelist.Clear();

                while (Import.Peek() >= 0) _IgnoredTagsFunction.Ignorelist.Add(Convert.ToString(Import.ReadLine()).PadLeft(_tagLength, padChar));

                Import.Close();
                listBoxIgnoreList.Items.AddRange(_IgnoredTagsFunction.Ignorelist.ToArray());
                
            }
            catch { MessageBox.Show("Ignored Tags Not Loaded"); }
        }

        private void ButtonLoadIgnorelist_Click(object sender, EventArgs e)
        {
            this.listBoxIgnoreList.Items.Clear();
            OpenFileDialog Open = new OpenFileDialog()
            {
                FileName = _IgnoredTagsFunction.IgnoreListFileName,
                Filter = "XML Document|*.xml|All Files|*.*"
            };
            try
            {
                DialogResult dr = Open.ShowDialog();
                if (dr == System.Windows.Forms.DialogResult.Cancel) return;
                StreamReader Import = new StreamReader(Convert.ToString(Open.FileName));
                _IgnoredTagsFunction.Ignorelist.Clear();

                while (Import.Peek() >= 0) _IgnoredTagsFunction.Ignorelist.Add(Convert.ToString(Import.ReadLine()).PadLeft(_tagLength, padChar));

                Import.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }
            
            _IgnoredTagsFunction.IgnoreListFileName = Open.FileName;
            listBoxIgnoreList.Items.AddRange(_IgnoredTagsFunction.Ignorelist.ToArray());
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            bool t = int.TryParse(textBox1.Text, out int x);
            if (!t) return;
            if (!_IgnoredTagsFunction.Ignorelist.Contains(textBox1.Text))
            {
                _IgnoredTagsFunction.Ignorelist.Add(textBox1.Text.PadLeft(_tagLength, padChar));
            }
            listBoxIgnoreList.Items.Clear();
            listBoxIgnoreList.Items.AddRange(_IgnoredTagsFunction.Ignorelist.ToArray());
            textBox1.Text = "";
           
        }

        private void ButtonSaveBlacklist_Click(object sender, EventArgs e)
        {
            StreamWriter Write;
            SaveFileDialog Open = new SaveFileDialog()
            {
                FileName = _IgnoredTagsFunction.IgnoreListFileName
            };
            try
            {
                Open.Filter = ("XML Document|*.xml|All Files|*.*");
                Open.ShowDialog();
                Write = new StreamWriter(Open.FileName);
                for (int I = 0; I < listBoxIgnoreList.Items.Count; I++)
                {
                    Write.WriteLine(Convert.ToString(listBoxIgnoreList.Items[I]));
                }
                Write.Close();
                _IgnoredTagsFunction.IgnoreListFileName = Open.FileName;
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(Convert.ToString(ex.Message));
                return;
            }
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            if (listBoxIgnoreList.SelectedIndex < 0) return;
            while (_IgnoredTagsFunction.Ignorelist.Contains(listBoxIgnoreList.SelectedItem.ToString())) _IgnoredTagsFunction.Ignorelist.Remove(listBoxIgnoreList.SelectedItem.ToString());
            listBoxIgnoreList.Items.Clear();
            listBoxIgnoreList.Items.AddRange(_IgnoredTagsFunction.Ignorelist.ToArray());
        }
    }
}
