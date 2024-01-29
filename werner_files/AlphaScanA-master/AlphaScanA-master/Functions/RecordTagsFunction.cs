using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Collections.Concurrent;

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
            PermitsToSave.Enqueue(TagData);

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
        public ConcurrentQueue<RFIDTag> PermitsToSave { get => _PermitsScanned; set => _PermitsScanned = value; }
        public List<RFIDTag> ScannedTags1 { get => ScannedTags; set => ScannedTags = value; }
        public string RecordFilePath { get => _RecordFilePath; set => _RecordFilePath = value; }

        private ConcurrentQueue<RFIDTag> _PermitsScanned = new ConcurrentQueue<RFIDTag>();
        private List<RFIDTag> ScannedTags = new List<RFIDTag>();
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
           List<RFIDTag> TagsToRecord = new List<RFIDTag>();
            RFIDTag tag;
            while(PermitsToSave.TryDequeue(out tag))
            {
                
                if (!ScannedTags.Contains(tag))
                {

                    ScannedTags.Add(tag);
                    TagsToRecord.Add(tag);
                }
                else
                {
                    RFIDTag old = ScannedTags.Find(c=>c.RawHex == tag.RawHex);
                    if (DateTime.Parse(tag.LastSeenTime) > DateTime.Parse(old.LastSeenTime).AddMinutes(5))
                    {
                        TagsToRecord.Add(tag);

                    }
                }


            }

               // ScannedTags.Sort();
                using (StreamWriter sw = File.AppendText(_TempFileString))
                {
                    string s;
                    foreach (RFIDTag Tag in TagsToRecord)
                    {
                        s = Tag.PermitString + "," + Tag.RawHex + "," +Tag.Lot + "," + Tag.Zone + "," + Tag.GPSData + "," +Tag.DiscoveryTime.ToString()+","+ Tag.LastSeenTime.ToString();
                        sw.WriteLine(s);
                    }
                TagsToRecord.Clear();
                    
                    
                }
            
           
        }
        public void StopRecording()
        {
            if(File.Exists(_TempFileString)) File.Move(_TempFileString, RecordFilePath);
            
         //   String[] lines = File.ReadAllLines(_TempFileString);
         //   Array.Sort(lines);
         //   File.WriteAllLines(RecordFilePath, lines);
           
         //   File.Delete(_TempFileString);
            _Recording = false;
            ScannedTags.Clear();
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
