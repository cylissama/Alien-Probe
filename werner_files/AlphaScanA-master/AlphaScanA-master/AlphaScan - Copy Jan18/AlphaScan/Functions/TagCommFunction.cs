﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;


namespace AlphaScan.Functions
{
    class TagCommFunction : ITagFunction
    {

        static string _RecordFilePath = "ASTagsTemp.tmp";
        public string RecordFilePath { get => _RecordFilePath; set => _RecordFilePath = value; }
        static string _PhotoFilePath = "ASPhotoTemp.tmp";
        public string PhotoFilePath { get => _PhotoFilePath; set => _PhotoFilePath = value; }
        BackgroundWorker Fileworker = new BackgroundWorker();
        BackgroundWorker Commworker = new BackgroundWorker();
        BackgroundWorker PhotoFileworker = new BackgroundWorker();
        BackgroundWorker PhotoCommworker = new BackgroundWorker();
        private static List<RFIDTag> _TagsToSend;
        private static List<TagPhoto> _PhotosToSend;
        private int CommQueLength = 3;
        private int CommMaxListSize = 10;
        public static List<RFIDTag> TagsToSend { get => _TagsToSend; set => _TagsToSend = value; }
        public PimServiceRef.PIMServiceClient PIM = new PimServiceRef.PIMServiceClient();
        
        /* 
Look to see if there is a backup file (recorded)
Load any tags in the backup file into the que file.
Transmit 1st tag, wait for response.
If response, remove from que and delete temp file. 
If no respomse, dump queue to temp file, new tags added to que and file.


*/

        public TagCommFunction()
        {
            //start the background threads
            InitializeBackgroundWorkers();
            if (File.Exists(_RecordFilePath)) // there's a file out there so load it into the comm que.

            {
                TagsToSend = DeSerializeObject<List<RFIDTag>>(_RecordFilePath);

                if (TagsToSend.Count > 0) Commworker.RunWorkerAsync();
            }
            else TagsToSend = new List<RFIDTag>();
            if (File.Exists(_PhotoFilePath)) // there's a file out there so load it into the comm que.

            {
                _PhotosToSend = DeSerializeObject<List<TagPhoto>>(_PhotoFilePath);

                if (_PhotosToSend.Count > 0) PhotoCommworker.RunWorkerAsync();
            }
            else _PhotosToSend = new List<TagPhoto>();

        }



        void ITagFunction.AddMenuItem(MenuStrip menustrip1)
        {
            throw new NotImplementedException();
        }

        TagFunctionResponse ITagFunction.CheckTag(RFIDTag TagData)
        {

            TagsToSend.Add(TagData);

            if (!Commworker.IsBusy) Commworker.RunWorkerAsync();
            if (TagData.ImageList.Count > 0)
            {
                foreach (Bitmap b in TagData.ImageList)
                {
                    _PhotosToSend.Add(new TagPhoto(b, TagData));
                }
                if (!PhotoCommworker.IsBusy) PhotoCommworker.RunWorkerAsync();
            }

            return TagData.Tagresponse;
        }

        Dictionary<object, object> ITagFunction.GetAppSettings()
        {
            throw new NotImplementedException();
        }

        UserControl ITagFunction.GetControl()
        {
            return new BlankUserControl();
        }

        string ITagFunction.GetDisplayName()
        {
            return "Comms";
        }

        UserControl ITagFunction.GetSettingsControl()
        {
            return new BlankUserControl();
        }
        private void InitializeBackgroundWorkers()
        {
            Commworker.DoWork += new DoWorkEventHandler(Commworker_DoWork);
            Commworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Commworker_WorkCompleted);
            Commworker.ProgressChanged += new ProgressChangedEventHandler(Commworker_ProgressChanged);
            Fileworker.DoWork += new DoWorkEventHandler(Fileworker_DoWork);
            Fileworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Fileworker_WorkCompleted);
            Fileworker.ProgressChanged += new ProgressChangedEventHandler(Fileworker_ProgressChanged);
            PhotoCommworker.DoWork += new DoWorkEventHandler(PhotoCommworker_DoWork);
            PhotoCommworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PhotoCommworker_WorkCompleted);
            PhotoCommworker.ProgressChanged += new ProgressChangedEventHandler(PhotoCommworker_ProgressChanged);
            PhotoFileworker.DoWork += new DoWorkEventHandler(PhotoFileworker_DoWork);
            PhotoFileworker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PhotoFileworker_WorkCompleted);
            PhotoFileworker.ProgressChanged += new ProgressChangedEventHandler(PhotoFileworker_ProgressChanged);
        }

        private void PhotoFileworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PhotoFileworker_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PhotoFileworker_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        private void PhotoCommworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
           
        }

        private void PhotoCommworker_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
           
        }

        private void PhotoCommworker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            //check if there are tags waiting to be written FROM the filelist.

            if (File.Exists(_PhotoFilePath)) { ReadInPhotos(); }
            //while (TagsToSend.Count > 0)
            //  {
            foreach (TagPhoto tPhoto in _PhotosToSend)
            {

                string res = "";
                PimServiceRef.RFIDTag t = GetServiceTag(tPhoto.Tag);
                try
                {
                    res = PIM.AddPhoto(t, tPhoto.Photo, t.LastDetect);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                if (res == "1")
                {

                    
                    _PhotosToSend.Remove(tPhoto);

                }
            }
       

        //}


        e.Result = 1;

        }


        private void Commworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private void Commworker_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
           
                if (!Fileworker.IsBusy) Fileworker.RunWorkerAsync();
            
                
        }

        private void Commworker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            //check if there are tags waiting to be written FROM the filelist.

            if (File.Exists(_RecordFilePath)) { ReadInFiles(); }
            //while (TagsToSend.Count > 0)
          //  {
                foreach (RFIDTag tag in TagsToSend.ToList<RFIDTag>())
                {

                    if (!tag.isWritten)
                    {
                        string result = "";
                        int res = 0;
                        try
                        {
                        PimServiceRef.RFIDTag t = GetServiceTag(tag);                      




                            // t.LastDetect = DateTime.Now;




                            result = PIM.AddRFIDData(t);
                            int.TryParse(result, out res) ;
                    }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        if (res == 1)
                        {

                            tag.isWritten = true;

                            TagsToSend.Remove(tag);

                        }
                    }
                }
                
            //}


            e.Result = 1;
            
        }
        private void ReadInFiles()
        {
            HashSet<RFIDTag> hs = new HashSet<RFIDTag>(DeSerializeObject<List<RFIDTag>>(_RecordFilePath));
           hs.UnionWith(TagsToSend);
            TagsToSend = hs.ToList<RFIDTag>();
           

        }
        private void ReadInPhotos()
        {
            HashSet<TagPhoto> hstp = new HashSet<TagPhoto>(DeSerializeObject<List<TagPhoto>>(_PhotoFilePath));
            hstp.UnionWith(_PhotosToSend);
            _PhotosToSend = hstp.ToList<TagPhoto>();
        }

        private static void Fileworker_DoWork(object sender, DoWorkEventArgs e)
        {
          if (TagsToSend.Count > 0)
            {
                if (File.Exists(_RecordFilePath)) File.Delete(_RecordFilePath);
                SerializeObject(TagsToSend, _RecordFilePath);

            }
           else if (File.Exists(_RecordFilePath)) File.Delete(_RecordFilePath);
         
        }

        private void Fileworker_WorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }
public PimServiceRef.RFIDTag GetServiceTag(RFIDTag tag)
        {
            PimServiceRef.RFIDTag t = new PimServiceRef.RFIDTag()
            {
                Antenna = tag.Antenna ?? "1",
                Card = tag.Card,
                FAC = tag.FAC,
                GPSData = tag.GPSData ?? "1",
                LastDetect = tag.LastDetect < DateTime.Parse("1/1/2017") ? DateTime.Now : tag.LastDetect,
                Lot = tag.Lot ?? "1",
                permitString = tag.PermitString ?? "1",
                prefix = tag.prefix ?? "1",
                Range = tag.Range,
                RawHex = tag.RawHex ?? "1",
                TagHex = tag.TagHex ?? "1",
                Zone = tag.Zone ?? "1",
                Response = new PimServiceRef.TagFunctionResponse()
            };
            t.Response.Color = tag.Tagresponse.Color;
            t.Response.IsIgnored = tag.Tagresponse.IsIgnored;
            t.Response.Handled = tag.Tagresponse.Handled;
            t.Response.IsValid = tag.Tagresponse.IsValid;
            t.Response.Message = tag.Tagresponse.Message;
            t.Response.Response = tag.Tagresponse.Response;

            return t;



        }
        private void Fileworker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
        public static void SerializeObject<T>(T serializableObject, string fileName)
        {
            if (serializableObject == null) { return; }

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                using (MemoryStream stream = new MemoryStream())
                {
                    serializer.Serialize(stream, serializableObject);
                    stream.Position = 0;
                    xmlDocument.Load(stream);
                    xmlDocument.Save(fileName);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                //Log exception here
            }
        }


        /// <summary>
        /// Deserializes an xml file into an object list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public T DeSerializeObject<T>(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default(T); }

            T objectOut = default(T);

            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(fileName);
                string xmlString = xmlDocument.OuterXml;

                using (StringReader read = new StringReader(xmlString))
                {
                    Type outType = typeof(T);

                    XmlSerializer serializer = new XmlSerializer(outType);
                    using (XmlReader reader = new XmlTextReader(read))
                    {
                        objectOut = (T)serializer.Deserialize(reader);
                        reader.Close();
                    }

                    read.Close();
                }
            }
            catch (Exception ex)
            {
                //Log exception here
            }

            return objectOut;
        }
    }
    public class TagPhoto 
    {
        Bitmap photo;
        string tagID;
        RFIDTag tag;
        public TagPhoto() { }
        public TagPhoto(Bitmap bitmap, RFIDTag rfidtag)
        {
            Photo = bitmap;
            TagID = rfidtag.RawHex;
            tag = rfidtag;
           
        }

        public Bitmap Photo { get => photo; set => photo = value; }
        public RFIDTag Tag { get => tag; set => tag = value; }
        public string TagID { get => tagID; set => tagID = value; }
    }
}