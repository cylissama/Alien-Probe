using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AlphaScanCam.Entities.Implement;
using AlphaScanCam.Functions.Implement;
using AlphaScanCam.Entities;
using AlphaScanCam.Functions;


namespace AlphaScanCam
{
    public partial class Form1 : Form , IObserver<RFIDResponse>
    {
        public ObservableIDReader<IDResponse> Reader;
        static public List<TagFunction> TagFunctionList = new List<TagFunction>();
        public Form1()
        {
            InitializeComponent();
            //crete the reader
            Reader = new M6RFIDReader("TEST");

            //functions
            
            TagFunctionList.Add(new BlacklistTagFunction());
           
           
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(RFIDResponse value)
        {
            throw new NotImplementedException();
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
