using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml;
using System.Runtime.Serialization;

namespace AlphaScan
{
    public partial class DisplayAndCountSettings : UserControl
    {
         static XDocument PermitZonesDoc;
         string filename = "permitzones.xml";
         TreeNode currTreeNode;
       
        private Button buttonLoadTree;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private ListBox listBox1;
        private TabPage tabPage3;
        private CheckBox checkBoxNoReder;
        TagDisplayAndCount CallingFunction;
        public DisplayAndCountSettings(TagDisplayAndCount callingFunction)
        {
            InitializeComponent();
            CallingFunction = callingFunction;
            LoadSettings();
            
            
        }
        public void LoadSettings()
        {
            #region #######Load Configuration Info ###########

           

            try
            {
                PermitZonesDoc = XDocument.Load(Properties.Settings.Default["Filename"].ToString());
                checkBoxNoReder.Checked = (bool)Properties.Settings.Default["NoReaderMode"];
            }
            catch (Exception )
            {
                OpenFileDialog of = new OpenFileDialog()
                {
                    Filter = "XML Files | *.xml",
                    FileName = "PermitZones.xml"
                };
                DialogResult dr = of.ShowDialog();
                if (dr == DialogResult.Cancel) return;
                filename = of.FileName;
                LoadDoc();
                PermitZonesDoc = XDocument.Load(filename);
                Properties.Settings.Default["Filename"] = filename;

            }
            LoadPermitPrefixes();
            LoadtreeFromXdoc(PermitZonesDoc);
            LoadZonesFromXDoc();
            LoadLotInfo();
            CallingFunction.LoadZoneInfo(PermitZonesDoc);
        }
        public void LoadLotInfo()
        {
            listBoxLots.Items.Clear();
            XElement xEl = PermitZonesDoc.Root.Element("LOTNAMES");
            foreach (XElement e1 in xEl.Elements())
            {
                this.listBoxLots.Items.Add(e1.Attribute("name").Value);
            }
            listBoxLots.Sorted = true;
            SaveDoc();

        }
        public void LoadZonesFromXDoc()
        {
            XElement xEl = PermitZonesDoc.Element("PERMITZONES").Element("ZONES");
            foreach (XElement e1 in xEl.Elements())
            {
                this.comboBoxZone.Items.Add(e1.Attribute("name").Value);
            }
        }
           
            #endregion
        private void LoadPermitPrefixes()
        {
            ComboBox.ObjectCollection oc = comboBoxPermitPrefix.Items;
            oc.Clear();
            listBox1.Items.Clear();
            foreach (XElement e1 in PermitZonesDoc.Root.Descendants("PERMITPREFIXES").Descendants("Permit"))
            {
                
               oc.Add(e1.Attribute("prefix").Value);
                PermitListboxItem pli = new PermitListboxItem(e1);
                listBox1.Items.Add(pli);
                 
            }
            comboBoxPermitPrefix.Sorted = true;    
        }

        private void ButtonAddPermitZoneTime_Click(object sender, EventArgs e)
        {
            string zoneString = this.comboBoxZone.Text;
            string TZTimeStart = dateTimePicker1.Value.ToShortTimeString();
            string TZTimeEnd =   dateTimePicker2.Value.ToShortTimeString();
            string prefixString = comboBoxPermitPrefix.Text;
            XElement ZoneElement;
            XElement TimeSpanElement;
            XElement PermitElement;
            //go throughthe doc and see if there is a zone element with this name
            //get to the base of the zones
            
            IEnumerable < XElement > ZoneElements =(from el in PermitZonesDoc.Root.Descendants("ZONE") where el.Attribute("name").Value == zoneString select el);
            if (ZoneElements.Count() < 1)
            {
                ZoneElement = new XElement("ZONE");
                ZoneElement.SetAttributeValue("name", zoneString);
                PermitZonesDoc.Root.Element("ZONES").Add(ZoneElement);


            }
            else ZoneElement = ZoneElements.First();

            IEnumerable<XElement> TimespanElements = (from el in ZoneElement.Descendants("TimeSpan") where (el.Attribute("Start").Value == TZTimeStart) && (el.Attribute("End").Value == TZTimeEnd) select el);
            if (TimespanElements.Count() < 1)
            {
                TimeSpanElement = new XElement("TimeSpan");
                TimeSpanElement.SetAttributeValue("Start", TZTimeStart);
                TimeSpanElement.SetAttributeValue("End", TZTimeEnd);
                ZoneElement.Add(TimeSpanElement);
            }
            else TimeSpanElement = TimespanElements.First();
            IEnumerable<XElement> PermitElements = (from el in TimeSpanElement.Descendants("Permit") where (el.Attribute("prefix").Value == prefixString) select el);
            if (PermitElements.Count() < 1)
            {
                PermitElement = new XElement("Permit");
                PermitElement.SetAttributeValue("prefix", prefixString);
                TimeSpanElement.Add(PermitElement);
                //PermitZonesDoc.Save(filename);
                
            }

            LoadtreeFromXdoc(PermitZonesDoc); 
            


        }

        private void ButtonSaveTree_Click(object sender, EventArgs e)
        {
            SaveFileDialog sf = new SaveFileDialog()
            {
                FileName = filename
            };
            DialogResult dr = sf.ShowDialog();
            if (dr == DialogResult.Cancel) return;
           
            filename = sf.FileName;
            SaveDoc();
        }
        public void LoadtreeFromXdoc(XDocument xdoc)
        {
            TreeNode root;
            if (treeView1.Nodes.Count == 0) root = new TreeNode("Zones"); else root = treeView1.Nodes[0];
            root.Name = "Zones";
            XElement xEl = xdoc.Element("PERMITZONES");
 
            treeView1.Nodes.Clear();
            TreeNode node = new TreeNode("Zones");
            foreach (XElement ZoneElements in xEl.Descendants("ZONE"))
            {
                TreeNode ZoneNode = new TreeNode(ZoneElements.Attribute("name").Value);
                node.Nodes.Add(ZoneNode);
                foreach (XElement TimespanElement in ZoneElements.Elements("TimeSpan"))
                {
                    TreeNode TimespanNode = new TimeZoneTreeNode(TimespanElement.Attribute("Start").Value, TimespanElement.Attribute("End").Value)
                    {
                        ToolTipText = TimespanElement.Attribute("End").Value
                    };
                    ZoneNode.Nodes.Add(TimespanNode);
                    foreach (XElement PrefixElement in TimespanElement.Elements("Permit"))
                    {
                        TreeNode PrefixNode = new TreeNode(PrefixElement.Attribute("prefix").Value);

                        TimespanNode.Nodes.Add(PrefixNode);
                    }
                }
            }

            treeView1.Nodes.Add(node);
            treeView1.Sort();
           
        }

        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            currTreeNode = e.Node;



            if (currTreeNode.Parent != null && currTreeNode.Parent.GetType() == typeof(TimeZoneTreeNode))
            {
                comboBoxZone.Text = currTreeNode.Parent.Parent.Text;
                TimeZoneTreeNode tzNode = (TimeZoneTreeNode)currTreeNode.Parent;
                dateTimePicker1.Text = tzNode.StartTime;
                dateTimePicker2.Text = tzNode.EndTime;
                comboBoxPermitPrefix.Text = currTreeNode.Text;
                


            }
            else
            { return; }

            
        }

        private void ButtonRemoveNode_Click(object sender, EventArgs e)
        {
            
            
            if (currTreeNode == null) return; //no treenode selected

            TreeNode zoneNode;
            XElement zoneElement;

           

            TimeZoneTreeNode tzNode;
            XElement tzElement;
            
            
            if (currTreeNode.Parent == null)
            {
                MessageBox.Show("Top Level node can not be deleted");
                return;
            }
            if(currTreeNode.GetType() == typeof(TimeZoneTreeNode))
            {
                //its the tz node
                tzNode = (TimeZoneTreeNode)currTreeNode;
                zoneNode = tzNode.Parent;

                IEnumerable<XElement> ZoneElements = (from el in PermitZonesDoc.Root.Descendants("ZONE") where el.Attribute("name").Value == zoneNode.Text select el);
                if (ZoneElements.Count() < 1)
                {
                    return;
                }
                zoneElement = ZoneElements.First();
                IEnumerable<XElement> TimespanElements = (from el in zoneElement.Descendants("TimeSpan") where (el.Attribute("Start").Value == tzNode.StartTime) && (el.Attribute("End").Value == tzNode.EndTime) select el);
                if (TimespanElements.Count() < 1)
                {
                    return;
                }
                TimespanElements.First().Remove();
 

            }
            else if (currTreeNode.Parent.GetType() == typeof(TimeZoneTreeNode))
            {
               // it's the permit
                tzNode = (TimeZoneTreeNode)currTreeNode.Parent;
                zoneNode = tzNode.Parent;
                IEnumerable<XElement> ZoneElements = (from el in PermitZonesDoc.Root.Descendants("ZONE") where el.Attribute("name").Value == zoneNode.Text select el);
                if (ZoneElements.Count() < 1)
                {
                    return;
                }
                zoneElement = ZoneElements.First();
                IEnumerable<XElement> TimespanElements = (from el in zoneElement.Descendants("TimeSpan") where (el.Attribute("Start").Value == tzNode.StartTime) && (el.Attribute("End").Value == tzNode.EndTime) select el);
                if (TimespanElements.Count() < 1)
                {
                    return;
                }
                tzElement = TimespanElements.First();
                IEnumerable<XElement> PermitElements = (from el in tzElement.Descendants("Permit") where (el.Attribute("prefix").Value == currTreeNode.Text) select el);
                if (PermitElements.Count() < 1)
                {
                    return;
                }
                PermitElements.First().Remove();

            }
            else 
            {
                //its the zone
                zoneNode = currTreeNode;
                IEnumerable<XElement> ZoneElements = (from el in PermitZonesDoc.Root.Descendants("ZONE") where el.Attribute("name").Value == zoneNode.Text select el);
                if (ZoneElements.Count() < 1)
                {
                    return;
                }
                ZoneElements.First().Remove();
            }

            LoadtreeFromXdoc(PermitZonesDoc);
        }

        private void ComboBoxPermitPrefix_SelectedIndexChanged(object sender, EventArgs e)
        {
            //Get the selected prefix from the selected.
            string selectedPrefix = comboBoxPermitPrefix.Text;
            //find the prefix in the xdoc
            XElement PrefixElement = (from el in PermitZonesDoc.Root.Descendants("PERMITPREFIXES").Descendants("Permit") where el.Attribute("prefix").Value == selectedPrefix select el).First();
            if (PrefixElement != null)
            {
                textBoxPrefix.Text = PrefixElement.Attribute("prefix").Value;
                textBoxFAC.Text = PrefixElement.Attribute("fac").Value;
                textBoxStartRange.Text = PrefixElement.Attribute("start").Value;
                textBoxEndRange.Text = PrefixElement.Attribute("end").Value;
                textBoxOffset.Text = PrefixElement.Attribute("offset").Value;
            }

        }

        private void ButtonAddPermitPrefix_Click(object sender, EventArgs e)
        {
            string selectedPrefix = textBoxPrefix.Text;
                //look to see if this prefix is already in use
           IEnumerable<XElement> PrefixElements = (from el in PermitZonesDoc.Root.Descendants("PERMITPREFIXES").Descendants("Permit") where el.Attribute("prefix").Value == selectedPrefix select el);
           while (PrefixElements.Count() > 0)
           {
               PrefixElements.First().Remove();
           }
           XElement newEl = new XElement("Permit");
           newEl.SetAttributeValue("prefix", textBoxPrefix.Text);
           newEl.SetAttributeValue("fac", textBoxFAC.Text);
           newEl.SetAttributeValue("start", textBoxStartRange.Text);
           newEl.SetAttributeValue("end", textBoxEndRange.Text);
           newEl.SetAttributeValue("offset", textBoxOffset.Text);

           PermitZonesDoc.Root.Descendants("PERMITPREFIXES").First().Add(newEl);

           SaveDoc();
           LoadPermitPrefixes();
            textBoxPrefix.Clear();
            textBoxStartRange.Clear();
            textBoxFAC.Clear();
            textBoxEndRange.Clear();
            textBoxOffset.Clear();
            textBoxPrefix.Focus();
        }
        public  XDocument PermitZoneDoc { get { 
            return PermitZoneDoc; 
        } }

        private void ButtonAddLot_Click(object sender, EventArgs e)
        {
            string lotString = textBoxLotname.Text;
            //look to see if this lot is already in use
            IEnumerable<XElement> LotElements = (from el in PermitZonesDoc.Root.Descendants("LOTNAMES").Descendants("lot") where el.Attribute("prefix").Value == lotString select el);
            while (LotElements.Count() > 0)
            {
                LotElements.First().Remove();
            }
            XElement newEl = new XElement("LOT");
            newEl.SetAttributeValue("name", lotString);
            PermitZonesDoc.Root.Descendants("LOTNAMES").First().Add(newEl);
            LoadLotInfo();
        }

        private void ButtonRemoveLot_Click(object sender, EventArgs e)
        {
            foreach (object selected in listBoxLots.SelectedItems)
            {
                IEnumerable<XElement> LotElements = (from el in PermitZonesDoc.Root.Descendants("LOTNAMES").Descendants("LOT") where el.Attribute("name").Value == selected.ToString() select el);
                while (LotElements.Count() > 0)
                {
                    LotElements.First().Remove();
                }
            }
            LoadLotInfo();
        }
        private void SaveDoc()
        {

            PermitZonesDoc.Save(filename);
            CallingFunction.LoadZoneInfo(PermitZonesDoc);
            
        }
        private void LoadDoc()
        {
            PermitZonesDoc = XDocument.Load(filename);
            CallingFunction.LoadZoneInfo(PermitZonesDoc);

        }
#region Designer Code
        private void InitializeComponent()
        {
            this.comboBoxPermitPrefix = new System.Windows.Forms.ComboBox();
            this.comboBoxZone = new System.Windows.Forms.ComboBox();
            this.dateTimePicker1 = new System.Windows.Forms.DateTimePicker();
            this.dateTimePicker2 = new System.Windows.Forms.DateTimePicker();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.buttonAddPermitZoneTime = new System.Windows.Forms.Button();
            this.treeView1 = new System.Windows.Forms.TreeView();
            this.buttonSaveTree = new System.Windows.Forms.Button();
            this.buttonRemoveNode = new System.Windows.Forms.Button();
            this.textBoxPrefix = new System.Windows.Forms.TextBox();
            this.textBoxFAC = new System.Windows.Forms.TextBox();
            this.textBoxStartRange = new System.Windows.Forms.TextBox();
            this.textBoxEndRange = new System.Windows.Forms.TextBox();
            this.buttonAddPermitPrefix = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.listBoxLots = new System.Windows.Forms.ListBox();
            this.buttonAddLot = new System.Windows.Forms.Button();
            this.buttonRemoveLot = new System.Windows.Forms.Button();
            this.textBoxLotname = new System.Windows.Forms.TextBox();
            this.textBoxOffset = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.buttonLoadTree = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.checkBoxNoReder = new System.Windows.Forms.CheckBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBoxPermitPrefix
            // 
            this.comboBoxPermitPrefix.FormattingEnabled = true;
            this.comboBoxPermitPrefix.Location = new System.Drawing.Point(97, 77);
            this.comboBoxPermitPrefix.Name = "comboBoxPermitPrefix";
            this.comboBoxPermitPrefix.Size = new System.Drawing.Size(121, 21);
            this.comboBoxPermitPrefix.TabIndex = 5;
            this.comboBoxPermitPrefix.SelectedIndexChanged += new System.EventHandler(this.ComboBoxPermitPrefix_SelectedIndexChanged);
            // 
            // comboBoxZone
            // 
            this.comboBoxZone.FormattingEnabled = true;
            this.comboBoxZone.Location = new System.Drawing.Point(97, 3);
            this.comboBoxZone.Name = "comboBoxZone";
            this.comboBoxZone.Size = new System.Drawing.Size(121, 21);
            this.comboBoxZone.Sorted = true;
            this.comboBoxZone.TabIndex = 6;
            // 
            // dateTimePicker1
            // 
            this.dateTimePicker1.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dateTimePicker1.Location = new System.Drawing.Point(97, 30);
            this.dateTimePicker1.Name = "dateTimePicker1";
            this.dateTimePicker1.Size = new System.Drawing.Size(99, 20);
            this.dateTimePicker1.TabIndex = 7;
            // 
            // dateTimePicker2
            // 
            this.dateTimePicker2.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dateTimePicker2.Location = new System.Drawing.Point(97, 51);
            this.dateTimePicker2.Name = "dateTimePicker2";
            this.dateTimePicker2.Size = new System.Drawing.Size(99, 20);
            this.dateTimePicker2.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "Zone";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Start Time";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 58);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(52, 13);
            this.label3.TabIndex = 11;
            this.label3.Text = "End Time";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 77);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(65, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Permit Prefix";
            // 
            // buttonAddPermitZoneTime
            // 
            this.buttonAddPermitZoneTime.Location = new System.Drawing.Point(269, 83);
            this.buttonAddPermitZoneTime.Name = "buttonAddPermitZoneTime";
            this.buttonAddPermitZoneTime.Size = new System.Drawing.Size(75, 23);
            this.buttonAddPermitZoneTime.TabIndex = 13;
            this.buttonAddPermitZoneTime.Text = "Add";
            this.buttonAddPermitZoneTime.UseVisualStyleBackColor = true;
            this.buttonAddPermitZoneTime.Click += new System.EventHandler(this.ButtonAddPermitZoneTime_Click);
            // 
            // treeView1
            // 
            this.treeView1.Location = new System.Drawing.Point(9, 125);
            this.treeView1.Name = "treeView1";
            this.treeView1.Size = new System.Drawing.Size(386, 97);
            this.treeView1.TabIndex = 14;
            this.treeView1.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView1_AfterSelect);
            // 
            // buttonSaveTree
            // 
            this.buttonSaveTree.Location = new System.Drawing.Point(484, 0);
            this.buttonSaveTree.Name = "buttonSaveTree";
            this.buttonSaveTree.Size = new System.Drawing.Size(75, 23);
            this.buttonSaveTree.TabIndex = 15;
            this.buttonSaveTree.Text = "Save Tree";
            this.buttonSaveTree.UseVisualStyleBackColor = true;
            this.buttonSaveTree.Click += new System.EventHandler(this.ButtonSaveTree_Click);
            // 
            // buttonRemoveNode
            // 
            this.buttonRemoveNode.Location = new System.Drawing.Point(429, 125);
            this.buttonRemoveNode.Name = "buttonRemoveNode";
            this.buttonRemoveNode.Size = new System.Drawing.Size(75, 23);
            this.buttonRemoveNode.TabIndex = 16;
            this.buttonRemoveNode.Text = "Remove";
            this.buttonRemoveNode.UseVisualStyleBackColor = true;
            this.buttonRemoveNode.Click += new System.EventHandler(this.ButtonRemoveNode_Click);
            // 
            // textBoxPrefix
            // 
            this.textBoxPrefix.Location = new System.Drawing.Point(8, 22);
            this.textBoxPrefix.Name = "textBoxPrefix";
            this.textBoxPrefix.Size = new System.Drawing.Size(100, 20);
            this.textBoxPrefix.TabIndex = 17;
            // 
            // textBoxFAC
            // 
            this.textBoxFAC.Location = new System.Drawing.Point(114, 22);
            this.textBoxFAC.Name = "textBoxFAC";
            this.textBoxFAC.Size = new System.Drawing.Size(100, 20);
            this.textBoxFAC.TabIndex = 18;
            // 
            // textBoxStartRange
            // 
            this.textBoxStartRange.Location = new System.Drawing.Point(220, 22);
            this.textBoxStartRange.Name = "textBoxStartRange";
            this.textBoxStartRange.Size = new System.Drawing.Size(100, 20);
            this.textBoxStartRange.TabIndex = 19;
            // 
            // textBoxEndRange
            // 
            this.textBoxEndRange.Location = new System.Drawing.Point(326, 22);
            this.textBoxEndRange.Name = "textBoxEndRange";
            this.textBoxEndRange.Size = new System.Drawing.Size(100, 20);
            this.textBoxEndRange.TabIndex = 20;
            // 
            // buttonAddPermitPrefix
            // 
            this.buttonAddPermitPrefix.Location = new System.Drawing.Point(515, 22);
            this.buttonAddPermitPrefix.Name = "buttonAddPermitPrefix";
            this.buttonAddPermitPrefix.Size = new System.Drawing.Size(75, 23);
            this.buttonAddPermitPrefix.TabIndex = 22;
            this.buttonAddPermitPrefix.Text = "Add Prefix";
            this.buttonAddPermitPrefix.UseVisualStyleBackColor = true;
            this.buttonAddPermitPrefix.Click += new System.EventHandler(this.ButtonAddPermitPrefix_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(33, 13);
            this.label5.TabIndex = 22;
            this.label5.Text = "Prefix";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(118, 3);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(27, 13);
            this.label6.TabIndex = 23;
            this.label6.Text = "FAC";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(217, 3);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(29, 13);
            this.label7.TabIndex = 24;
            this.label7.Text = "Start";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(323, 3);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(26, 13);
            this.label8.TabIndex = 25;
            this.label8.Text = "End";
            // 
            // listBoxLots
            // 
            this.listBoxLots.FormattingEnabled = true;
            this.listBoxLots.Location = new System.Drawing.Point(6, 26);
            this.listBoxLots.Name = "listBoxLots";
            this.listBoxLots.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.listBoxLots.Size = new System.Drawing.Size(312, 95);
            this.listBoxLots.TabIndex = 26;
            // 
            // buttonAddLot
            // 
            this.buttonAddLot.Location = new System.Drawing.Point(340, 26);
            this.buttonAddLot.Name = "buttonAddLot";
            this.buttonAddLot.Size = new System.Drawing.Size(75, 23);
            this.buttonAddLot.TabIndex = 27;
            this.buttonAddLot.Text = "Add Lot";
            this.buttonAddLot.UseVisualStyleBackColor = true;
            this.buttonAddLot.Click += new System.EventHandler(this.ButtonAddLot_Click);
            // 
            // buttonRemoveLot
            // 
            this.buttonRemoveLot.Location = new System.Drawing.Point(340, 56);
            this.buttonRemoveLot.Name = "buttonRemoveLot";
            this.buttonRemoveLot.Size = new System.Drawing.Size(75, 23);
            this.buttonRemoveLot.TabIndex = 28;
            this.buttonRemoveLot.Text = "Remove Lot";
            this.buttonRemoveLot.UseVisualStyleBackColor = true;
            this.buttonRemoveLot.Click += new System.EventHandler(this.ButtonRemoveLot_Click);
            // 
            // textBoxLotname
            // 
            this.textBoxLotname.Location = new System.Drawing.Point(443, 28);
            this.textBoxLotname.Name = "textBoxLotname";
            this.textBoxLotname.Size = new System.Drawing.Size(127, 20);
            this.textBoxLotname.TabIndex = 29;
            // 
            // textBoxOffset
            // 
            this.textBoxOffset.Location = new System.Drawing.Point(432, 22);
            this.textBoxOffset.Name = "textBoxOffset";
            this.textBoxOffset.Size = new System.Drawing.Size(77, 20);
            this.textBoxOffset.TabIndex = 21;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(429, 3);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(35, 13);
            this.label9.TabIndex = 31;
            this.label9.Text = "Offset";
            // 
            // buttonLoadTree
            // 
            this.buttonLoadTree.Location = new System.Drawing.Point(365, 0);
            this.buttonLoadTree.Name = "buttonLoadTree";
            this.buttonLoadTree.Size = new System.Drawing.Size(75, 23);
            this.buttonLoadTree.TabIndex = 32;
            this.buttonLoadTree.Text = "LoadTree";
            this.buttonLoadTree.UseVisualStyleBackColor = true;
            this.buttonLoadTree.Click += new System.EventHandler(this.ButtonLoadTree_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(6, 32);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(607, 526);
            this.tabControl1.TabIndex = 33;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.comboBoxPermitPrefix);
            this.tabPage1.Controls.Add(this.comboBoxZone);
            this.tabPage1.Controls.Add(this.dateTimePicker1);
            this.tabPage1.Controls.Add(this.dateTimePicker2);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.buttonAddPermitZoneTime);
            this.tabPage1.Controls.Add(this.treeView1);
            this.tabPage1.Controls.Add(this.buttonRemoveNode);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(599, 500);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Time Zones";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.listBox1);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.label9);
            this.tabPage2.Controls.Add(this.textBoxPrefix);
            this.tabPage2.Controls.Add(this.textBoxOffset);
            this.tabPage2.Controls.Add(this.textBoxFAC);
            this.tabPage2.Controls.Add(this.textBoxStartRange);
            this.tabPage2.Controls.Add(this.textBoxEndRange);
            this.tabPage2.Controls.Add(this.buttonAddPermitPrefix);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.label8);
            this.tabPage2.Controls.Add(this.label7);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(599, 500);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Permits";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(9, 48);
            this.listBox1.Name = "listBox1";
            this.listBox1.ScrollAlwaysVisible = true;
            this.listBox1.Size = new System.Drawing.Size(528, 212);
            this.listBox1.Sorted = true;
            this.listBox1.TabIndex = 32;
            this.listBox1.DoubleClick += new System.EventHandler(this.ListBox1_DoubleClick);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.listBoxLots);
            this.tabPage3.Controls.Add(this.textBoxLotname);
            this.tabPage3.Controls.Add(this.buttonAddLot);
            this.tabPage3.Controls.Add(this.buttonRemoveLot);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(599, 500);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Lots";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // checkBoxNoReder
            // 
            this.checkBoxNoReder.AutoSize = true;
            this.checkBoxNoReder.Location = new System.Drawing.Point(10, 0);
            this.checkBoxNoReder.Name = "checkBoxNoReder";
            this.checkBoxNoReder.Size = new System.Drawing.Size(108, 17);
            this.checkBoxNoReder.TabIndex = 34;
            this.checkBoxNoReder.Text = "No Reader Mode";
            this.checkBoxNoReder.UseVisualStyleBackColor = true;
            this.checkBoxNoReder.CheckedChanged += new System.EventHandler(this.CheckBoxNoReder_CheckedChanged);
            // 
            // DisplayAndCountSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBoxNoReder);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.buttonLoadTree);
            this.Controls.Add(this.buttonSaveTree);
            this.Name = "DisplayAndCountSettings";
            this.Size = new System.Drawing.Size(613, 532);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }



        private System.Windows.Forms.ComboBox comboBoxPermitPrefix;
        private System.Windows.Forms.ComboBox comboBoxZone;
        private System.Windows.Forms.DateTimePicker dateTimePicker1;
        private System.Windows.Forms.DateTimePicker dateTimePicker2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button buttonAddPermitZoneTime;
        private System.Windows.Forms.TreeView treeView1;
        private System.Windows.Forms.Button buttonSaveTree;
        private System.Windows.Forms.Button buttonRemoveNode;
        private System.Windows.Forms.TextBox textBoxPrefix;
        private System.Windows.Forms.TextBox textBoxFAC;
        private System.Windows.Forms.TextBox textBoxStartRange;
        private System.Windows.Forms.TextBox textBoxEndRange;
        private System.Windows.Forms.Button buttonAddPermitPrefix;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ListBox listBoxLots;
        private System.Windows.Forms.Button buttonAddLot;
        private System.Windows.Forms.Button buttonRemoveLot;
        private System.Windows.Forms.TextBox textBoxLotname;
        private System.Windows.Forms.TextBox textBoxOffset;
        private System.Windows.Forms.Label label9;
#endregion 
        private void ButtonLoadTree_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog()
            {
                Filter = "XML Files | *.xml",
                FileName = "PermitZones.xml"
            };
            DialogResult dr = of.ShowDialog();
            if (dr == DialogResult.Cancel) return;
            filename = of.FileName;
            LoadDoc();
            PermitZonesDoc = XDocument.Load(filename);
            Properties.Settings.Default["Filename"] = filename;
            LoadSettings();

        }

        private void ListBox1_DoubleClick(object sender, EventArgs e)
        {
            string message = "Delete the selected prefixes?";
            string caption = "Delete Prefix";
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.

            result = MessageBox.Show(this, message, caption, buttons,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button1,
                MessageBoxOptions.RightAlign);

            if (result == DialogResult.Yes)
            {

                foreach (object selected in listBox1.SelectedItems)
                {

                    PermitListboxItem pli = (PermitListboxItem)selected;
                    IEnumerable<XElement> PrefixElements = (from el in PermitZonesDoc.Root.Descendants("PERMITPREFIXES").Descendants("Permit") where el.Attribute("prefix").Value == pli.Prefix select el);
                    while (PrefixElements.Count() > 0)
                    {
                        PrefixElements.First().Remove();
                    }
                }
                LoadPermitPrefixes();
            }
        }

        private void CheckBoxNoReder_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default["NoReaderMode"] = checkBoxNoReder.Checked;
            Properties.Settings.Default.Save();
            MessageBox.Show("You must restart the program for changes to take effect.");
        }
    }
    [Serializable]
    public  class TimeZoneTreeNode : TreeNode
    {
       
        string _start;
        string _end;
        protected TimeZoneTreeNode(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }

        public TimeZoneTreeNode(string startTime, string endTime):base((startTime.Substring(startTime.IndexOf('M') - 1, 1) == "A" ? " " + startTime : startTime) + " to " + endTime)
        {
           
            StartTime = startTime;
            _start = StartTime;
            EndTime = endTime;
            _end = endTime;

        }
        public string EndTime { get; set; }
        public string StartTime { get; set; }
        public new string  Text
        {
            get
            {
                string s = _start.Substring(_start.IndexOf('M') - 1, 1);
                if (s == "A") { return " " + base.Text; }
                else return base.Text;
            }

            set
            { base.Text = value; }
        }



    }
     public class PermitListboxItem
    {
        // <Permit prefix = "17C1" fac="117" start="30001" end="39999" offset="30000" />

       public PermitListboxItem(XElement e1)
        {
            Prefix = e1.Attribute("prefix").Value;
            FAC = e1.Attribute("fac").Value;
            int.TryParse(e1.Attribute("start").Value, out _Start);
            int.TryParse(e1.Attribute("end").Value, out _end);
            int.TryParse(e1.Attribute("offset").Value, out _offset);

        }

        public string Prefix { get; set; }
        public string FAC { get; set; }
        int _Start;
        public int START { get=>_Start; set { _Start = value; } }
        int _end;
        public int END { get=>_end; set { _end = value; } }
        int _offset;
        public int OFFEST { get => _offset; set { _offset = value; } }


        public override string ToString()
        {
            return String.Format("{0}\t\t{1}\t{2}\t{3}\t{4}", Prefix, FAC.PadLeft(30) , START.ToString().PadLeft(15) , END.ToString().PadLeft(10) , OFFEST.ToString().PadLeft(30));
        }
    }
   
}
