﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AlphaScan
{
    public partial class AntennaView : UserControl
    {
        public enum AlertState { Normal, Warning, Alert, Inactive }
        private int[] _AssocaitedCams;
        private bool hasCam;
        public AntennaView()
        {
            InitializeComponent();
            SetState(AlertState.Normal);
            
        }
        public void SetActive()
        {
            this.Enabled = true;
            this.label3.Text = "Enabled";
            panelAnt1.BackColor = SystemColors.Control;
        }
        public void TagRead(RFIDTag tag)
        {
            if (tag.PermitString == "") tag.PermitString = tag.FAC.ToString() + tag.Card.ToString();
            label3.Invoke(new Action(() => label3.Text =  tag.PermitString));
            panelAnt1.BackColor = tag.Tagresponse.Color;
            if (HasCam) TakePhoto();
            
        }
        [Category("Reader")]
        [Browsable(true)]
        private string _AntennaID = "Antenna 1";
        public string AntennaID
        {
            get
            {
                return _AntennaID;
            }

            set
            {
                _AntennaID = value;
                label2.Text = _AntennaID;
            }
        }
        public void TakePhoto()
        {
            
        }
        public  override string Text { get => text1; set { text1 = value; label3.Text = text1; } }

        public int[] AssocaitedCams { get => _AssocaitedCams; set { _AssocaitedCams = value; HasCam = _AssocaitedCams.Length > 0; } }
        public bool HasCam { get => hasCam; set => hasCam = value; }

        private string text1;
        public void SetState(AntennaView.AlertState state)
        {
            switch (state)
            {
                case AlertState.Normal:
                    
                    panelAnt1.BackColor = SystemColors.Control;
                    break;
                case AlertState.Warning:
                    panelAnt1.BackColor = Color.Yellow;
                    break;
                case AlertState.Alert:
                    panelAnt1.BackColor = Color.Red;
                    break;
                case AlertState.Inactive:
                    SetState(AlertState.Normal);
                    this.Enabled = false;
                    break;

            }
        }
       
    }
}