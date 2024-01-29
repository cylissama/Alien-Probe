using System;
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
    public partial class RecordTagsControl : UserControl
    {
        private RecordTagsFunction ParentFunction;
        public RecordTagsControl( RecordTagsFunction parentFunction)
        {
            InitializeComponent();
            ParentFunction = parentFunction;
            ParentFunction.Recording = false;

        }
       

        private void Button1_Click(object sender, EventArgs e)
        {
            if (!ParentFunction.Recording)
            {
                ParentFunction.StartRecording();



            }

            else
            {

                SaveFileDialog sfd = new SaveFileDialog()
                {
                    CreatePrompt = true
                };
                sfd.ShowDialog();
                ParentFunction.RecordFilePath = sfd.FileName;
                ParentFunction.StopRecording();

            }

            labelRecording.Text = (ParentFunction.Recording) ? "Recording" : "";
            button1.Text = ParentFunction.Recording ? "Stop" : "Record";
        }
    }
   
}
