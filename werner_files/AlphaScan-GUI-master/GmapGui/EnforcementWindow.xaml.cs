using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using ParkDS;
using System.IO;
using Microsoft.Win32;
using CsvHelper;
using System.Globalization;
using CES.AlphaScan.Acquisition;


namespace GmapGui
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>

    public partial class EnforcementWindow : Window
    {
        private readonly string defaultMapDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps");

        private MapManager mapManager;
        private PermManager permManager;
        private double thinLine = 3;
        private bool adminOn;
        List<string> lats
                    = new List<string>();
        List<string> longs
                    = new List<string>();
        List<PointLatLng> LatLngs
                    = new List<PointLatLng>();
        List<string> tags
                    = new List<string>();
        List<string> timeString
                    = new List<string>();
        List<DateTime> times
                    = new List<DateTime>();

        List<PointLatLng> MissedLatLngs
                   = new List<PointLatLng>();
        List<Tag> MissedTags
                    = new List<Tag>();

        List<PointLatLng> PointsInArea
                    = new List<PointLatLng>();

        List<Tag> AllTags
                    = new List<Tag>();

        List<PointLatLng> AllCoords
                    = new List<PointLatLng>();
        List<string> BlacklistedTags
                    = new List<string>();

        private double thickLine = 3;
        public EnforcementWindow(bool adminMode)
        {
            InitializeComponent();

            try
            {
                System.Net.IPHostEntry e =
                     System.Net.Dns.GetHostEntry("www.google.com");
            }
            catch
            {
                MainMap.Manager.Mode = AccessMode.CacheOnly;
                MessageBox.Show("No internet connection avaible, going to CacheOnly mode.",
                      "GMap.NET - Demo.WindowsForms");
            }

            adminOn = adminMode;
            mapManager = new MapManager(new ParkZone());
            permManager = new PermManager();

            //load in map data
            MainMap.MapProvider = GMapProviders.GoogleMap;
            MainMap.Position = new PointLatLng(36.9835, -86.4574);
            MainMap.MinZoom = 12;
            MainMap.MaxZoom = 24;
            MainMap.Zoom = 18;

            // get map settings from the map config file
            SettingsData settings = new SettingsData();
            IDictionary<string, string> mapConfig = settings.MapSettings;
            try
            {
                // get default permission file and load it
                string loadperm = mapConfig["Default Perm"];
                try
                {
                    permManager.LoadPerm(loadperm);
                }
                catch { MessageBox.Show("Make sure the Default Permission setting in the Map Configuration is linked to a permission file and is not blank"); }
                // get default map file and load it
                string loadmap = mapConfig["Default Map"];
                try
                {
                    mapManager.LoadMap(loadmap);
                }
                catch { MessageBox.Show("Make sure the Default Map setting in the Map Configuration is linked to a permission file and it not blank"); }
                paintLoadedMap(mapManager.Campus);
                mapManager.Avail_perms = permManager;
            }
            catch
            {
                MessageBox.Show("Please verify that a default map file and permission file are selected.");
            }
            populateBlacklist();
            MessageBox.Show("Please select data to process.");
            if (!LocalizeTags_Click())
            {
                cancel_Enforcement();
                return;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainMap.Zoom = slr_Zoom.Value + 12;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //Replacement for the (kinda bad) zoom function built into gmaps. Lets the user scroll on polygons and moves the slider in response
        //FOR SOME REASON this doesnt work how it should in this new window, scroll bar doesnt move with wheel... INVESTIGATE LATER AND COMMENT WHEN THATS DONE - 9/14/2020 LANDON OWENS

        private void MainMap_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                slr_Zoom.Value += 1;
            }
            if (e.Delta < 0)
            {
                slr_Zoom.Value -= 1;
            }
        }
        //------------------------
        //Returns to the main menu

        private void exit_Enforcement_Click(object sender, RoutedEventArgs e)
        {
            alphaScanMainMenu open = new alphaScanMainMenu(adminOn);
            open.Show();
            this.Close();
            
        }
        //------------------------
        //Returns to the main menu because of an error
        private void cancel_Enforcement()
        {
            alphaScanMainMenu open = new alphaScanMainMenu(adminOn);
            this.Close();

        }
        //---------------------------------------------------------------------------------------------------------------------------------------------
        //This does the bulk of significant stuff for this window. A lot of this logic should be reused for drawing centroids on valid or invalid spots.  
        /// <summary>
        /// Populates the Tree View with lots and high level information.
        /// </summary>
        /// <param name="parent"></param>
        private void populateLotData(ParkZone parent)
        {
            int issueCount = 0;
            int spotsFilled = 0;
            int totalSpots = 0;

            int lotTag = 0;
            int areaTag = 0; //not sure if i will use this

            foreach(ParkZone p in parent.Subareas)
            {
                issueCount = 0;
                spotsFilled = 0;
                totalSpots = 0;

                string tempText = p.Name;

                TreeViewItem tempTV = new TreeViewItem();

                tempTV.Header = tempText;
                tempTV.Tag = lotTag++;
                tempTV.MouseDoubleClick += TempTV_MouseDoubleClick;

                trv_LotList.Items.Add(tempTV);

                foreach (ParkZone a in p.Subareas)
                {

                    int areaIssues = 0;
                    int areaOccupency = 0;
                    int areaSpotsFilled = 0;

                    string tempAreaName = a.Name;
                    TreeViewItem tempTV2 = new TreeViewItem();

                    tempTV2.Header = tempAreaName;
                    tempTV.Items.Add(tempTV2);

                    foreach(ParkZone s in a.Subareas)
                    {
                        areaOccupency++;
                        totalSpots++;
                        if (checkParkedVehicle(s) != "Empty")
                        {
                            spotsFilled++;
                            areaSpotsFilled++;
                        }
                        if (checkParkedVehicle(s) == "Invalid")
                        {
                            issueCount++;
                            areaIssues++;                           
                        }
                    }
                    tempTV2.Header = tempTV2.Header + "  (" + areaSpotsFilled + "/" + areaOccupency + ")";
                    if(areaIssues > 0)
                    tempTV2.Header = tempTV2.Header + "(" + areaIssues + "🏳)";                    
                }

                tempTV.Header = tempTV.Header + "  (" + spotsFilled + "/" + totalSpots + ")";
                if(issueCount > 0)
                tempTV.Header = tempTV.Header + "(" + issueCount + "🏳)";               
            }
        }
        //----------------------------------------------------------------------------------------------------------------------------------------------------------
        //When double clicking, paint the loaded map data for the selected lot or area. Also updates the summary in the bottom left to show all info on lot selected.
        /// <summary>
        /// Functionality for lot selection. Clicking sets desired lot to view and give more detailed info.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TempTV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {            

            MainMap.Markers.Clear();
            paintLoadedMap(mapManager.Campus);
            TreeViewItem temp = (TreeViewItem)sender;
            
            ParkZone tempZone = mapManager.SelectArea(0, (int)temp.Tag);
            paintLoadedMap(tempZone);
            foreach (ParkZone pz in tempZone.Subareas)
                paintLoadedMap(pz);
            double[] center = tempZone.Centroid;
            PointLatLng centroid = new PointLatLng();
            
            centroid.Lat = center[0];
            centroid.Lng = center[1];

            MainMap.Position = centroid;
            
            foreach(ParkZone area in tempZone.Subareas)
            {
                string name = "empty";

                foreach(ParkZone spot in area.Subareas)
                {
                    if (spot.TagWithin != null)
                        name = spot.TagWithin.ID;
                    drawOccupiedSpace(checkParkedVehicle(spot), spot.Centroid,name);
                }
            }

            updateSummary((int)temp.Tag);
        }

        //This is how the summary is updated.
        /// <summary>
        /// Fills the summary box with details. Counts issues, blacklisted tags, valid occupied spaces, etc.
        /// </summary>
        /// <param name="tag"></param>
        private void updateSummary(int tag)
        {
            int occupancy = 0;
            int occupied = 0;
            int valid = 0;
            int invalid = 0;
            int blacklist = 0;
            string status = "Empty";
            ParkZone temp = mapManager.SelectArea(0, tag);
            txb_lotName.Text = "Selected Lot: " + temp.Name;

            foreach (ParkZone p in temp.Subareas)
            {
                foreach(ParkZone s in p.Subareas)
                {
                    status = "Empty";

                    if (s.TagWithin != null)
                    {
                       status = checkParkedVehicle(s);
                    }

                    occupancy++;
                    if (status == "Empty")
                        continue;
                    if (status == "Valid")
                    {
                        occupied++;
                        valid++;
                    }
                    if(status=="Invalid")
                    {
                        occupied++;
                        invalid++;
                    }
                    if(status == "Blacklisted")
                    {
                        occupied++;
                        blacklist++;
                    }
                }
            }

            txb_blacklistedParks.Text = "Blacklisted: " + blacklist;
            txb_invalidParks.Text = "Invalid: " + invalid;
            txb_maxOccupancy.Text = "Maximum Occupancy: " + occupancy;
            txb_spotsOccupied.Text = "Spots Occupied: " + occupied;
            txb_validParks.Text = "Valid: " + valid;

        }

        //This marks the spaces according to what they were classified as.
        /// <summary>
        /// This checks what the status of the tag within a spot is and paints a colored dot to indicate status.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="spotCoord"></param>
        /// <param name="identity"></param>
        private void drawOccupiedSpace(string status, double[] spotCoord,string identity)
        {
            PointLatLng drawHere = new PointLatLng(spotCoord[0],spotCoord[1]);

            if (status == "Empty") 
                return;

            GMapMarker marker = new GMapMarker(drawHere);
            if (status == "Blacklisted")
            {
                marker.Shape = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    StrokeThickness = 1.5,
                    ToolTip = ("This car is blacklisted: " + identity),
                    Visibility = Visibility.Visible,
                    Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    Data = new EllipseGeometry
                    {
                        RadiusX = 5,
                        RadiusY = 5,
                    },
                };
            }          
            if (status == "Valid")
            {
                marker.Shape = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    StrokeThickness = 1.5,
                    ToolTip = ("This space is occupied: " + identity),
                    Visibility = Visibility.Visible,
                    Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                    Data = new EllipseGeometry
                    {
                        RadiusX = 5,
                        RadiusY = 5,
                    },
                };
            }
            if(status == "Invalid")
            {
                marker.Shape = new System.Windows.Shapes.Path
                {
                    Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                    StrokeThickness = 1.5,
                    
                    ToolTip = (identity is "None" ? "This car does not have a tag" : "This car does not have permission: " + identity),

                    //ToolTip = ("This car does not have permission: " + identity),
                    Visibility = Visibility.Visible,
                    Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
                    Data = new EllipseGeometry
                    {
                        RadiusX = 5,
                        RadiusY = 5,

                    },
                };
            }
            MainMap.Markers.Add(marker);
        }

        //Loads in the visuals for whatever parkzone
        /// <summary>
        /// Draws out all subareas of a parkzone.
        /// </summary>
        /// <param name="parent"></param>
        private void paintLoadedMap(ParkZone parent)
        {
            List<PointLatLng> vertsTemp = new List<PointLatLng>();
            PointLatLng tempPoint = new PointLatLng(0, 0);

            int j = 1;
            int length;

            try
            {
                foreach (ParkZone p in parent.Subareas)
                {

                    length = p.Vertices.Length;
                    for (int i = 0; i < length; i += 2)
                    {
                        tempPoint.Lat = p.Vertices[i];
                        tempPoint.Lng = p.Vertices[i + 1];
                        vertsTemp.Add(tempPoint);
                    }

                    GMapPolygon polygon = new GMapPolygon(vertsTemp);

                    polygon.RegenerateShape(MainMap);
                    (polygon.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Red;
                    (polygon.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                    (polygon.Shape as System.Windows.Shapes.Path).StrokeThickness = thinLine;
                    (polygon.Shape as System.Windows.Shapes.Path).Effect = null;
                    (polygon.Shape as System.Windows.Shapes.Path).ToolTip = p.Name;
                    //To add polygon in gmap

                    MainMap.Markers.Add(polygon);
                    vertsTemp.Clear();
                }
                j++;

            }

            catch
            {

            }
        }

        //Returns a string indicating the status of a lot. Used everywhere. 
        /// <summary>
        /// Returns a string with status of a desired spot. Returned strings are "Blacklisted", "Valid", "Invalid", and "Empty"
        /// </summary>
        /// <param name="thisSpot"></param>
        /// <returns></returns>
        private string checkParkedVehicle(ParkZone thisSpot)
        {
            foreach (string s in BlacklistedTags)
              {
                if (thisSpot.TagWithin != null)
                  {
                    if (thisSpot.TagWithin.ID == s)
                    {
                        // BlacklistedTags.Remove(s);
                        return "Blacklisted";
                    }
                }
            }
            
            if (thisSpot.TagWithin == null)            
                return "Empty";
            
            if (thisSpot.TagWithin.Valid)            
                return "Valid";                       
            else           
                return "Invalid";            
        }

        //THIS IS FOR TESTING ONLY, randomly generates statuses for each and every parking spot. 
        /// <summary>
        /// Test function that randomly assigns a status to each spot.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BUTTON_Click(object sender, RoutedEventArgs e)
        {
            Random rnd = new Random();
            foreach (ParkZone p in mapManager.Campus.Subareas)
            {
                foreach (ParkZone a in p.Subareas)
                {
                    foreach (ParkZone s in a.Subareas)
                    {
                        int tag = rnd.Next(1, 4);

                        if(tag == 1)
                            continue;
                        if(tag == 2)
                        {
                            s.TagWithin = new Tag();
                            s.TagWithin.Valid = true;
                        }
                        else
                        {
                            s.TagWithin = new Tag();
                            s.TagWithin.Valid = false;
                        }
                    }
                }

            }
         //   mapManager.SaveMap("TestFile.xml");
        }
        /// <summary>
        /// Allows map selection to be changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mapView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem temp = (MenuItem)sender;
            if (temp.Name == "mapView_Sat")
            {
                MainMap.MapProvider = GMapProviders.BingSatelliteMap;
            }
            else
            {
                MainMap.MapProvider = GMapProviders.GoogleMap;
            }
        }

        //below is the code for localizing tags. it will attempt to localize each tag to a spot and for all tags that fail, find the NEAREST spot.
        //UPDATE: For the most part this seems to be completely functional and is likely to  only need a few tweaks before completion.
        /// <summary>
        /// Iterates through each tag in the CSV file and finds the most appropriate parking spot to assign it to. 
        /// </summary>
        /// <returns></returns>
        private bool LocalizeTags_Click()
        {
            // test variable, remove later
            List<double> weights = new List<double>();

            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "CSV Files (*.csv)|*.csv*";
            openFileDialog.Title = "Select data to load";

            if (openFileDialog.ShowDialog() != true)
            {
                return false;
            }
            else
                try { using (var reader1 = new StreamReader(openFileDialog.FileName))
                        {//This is just to ensure that the app wont crash due to the desired CSV file being open already.
                    }
                
                }
                catch
                {
                    MessageBox.Show("Failed to open file, please make sure the file is not open in another location.");
                    LocalizeTags_Click();
                    return false;
                }

            using (var reader = new StreamReader(openFileDialog.FileName))
            {
                reader.ReadLine();
                int latCol = 0;
                int lngCol = 1;
                int tagCol = 6;
                int timeCol = 2;
                String[] splitDirectory = openFileDialog.FileName.Split('\\');
                if (splitDirectory[splitDirectory.Length - 1] == "GPS.csv")
                {
                    latCol = 1;
                    lngCol = 2;
                    tagCol = 7;
                    timeCol = 3;
                }

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    lats.Add(values[latCol]);
                    longs.Add(values[lngCol]);

                    PointLatLng coord = new PointLatLng(Double.Parse(values[latCol]), Double.Parse(values[lngCol]));

                    GMapMarker marker = new GMapMarker(coord);
                    //Comparing data read to localization results (In => Out)
                    marker.Shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                        StrokeThickness = 1.5,
                        ToolTip = ("Original Coordinate"),
                        Visibility = Visibility.Visible,
                        Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                        Data = new EllipseGeometry
                        {
                            RadiusX = 5,
                            RadiusY = 5,
                        },
                    };

                    MainMap.Markers.Add(marker);

                    int place = -1;
                    DateTime next = new DateTime(long.Parse(values[timeCol]));
                    if (times.Count > 0 && next.CompareTo(times[times.Count-1]) < 0)
                    {
                        place = times.Count - 2;
                        times.Add(times[times.Count - 1]);
                        times[times.Count - 2] = times[times.Count - 3];
                        while(next.CompareTo(times[place]) < 0)
                        {
                            times[place+1] = times[place];
                            place--;
                        }
                        times[++place] = next;
                    }
                    else
                        times.Add(next);
                        
                    string thisTag = values[tagCol];
                                                
                    var charsToRemove = new string[] { " " };
                    thisTag = thisTag.Replace(" ", string.Empty);
                    string nextTag;
                    if (thisTag != "None")
                    {
                        var lastFive = thisTag.Substring(thisTag.Length - 5);
                        nextTag = lastFive;
                    }
                    else
                        nextTag = "None";
                    if (place >= 0)
                    {
                        tags.Add(tags[tags.Count - 1]);
                        for (int i = tags.Count - 2; i > place; i--)
                        {
                            tags[i] = tags[i-1];
                        }
                        tags[place] = nextTag;
                    }
                    else
                        tags.Add(nextTag);

                    try
                    {
                        // use values 1 and 2 for GPS files, 0 and 1 for combined objects
                        PointLatLng temp = new PointLatLng();
                        temp.Lat = Double.Parse(values[latCol]);
                        temp.Lng = Double.Parse(values[lngCol]);
                        LatLngs.Add(temp);
                        if (place >= 0)
                        {
                            LatLngs.Add(LatLngs[LatLngs.Count - 1]);
                            for (int i = LatLngs.Count - 2; i > place; i--)
                            {
                                LatLngs[i] = LatLngs[i - 1];
                            }
                            LatLngs[place] = temp;
                        }
                        else
                            LatLngs.Add(temp);
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            //Works like a foreach loop but is iterating three lists simulatneously
            using (var GPSPoints = LatLngs.GetEnumerator())
            using (var TagsListed = tags.GetEnumerator())
            using (var TimesListed = times.GetEnumerator())
            {
                while (GPSPoints.MoveNext() && TagsListed.MoveNext() && TimesListed.MoveNext())
                {
                    var location = GPSPoints.Current;
                    var tag = TagsListed.Current;
                    var time = TimesListed.Current;

                    double[] point = new double[2];
                    point[0] = location.Lat;
                    point[1] = location.Lng;

                    Tag temp = new Tag(tag, false, time);
                    //Any tag within a spot gets localized, otherwise it is added to a list of tags that had no corresponding space
                    if (!mapManager.LocalizeTag(point, temp))
                    {
                        MissedTags.Add(temp);
                        MissedLatLngs.Add(location);
                        weights.Add(1);
                    }
                    AllTags.Add(temp);
                    AllCoords.Add(location);
                }
            }


            // goes through all coordiantes and sees distance, if within a treshold, remove the points
            int pIdx = 0;
            // using while due to original size of MissedLatLngs varying
            while (true)
            {
                // if at the count exit loop
                if (pIdx == MissedLatLngs.Count)
                    break;

                // iterate through all points ahead of the current point
                for (int j = pIdx + 1; j < MissedLatLngs.Count; j++)
                {
                    // find distance between the two coordiantes
                    double deltaLat = MissedLatLngs[j].Lat - MissedLatLngs[pIdx].Lat;
                    double deltaLong = MissedLatLngs[j].Lng - MissedLatLngs[pIdx].Lng;

                    double dLat2 = Math.Pow(deltaLat, 2);
                    double dLng2 = Math.Pow(deltaLong, 2);

                    double distance = Math.Sqrt(dLat2 + dLng2);
                    // this is the distance threshold check
                    if (distance < 2 * Math.Pow(10, -5))
                    {
                       if (MissedTags[pIdx].ID != "None" && MissedTags[j].ID != "None")
                       {
                            continue;
                       }
                        else if (MissedTags[pIdx].ID != "None" || MissedTags[j].ID != "None")
                        {
                            MissedTags[pIdx] = (MissedTags[pIdx].ID != "None" ? MissedTags[pIdx] : MissedTags[j]);
                        }
                        MissedTags.RemoveAt(j);

                        MissedLatLngs[pIdx] = new PointLatLng(MissedLatLngs[pIdx].Lat * weights[pIdx] / (weights[pIdx] + weights[j]) + MissedLatLngs[j].Lat * weights[j] / (weights[pIdx] + weights[j]),
                            MissedLatLngs[pIdx].Lng * weights[pIdx] / (weights[pIdx] + weights[j]) + MissedLatLngs[j].Lng * weights[j] / (weights[pIdx] + weights[j]));
                        MissedLatLngs.RemoveAt(j);

                        weights[pIdx] += weights[j];
                        weights.RemoveAt(j);

                        pIdx = 0;
                        //--pIdx;
                        break;
                    }
                }
                ++pIdx;
            }

            //Once all tags that were within a spot perfectly are assigned, the list of orphan tags is iterated to find which space is nearest to it. This should help correct some hardware innacuracies.
            //NOTE: Later add a parameter of tolerance to ask "How far from the nearest space is TOO far?"
            using (var Points = MissedLatLngs.GetEnumerator())
            using (var Tags = MissedTags.GetEnumerator())
            {
                VectorToSpace Closest = new VectorToSpace(1000, null);
                while (Tags.MoveNext() && Points.MoveNext())
                {
                    var point = Points.Current;
                    var tag = Tags.Current;

                    var pointTag = tag;

                    double[] Coord = new double[2];
                    Coord[0] = point.Lat;
                    Coord[1] = point.Lng;
                    //reset the value on each run.
                    Closest.Distance = 0.0001;
                    Closest.Spot = null;

                    ParkZone temp = mapManager.LocalizePointToArea(Coord);
                    if(temp!=null)
                    {

                        foreach (ParkZone s in temp.Subareas)
                        {
                            if (s.TagWithin != null && s.TagWithin.ID != "None" && tag.ID != "None")
                                continue;

                            //Skip any space that already has a tag within it.
                            if (tag.ID == "None" || (s.TagWithin == null || s.TagWithin.ID == "None"))
                            {
                                //This checks the distance from the tag to every single existing spot WITHIN the sub area the tag was determined to be in,
                                //at the end the spot with the shortest distance that is not yet filled is assumed to be correct
                                double deltaLat = s.Centroid[0] - point.Lat;
                                double deltaLong = s.Centroid[1] - point.Lng;

                                double dLat2 = Math.Pow(deltaLat, 2);
                                double dLng2 = Math.Pow(deltaLong, 2);

                                double distance = Math.Sqrt(dLat2 + dLng2);

                                if (distance < Closest.Distance)
                                { 
                                    Closest.Distance = distance;

                                    Closest.Spot = s;

                                    if (tag.ID != "None")
                                    {
                                        pointTag = tag;
                                    }
                                }

                            }

                            else
                            {
                                continue;
                            }

                        }

                        try
                        {
                            if ( tag.ID != "None" || Closest.Spot.TagWithin == null)
                            {
                                Closest.Spot.TagWithin = pointTag;
                                Enforce.DetermineValidity(Closest.Spot, mapManager.Day, mapManager.Avail_perms);
                            }
                        }
                        catch { }
                    }
                }
                populateLotData(mapManager.Campus);

                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Would you like to generate a .csv report?", "", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                    LocalizeAreasTags(AllCoords, AllTags);
            }
            return true;
        }

        //this may never be used, trying to write data about areas to a csv file.
        /// <summary>
        /// This records a more detailed report to a CSV file
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="Tags"></param>
        private void LocalizeAreasTags(List<PointLatLng> coordinates,List<Tag> Tags)
        {
            string programDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");

            string LotName;
            string AreaName;
            string Header;
            string CurrentTime = System.DateTime.Now.ToString("D");
            if(File.Exists(CurrentTime + "_Report.csv"))
            {
                MessageBox.Show("You are about to overwrite a report from earlier today. Please make sure you have a backup of all vital reports before continuing");
            }
            if (!Directory.Exists(Path.Combine(programDirectory, "Enforcement Reports")))
            {
                Directory.CreateDirectory(Path.Combine(programDirectory, "Enforcement Reports"));
            }
            CurrentTime = Path.Combine(programDirectory, "Enforcement Reports", CurrentTime);
            CurrentTime = CurrentTime + "_Report.csv";

            foreach(ParkZone lot in mapManager.Campus.Subareas)
            {
                LotName = lot.Name;
                foreach(ParkZone area in lot.Subareas)
                {
                    AreaName = area.Name;
                    Header = LotName + " - " + AreaName;                    
                    using (var Coords = coordinates.GetEnumerator())
                    using (var tagsListed = Tags.GetEnumerator())
                   // using (var mem = new MemoryStream())
                    using (var writer = new StreamWriter(CurrentTime))                    
                    using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                                               
                    {
                        csvWriter.Configuration.Delimiter = ",";
                        
                        //VectorToSpace Closest = new VectorToSpace(1000, null);

                        while (tagsListed.MoveNext() && Coords.MoveNext())
                        {
                            var tag = tagsListed.Current;
                            var location = Coords.Current;
                            double[] position = new double[2];
                            position[0] = location.Lat;
                            position[1] = location.Lng;

                            var records = new List<TagInfo>();
                           
                            ParkZone temp = mapManager.LocalizePointToArea(position);

                            if (temp == area)
                            {
                                //print location, key and time
                                temp.TagWithin = tag;
                                Enforce.DetermineValidity(temp, mapManager.Day, permManager);

                                                              
                                if (temp.TagWithin.Valid == true)
                                {
                                    records.Add(new TagInfo { lat = position[0], lng = position[1], time = tag.Time, key = tag.ID, validity = "Valid" ,area = Header});
                                }
                                else
                                {
                                    records.Add(new TagInfo { lat = position[0], lng = position[1], time = tag.Time, key = tag.ID, validity = "Invalid", area = Header }) ;
                                }
                                csvWriter.WriteRecords(records);
                                temp.TagWithin = null;
                            }
                        }
                    }
                }
            }
        }

        //This adds every blacklisted tag from a .CSV file to a list. The file select stuff is temporary, but the list is not. Whatever method for loading CSV files is final will add
        // each tag to a list just like it does now.

        //needs to do this from a text file
        /// <summary>
        /// Takes the blacklist file and creates a record of every current blacklisted tag to use for data processing
        /// </summary>
        private void populateBlacklist()
        {
            /*
             Something else will eventually go here to select what csv file will be loaded from.
            The XAML will also be different. Probably no file selection at all.
             */
            IDictionary<string,object> rfidConfig =  ReadConfig.SensorConfigReader("RFID");
            string path = "";
            if (rfidConfig.ContainsKey("BlacklistDetector.BlacklistFileName"))
            {
                path = rfidConfig["BlacklistDetector.BlacklistFileName"].ToString();
            }
            else
            {
                //$$ No path for blacklist file. Handle error.
                MessageBox.Show("No path for blacklist specified in application settings. Please select a path.");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Text Files (*.txt)|*.txt*";
                openFileDialog.Title = "Select a blacklist text file";
                if (openFileDialog.ShowDialog() == true)
                    path = openFileDialog.FileName;
                return;
            }
            
            if (System.IO.File.Exists(path))
            {
                string[] bl = System.IO.File.ReadAllLines(path);

                foreach (string s in bl)
                {
                    var charsToRemove = new string[] { " " };
                    string bltag = s.Replace(" ", string.Empty);
                    var lastFive = bltag.Substring(bltag.Length - 5);

                    BlacklistedTags.Add(lastFive);
                }
            }
            else
            {
                //$$ Blacklist does not exist. Handle?
                MessageBox.Show("Blacklist file does not exist, please specify a different path.");
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Text Files (*.txt)|*.txt*";
                openFileDialog.Title = "Select a blacklist text file";
                if (openFileDialog.ShowDialog() == true)
                    path = openFileDialog.FileName;
            }           
        }
    }    
}
