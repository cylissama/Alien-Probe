using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace ParkDS
{
    /// <summary>
    /// Wrapper class for the parking data structure. Permits the creation, 
    /// update, selection, and deletion of areas.
    /// </summary>
    class MapManager
    {
        //$$ Some of these setters should probably be private. Also why internal?

        /// <summary>
        /// The areas currently selected. Three levels of selection: 
        /// 0 - Parking lot, 1 - ID area, 2 - Parking space.
        /// </summary>
        private ParkZone[] selectedAreas = new ParkZone[3];

        /// <summary>
        /// The current level of parking area selection. Levels are as follows: 
        /// 0 - Campus, 1 - Parking lot, 2 - ID area, 3 - Parking space.
        /// </summary>
        private int level = 0;

        /// <summary>
        /// Campus for this map. Ancestor of all <see cref="ParkZone"/> areas on the map.
        /// </summary>
        internal ParkZone Campus { get; set; }

        /// <summary>
        /// The day of the week. Starting with 0 = Monday to 6 = Sunday.
        /// </summary>
        public int Day { get; set; } = 0;

        /// <summary>
        /// Permission manager containing all the available permissions for this map.
        /// </summary>
        internal PermManager Avail_perms { get; set; }

        #region Core Functions
        /// <summary>
        /// Create default map manager.
        /// </summary>
        public MapManager()
        {
            Campus = null;
            Avail_perms = new PermManager();
        }

        /// <summary>
        /// Create map manager with known <see cref="ParkZone"/> structure.
        /// </summary>
        /// <param name="campus">Known <see cref="ParkZone"/> structure.</param>
        public MapManager(ParkZone campus)
        {
            Campus = campus;
            Avail_perms = new PermManager();
        }

        /// <summary>
        /// Create an area using just an assigned name. (Uses parent's parameters for all other parameters.)
        /// </summary>
        /// <param name="name"></param>
        public void CreateArea(string name, string areaType)
        {
            if (level > 0) //subareas below Parking Lot
            {
                CreateSubarea(this.selectedAreas[level - 1], name, this.selectedAreas[level-1].Vertices, areaType);
            }
            else //create parking lot
            {
                CreateSubarea(this.Campus, name, this.Campus.Vertices, areaType);
            }
        }

        /// <summary>
        /// Create an area using a name and vertices defined clockwise. 
        /// (Uses parent's parameters for all other parameters.)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="vert"></param>
        public void CreateArea(string name, double[] vert, string areaType)
        {
            if (level > 0)
            {
                CreateSubarea(this.selectedAreas[level - 1], name, vert, areaType);
            }
            else
            {
                CreateSubarea(this.Campus, name, vert, areaType);
            }
        }

        /// <summary>
        /// Create a subarea of the selected area.
        /// </summary>
        /// <param name="area">Parent area.</param>
        /// <param name="name">Name of subarea.</param>
        /// <param name="verts">Vertices of the subarea.</param>
        private void CreateSubarea(ParkZone area, string name, double[] verts, string areaType)
        {
            //Ensure all vertices of the subarea are within the parent area.
            for (int i = 0; i < verts.Length/2; i++)
                if (!IsWithin(area, new double[] { verts[2 * i], verts[2 * i + 1] }))
                {
                    return;
                }

            //Prevent creating subarea of a parking space
            if (this.level <= 2) 
            {
                if (area.Subareas != null) //append new area to list if it already exists
                {
                    area.Subareas.Add(new ParkZone(area.Permissions, verts, name, areaType));
                    area.Subareas.Sort();
                }
                else //create subarea list and add new area
                {
                    area.Subareas = new List<ParkZone>();
                    area.Subareas.Add(new ParkZone(area.Permissions, verts, name, areaType));
                }
            }
        }

        /// <summary>
        /// List subareas of all selected <see cref="ParkZone"/> objects.
        /// </summary>
        /// <returns>List of lists of subareas.</returns>
        public List<ParkZone>[] ListAvailableAreas()
        {
            //initialize output
            List<ParkZone>[] availAreas = { null, null, null };

            //add parking lots in campus
            availAreas[0] = this.Campus.Subareas;

            //if selected Parking Lot, add Areas
            if (this.selectedAreas[0] != null)
            {
                availAreas[1] = this.selectedAreas[0].Subareas;
                
                //if selected Area, and contained spaces
                if (this.selectedAreas[1] != null)
                {
                    availAreas[2] = this.selectedAreas[1].Subareas;
                }
            }

            return availAreas;
        }

        /// <summary>
        /// Select <see cref="ParkZone"/> area for later processing (ex. subarea creation, area updating). 
        /// Three levels of selection: 0 - Parking lot, 1 - ID area, 2 - Parking space.
        /// </summary>
        /// <param name="choiceLevel">Which level of selection to select from.</param>
        /// <param name="choiceArea">Index of subarea in level to select.</param>
        /// <returns>Selected <see cref="ParkZone"/> object.</returns>
        public ParkZone SelectArea(int choiceLevel, int choiceArea)
        {
            if (choiceLevel >= 0)
                for (int i = choiceLevel; i < 3; i++)
                    this.selectedAreas[i] = null;
            else
                this.selectedAreas = new ParkZone[] { null, null, null };
            
            List<ParkZone> subs;
            if (choiceLevel < 0)
            {
                this.level = 0;
                return this.Campus;
            }
            else if (choiceLevel == 0)
            {
                subs = this.Campus.Subareas;
            }
            else
            {
                // Select from subareas of higher level selected area.
                subs = this.selectedAreas[choiceLevel - 1].Subareas;
            }

            this.selectedAreas[choiceLevel] = subs[choiceArea];
            this.level = choiceLevel+1;

            return this.selectedAreas[choiceLevel];
        }

        /// <summary>
        /// Delete area. Remove from selected area list if selected.
        /// </summary>
        /// <param name="choiceLevel">Which level of selection to delete from.</param>
        /// <param name="choiceArea">Index of subarea in level to delete.</param>
        public void DeleteAreas(int choiceLevel, int choiceArea)
        {
            ParkZone toBeDel;
            if (choiceLevel < 1)
            {
                toBeDel = this.Campus.Subareas[choiceArea];
                this.Campus.Subareas.RemoveAt(choiceArea);
            }
            else
            {
                toBeDel = this.selectedAreas[choiceLevel - 1].Subareas[choiceArea];
                this.selectedAreas[choiceLevel - 1].Subareas.RemoveAt(choiceArea);
            }
            //search for whether the deleted area was a selected area, if so, delete all selected areas of that level or higher
            if (this.level > 0)
                if (toBeDel.Equals(this.selectedAreas[choiceLevel]))
                {
                    for (int i = choiceLevel; i < 3; i++)
                        this.selectedAreas[i] = null;
                    this.level = choiceLevel;
                }
        }

        /// <summary>
        /// Delete area. Remove from selected area list if selected.
        /// </summary>
        /// <param name="deleteThis">Area to delete.</param>
        public void DeleteAreas(ParkZone deleteThis)
        {
            this.selectedAreas[1].Subareas.Remove(deleteThis);

            if (deleteThis.Equals(this.selectedAreas[2]))
            {
                this.selectedAreas[2] = null;
                this.level = 2;
            }

            //search for whether the deleted area was a selected area, if so, delete all selected areas of that level or higher
            if (this.level > 0)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (this.selectedAreas[i] != null && deleteThis.Equals(this.selectedAreas[i]))
                    {
                        for (int j = i; j < 3; j++)
                            this.selectedAreas[j] = null;
                        this.level = i;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Update parameters of the selected area.
        /// </summary>
        /// <param name="nombre">New name for selected area.</param>
        /// <param name="vert">New vertex list for selected area.</param>
        /// <param name="newPerm">New permission time slot to add to selected area.</param>
        /// <returns>Updated area. If none selected, returns null.</returns>
        public ParkZone UpdateAreas(string nombre, double[] vert, PermSlot newPerm)
        {
            ParkZone curr;
            if (this.level < 1)
            {
                return null;
            }
            else
            {
                curr = this.selectedAreas[this.level - 1]; //set curr to the currently selected area
            }
            if (curr == null)
                return null;

            curr.Name = nombre;
            curr.Vertices = vert;
            PermManager.AddParkingPerm(curr, newPerm);

            if (this.level == 1)
                this.Campus.Subareas.Sort();
            else if (this.level > 1)
                this.selectedAreas[this.level - 2].Subareas.Sort();

            return curr;
        }

        /// <summary>
        /// Add a permission to the selected area.
        /// </summary>
        /// <param name="newPerm">New permission time slot to add to selected area.</param>
        /// <returns>Updated area. If none selected, returns null.</returns>
        public ParkZone UpdateAreaPerms(PermSlot newPerm)
        {
            ParkZone curr;
            if (this.level < 1)
            {
                return null;
            }
            else
                curr = this.selectedAreas[this.level - 1];
            if (curr == null)
                return null; 

            PermManager.AddParkingPerm(curr, newPerm);

            return curr;
        }

        #endregion

        #region Localization and Permissions
        /// <summary>
        /// Given a tag, finds its location and determines validity.
        /// </summary>
        /// <param name="point">Location of the tag. In form: {latitude, longitude} in degrees.</param>
        /// <param name="tag">Tag to validate.</param>
        /// <returns>Whether successfully localized and validated tag.</returns>
        public bool LocalizeTag(double[] point, Tag tag)
        {
            ParkZone space = LocalizePoint(point); //find space the tag is in
            if (space == null)
                return false;
            space.TagWithin = tag;
            Enforce.DetermineValidity(space, this.Day,this.Avail_perms); //determine if valid
            return true;
        }

        /// <summary>
        /// Finds the parking spot (if any), which contains a given coordinate.
        /// </summary>
        /// <param name="point">Point to localize to a parking spot. In form: {latitude, longitude} in degrees.</param>
        /// <returns>The parking spot containing the point <paramref name="point"/> or null if none found.</returns>
        public ParkZone LocalizePoint(double[] point)
        {
            //determine parking lot containing the point
            if (this.selectedAreas[0] == null || !IsWithin(this.selectedAreas[0], point))
            {
                this.selectedAreas = new ParkZone[] { null, null, null };
                this.SearchSubArea(this.Campus.Subareas, point, 0);
            }
            if (this.selectedAreas[0] == null) //No parking lot contains the point.
                return null;

            //determine Area containing the point
            if (this.selectedAreas[1] == null || !IsWithin(this.selectedAreas[1], point))
            {
                this.selectedAreas[1] = null;
                this.selectedAreas[2] = null;
                this.SearchSubArea(this.selectedAreas[0].Subareas, point, 1);
            }
            if (this.selectedAreas[1] == null) //No ID area contains the point.
                return null;

            //determine parking spot containing the point and mark w/ tag
            if (this.selectedAreas[2] == null || !IsWithin(this.selectedAreas[2], point))
            {
                this.selectedAreas[2] = null;
                this.SearchSubArea(this.selectedAreas[1].Subareas, point, 2);
            }
            return this.selectedAreas[2];
        }

        /// <summary>
        /// Finds the ID area (if any), which contains a given coordinate.
        /// </summary>
        /// <param name="point">Point to localize to an ID area. In form: {latitude, longitude} in degrees.</param>
        /// <returns>The ID area containing the point <paramref name="point"/> or null if none found.</returns>
        public ParkZone LocalizePointToArea(double[] point)
        {
            //determine parking lot containing the point
            if (this.selectedAreas[0] == null || !IsWithin(this.selectedAreas[0], point))
            {
                this.selectedAreas = new ParkZone[] { null, null, null };
                this.SearchSubArea(this.Campus.Subareas, point, 0);
            }
            if (this.selectedAreas[0] == null) //No parking lot contains the point.
                return null;

            //determine Area containing the point
            if (this.selectedAreas[1] == null || !IsWithin(this.selectedAreas[1], point))
            {
                this.selectedAreas[1] = null;
                this.selectedAreas[2] = null;
                this.SearchSubArea(this.selectedAreas[0].Subareas, point, 1);
            }
            return this.selectedAreas[1];
        }

        /// <summary>
        /// Determines if a given point is within a specified area.
        /// </summary>
        /// <param name="area">Area to check if contains <paramref name="point"/>.</param>
        /// <param name="point">Point to check if is inside <paramref name="area"/>. In form: {latitude, longitude} in degrees.</param>
        /// <returns>Whether <paramref name="area"/> contains the point <paramref name="point"/>.</returns>
        public static bool IsWithin(ParkZone area, double[] point)
        {
            //generate vectors for the chosen point, a point outside the parking lot, and the currently selected boundary
            Vector target = new Vector(point[1],point[0]);
            Vector outside = new Vector(91, 181); // Point at "near infinite" distance.
            Vector vert1 = new Vector(area.Vertices[area.Vertices.Length - 1], area.Vertices[area.Vertices.Length - 2]);
            Vector vert2 = new Vector();

            //count the number of boundary crosses
            int crosses = 0;
            for (int i = 0; i < area.Vertices.Length/2; i++)
            {
                vert2.X = area.Vertices[2 * i + 1];
                vert2.Y = area.Vertices[2 * i];

                //determine if lines cross
                if (Utilities.LineSegmentsIntersect(target, outside, vert1, vert2, out Vector intersection))
                    crosses++;

                vert1.X = vert2.X;
                vert1.Y = vert2.Y;
            }

            // if odd, the point is inside the polygon
            return (crosses % 2) == 1;
        }

        /// <summary>
        /// Search list of subareas to determine if they contain a point, assuming parent area contains the point. Selects the subarea containing the point.
        /// </summary>
        /// <param name="subs">List of subareas to check whether contain <paramref name="point"/>.</param>
        /// <param name="point">Point to localize. In form: {latitude, longitude} in degrees.</param>
        /// <param name="level">Level of parking data structure currently searching.</param>
        private void SearchSubArea(List<ParkZone> subs, double[] point, int level)
        {
            for (int i = 0; i < subs.Count; i++)
                if (IsWithin(subs[i], point))
                {
                    this.SelectArea(level, i);                    
                    break;
                }
        }

        #endregion

        /// <summary>
        /// Saves the map to XML file. Saves in Alphascan/Maps directory.
        /// </summary>
        /// <param name="filename">Name of the file to save the map to. Name only, not full path.</param>
        public void SaveMap(string filename)
        {
            // Determine full file path.
            string programDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");
            if (!Directory.Exists(Path.Combine(programDirectory, "Maps")))
            {
                Directory.CreateDirectory(Path.Combine(programDirectory, "Maps"));
            }
            filename = Path.Combine(programDirectory, "Maps", filename);

            XmlSerializer serializer = new XmlSerializer(typeof(ParkZone));

            // Serialize campus and save to file.
            TextWriter writer = new StreamWriter(filename);
            serializer.Serialize(writer, this.Campus);
            writer.Close();
        }

        /// <summary>
        /// Loads saved map from XML file into the workspace.
        /// </summary>
        /// <param name="filename">Name of map file to load. Name only, not full path.</param>
        public void LoadMap(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ParkZone));
            
            serializer.UnknownNode += new  XmlNodeEventHandler(Serializer_UnknownNode);
            serializer.UnknownAttribute += new  XmlAttributeEventHandler(Serializer_UnknownAttribute);

            // Read and deserialize map file.
            FileStream fs = new FileStream(filename, FileMode.Open);
            this.Campus = (ParkZone)serializer.Deserialize(fs);
            fs.Close();
        }

        /// <summary>
        /// Prints information about each parking area in the campus to the debug console.  Used only for viewing  debugging
        /// </summary>
        public void PrintAll()
        {
            Debug.WriteLine(">>>>>>>> Parking Lots <<<<<<<<<");
            foreach (ParkZone park in this.Campus.Subareas)
            {
                park.Printer();
                Debug.WriteLine(">>>>>>>>> Areas <<<<<<<<<<");
                if (park.Subareas != null)
                    foreach (ParkZone area in park.Subareas)
                    {
                        area.Printer();
                        Debug.WriteLine(">>>>>>>> Spaces <<<<<<<<<");
                        if (area.Subareas != null)
                            foreach (ParkZone space in area.Subareas)
                                space.Printer();
                        Debug.WriteLine("<<<<<<<<<<<<>>>>>>>>>>>>>");
                    }
                Debug.WriteLine("<<<<<<<<<<<<|>>>>>>>>>>>>>");
            }

            Debug.WriteLine("<<<<<<<<<<<< End >>>>>>>>>>>>>>>");
        }

        /// <summary>
        /// Event handler for XML deserialization. Raised when an unknown node is found. Outputs info to the console.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Serializer_UnknownNode(object sender, XmlNodeEventArgs e)
        {
            Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
        }

        /// <summary>
        /// Event handler for XML deserialization. Raised when an unknown attribute is found. Outputs info to the console.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Serializer_UnknownAttribute(object sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            Console.WriteLine("Unknown attribute " +
            attr.Name + "='" + attr.Value + "'");
        }
    }
}