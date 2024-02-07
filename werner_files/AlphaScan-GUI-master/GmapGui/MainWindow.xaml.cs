using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.VisualBasic;

/*GUI Design by Landon Owens for the Center for Energy Systems Project "AlphaScan" 2020
 * 
 */

namespace GmapGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private PointLatLng last = new PointLatLng(0, 0);
        private PointLatLng lastLast = new PointLatLng(0, 0);

        //private string selectedText;

        //Used to determine if the user can draw shapes
        private bool isPointSelection = false;
        //Used to detect if the polygon has been drawn yet
        private bool polyDrawn = false;
        //Used to determine if the cursor is hovering above the desired polygon
        private bool cursorOverPolygon = false;
        //Modify this when you want the polygons to be colored in
        private bool polygonFill = true;
        //Used to determine if the user can delete parking spots by clicking on them
        private bool isDeleteSpot = false;
        //Used to determine if a user can add permissions to parking spots by clicking them
        private bool isEditSpotPerms = false;
        //Pass this admin key between windows to maintain users permisisons
        private bool isAdmin;

        //List of all vertices kept temporarily while drawing polygons
        private List<PointLatLng> vertices = new List<PointLatLng>();
        //List of points in an edge used to draw each line segment of a polygon
        private List<PointLatLng> edge = new List<PointLatLng>();
        //Vertices put in here when converted to an array
        private double[] vertsAsArray;

        //Layers 1, 2, and 3 distinguish lots, areas, and spots. Used in parkzone creation
        private int newZoneLayer;
        //Index of parkzone discovered from menu to locate in the data structure
        private int selectedParkzoneIndex;
        //ID of the lot selected
        private int selectedLot;
        //ID of the area selected
        private int selectedSubarea;
        //Values for polygon edge thickness
        private double thinLine = 3;
        private double thickLine = 3;

        //Indicates how many polygons were added when you viewed a lot. This will let you set up a loop to select valid index for drawing new polys.
        private int newPolys = 0;

        //Initialize the map and perm managers
        private MapManager mapManager;
        private PermManager permManager;

        //private int currentLayer = 0;
        int arrayPos = 0;
        int sideCount = 0;

        //This is for testing
        //This is totally ridiculous there must be a better way
        private double[] xArray = new double[100];
        private double[] yArray = new double[100];
        //maybe use an array as temp storage while creating polygons

        private string fileLocation;

        // string for setting area type
        private string areaType;

        public MainWindow(bool adminActive)
        {
            InitializeComponent();
            isAdmin = adminActive;
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

            mapManager = new MapManager(new ParkZone());
            permManager = new PermManager();

            //load in map data
            MainMap.MapProvider = GMapProviders.GoogleMap;
            MainMap.Position = new PointLatLng(36.9835, -86.4574);
            MainMap.MinZoom = 12;
            MainMap.MaxZoom = 24;
            MainMap.Zoom = 18;

            try
            {
                StreamReader streamReader1 = new StreamReader(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps", "DefaultPermLoad.txt"));
                string loadPerm = streamReader1.ReadLine();
                if (loadPerm == null)
                    loadPerm = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps", "DefaultMap_perm.xml");
                try
                {
                    permManager.LoadPerm(loadPerm);
                }
                catch
                {
                    loadPerm = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps", "DefaultMap_perm.xml");
                    permManager.LoadPerm(loadPerm);
                }
                streamReader1.Close();

                if (MessageBox.Show("Would you like to load a map file?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Filter = "XML Files (*.xml)|*.xml*";
                    openFileDialog.Title = "Select a file to load";
                    if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Maps")))
                        openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Maps");
                    else
                        openFileDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps");

                    if (openFileDialog.ShowDialog() == true)
                    {
                        string mapLoad = openFileDialog.FileName;
                        mapManager.LoadMap(mapLoad);
                        fileLocation = mapLoad;
                        paintLoadedMap(mapManager.Campus);
                        populatePermSelect();
                    }
                    // cancel button is pressed, return to the main menu
                    else
                    {
                        cancel_MapManager();
                        return;
                    }
                }
                else if(MessageBox.Show("Would you like to create a new map file?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                    // prompt user for a new map file name, do not allow clones of the same file name, notify if it is a clone
                    string fileName = null;
                    do
                    {
                        fileName = Interaction.InputBox("Please enter a name for the file.", "Name:", "newMap");
                        // a filename length of 0 means that the cancel button was pressed, could trigger if user inputs empty name
                        if (fileName.Length == 0)
                        {
                            cancel_MapManager();
                            return;
                        }

                        fileName += "_Editor.xml";
                        if (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Maps", fileName)))
                            MessageBox.Show("File name already exists. Please choose a different name");
                    } while (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan", "Maps", fileName)));

                    mapManager.SaveMap(fileName);
                    fileLocation = fileName;
                    mapManager = new MapManager(new ParkZone());
                    populatePermSelect();

                    PermSlot npr = new PermSlot();
                    npr.Name = "NPR";

                    DateTime start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
                    DateTime end = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 0, 0);

                    npr.ValidTimes = new DateTime[] {start, end};

                    mapManager.Campus.Permissions.Add(npr);

                }
                
                else
                {
                    StreamReader streamReader2 = new StreamReader(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps", "DefaultLoad.txt"));
                    string load = streamReader2.ReadLine();
                    if (load == null)
                    {
                        load = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Alphascan", "DefaultMaps", "DefaultMap.xml");
                    }
                    fileLocation = load;
                    try
                    {
                        mapManager.LoadMap(load);
                    }
                    catch
                    {
                        MessageBox.Show("The default map file has been removed from its original location and cannot be loaded.");
                        exit_MapManager_Click(null,null);
                    }
                    streamReader2.Close();
                    paintLoadedMap(mapManager.Campus);
                    populatePermSelect();
                }
            }
            catch
            {
            //    mapManager.SaveMap("TestFile.xml");
            //   mapManager = new MapManager(new ParkZone());
            }
        
            //WKU coords
            /*  PointLatLng point = new PointLatLng(36.9835, -86.4574);
               GMapMarker marker = new GMapMarker(point);

               //Draw point at defined coordinates
               marker.Shape = new System.Windows.Shapes.Path
               {
                   Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                   StrokeThickness = 1.5,
                   ToolTip = "This is tooltip",
                   Visibility = Visibility.Visible,
                   Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                   Data = new EllipseGeometry
                   {
                       RadiusX = 5,
                       RadiusY = 5,

                   },
               };

               //Actually paint what was defined above
               MainMap.Markers.Add(marker);
            
           */

            // occupancyCheck();
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        //This is a neat way of tracking coords that the cursor is currently over, unsure if it needs to stay though.
        //An address display may be more practical for this project if that is possible.
        /// <summary>
        /// This serves two purposes. One is the track the coordinates of the mouse and one it to detect whether or not the cursor is over a polygon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainMap_MouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(MainMap);
            var pointCon = MainMap.FromLocalToLatLng((int)point.X, (int)point.Y);

            //txbCoords.Text = pointCon.Lat + "";
            //  txbCoords_Copy.Text = pointCon.Lng + "";

            int i = 0;

            foreach (GMapMarker polys in MainMap.Markers)
            {
                if (i == selectedParkzoneIndex)
                {
                    if ((polys.Shape as System.Windows.Shapes.Path).IsMouseDirectlyOver == true)
                    {
                        cursorOverPolygon = true;
                    }
                    else
                    {
                        cursorOverPolygon = false;
                    }
                }
                i++;
            }

        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //Cancel button calls the kill shape controls function. This is probably redundant
        private void btn_cancelShape_Click(object sender, RoutedEventArgs e)
        {
            //killShapeControls();
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        //This was just me messing with some visual effects in the drop down window for layer selection. Layer selection will be redesigned,
        //but this will be kept for reference for now. - (8/4/2020)
        /*  private void killShapeControls()
          {
              MainMap.Effect = null;
              MainMap.CanDragMap = true;
              cbx_AddShape.Visibility = Visibility.Hidden;
              btn_cancelShape.Visibility = Visibility.Hidden;
              btn_makeShape.Visibility = Visibility.Hidden;

              //cbx_senControls.Visibility = Visibility.Visible;
              txbCoords.Visibility = Visibility.Visible;
              txbCoords_Copy.Visibility = Visibility.Visible;

              MainMap.MinZoom = 0;
              MainMap.MaxZoom = 24;
              MainMap.Zoom = 18;
          }*/


        //-------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// This is the click event for a new parking lot. It really just swaps flags to allow the drawing mode to be used and start a count of array index.
        /// Also throws a flag indicating that a lot is being drawn.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void new_Parkzone_Click(object sender, RoutedEventArgs e)
        {
            //  MessageBox.Show("New");
            newZoneLayer = 1;

            isPointSelection = true;

            arrayPos = 0;
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        //function to  activate "Drawing Mode" for polygons. 
        /*
         Should focus on getting rid of unnecessary steps in this function.
        This needs to 
        a) Draw a polygon with clear representation while its being drawn -DONE-
        b) Get rid of temporary points once the final shape is drawn -DONE-
        c) Be used to collect points that will be sent as a 1D array to the DS -DONE-
         */
        /*
         The MouseLeftButtonUp function below 
         */
        /// <summary>
        /// This has been extremely generalized. It serves to draw points, delete parking spaces, and assign permissions. Different flags 
        /// being set true by other events are what determines the function of clicking on the map.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainMap_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(isDeleteSpot == true)
                //When the delete spot flag is true the area clicked is located to a spot and that spot is deleted.
            {
                var p = e.GetPosition(MainMap);
                var pC = MainMap.FromLocalToLatLng((int)p.X, (int)p.Y);
                double[] localToCheck = { pC.Lat, pC.Lng };
                try
                {
                    mapManager.DeleteAreas(mapManager.LocalizePoint(localToCheck));
                    MainMap.Markers.Clear();
                    paintLoadedMap(mapManager.Campus);
                    paintLoadedMap(mapManager.SelectArea(0,selectedLot));
                    paintLoadedMap(mapManager.SelectArea(1,selectedSubarea));
                }
                catch
                {
                    MessageBox.Show("Please Select a Spot to Delete", "Alert");
                }
            }

            if (isEditSpotPerms == true)
            {
                //Similar to deleting spots except permission is assigned to the spot instead of deleting it
                var p = e.GetPosition(MainMap);
                var pC = MainMap.FromLocalToLatLng((int)p.X, (int)p.Y);
                double[] localToCheck = { pC.Lat, pC.Lng };
                try
                {
                    // mapManager.localizePoint(localToCheck);

                    PermSlot temp = new PermSlot();
                    temp.ValidTimes = timeBoxConvert();
                    temp.Name = cbx_PermAddSelect.Text;
                    ParkZone tempZone = mapManager.LocalizePoint(localToCheck);
                    // may want to do a new line for a long name
                    txbPermStatus.Text = "Permission successfully added to: " + tempZone.Name;
                    mapManager.UpdateAreas(tempZone.Name, tempZone.Vertices, temp);

                    //dpl_NewPerm.Visibility = Visibility.Collapsed;
                    //isEditSpotPerms = false;
                    
                    
                }
                catch
                {
                    MessageBox.Show("Please try again, spot was not detected.", "Alert");
                }
            }

            if (((isPointSelection == true) && (newZoneLayer == 1)) ||
                ((isPointSelection == true) && (newZoneLayer == 2) && (cursorOverPolygon == true)) ||
                ((isPointSelection == true) && (newZoneLayer == 3) && (cursorOverPolygon == true))
                )
            {
                //There are three ways to access drawing a point here. Each one represents a seperate layer of drawing. One is for parking lots (Layer 1), one for areas(2) and one for spots (3)
                //For the cases in which a parkzone is being drawn in a parent area there is a check to ensure that the cursor is hovering over the desired parent area.
                menuItem_UndoClick.IsEnabled = true;
                menuItem_clearDrawing.IsEnabled = true;

                if(arrayPos >= 99)
                {
                    MessageBox.Show("Maximum number of points reached.");
                    return;
                }

                var p = e.GetPosition(MainMap);
                var pC = MainMap.FromLocalToLatLng((int)p.X, (int)p.Y);

                PointLatLng pnt = new PointLatLng(pC.Lat, pC.Lng);
                GMapMarker m = new GMapMarker(pnt);

                PointLatLng temp = new PointLatLng(pnt.Lat, pnt.Lng);
                /*
                 Drawing polygons is simply the addition of coordinates into an array in a specific order. The code below manages the location of each vertex in an array while drawing
                visual indication of the coordinates being recorded.
                 */
                if (arrayPos != 0)
                {
                    edge.Add(temp);
                    edge.Add(last);
                    GMapPolygon pEdge = new GMapPolygon(edge);

                    pEdge.RegenerateShape(MainMap);
                    if (polygonFill == true)
                    {
                        (pEdge.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Red;
                        (pEdge.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                        (pEdge.Shape as System.Windows.Shapes.Path).StrokeThickness = 1.5;
                    }
                    else
                    {
                        (pEdge.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Red;
                        (pEdge.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                        (pEdge.Shape as System.Windows.Shapes.Path).StrokeThickness = 3;
                    }

                    (pEdge.Shape as System.Windows.Shapes.Path).Effect = null;

                    MainMap.Markers.Add(pEdge);

                    edge.Clear();
                    sideCount++;

                    lastLast.Lat = last.Lat;
                    lastLast.Lng = last.Lng;
                }

                vertices.Add(new PointLatLng(pnt.Lat, pnt.Lng));


                //The center gets saved here.

                xArray[arrayPos] = pnt.Lat;
                yArray[arrayPos] = pnt.Lng;

                arrayPos++;

                if (arrayPos >= 3)
                {
                    menuItem_AddShape.IsEnabled = true;
                }

                if (polygonFill == true)
                {
                    m.Shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                        StrokeThickness = 1,
 
                        Visibility = Visibility.Visible,
                        Fill = new SolidColorBrush(Color.FromRgb(255, 0, 0)),
                        Data = new EllipseGeometry
                        {
                            RadiusX = 1,
                            RadiusY = 1,
                        },
                    };
                }
                else
                {
                    m.Shape = new System.Windows.Shapes.Path
                    {
                        Stroke = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                        StrokeThickness = 1,
                        // ToolTip = "This is tooltip",
                        Visibility = Visibility.Visible,
                        Fill = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                        Data = new EllipseGeometry
                        {
                            RadiusX = 2,
                            RadiusY = 2,
                        },
                    };
                }

                //Markers are just the points being drawn.
                MainMap.Markers.Add(m);

                if (arrayPos != 0)
                {
                    last.Lat = pnt.Lat;
                    last.Lng = pnt.Lng;
                    //Each time a point is drawn, it becomes remembered as the "Last" point in order to manage drawing the line from the last and new point.
                    //When a point is selected a line is drawn from it to the last point, and it becomes the new last point.
                }
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        //function to actually create the overlay of a polygon after it has been drawn. compare to the function for "Drawing Mode"
        /*
         drawPoly will just draw the polygon specified by the coordinates
        */
        /// <summary>
        ///  Draws the polygon specified by the coordinates to the specified parent parkzone.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_drawPoly_Click(object sender, RoutedEventArgs e)
        {
            //btn_drawPoly.Visibility = Visibility.Hidden;
            // dpl_polyMenu.Visibility = Visibility.Hidden;
            //cbx_senControls.Visibility = Visibility.Visible;

            isPointSelection = false;
            polyDrawn = true;
            menuItem_AddShape.IsEnabled = false;

            removeMarkers(arrayPos + sideCount);

            double[] center = findCenter(xArray, yArray, arrayPos);
            PointLatLng cntr = new PointLatLng(center[0], center[1]);
            GMapMarker C = new GMapMarker(cntr);

            C.Shape = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                StrokeThickness = 1.5,
                // ToolTip = "This is tooltip",
                Visibility = Visibility.Visible,
                Fill = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                Data = new EllipseGeometry
                {
                    RadiusX = 2,
                    RadiusY = 2,

                },
            };

            GMapPolygon polygon = new GMapPolygon(vertices)
            {
            };
     
            polygon.RegenerateShape(MainMap);

            //Each layer is differentiated by a color when being drawn
            switch (areaType)
            {
                case "ParkingLot":
                    (polygon.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Red;
                    break;
                case "SubArea":
                    (polygon.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Green;
                    break;
                case "ParkingSpace":
                    (polygon.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Blue;
                    break;
            }


            if (polygonFill == true)
            {
                (polygon.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                (polygon.Shape as System.Windows.Shapes.Path).StrokeThickness = thinLine;
            }
            else
            {
                (polygon.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                (polygon.Shape as System.Windows.Shapes.Path).StrokeThickness = thickLine;
            }
            (polygon.Shape as System.Windows.Shapes.Path).Effect = null;
            (polygon.Shape as System.Windows.Shapes.Path).ToolTip = txb_parkZoneTitle.Text;
            //To add polygon in gmap

            MainMap.Markers.Add(polygon);
            // MainMap.Markers.Add(C);
            MainMap.Position = new PointLatLng(center[0], center[1]);
            vertsAsArray = convertArrays(xArray, yArray, arrayPos);

            //  ParkZone test = new ParkZone();

            //test.Vertices = vertsAsArray;
            //test.Vertices = vertsAsArray;

            vertices.Clear();
            arrayPos = 0;
            sideCount = 0;
            menuItem_clearDrawing.IsEnabled = false;
            menuItem_UndoClick.IsEnabled = false;
            menuItem_createParkZone.IsEnabled = true;
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        //Function to find the approximate center of a polygon (For map navigation purposes)
        /*
         This function is ideal for convex shapes. Concave centroid finding is kind of messed up
        but perhaps that is fine. The map finding the polygon is the important part, not finding the exact COM.

        In addition to this, it is probably useful to create a function that will auto resize the map window to show an entire lot, since lots
        vary in size, and 1 zoom level may not fit all.
         */
        /// <summary>
        /// Finds the centroid of a polygon
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private double[] findCenter(double[] x, double[] y, int size)
        {
            double[] Center = new double[2];
            double tempX = 0;
            double tempY = 0;

            for (int i = 0; i < size; i++)
            {
                tempX = tempX + x[i];
                tempY = tempY + y[i];
            }

            Center[0] = tempX / size;
            Center[1] = tempY / size;

            return Center;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //Function to remove temp markers
        /// <summary>
        /// Clears all temporary markers
        /// </summary>
        /// <param name="c">Number of points to be undone.</param>
        private void removeMarkers(int c)
        {
            int temp = MainMap.Markers.Count();
            try
            {
                for (int i = 0; i < c; i++)
                {

                    MainMap.Markers.RemoveAt((temp - i) - 1);

                }
            }
            catch
            {
                MainMap.Markers.Clear();
                paintLoadedMap(mapManager.Campus);
            }
        }

    
        //-------------------------------------------------------------------------------------------------------------------------------
        //Function to convert the 2 arrays of lat and lng into one consolidated array of lat,lng,lat,lng...etc
        /// <summary>
        /// Converts 2 1D arrays into one 1D array composed of each array alternating. Example: Array1 = X1,X2 ... Array2 = Y1,Y2 ... ArrayOut = X1,Y1,X2,Y2
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private double[] convertArrays(double[] X, double[] Y, int size)
        {
            double[] temp = new double[size * 2];
            for (int i = 0; i < size; i++)
            {
                temp[2 * i] = X[i];
                temp[2 * i + 1] = Y[i];
            }
            return temp;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This undoes ONE point. More than one is extremely complex and likely not needed.
        /// <summary>
        /// Used to undo the most recently drawn point in drawing mode.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void undoLastPoint(object sender, RoutedEventArgs e)
        {
            if (arrayPos <= 1)
            {
                removeMarkers(1);
                vertices.RemoveAt(0);
                arrayPos--;
                sideCount--;
            }
            else
            {
                removeMarkers(2);
                vertices.RemoveAt(arrayPos - 1);
                arrayPos--;
                sideCount--;
            }

            if (arrayPos < 3)
            {
                menuItem_AddShape.IsEnabled = false;
            }

            last.Lat = lastLast.Lat;
            last.Lng = lastLast.Lng;

            menuItem_UndoClick.IsEnabled = false;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This ungreys the title slot basically

        /// <summary>
        /// Used to alternate text from grey
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void changeNameText(object sender, MouseEventArgs e)
        {
            if (txb_parkZoneTitle.Text == "Name")
            {
                txb_parkZoneTitle.Background = Brushes.White;
                txb_parkZoneTitle.Foreground = Brushes.Black;
                txb_parkZoneTitle.Text = "";
            }
        }

        //------------------------------------------------------------------------------------------------------------------------------------
        //Same Concept from Earlier, just display the menu for creating a new parking lot
        /// <summary>
        /// Initializes parking lot creation. Displays menu and sets zone flag to 1, which is the parking lot layer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void new_ParkingLot_Click(object sender, RoutedEventArgs e)
        {
            newZoneLayer = 1;
            dpl_NewParkZone.Visibility = Visibility.Visible;
            cbx_ChooseSubarea.Visibility = Visibility.Collapsed;
            isPointSelection = true;
            txb_parkZoneTitle.Text = "Name";
            areaType = "ParkingLot";

            /* MenuItem temp = (MenuItem)sender;
              switch ((string)temp.Tag)
              {
                  case "Campus":
                      currentLayer = 0;
                      break;
                  case "Lot":
                      currentLayer = 1;
                      break;
                  case "Area":
                      currentLayer = 2;
                      break;
                  case "Spot":
                      currentLayer = 3;
                      break;
              }*/
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This should turn off the undo button, clear the polygon, clear the name...
        /// <summary>
        /// Fully cancels a new parking lot. Closes the menu, deletes all points drawn so far.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_CancelParkingLot_Click(object sender, RoutedEventArgs e)
        {
            dpl_NewParkZone.Visibility = Visibility.Hidden;
            vertices.Clear();
            removeMarkers(sideCount + arrayPos);
            sideCount = 0;
            arrayPos = 0;
            isPointSelection = false;
            txb_parkZoneTitle.Text = "Name";
            txb_parkZoneTitle.Background = new SolidColorBrush(Color.FromArgb(0xFF, 0xDE, 0xDE, 0xDE));

            menuItem_UndoClick.IsEnabled = false;
            menuItem_clearDrawing.IsEnabled = false;

            if (polyDrawn == true)
            {
                int pos = MainMap.Markers.Count;
                MainMap.Markers.RemoveAt(pos - 1);
            }

            polyDrawn = false;
            menuItem_AddShape.IsEnabled = false;
            menuItem_createParkZone.IsEnabled = false;

            newZoneLayer = 0;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This removes the points drawn thus far in a polygon on any map option.
        /// <summary>
        /// This will clear all of the points drawn when creating a new parkzone.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_clearDrawing_Click(object sender, RoutedEventArgs e)
        {
            removeMarkers(arrayPos + sideCount);
            vertices.Clear();
            arrayPos = 0;
            sideCount = 0;

            polyDrawn = false;
            menuItem_UndoClick.IsEnabled = false;
            menuItem_clearDrawing.IsEnabled = false;
            menuItem_AddShape.IsEnabled = false;
            menuItem_createParkZone.IsEnabled = false;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This will take the points specified and add them to the xml file by accessing the data structure.
        /// <summary>
        /// This will take the points specified and add them to the xml file by accessing the data structure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_createParkZone_Click(object sender, RoutedEventArgs e)
        {
 
            if (newZoneLayer == 1)
            {
                cbx_ChooseSubarea.Visibility = Visibility.Collapsed;
                string name = txb_parkZoneTitle.Text;
                double[] points = vertsAsArray;

                //ParkZone pz = new ParkZone(null, points, name);

                mapManager.SelectArea(-1, 0);
                mapManager.CreateArea(name, points, areaType);
                // mapManager.SaveMap("TestFile.xml");
                menuItem_createParkZone.IsEnabled = false;
                dpl_NewParkZone.Visibility = Visibility.Hidden;
                reloadMenu();
            }

            else if (newZoneLayer == 2)
            {
                cbx_ChooseSubarea.Visibility = Visibility.Collapsed;
                string name = txb_parkZoneTitle.Text;
                double[] points = vertsAsArray;
                int i = 0;
                try
                {
                    foreach (ParkZone pz in mapManager.Campus.Subareas)
                    {
                        if (i == selectedParkzoneIndex)
                        {
                            mapManager.SelectArea(0, i);
                            mapManager.CreateArea(name, points, areaType);
                            menuItem_createParkZone.IsEnabled = false;
                            dpl_NewParkZone.Visibility = Visibility.Hidden;
                            mapManager.SaveMap(fileLocation);
                            reloadMenu();
                        }
                        i++;
                    }
                }
                catch
                {
                    MessageBox.Show("Please wait. File has recently been modified and cannot be accessed.", "Alert");
                }

            }

            else if (newZoneLayer == 3)
            {
                string name = txb_parkZoneTitle.Text;
                double[] points = vertsAsArray;

                try
                {
                    mapManager.SelectArea(0, selectedLot);
                    mapManager.SelectArea(1, selectedSubarea);
                    mapManager.CreateArea(name, points, areaType);
                    mapManager.SaveMap(fileLocation);
                    menuItem_createParkZone.IsEnabled = false;
                    menuItem_doneWithLots.IsEnabled = true;                   
                    // dpl_NewParkZone.Visibility = Visibility.Hidden;
                    isPointSelection = true;
                }
                catch
                {
                    MessageBox.Show("Please wait. File has recently been modified and cannot be accessed.", "Alert");
                }
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// After Loading the map manager, call this to actually draw every lot and the campus boundaries.
        /// </summary>
        /// <param name="parent"></param>
        private void paintLoadedMap(ParkZone parent)
        {
            List<PointLatLng> vertsTemp = new List<PointLatLng>();
            PointLatLng tempPoint = new PointLatLng(0, 0);
            int j = 1;

            int length;

            reloadMenu();

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

                    // get area type, making the polygon the desired layer color
                    switch  (p.AreaType)
                    {
                        case "ParkingLot":
                            (polygon.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Red;
                            break;
                        case "SubArea":
                            (polygon.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Green;
                            break;
                        case "ParkingSpace":
                            (polygon.Shape as System.Windows.Shapes.Path).Stroke = Brushes.Blue;
                            break;
                    }

                    if (polygonFill == true)
                    {
                        (polygon.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                        (polygon.Shape as System.Windows.Shapes.Path).StrokeThickness = thinLine;
                    }
                    else
                    {
                        (polygon.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                        (polygon.Shape as System.Windows.Shapes.Path).StrokeThickness = thickLine;
                    }
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
        

        //-------------------------------------------------------------------------------------------------------------------------------
        //this deletes any area type
        /// <summary>
        /// This is a generalized function used to delete parkzones.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteArea(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem temp = (MenuItem)sender;
                MessageBoxResult messageBoxResult = System.Windows.MessageBox.Show("Are you sure you want to delete " + temp.Header + "? This action is irreversable.", "Delete Confirmation", System.Windows.MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    int position = (int)temp.Tag;
                    MainMap.Markers.RemoveAt(position);
                    mapManager.DeleteAreas(0, position);
                    mapManager.SaveMap(fileLocation);
                    MainMap.Markers.Clear();
                    paintLoadedMap(mapManager.Campus);
                }
            }
            catch
            {
                MessageBox.Show("Please wait. File has recently been modified and cannot be accessed.", "Alert");
            }

        }
        //Functionality for the sub area creation button
        private void newSubArea(object sender, RoutedEventArgs e)
        {
            cbx_ChooseSubarea.Visibility = Visibility.Collapsed;

            mapManager.SaveMap(fileLocation);
            MainMap.Markers.Clear();
            paintLoadedMap(mapManager.Campus);
            newZoneLayer = 2;
            dpl_NewParkZone.Visibility = Visibility.Visible;
            isPointSelection = true;

            areaType = "SubArea";

            MenuItem temp = (MenuItem)sender;
            selectedParkzoneIndex = (int)temp.Tag;

            viewParkingLot(temp, null);
            //open up area creator.
            //Area creator should have a title + polygon, be connected to a parkzone (Position referenced by tag)
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //load menus
        /*
         This is a really bad system for adding NEW menu items, super inconvenient but it works.

         Essentially, everything that can be listed in a menu that a user defines (parking lot names, etc.) must be reloaded whenever a change is made to the file,
         for example when a parking lot is created the list of parking lots that can be deleted must be updated. Vice versa when deleting something it shouldn't be visible
         in the menus anymore.
         */

    /// <summary>
    /// This refreshes menus within the GUI that list parking areas.
    /// </summary>
        private void reloadMenu()
        {
            delete_Lot.Items.Clear();
            new_Area.Items.Clear();
            view_ParkZone.Items.Clear();
            //edit_ParkingLot.Items.Clear();
            new_ParkingSpace.Items.Clear();
            delete_Space.Items.Clear();
            delete_Area.Items.Clear();
            edit_ParkingLotPerms.Items.Clear();
            edit_AreaPerms.Items.Clear();
            edit_SpotPerms.Items.Clear();
            /*remove_AreaPerms.Items.Clear();
            remove_ParkingLotPerms.Items.Clear();
            remove_SpotPerms.Items.Clear();*/
            int j = 0;

            foreach (ParkZone p in mapManager.Campus.Subareas)
            {
                MenuItem tempItem = new MenuItem();
                tempItem.Header = p.Name;
                tempItem.Style = Resources["EndMenuStyle"] as Style;
                tempItem.Click += new RoutedEventHandler(deleteArea);
                tempItem.Tag = j;

                delete_Lot.Items.Add(tempItem);

                MenuItem tempItem2 = new MenuItem();
                tempItem2.Header = p.Name;
                tempItem2.Style = Resources["EndMenuStyle"] as Style;
                tempItem2.Click += new RoutedEventHandler(newSubArea);
                tempItem2.Tag = j;
                new_Area.Items.Add(tempItem2);

                MenuItem tempItem3 = new MenuItem();
                tempItem3.Header = p.Name;
                tempItem3.Style = Resources["EndMenuStyle"] as Style;
                tempItem3.Click += new RoutedEventHandler(viewParkingLot);
                tempItem3.Tag = j;
                view_ParkZone.Items.Add(tempItem3);

                MenuItem tempItem4 = new MenuItem();
                tempItem4.Header = p.Name;
                tempItem4.Style = Resources["EndMenuStyle"] as Style;
                tempItem4.Click += new RoutedEventHandler(editLotPermissions);
                tempItem4.Tag = j;
                edit_ParkingLotPerms.Items.Add(tempItem4);

                MenuItem tempItem5 = new MenuItem();
                tempItem5.Header = p.Name;
                tempItem5.Style = Resources["EndMenuStyle"] as Style;
                tempItem5.Click += new RoutedEventHandler(newParkingSpace);
                tempItem5.Tag = j;
                new_ParkingSpace.Items.Add(tempItem5);

                MenuItem tempItem6 = new MenuItem();
                tempItem6.Header = p.Name;
                tempItem6.Style = Resources["EndMenuStyle"] as Style;
                tempItem6.MouseEnter += (delete_Area_Click);
                tempItem6.Tag = j;
                delete_Area.Items.Add(tempItem6);

                MenuItem tempItem7 = new MenuItem();
                tempItem7.Header = p.Name;
                tempItem7.Style = Resources["EndMenuStyle"] as Style;
                tempItem7.MouseEnter += (delete_Spot_Click);
                tempItem7.Tag = j;
                delete_Space.Items.Add(tempItem7);

                MenuItem tempItem8 = new MenuItem();
                tempItem8.Header = p.Name;
                tempItem8.Style = Resources["EndMenuStyle"] as Style;
                tempItem8.MouseEnter += (editAreaPermissions);
                tempItem8.Tag = j;
                edit_AreaPerms.Items.Add(tempItem8);
                
                MenuItem tempItem9 = new MenuItem();
                tempItem9.Header = p.Name;
                tempItem9.Style = Resources["EndMenuStyle"] as Style;
                tempItem9.MouseEnter +=  (editSpotPermissions);
                tempItem9.Tag = j++;
                edit_SpotPerms.Items.Add(tempItem9);
            }
        }


        //-------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// this just sets things in place for new spots to be drawn WITHIN an allowed polygon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newParkingSpace(object sender, RoutedEventArgs e)
        {
            cbx_ChooseSubarea.Visibility = Visibility.Visible;
            dpl_NewParkZone.Visibility = Visibility.Visible;
            menuItem_doneWithLots.Visibility = Visibility.Visible;
            tempSeperator.Visibility = Visibility.Visible;
            menuItem_doneWithLots.IsEnabled = false;

            newZoneLayer = 3;

            int i = 0;

            cbx_ChooseSubarea.Items.Clear();

            areaType = "ParkingSpace";

            MenuItem temp = (MenuItem)sender;
            viewParkingLot(temp, null);
            dpl_NewParkZone.Visibility = Visibility.Visible;

            selectedLot = (int)temp.Tag;

            ParkZone p = mapManager.SelectArea(0, (int)temp.Tag);

            foreach (ParkZone sub in p.Subareas)
            {
                ComboBoxItem newSub = new ComboBoxItem();
                newSub.Content = sub.Name;
                newSub.Tag = i;
                newSub.Selected += newSubSelected;
                cbx_ChooseSubarea.Items.Add(newSub);
                newPolys++;
                i++;
                try
                {
                    foreach (ParkZone space in sub.Subareas)
                    {
                        // newPolys++;
                    }
                }
                catch { }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// This will take the users selection from a menu and connect them to the area represented by that menu item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newSubSelected(object sender, RoutedEventArgs e)
        {
            int polycount = MainMap.Markers.Count;
            if (isDeleteSpot == false && isEditSpotPerms == false)
            {
                ComboBoxItem temp = (ComboBoxItem)sender;
                selectedSubarea = (int)temp.Tag;
            }
            else
            {
                MenuItem temp = (MenuItem)sender;
                selectedSubarea = (int)temp.Tag;
            }
            isPointSelection = true;
            
            //#of lots
            int j = 0;
            //#of areas in the selected lot
            int k = 0;
            //#of spots
            int h = 0;
            for (int i = 0; i < polycount; i++)
            {
                if (i == (selectedSubarea + (polycount - newPolys)))
                {
                    selectedParkzoneIndex = i + (polycount - newPolys) - 1;
                    //selectedParkzoneIndex = i;
                }

            }
            MainMap.Markers.Clear();
            paintLoadedMap(mapManager.Campus);
            foreach (ParkZone p in mapManager.Campus.Subareas)
            {

                if (j == selectedLot)
                {
                    paintLoadedMap(p);
                    foreach (ParkZone ar in p.Subareas)
                    {
                        if (k == selectedSubarea)
                        {
                            h = k;
                            paintLoadedMap(ar);

                        }
                        k++;
                    }
                }
                j++;
            }

            selectedParkzoneIndex = j + selectedSubarea;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This just moves the users view to the centroid of the desired lot and draws the details of the lot.
        /// <summary>
        /// Centers the users view on a lot and draws in the details.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewParkingLot(object sender, RoutedEventArgs e)
        {
            MenuItem temp = (MenuItem)sender;
            double[] tempLat = new double[100];
            double[] tempLng = new double[100];
            double[] tempCent = new double[2];

            int position = (int)temp.Tag;

            int length;
            int j = 0;
            int k = 0;

            foreach (ParkZone p in mapManager.Campus.Subareas)
            {
                length = p.Vertices.Length;
                if (j == position)
                {
                    if (isDeleteSpot == false && isEditSpotPerms == false)
                    {
                        MainMap.Markers.Clear();
                        paintLoadedMap(mapManager.Campus);
                        paintLoadedMap(p);
                    }
                    try
                    {
                        foreach (ParkZone sp in p.Subareas)
                        {
                            // paintLoadedMap(sp);
                        }
                    }
                    catch { }
                    for (int i = 0; i < length; i += 2)
                    {
                        tempLat[k] = p.Vertices[i];
                        tempLng[k] = p.Vertices[i + 1];
                        k++;
                    }
                }
                j++;
            }


            tempCent = findCenter(tempLat, tempLng, k);
            MainMap.Position = new PointLatLng(tempCent[0], tempCent[1]);
            MainMap.Zoom = 19;

        }

        //-------------------------------------------------------------------------------------------------------------------------------
        /// <summary>
        /// This closes the map manager and returns to the main menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exit_MapManager_Click(object sender, RoutedEventArgs e)
        {
            alphaScanMainMenu open = new alphaScanMainMenu(isAdmin);
            open.Show();
            this.Close();
            dpl_mapManager.Visibility = Visibility.Collapsed;

        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //Returns to the main menu because of an error
        /// <summary>
        /// Returns to the main menu in the event of an error
        /// </summary>
        private void cancel_MapManager()
        {
            alphaScanMainMenu open = new alphaScanMainMenu(isAdmin);
            this.Close();

        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //Saves the map...
        /// <summary>
        /// This saves the map.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void save_Map_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                mapManager.SaveMap(fileLocation);
            }
            catch
            {
                MessageBox.Show("Please wait. File has recently been modified and cannot be accessed.", "Alert");
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //Slider functionality 
        /// <summary>
        /// Makes the slider functional
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainMap.Zoom = slr_Zoom.Value + 12;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //Replacement for the (kinda bad) zoom function built into gmaps. Lets the user scroll on polygons and moves the slider in response
        /// <summary>
        /// Allows mouse wheel to control the slider
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        //-------------------------------------------------------------------------------------------------------------------------------
        //Switch for the view, makes it easier sometimes to add parkzones into the map
        /// <summary>
        /// This switches the visuals on gmap to a simplified and stylized view.
        /// </summary>
        /// <param name="sender">button that sent the request</param>
        /// <param name="e"></param>
        private void mapView_Click(object sender, RoutedEventArgs e)
        {
            MenuItem temp = (MenuItem)sender;
            if (temp.Name == "mapView_Sat")
            {
                polygonFill = false;
                MainMap.MapProvider = GMapProviders.BingSatelliteMap;
                foreach (GMapPolygon shape in MainMap.Markers)
                {
                    (shape.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                    (shape.Shape as System.Windows.Shapes.Path).StrokeThickness = thickLine;
                    shape.RegenerateShape(MainMap);
                }
            }
            else
            {
                polygonFill = true;
                MainMap.MapProvider = GMapProviders.GoogleMap;
                foreach (GMapPolygon shape in MainMap.Markers)
                {
                    (shape.Shape as System.Windows.Shapes.Path).Fill = Brushes.Transparent;
                    (shape.Shape as System.Windows.Shapes.Path).StrokeThickness = thinLine;
                    shape.RegenerateShape(MainMap);
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This is the first part of deleting an area. It populates the list on the drop down and activate delete_General
        // as well as setting up important variables. It is not a click event, but a hover. CHANGE THE NAME!
        /// <summary>
        /// Populates the drop down list with deletable areas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delete_Area_Click(object sender, RoutedEventArgs e)
        {
            
            MenuItem temp = (MenuItem)sender;
            selectedLot = (int)temp.Tag;

            ParkZone p = mapManager.SelectArea(0, (int)temp.Tag);

            //delete_Area.Items.Clear();

            int i = 0;
            foreach (MenuItem mi in delete_Area.Items)
            {
                if ((int)mi.Tag == selectedLot)
                {
                    mi.Items.Clear();
                    foreach (ParkZone lot in p.Subareas)
                    {
                        MenuItem temp2 = new MenuItem();
                        temp2.Header = lot.Name;
                        temp2.Tag = i;
                        temp2.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#b01e24");
                        temp2.Click += (delete_General);
                        mi.Items.Add(temp2);
                        i++;
                    }
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This is a bad name for deleting an area. It just takes the selected lot and area and deletes it. No need for it to be its own function but here we are.
        /// <summary>
        /// Deletes desired area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delete_General(object sender, RoutedEventArgs e)
        {

            MenuItem temp = (MenuItem)sender;
            selectedSubarea = (int)temp.Tag;
            ParkZone tempZone = mapManager.SelectArea(0, selectedLot);

            mapManager.SelectArea(0, selectedLot);
            mapManager.SelectArea(1, selectedSubarea);
            mapManager.DeleteAreas(1, selectedSubarea);
            try
            {
                mapManager.SaveMap(fileLocation);
                reloadMenu();

                MainMap.Markers.Clear();
                paintLoadedMap(mapManager.Campus);
                paintLoadedMap(tempZone);
            }
            catch
            {
                MessageBox.Show("Please wait. File has recently been modified and cannot be accessed.", "Alert");
            }

        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This also is a hover not a click. Very similar to delete area, populates a list and selects a lot. TBH these could probably be one function,
        //but once again, here we are.
        /// <summary>
        /// Populates a list of available lots to delete spots in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delete_Spot_Click(object sender, RoutedEventArgs e)
        {
            MenuItem temp = (MenuItem)sender;

            selectedLot = (int)temp.Tag;

            ParkZone p = mapManager.SelectArea(0, (int)temp.Tag);

            int i = 0;
            foreach (MenuItem mi in delete_Space.Items)
            {
                if ((int)mi.Tag == selectedLot)
                {
                    mi.Items.Clear();
                    foreach (ParkZone lot in p.Subareas)
                    {
                        MenuItem temp2 = new MenuItem();
                        temp2.Header = lot.Name;
                        temp2.Tag = i;
                        temp2.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#b01e24");
                        temp2.Click += delete_clickedSpot;
                        mi.Items.Add(temp2);
                        i++;
                    }
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //This is cool, it selects a polygon (the selected subarea) and activates delete mode
        //letting the left click event from FOREVER ago know when the cursor is over a polygon that has deletable stuff in it. This is to make sure only spots
        // within the selected area get deleted and not some invisible spot from another on a misclick, VERY important.
        /// <summary>
        /// This selects the area that spots can be deleted in. Only works when the user clicks a parking spot polygon within an area polygon
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void delete_clickedSpot(object sender, RoutedEventArgs e)
        {
           // newZoneLayer = 2;

            isDeleteSpot = true;
            MenuItem temp = (MenuItem)sender;
            newSubSelected(temp, null);
            isPointSelection = false;
            selectedSubarea = (int)temp.Tag;
            newZoneLayer = 2;
            mapManager.SelectArea(0, selectedLot);
           ParkZone paint = mapManager.SelectArea(1, selectedSubarea);
            paintLoadedMap(paint);

            new_Parkzone.IsEnabled = false;
            edit_ParkZone.IsEnabled = false;
            delete_ParkZone.Header = "Done Deleting";
           
            foreach(MenuItem item in delete_ParkZone.Items)
            {
                if (item.Visibility == Visibility.Visible)
                {
                    item.Visibility = Visibility.Collapsed;
                }
                else if (item.Visibility == Visibility.Collapsed)
                    item.Visibility = Visibility.Visible;
            }

            temp.Tag = selectedLot;
            viewParkingLot(temp, null);

        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //Functionality for the button that ends the parking spot deletion
        /// <summary>
        /// Closes the parking lot deletion mode out
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stop_Deleting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                isDeleteSpot = false;
                new_Parkzone.IsEnabled = true;
                edit_ParkZone.IsEnabled = true;
                delete_ParkZone.Header = "Delete";

                foreach (MenuItem item in delete_ParkZone.Items)
                {
                    if (item.Visibility == Visibility.Visible)
                    {
                        item.Visibility = Visibility.Collapsed;
                    }
                    else if (item.Visibility == Visibility.Collapsed)
                        item.Visibility = Visibility.Visible;
                }

                delete_ParkZone.AllowDrop = true;
                mapManager.SaveMap(fileLocation);
                reloadMenu();
            }
            catch
            {
                MessageBox.Show("Please wait. File has recently been modified and cannot be accessed.", "Alert");
            }

        }

        //This closes all of the visual elements relating to parkzone creation
        /// <summary>
        /// Closes parkzone creation menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_doneWithLots_Click(object sender, RoutedEventArgs e)
        {
            menuItem_doneWithLots.Visibility = Visibility.Collapsed;
            tempSeperator.Visibility = Visibility.Collapsed;
            isPointSelection = false;
            txb_parkZoneTitle.Visibility = Visibility.Visible;
            dpl_NewParkZone.Visibility = Visibility.Hidden;
        }

        //This code is just prototype code for what would eventually become the enforcement window
        private void occupancyCheck()
        {
            ParkZone Area = mapManager.SelectArea(-1, 0);
            foreach(ParkZone l in Area.Subareas) { 
                foreach(ParkZone a in l.Subareas)
                {
                    foreach(ParkZone s in a.Subareas)
                    {                       
                    PointLatLng point = new PointLatLng();
                    point.Lat = s.Centroid[0];
                    point.Lng = s.Centroid[1];

                    GMapMarker marker = new GMapMarker(point);
                        if (s.TagWithin != null)
                        {
                            marker.Shape = new System.Windows.Shapes.Path
                            {
                                Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                                StrokeThickness = 1.5,
                                ToolTip = ("This space is occupied"),
                                Visibility = Visibility.Visible,
                                Fill = new SolidColorBrush(Color.FromRgb(0, 255, 0)),
                                Data = new EllipseGeometry
                                {
                                    RadiusX = 5,
                                    RadiusY = 5,

                                },
                            };
                        }
                        else
                        {
                            marker.Shape = new System.Windows.Shapes.Path
                            {
                                Stroke = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                                StrokeThickness = 1.5,
                                ToolTip = ("This space is not occupied"),
                                Visibility = Visibility.Visible,
                                Fill = Brushes.Transparent,
                                Data = new EllipseGeometry
                                {
                                    RadiusX = 5,
                                    RadiusY = 5,

                                },
                            };
                        }

                        MainMap.Markers.Add(marker);
                    }
                }
            
                
            }
        }

        /*---------------------------------------------------------------------------------------------------------
        -----------------------------------------------------------------------------------------------------------
         _____  ______ _____  __  __ _____  _____ _____ _____ ____  _   _    _____ ____  _____  ______
        |  __ \|  ____|  __ \|  \/  |_   _|/ ____/ ____|_   _/ __ \| \ | |  / ____/ __ \|  __ \|  ____|
        | |__) | |__  | |__) | \  / | | | | (___| (___   | || |  | |  \| | | |   | |  | | |  | | |__   
        |  ___/|  __| |  _  /| |\/| | | |  \___ \\___ \  | || |  | | . ` | | |   | |  | | |  | |  __|  
        | |    | |____| | \ \| |  | |_| |_ ____) |___) |_| || |__| | |\  | | |___| |__| | |__| | |____ 
        |_|    |______|_|  \_\_|  |_|_____|_____/_____/|_____\____/|_| \_|  \_____\____/|_____/|______|
        ---------------------------------------------------------------------------------------------------------*/
  
        //This returns the user selected time as an array of doubles
        /// <summary>
        /// Converts the values within the time boxes into values useable in the data structure
        /// </summary>
        /// <returns></returns>
        private DateTime[] timeBoxConvert()
        {
            int hourStart = Convert.ToInt16(cbx_StartHour.Text);
            int minuteStart = Convert.ToInt16(cbx_StartMinute.Text);
            int hourEnd = Convert.ToInt16(cbx_EndHour.Text);
            int minuteEnd = Convert.ToInt16(cbx_EndMinute.Text);

            // add 12 hours if the value is a PM time
            if (StartPM.IsChecked == true)
                hourStart += 12;
            else if (hourStart == 12)
            {
                hourStart = 0;
            }


            if (EndPM.IsChecked == true)
                hourEnd += 12;
            else if (hourEnd == 12)
            {
                hourEnd = 0;
            }

            // convert hour values from CST to UTC 
            if ((hourStart += 6) >= 24)
                hourStart %= 24;
            if ((hourEnd += 6) >= 24)
                hourEnd %= 24;

            // this converts timewindow to a datetime where only hour and minute are used in UTC
            DateTime[] timeWindow = { new DateTime(DateTime.Now.Hour, DateTime.Now.Month, DateTime.Now.Day, hourStart, minuteStart,0), 
                new DateTime(DateTime.Now.Hour, DateTime.Now.Month, DateTime.Now.Day, hourEnd, minuteEnd,0) };
            return timeWindow;
        }


        //This will load in all of the available permissions to the list in the combobox.
        /// <summary>
        /// Loads in available permissions
        /// </summary>
        private void populatePermSelect()
        {
            List<Permission> perms = permManager.GetAvailablePerms();
            foreach(Permission perm in perms)
            {
                ComboBoxItem temp = new ComboBoxItem();
                temp.Content = perm.Name;
                temp.Foreground = Brushes.Black;
                //More functionality here

                cbx_PermAddSelect.Items.Add(temp);

                //this is done
            }
        }


        //This is now creating a temp perm slot with valid times and whatnot
        /// <summary>
        /// This adds the permission specified by the user to the specified parkzone
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addPermission_Click(object sender, RoutedEventArgs e)
        {
            if (isEditSpotPerms == true)
            {
                MessageBox.Show("Please Select a Spot for this permission");
                isEditSpotPerms = true;
            }
            else
            {
               // dpl_NewPerm.Visibility = Visibility.Collapsed;
                PermSlot temp = new PermSlot();
                temp.ValidTimes = timeBoxConvert();
                temp.Name = cbx_PermAddSelect.Text;
               
                //Code for adding to the selected parkzone goes here
                if (newZoneLayer < 2)
                    mapManager.SelectArea(newZoneLayer - 1, selectedLot).Permissions.Add(temp);

                ParkZone tempZone = mapManager.SelectArea(newZoneLayer - 1, selectedLot);
                
                mapManager.UpdateAreaPerms(temp);
            }
        }

        //Following 4 functions just change the state of checkboxes. When AM is made true, PM goes false...

        private void StartAM_Click(object sender, RoutedEventArgs e)
        {
            StartPM.IsChecked = false;
        }

        private void EndAM_Click(object sender, RoutedEventArgs e)
        {
            EndPM.IsChecked = false;
        }

        private void StartPM_Click(object sender, RoutedEventArgs e)
        {
            StartAM.IsChecked = false;
        }

        private void EndPM_Click(object sender, RoutedEventArgs e)
        {
            EndAM.IsChecked = false;
        }


        private void editLotPermissions(object sender, RoutedEventArgs e)
        {
            
            newZoneLayer = 1;
            dpl_NewPerm.Visibility = Visibility.Visible;
            MenuItem temp = (MenuItem)sender;

            int position = (int)temp.Tag;
            selectedLot = position;
        }

        private void cancelPermission_Click(object sender, RoutedEventArgs e)
        {
            dpl_NewPerm.Visibility = Visibility.Collapsed;

        }

      
        private void editAreaPermissions(object sender, RoutedEventArgs e)
        {
            
            MenuItem temp = (MenuItem)sender;

            selectedLot = (int)temp.Tag;
            newZoneLayer = 2;
            ParkZone p = mapManager.SelectArea(0, (int)temp.Tag);

            //delete_Area.Items.Clear();

            int i = 0;
            foreach (MenuItem mi in edit_AreaPerms.Items)
            {
                if ((int)mi.Tag == selectedLot)
                {
                    mi.Items.Clear();
                    foreach (ParkZone lot in p.Subareas)
                    {
                        MenuItem temp2 = new MenuItem();
                        temp2.Header = lot.Name;
                        temp2.Tag = i;
                        temp2.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#b01e24");
                        temp2.Click += (edit_ClickedArea);
                        mi.Items.Add(temp2);
                        i++;
                    }
                }
            }
         
        }


        private void editSpotPermissions(object sender, MouseEventArgs e)
        {
            MenuItem temp = (MenuItem)sender;

            selectedLot = (int)temp.Tag;
            newZoneLayer = 3;
            ParkZone p = mapManager.SelectArea(0, (int)temp.Tag);


            int i = 0;
            foreach (MenuItem mi in edit_SpotPerms.Items)
            {
                if ((int)mi.Tag == selectedLot)
                {
                    mi.Items.Clear();
                    foreach (ParkZone lot in p.Subareas)
                    {
                        MenuItem temp2 = new MenuItem();
                        temp2.Header = lot.Name;
                        temp2.Tag = i;
                        temp2.Background = (SolidColorBrush)new BrushConverter().ConvertFromString("#b01e24");
                        temp2.Click += (edit_SelectSpot);
                        mi.Items.Add(temp2);
                        i++;
                    }
                }
            }
        }

        private void edit_SelectSpot(object sender, RoutedEventArgs e)
        {
            dpl_NewPerm.Visibility = Visibility.Visible;

            isEditSpotPerms = true;
            isDeleteSpot = false;
            MenuItem temp = (MenuItem)sender;
            newSubSelected(temp, null);
            isPointSelection = false;
            selectedSubarea = (int)temp.Tag;
            newZoneLayer = 2;
            mapManager.SelectArea(0, selectedLot);
            ParkZone paint = mapManager.SelectArea(1, selectedSubarea);
            paintLoadedMap(paint);

           // new_Parkzone.IsEnabled = false;
           // edit_ParkZone.IsEnabled = false;
                
            temp.Tag = selectedLot;
            viewParkingLot(temp, null);
        }

        private void edit_ClickedArea(object sender, RoutedEventArgs e)
        {
            dpl_NewPerm.Visibility = Visibility.Visible;
            MenuItem temp = (MenuItem)sender;

            int position = (int)temp.Tag;
            selectedLot = position;           
        }
    }
}
