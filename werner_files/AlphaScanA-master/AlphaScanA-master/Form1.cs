using AlphaScan.Functions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
namespace AlphaScan
{
    public partial class Form1 : Form
    {
        static List<ITagFunction> TagFunctions= new List<ITagFunction>();
        static List<RFIDTag> Ignoredtags = new List<RFIDTag>();
        static List<RFIDTag> AlertTags = new List<RFIDTag>();
        static TagDisplayAndCount TC = new TagDisplayAndCount();
        static BlacklistedTagFunction BL = new BlacklistedTagFunction();
        static List<RFIDTag> TagsOfInterest = new List<RFIDTag>();
        static IgnoredTagsFunction ITF = new IgnoredTagsFunction();
       
        static RecordTagsFunction RT = new RecordTagsFunction();
       
        static TagCommFunction TCF = new TagCommFunction();
        bool IsScanning = false;
        public Form1()
        {
            InitializeComponent();

            TC.OnCountReset += TC_OnCountReset;
            TC.OnCountChanging += TC_OnCountChanging;
            flowLayoutPanel1.Controls.Add(RT.GetControl());
            flowLayoutPanel1.Controls.Add(TC.GetDisplayAndCountControl);


            
            TagFunctions.Add(ITF);
            TagFunctions.Add(BL);
            TagFunctions.Add(TC);
            TagFunctions.Add(TCF);
            TagFunctions.Add(RT);
            

            foreach(M6ReaderControl c in this.Controls.OfType<M6ReaderControl>())
            {
                c.SetTagFunctions(ref TagFunctions);
                c.SetIgnoredtags(ref Ignoredtags);
                c.SetTagsOfInterest(ref TagsOfInterest);
                
            }
            
        }

        private void TC_OnCountChanging(object sender, EventArgs e)
        {
            if (IsScanning) Button1_Click(sender, e);
        }

        private void TC_OnCountReset(object sender, EventArgs e)
        {
            this.listViewAlerts.Items.Clear();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
           

        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SettingsForm s = new SettingsForm(this);
            s.ShowDialog();
        }
        public List<ITagFunction> TagFunctionsList
        {
            get { return TagFunctions; }
        }
       public void AddAlertTag (RFIDTag tag)
        {
            if (tag.Tagresponse.Color.Equals(Color.Red)) System.Media.SystemSounds.Exclamation.Play(); else    System.Media.SystemSounds.Asterisk.Play();
            listViewAlerts.Invoke(new Action(() =>
            {
                foreach (ListViewItem listViewItem in listViewAlerts.Items) 
           
            {
                listViewItem.BackColor = Color.White;
            }
            }));
            if (AlertTags.Contains(tag))
            {
                listViewAlerts.Invoke(new Action(() =>
                {
                    if (listViewAlerts.Items.ContainsKey(tag.TagHex)){
                        ListViewItem viewItem = listViewAlerts.Items.Find(tag.TagHex, true)[0];
                        viewItem.BackColor = tag.Tagresponse.Color;
                    }
                }));
            }
            else
            {
                AlertTags.Add(tag);
                ListViewItem lvi = new ListViewItem()
                {
                    Name = tag.TagHex,
                   
                };
                
                lvi.SubItems.Add("message").Text = tag.Tagresponse.Message;
                lvi.Tag = tag;



                lvi.SubItems[0] = new ListViewItem.ListViewSubItem(lvi, tag.PermitString);
                //   lvi.SubItems[1] = new ListViewItem.ListViewSubItem(lvi,tag.permitString);
                if (!listViewAlerts.Items.Contains(lvi))
                {
                    listViewAlerts.Invoke(new Action(() => listViewAlerts.Items.Add(lvi)));
                }
               

            }

        }
        public string StatusText
        {
            get => this.toolStripStatusLabel1.Text;
            set
            {
                toolStripStatusLabel1.Text = value;
                ResetStatus();

            }
        }
        private async void  ResetStatus()
        {
            Task<string> task = Task.Run(() => "Status");
            await Task.Delay(5000);
            toolStripStatusLabel1.Text = await task;

            
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            IsScanning = !IsScanning;
            foreach (M6ReaderControl c in this.Controls.OfType<M6ReaderControl>()) if (IsScanning) c.Start(); else c.Stop();
               
            if (IsScanning) button1.Text = "Pause"; else button1.Text = "Scan";
        }

        private void ListViewAlerts_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (!_Switching) return;
            RFIDTag tag = (RFIDTag)e.Item.Tag;
            if (e.Item.Checked)
            {
                if (!TagsOfInterest.Contains(tag))
                {
                    TagsOfInterest.Add(tag);
                }
            }
            else
            {
               
                while (TagsOfInterest.Contains(tag))
                {
                    TagsOfInterest.Remove(tag);
                }
            }
            panelSearchMode.Visible = (TagsOfInterest.Count > 0);

        }
      

        

        public void FindTags()
        {


        }
        private bool _Switching = false;
        private void ListViewAlerts_ItemCheck(object sender, ItemCheckEventArgs e)
        {

            if (e.CurrentValue == e.NewValue)
            {
                _Switching = false;
            }
            else
            {
                _Switching = true;    
            }
        }

        private void ListViewAlerts_ColumnClick(object sender, ColumnClickEventArgs e)
        {

            if (listViewAlerts.Sorting == SortOrder.Descending) listViewAlerts.Sorting = SortOrder.Ascending; else listViewAlerts.Sorting =SortOrder.Descending;
            listViewAlerts.Sort();
        }
    }
    
}
