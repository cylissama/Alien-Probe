using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml;
using System.Windows.Forms;

namespace AlphaScan
{
    public partial class DisplayAndCount : UserControl
    {
        public event EventHandler<EventArgs> OnCountChanged;
        public event EventHandler OnCountChanging;
        TagDisplayAndCount Controler;
        public bool IsCounting = false;
      public  int SelectedLotIndex = -1;
       public int SelectedZoneIndex = -1;
       public string SelectedLotName = "";
        private ComboBox comboBoxLotSelect;
        private ComboBox comboBoxZoneSelect;
        private ListView listViewCounts;
        private Button button1;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        public string SelectedZoneName = "";
        public DisplayAndCount(TagDisplayAndCount control)
        {
            InitializeComponent();
            Controler = control;
            Refresh();
           
            
        }
        public override void  Refresh()
        {
            base.Refresh();
            this.comboBoxLotSelect.Items.Clear();
            XElement lots = Controler.GetLots();
            foreach (XElement el in lots.Descendants("LOT"))
            {
                comboBoxLotSelect.Items.Add(el.Attribute("name").Value);
            }
            comboBoxLotSelect.Sorted = true;
            this.comboBoxZoneSelect.Items.Clear();
            XElement zones = Controler.GetZones();
            foreach (XElement e1 in zones.Elements())
            {
                this.comboBoxZoneSelect.Items.Add(e1.Attribute("name").Value);
            }
        }

        
 
        
        public  void UpdateCount(string key, string value)
        {

            if (listViewCounts.Items.ContainsKey(key))
            {
                ListViewItem lvi = listViewCounts.Items[key];
                lvi.SubItems[2].Text = value;
            }
            else
            {
                ListViewItem lvi = new ListViewItem()
                {
                    Name = key
                };
                lvi.SubItems.Add("Prefix").Text = key;
                lvi.SubItems.Add("Count").Text = value;
                listViewCounts.Invoke(new Action(() => listViewCounts.Items.Add(lvi)));
             

            }
        }
        public delegate void Myhandler(string key, string value);




        private void ComboBoxLotSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckIfCounting( comboBoxLotSelect, SelectedLotIndex);
            SelectedLotIndex = comboBoxLotSelect.SelectedIndex;
            SelectedLotName = SelectedLotIndex >= 0 ? comboBoxLotSelect.Items[SelectedLotIndex].ToString() : "";
        }
        private void CheckIfCounting(ComboBox cb,  int previousIndex )
        {
            if (IsCounting)
            {
                
                DialogResult dr = MessageBox.Show("End currrent count?", "End Count", MessageBoxButtons.YesNo);
                if (dr == DialogResult.No)
                {
                    cb.SelectedIndex = previousIndex;
                    return;
                }
                else
                {
                    OnCountChanging(this, EventArgs.Empty);
                    SelectedLotIndex = cb.SelectedIndex;
                    Controler.SaveCount();
                    IsCounting = false;
                    //clear the list
                    listViewCounts.Items.Clear();
                    OnCountChanged(this, EventArgs.Empty);
                   
                }
          
            }

            

        }

        private void ComboBoxZoneSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckIfCounting(comboBoxZoneSelect, SelectedZoneIndex);
            SelectedZoneIndex = comboBoxZoneSelect.SelectedIndex;
            SelectedZoneName = SelectedZoneIndex >= 0 ? comboBoxZoneSelect.Items[SelectedZoneIndex].ToString() : "";
            Controler.SetZoneElement(SelectedZoneName);
        }

        private void InitializeComponent()
        {
            this.comboBoxLotSelect = new System.Windows.Forms.ComboBox();
            this.comboBoxZoneSelect = new System.Windows.Forms.ComboBox();
            this.listViewCounts = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // comboBoxLotSelect
            // 
            this.comboBoxLotSelect.FormattingEnabled = true;
            this.comboBoxLotSelect.Location = new System.Drawing.Point(4, 4);
            this.comboBoxLotSelect.Name = "comboBoxLotSelect";
            this.comboBoxLotSelect.Size = new System.Drawing.Size(229, 21);
            this.comboBoxLotSelect.TabIndex = 0;
            this.comboBoxLotSelect.SelectedIndexChanged += new System.EventHandler(this.ComboBoxLotSelect_SelectedIndexChanged);
            // 
            // comboBoxZoneSelect
            // 
            this.comboBoxZoneSelect.FormattingEnabled = true;
            this.comboBoxZoneSelect.Location = new System.Drawing.Point(255, 5);
            this.comboBoxZoneSelect.Name = "comboBoxZoneSelect";
            this.comboBoxZoneSelect.Size = new System.Drawing.Size(88, 21);
            this.comboBoxZoneSelect.TabIndex = 1;
            this.comboBoxZoneSelect.SelectedIndexChanged += new System.EventHandler(this.ComboBoxZoneSelect_SelectedIndexChanged);
            // 
            // listViewCounts
            // 
            this.listViewCounts.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.listViewCounts.Location = new System.Drawing.Point(4, 32);
            this.listViewCounts.Name = "listViewCounts";
            this.listViewCounts.Size = new System.Drawing.Size(462, 97);
            this.listViewCounts.TabIndex = 2;
            this.listViewCounts.UseCompatibleStateImageBehavior = false;
            this.listViewCounts.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Tag";
            this.columnHeader1.Width = 1;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Prefix";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Count";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(391, 5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "End Count";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // DisplayAndCount
            // 
            this.Controls.Add(this.button1);
            this.Controls.Add(this.listViewCounts);
            this.Controls.Add(this.comboBoxZoneSelect);
            this.Controls.Add(this.comboBoxLotSelect);
            this.Name = "DisplayAndCount";
            this.Size = new System.Drawing.Size(487, 150);
            this.ResumeLayout(false);

        }

        private void Button1_Click(object sender, EventArgs e)
        {
            if (!IsCounting) return;
            CheckIfCounting(comboBoxZoneSelect, SelectedZoneIndex);
            SelectedZoneIndex = comboBoxZoneSelect.SelectedIndex;
            SelectedZoneName = SelectedZoneIndex >= 0 ? comboBoxZoneSelect.Items[SelectedZoneIndex].ToString() : "";
            Controler.SetZoneElement(SelectedZoneName);
            OnCountChanged(this, EventArgs.Empty);
        }
       

    }
}
