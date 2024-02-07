using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Threading;
using CES.AlphaScan.Base;
using CES.AlphaScan.Acquisition;
using CES.AlphaScan.Gps;
using CES.AlphaScan.Rfid;
using CES.AlphaScan.CombinedSensors;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using ParkDS;


namespace GmapGui
{
    /// <summary>
    /// Interaction logic for DataAquisition.xaml
    /// </summary>
    public partial class DataAquisition : Window
    {
        private readonly IAcquisitionManager acquisitionManager = null;
        bool adminCheck;
        private List<PointLatLng> positionDelta = new List<PointLatLng>();

        

        public DataAquisition(bool admin)
        {
            string defaultMapDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps");

            InitializeComponent();
            MainMap.MapProvider = GMapProviders.GoogleMap;
            MainMap.Position = new PointLatLng(36.9835, -86.4574);
            MainMap.MinZoom = 12;
            MainMap.MaxZoom = 24;
            MainMap.Zoom = 18;

            MapManager mapManager = new MapManager();

            //StreamReader streamReader = new StreamReader("DefaultLoad.txt");
            StreamReader streamReader = new StreamReader(Path.Combine(defaultMapDirectory, "DefaultLoad.txt"));
            string load = streamReader.ReadLine();
            if (load == null)
                load = Path.Combine(defaultMapDirectory, "DefaultMap.xml");
            mapManager.LoadMap(load);
            streamReader.Close();
            PaintLoadedMap(mapManager.Campus);

            adminCheck = admin;
            acquisitionManager = new AcquisitionManager();
            acquisitionManager.MessageLogged += UpdateLog;

            //Add blacklisted tag detected message box popup event
            acquisitionManager.BlacklistTagDetected += DisplayBlacklistDetectedMsgBox;

            //Add handle to event for drawing location objects to map.
            (acquisitionManager as AcquisitionManager).UpdateTagLocationMap += DrawTagLocationPoints;
        }

        /// <summary>
        /// Updates the log textbox with a new log message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateLog(object sender, LogMessageEventArgs e)
        {
            sensorLogBlock?.Dispatcher.Invoke(() =>
            {
                sensorLogBlock.AppendText(">> " + e.SentTime.ToLocalTime().ToString() + " - " + e.Sender + " >> " + e.Message + Environment.NewLine);
                sensorLogBlock.ScrollToEnd();
            });
        }

        /// <summary>
        /// Clears the log textbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearLogBtn_Click(object sender, RoutedEventArgs e)
        {
            sensorLogBlock.Dispatcher.Invoke(() => sensorLogBlock.Clear());
        }

        /// <summary>
        /// Exits the data acquisition portion of the program.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Exit_DataAcquisition_Click(object sender, RoutedEventArgs e)
        {
            alphaScanMainMenu open = new alphaScanMainMenu(adminCheck);
            open.Show();
            this.Close();
        }

        #region sensor controllers

        //$$$ What does this do? seems to always be null.
        private static Thread updateGPSMap= null;

        //$$$ What does this do? It seems to always be null.
        private static Thread tagLocationPointThread = null;

        /// <summary>
        /// Makes a sensor run an operation asynchronously. Then updates the sensor status UI.
        /// </summary>
        /// <param name="type">The sensor to perform the operation. ("All Sensors", "mmWave", "GPS", "RFID")</param>
        /// <param name="operation">The operation to perform. ("Start", "Stop", "Abort") 
        /// Note that Abort is currently only implemented for All Sensors.</param>
        private async void RunSensorOperationTask(string type, string operation)
        {
            // create new thread to run inputted operation on that sensor
            Task t = Task.CompletedTask;
            try
            {
                switch (type)
                {
                    case "All Sensors":
                        switch (operation)
                        {
                            case "Start":
                                t = Task.Run(async () => await acquisitionManager.StartAllSensors());
                                break;
                            case "Stop":
                                t = Task.Run(async () => await acquisitionManager.StopAllSensors());
                                break;
                            case "Abort":
                                t = Task.Run(async () => await acquisitionManager.AbortAllSensors());
                                break;
                        }
                        break;
                    case "mmWave":
                        switch (operation)
                        {
                            case "Start":
                                t = Task.Run(() => acquisitionManager.StartmmWave());
                                t.Wait();
                                break;
                            case "Stop":
                                t = Task.Run(async () => await acquisitionManager.StopmmWave());
                                break;
                        }
                        break;
                    case "GPS":
                        switch (operation)
                        {
                            case "Start":
                                t = Task.Run(() => acquisitionManager.StartGPS());
                                t.Wait();
                                break;
                            case "Stop":
                                t = Task.Run(async () => await acquisitionManager.StopGPS());
                                break;
                        }
                        break;
                    case "RFID":
                        switch (operation)
                        {
                            case "Start":
                                t = Task.Run(() => acquisitionManager.StartRFID());
                                t.Wait();
                                break;
                            case "Stop":
                                t = Task.Run(async () => await acquisitionManager.StopRFID());
                                break;
                        }
                        break;
                }
            }
            catch
            { return; }

            await t.ConfigureAwait(false);
            //t.Wait(250);

            // get number of sensors running, then update UI with the current status of each sensor
            UpdateSensorIndicators();

            await Task.Delay(200); //async version.

            // Display which combination processors are running.
            await UpdateProcessorIndicators();
        }

        //$$$ Is this needed?
        /// <summary>
        /// Waits for one of the combination processing threads to complete. Then updates 
        /// GUI to display that it stopped.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="processors"></param>
        /// <returns></returns>
        private async Task WaitForThreadCloseUpdate(string type, bool[] processors)
        {
            switch (type)
            {
                case "TagLocation":
                    if (!processors[1])
                    {
                        var waitProcessors = (acquisitionManager as AcquisitionManager).WaitForProcessorRunning();
                        await waitProcessors[1];
                    }
                    tagLocationCombinerBlock.Dispatcher.Invoke(() =>
                    {
                        tagLocationCombinerBlock.Text = "Not Processing";
                        tagLocationCombinerBlock.Background = new SolidColorBrush(Colors.Red);
                    });

                    if (tagLocationPointThread != null)
                    {
                        stopUpdatingTagLocation = true;
                        tagLocationPointThread = null;
                    }

                    break;
                case "Geolocation":
                    if (!processors[0])
                    {
                        var waitProcessors = (acquisitionManager as AcquisitionManager).WaitForProcessorRunning();
                        await waitProcessors[0];
                    }
                    geolocationCombinerBlock.Dispatcher.Invoke(() =>
                    {
                        geolocationCombinerBlock.Text = "Not Processing";
                        geolocationCombinerBlock.Background = new SolidColorBrush(Colors.Red);
                    });

                    break;
            }
        }

        //$? This is checked every time a button is pressed. Maybe less efficient than it could be.
        /// <summary>
        /// Gets which sensors are running and updates GUI to reflect that. To be called 
        /// after button press action performed.
        /// </summary>
        private void UpdateSensorIndicators()
        {
            // Get number of sensors running, then update UI with the current status of each sensor
            bool[] sensorsRunning = acquisitionManager.GetSensorsRunning();

            if (sensorsRunning[0])
            {
                mmWaveStatusBlock.Dispatcher.Invoke(() =>
                {
                    mmWaveStatusBlock.Text = "mmWave ON";
                    mmWaveStatusBlock.Background = new SolidColorBrush(Colors.Green);
                });
            }
            else
            {
                mmWaveStatusBlock.Dispatcher.Invoke(() =>
                {
                    mmWaveStatusBlock.Text = "mmWave OFF";
                    mmWaveStatusBlock.Background = new SolidColorBrush(Colors.Red);
                });
            }

            if (sensorsRunning[1])
            {
                GPSStatusBlock.Dispatcher.Invoke(() =>
                {
                    GPSStatusBlock.Text = "GPS ON";
                    GPSStatusBlock.Background = new SolidColorBrush(Colors.Green);
                });

                MainMap.Dispatcher.Invoke(() => MainMap.Markers.Clear());
                acquisitionManager.UpdateGPSMap += UpdateVehicleLocation;
            }
            else
            {
                GPSStatusBlock.Dispatcher.Invoke(() =>
                {
                    GPSStatusBlock.Text = "GPS OFF";
                    GPSStatusBlock.Background = new SolidColorBrush(Colors.Red);
                });

                if (updateGPSMap != null)
                {
                    stopUpdatingVLocation = true;
                    updateGPSMap = null;
                }

                acquisitionManager.UpdateGPSMap -= UpdateVehicleLocation;
            }

            if (sensorsRunning[2])
            {
                RFIDStatusBlock.Dispatcher.Invoke(() =>
                {
                    RFIDStatusBlock.Text = "RFID ON";
                    RFIDStatusBlock.Background = new SolidColorBrush(Colors.Green);
                });
            }
            else
            {
                RFIDStatusBlock.Dispatcher.Invoke(() =>
                {
                    RFIDStatusBlock.Text = "RFID OFF";
                    RFIDStatusBlock.Background = new SolidColorBrush(Colors.Red);
                });
            }
        }

        /// <summary>
        /// Gets which data combination loops are running and updates GUI to show that. 
        /// To be called after button press action performed.
        /// </summary>
        private async Task UpdateProcessorIndicators()
        {
            // Display which combination processors are running.
            bool[] sensorsRunning = acquisitionManager.GetSensorsRunning();
            bool[] processorsRunning = acquisitionManager.GetProcessorsRunning();

            if (processorsRunning[0])
            {
                if (!sensorsRunning[0] || !sensorsRunning[1])
                {
                    var waitProcessors = acquisitionManager.WaitForProcessorRunning();
                    await waitProcessors[0];
                    geolocationCombinerBlock.Dispatcher.Invoke(new Action(() =>
                    {
                        geolocationCombinerBlock.Text = "Not Geolocating";
                        geolocationCombinerBlock.Background = new SolidColorBrush(Colors.Red);
                    }));
                }
                else
                {
                    geolocationCombinerBlock.Dispatcher.Invoke(() =>
                    {
                        geolocationCombinerBlock.Text = "Geolocating";
                        geolocationCombinerBlock.Background = new SolidColorBrush(Colors.Green);
                    });
                }
            }
            else
            {
                geolocationCombinerBlock.Dispatcher.Invoke(new Action(() =>
                {
                    geolocationCombinerBlock.Text = "Not Geolocating";
                    geolocationCombinerBlock.Background = new SolidColorBrush(Colors.Red);
                }));
            }

            if (processorsRunning[1])
            {
                tagLocationCombinerBlock.Dispatcher.Invoke(() =>
                {
                    tagLocationCombinerBlock.Text = "Combining";
                    tagLocationCombinerBlock.Background = new SolidColorBrush(Colors.Green);
                });

                if (!sensorsRunning[2])
                {
                    if (!processorsRunning[1])
                    {
                        var waitProcessors = acquisitionManager.WaitForProcessorRunning();
                        await waitProcessors[1];
                    }

                    tagLocationCombinerBlock.Dispatcher.Invoke(() =>
                    {
                        tagLocationCombinerBlock.Text = "Not Combining";
                        tagLocationCombinerBlock.Background = new SolidColorBrush(Colors.Red);
                    });

                    if (tagLocationPointThread != null)
                    {
                        stopUpdatingTagLocation = true;
                        tagLocationPointThread = null;
                    }
                }
            }
            else
            {
                tagLocationCombinerBlock.Dispatcher.Invoke(() =>
                {
                    tagLocationCombinerBlock.Text = "Not Combining";
                    tagLocationCombinerBlock.Background = new SolidColorBrush(Colors.Red);
                });

                if (tagLocationPointThread != null)
                {
                    stopUpdatingTagLocation = true;
                    tagLocationPointThread = null;
                }
            }
        }

        #region Buttons
        private void startAllSensorsBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("All Sensors", "Start"));
        }

        private void stopAllSensorsBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("All Sensors", "Stop"));
        }

        private void abortAllBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("All Sensors", "Abort"));
        }

        private void startmmWaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("mmWave", "Start"));
        }

        private void stopmmWaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("mmWave", "Stop"));
        }

        private void startGPSBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("GPS", "Start"));
        }

        private void sttopGPSBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("GPS", "Stop"));
        }

        private void startRFIDBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("RFID", "Start"));
        }

        private void stopRFIDBtn_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => RunSensorOperationTask("RFID", "Stop"));
        }
        #endregion


        #endregion


        private void PaintLoadedMap(ParkZone parent)
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
                    (polygon.Shape as System.Windows.Shapes.Path).Fill = Brushes.White;
                    (polygon.Shape as System.Windows.Shapes.Path).StrokeThickness = 3;
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

        #region Update GPS/TagLocation Points on map WIP

        //$$$ Are these needed?
        private static bool stopUpdatingVLocation = false;
        private static bool stopUpdatingTagLocation = false;


        private static GMapMarker GPSmarker = null;

        /// <summary>
        /// Draws the vehicle location gotten from GPS data as a blue dot on the map.
        /// </summary>
        private void UpdateVehicleLocation()
        {
            stopUpdatingVLocation = false;
            Color fColor = Color.FromRgb(0, 0, 255);
            PointLatLng p;
            //Try to get GPS data points from acquisition manager.
            if (acquisitionManager.GpsMapData.TryDequeue(out GpsData mapData))
            {
                MainMap.Dispatcher.Invoke(() =>
                {
                    if (positionDelta.Count > 1)
                    {
                        GMapPolygon Line = new GMapPolygon(positionDelta);
                        Line.RegenerateShape(MainMap);

                        (Line.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Red;
                        (Line.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                        (Line.Shape as System.Windows.Shapes.Path).StrokeThickness = 3;

                        MainMap.Markers.Add(Line);
                        positionDelta.RemoveAt(0);
                    }

                    if (GPSmarker != null)
                        MainMap.Markers.Remove(GPSmarker);

                    p = new PointLatLng(mapData.Lat, mapData.Long);
                    //reposition the view of the map over the new location
                    MainMap.Position = p;
                    positionDelta.Add(p);
                    GPSmarker = new GMapMarker(p);
                    GPSmarker.Shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                        StrokeThickness = 1.5,
                        Visibility = Visibility.Visible,

                        Fill = new SolidColorBrush(fColor),
                        Data = new EllipseGeometry
                        {
                            RadiusX = 5,
                            RadiusY = 5,
                        },
                    };
                    
                    MainMap.Markers.Add(GPSmarker);
                });
            } 
        }

        /// <summary>
        /// Color of a valid TagLocation object.
        /// </summary>
        private readonly Color fColorValid = Color.FromRgb(0, 255, 0);

        /// <summary>
        /// Color of an invalid TagLocation object.
        /// </summary>
        private readonly Color fColorInvalid = Color.FromRgb(255, 0, 0);

        //$$$ THIS IS TEMPORARY, A BIT HACKY, check if this even needs to be static or not
        private List<TagObjectLocation> nonesToPlot = new List<TagObjectLocation>();

        /// <summary>
        /// Draws TagLocation objects as points on the map.
        /// </summary>
        private void DrawTagLocationPoints()
        {
            stopUpdatingTagLocation = false;
            GMapMarker TagLocationmarker;
            List<GMapMarker> mapMarkerList = new List<GMapMarker>();

            
            if (!CombinationManager.isRunOver)
            {
                while (CombinationManager.TagLocationToMap.TryDequeue(out TagObjectLocation item))
                {
                    if (item.TagId == "None")
                    {
                        nonesToPlot.Add(item);
                        continue;
                    }

                    MainMap.Dispatcher.Invoke(() =>
                    {
                        TagLocationmarker = new GMapMarker(new PointLatLng(item.Lat, item.Lng));
                        TagLocationmarker.Shape = new System.Windows.Shapes.Path
                        {
                            Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                            StrokeThickness = 1.5,
                            Visibility = Visibility.Visible,

                            // TEMPORARY UNTIL WE HAVE A TAG LIST TO COMPARE TO
                            //Fill = (item.TagId is "None") ? new SolidColorBrush(fColorInvalid) : new SolidColorBrush(fColorValid),
                            // with only plotting nones at end, this should only plot the valids, remove check
                            Fill = new SolidColorBrush(fColorValid),

                            Data = new EllipseGeometry
                            {
                                RadiusX = 5,
                                RadiusY = 5,
                            },
                        };
                        mapMarkerList.Add(TagLocationmarker);
                    });

                }
            }
            else
            {
                while (CombinationManager.TagLocationToMap.TryDequeue(out TagObjectLocation item))
                {
                    if (item.TagId == "None")
                    {
                        nonesToPlot.Add(item);
                        continue;
                    }
                }

                foreach (TagObjectLocation item in nonesToPlot)
                {
                    MainMap.Dispatcher.Invoke(() =>
                    {
                        TagLocationmarker = new GMapMarker(new PointLatLng(item.Lat, item.Lng));
                        TagLocationmarker.Shape = new System.Windows.Shapes.Path
                        {
                            Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                            StrokeThickness = 1.5,
                            Visibility = Visibility.Visible,

                            // TEMPORARY UNTIL WE HAVE A TAG LIST TO COMPARE TO
                            //Fill = (item.TagId is "None") ? new SolidColorBrush(fColorInvalid) : new SolidColorBrush(fColorValid),
                            // with only painting nones at end, this should only paint nones, remove check for not none
                            Fill = new SolidColorBrush(fColorInvalid),

                            Data = new EllipseGeometry
                            {
                                RadiusX = 5,
                                RadiusY = 5,
                            },
                        };
                        mapMarkerList.Add(TagLocationmarker);
                    });
                }

                nonesToPlot.Clear();
                CombinationManager.isRunOver = false;
            }

            MainMap.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < mapMarkerList.Count; i++)
                {
                    MainMap.Markers.Add(mapMarkerList[i]);
                }
            });
        }

        #endregion

        #region BUBBLED EVENT TESTING

        // TESTING FOR BUBBLED EVENTS

        /// <summary>
        /// Event handler. Displays message box when a blacklisted RFID tag is detected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="tag">Data regarding the tag that was detected.</param>
        public void DisplayBlacklistDetectedMsgBox(object sender, TagData tag)
        {
            //$$ add functionality to get side of vehicle
            MessageBox.Show("Blacklisted Tag " + tag.TagId + " found by antenna " + tag.RxAntenna);
        }


        #endregion

        /// <summary>
        /// Button click event handler that toggles the visibility of the log panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowLog_Click(object sender, RoutedEventArgs e)
        {
            if (LogPanel.Visibility != Visibility.Visible)
            {
                LogPanel.Visibility = Visibility.Visible;
            }
            else
            {
                LogPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Button click event handler that toggles the visibility of the sensor status indicator panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleLowLevel_Click(object sender, RoutedEventArgs e)
        {
            if (SensorStatusPanel.Visibility != Visibility.Visible)
            {
                SensorStatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                SensorStatusPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}
