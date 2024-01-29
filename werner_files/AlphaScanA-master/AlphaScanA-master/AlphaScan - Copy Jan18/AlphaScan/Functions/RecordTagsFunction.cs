using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;

namespace AlphaScan
{
   public class RecordTagsFunction : ITagFunction
    {
        BackgroundWorker worker;
        RecordTagsControl _RecordTagsControl;

        string _RecordFilePath;
        string _TempFileString;
        public RecordTagsFunction()
        {
            _RecordTagsControl = new RecordTagsControl(this);

            worker = new BackgroundWorker();
            InitializeBackgroundWorker();


        }
        public void AddMenuItem(MenuStrip menustrip1)
        {
            throw new NotImplementedException();
        }

        public TagFunctionResponse CheckTag(RFIDTag TagData)
        {
            if(!Recording) return TagData.Tagresponse;
            PermitsToSave.Enqueue(TagData.PermitString);

            if (!worker.IsBusy) worker.RunWorkerAsync();
            return TagData.Tagresponse;
        }

        public Dictionary<object, object> GetAppSettings()
        {
            throw new NotImplementedException();
        }

        public UserControl GetControl()
        {
            return _RecordTagsControl;
        }

        public string GetDisplayName()
        {
            return "Tag Recorder";
        }

        public UserControl GetSettingsControl()
        {
            return new ReaderSettingsControl();
        }
        public Queue<string> PermitsToSave { get => _PermitsScanned; set => _PermitsScanned = value; }
        public List<string> ScannedTags1 { get => ScannedTags; set => ScannedTags = value; }
        public string RecordFilePath { get => _RecordFilePath; set => _RecordFilePath = value; }

        private Queue<string> _PermitsScanned = new Queue<string>();
        private List<string> ScannedTags = new List<string>();
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (PermitsToSave.Count > 0)
            {
                while (PermitsToSave.Count > 0)

                {
                    string tag = PermitsToSave.Dequeue();
                    if (!ScannedTags.Contains(tag)) ScannedTags.Add(tag);

                }

                ScannedTags.Sort();
                using (StreamWriter sw = File.CreateText(_TempFileString))
                {
                    foreach (string s in ScannedTags) sw.WriteLine(s);
                }
            }
           
        }
        public void StopRecording()
        {
            String[] lines = File.ReadAllLines(_TempFileString);
            Array.Sort(lines);
            using (StreamWriter sw = File.CreateText(RecordFilePath))
            {
                foreach (string s in lines) sw.WriteLine(s);
                sw.Flush();
                Recording = false;
            }
            File.Delete(_TempFileString);
        }
        private void Worker_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            

        }
        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }
        private void InitializeBackgroundWorker()
        {
            worker.DoWork +=  new DoWorkEventHandler(Worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_WorkCompleted);
            worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
        }
        private bool _Recording;

        public bool Recording
        {
            get => _Recording;
            set => _Recording = value;
               
            

        }
        public void StartRecording()
        {
            _TempFileString = DateTime.Now.ToFileTime().ToString() + "AS.txt";
            Recording = true;

        }
    }
}
