using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ParkDS
{
    /// <summary>
    /// Object for managing the list of possible permissions. Also provides more functionality for permissions.
    /// </summary>
    public class PermManager
    {
        /// <summary>
        /// List of available permissions for parking zones.
        /// </summary>
        private List<Permission> availPermissions;

        /// <summary>
        /// Creates default permission manager.
        /// </summary>
        public PermManager()
        {
            availPermissions = new List<Permission>();
        }

        /// <summary>
        /// Update permissions of parking areas. Updates any subareas that have the same permissions as <paramref name="area"/>.
        /// </summary>
        /// <param name="area">The area to update permissions for.</param>
        /// <param name="perm">New permissions list to use in this area.</param>
        public static void UpdateParkPerms(ParkZone area, List<PermSlot> perm)
        {
            //generate list of permissions to add to the ParkZone and record former permission list
            List<PermSlot> finalList = Utilities.GeneratePermissionList(perm);
            List<PermSlot> formerPerm = area.Permissions;
            
            //set parkzone permissions to new list
            area.Permissions = finalList;

            //loop through subareas, replacing any permission lists matching the parent's
            if (area.Subareas != null)
                foreach (ParkZone p in area.Subareas)
                {
                    if (ListEqual(p.Permissions, formerPerm))
                        p.Permissions = finalList;
                }
        }

        /// <summary>
        /// Edits a permission in the list of possible permissions.
        /// </summary>
        /// <param name="oldName">Name of permission to update.</param>
        /// <param name="newName">New name of permission.</param>
        /// <param name="newKeys">New key values.</param>
        /// <param name="addPermits">New additional permits.</param>
        /// <param name="parent"></param>
        public void EditPermissionList(string oldName, string newName, string[] newKeys, List<string> addPermits)
        {
            Permission toEdit = SearchPerm(oldName, 0, this.availPermissions.Count - 1);

            toEdit.Keys = newKeys;
            toEdit.Name = newName;
            toEdit.AddPermits = new List<string>();
            foreach (string s in addPermits)
                toEdit.AddPermits.Add(s);
        }

        /// <summary>
        /// Add a parking permission to an area and all its subareas.
        /// </summary>
        /// <param name="area">Area to add permission to.</param>
        /// <param name="newPerm">Permission time slot to add.</param>
        public static void AddParkingPerm(ParkZone area, PermSlot newPerm)
        {
            List<PermSlot> newPermList = new List<PermSlot>();
            DateTime newStart = newPerm.ValidTimes[0];
            DateTime newEnd = newPerm.ValidTimes[1];

            // Add default permission if none exist.
            if (area.Permissions.Count == 0)
                area.Permissions = new List<PermSlot> { new PermSlot("NPR", new DateTime[] { new DateTime(DateTime.Now.Hour,DateTime.Now.Month,DateTime.Now.Day,0,0,0)
                    , new DateTime(DateTime.Now.Hour,DateTime.Now.Month,DateTime.Now.Day,23,0,0) }) }; //$$$ Is this supposed to be Now.Hour or Now.Year?

            // Add permissions to the area.
            foreach (PermSlot perm in area.Permissions)
            {
                if (newStart >= perm.ValidTimes[0] && newEnd <= perm.ValidTimes[1])
                {
                    if (perm.Name.Equals("NPR"))
                        newPermList.Add(newPerm);
                    else
                    {
                        PermManager.UpdateParkPerms(area, new List<PermSlot> { newPerm });
                        return;
                    }
                }
                else if(newStart < perm.ValidTimes[1] && newEnd >= perm.ValidTimes[1])
                {
                    PermManager.UpdateParkPerms(area, new List<PermSlot> { newPerm });
                    return;
                }
                else
                {
                    newPermList.Add(perm);
                }
            }

            PermManager.UpdateParkPerms(area, newPermList);
        }

        /// <summary>
        /// Determine if two lists of permissions are equivalent.
        /// </summary>
        /// <param name="first">First list to compare.</param>
        /// <param name="second">Second list to compare.</param>
        /// <returns>Whether the two lists are equivalent.</returns>
        private static bool ListEqual(List<PermSlot> first, List<PermSlot> second)
        {
            //lists should be same size
            if(first.Count == second.Count)
            {
                //loop though all permslots, if any info doesn't match, retunr false
                for(int i = 0; i < first.Count; i++)
                {
                    if (!(first[i].Name.Equals(second[i].Name) && first[i].ValidTimes[0] == second[i].ValidTimes[0] && first[i].ValidTimes[1] == second[i].ValidTimes[1]))
                        return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Find a permission by name in the list of possible permissions.
        /// </summary>
        /// <param name="perm">Name of the permission to find.</param>
        /// <returns>The permission object if found. Otherwise returns null.</returns>
        public Permission FindPerm(string perm)
        {
            return this.SearchPerm(perm, 0, this.availPermissions.Count-1);
        }

        /// <summary>
        /// Search the list of permissions by name, recursively searching smaller lists until the correct name is found.
        /// </summary>
        /// <param name="perm">Name of the permissions to find.</param>
        /// <param name="start">Start index in list to search.</param>
        /// <param name="last">End index in list to search.</param>
        /// <returns>The permission object with a matching name. Returns null if not in list.</returns>
        private Permission SearchPerm(string perm, int start, int last)
        {
            if (start > last) return null; // Null if not in list.

            int mid = (start + last) / 2;

            if (this.availPermissions[mid].Name.CompareTo(perm) < 0) // Too low
            {
                return SearchPerm(perm, mid + 1, last);
            }
            else if (this.availPermissions[mid].Name.CompareTo(perm) > 0) // Too high
            {
                return SearchPerm(perm, start, mid - 1);
            }
            else // Found name.
            {
                return this.availPermissions[mid];
            }
        }
        
        /// <summary>
        /// Return current permission list.
        /// </summary>
        /// <returns>A copy of the permission list.</returns>
        public List<Permission> GetAvailablePerms()
        {
            return this.availPermissions;
            //return new List<Permission>(this.availPermissions);
        }

        /// <summary>
        /// Add a permission to the list.
        /// </summary>
        /// <param name="name">Name of permission to add.</param>
        /// <param name="key">Range of RFID tag IDs that are valid. In form: {Lowest ID, Highest ID}.</param>
        /// <param name="additionalPermits"></param>
        public void AddPermission(string name, string[] key, List<string> additionalPermits)
        {
            string[] keysCopy = new string[2];
            for(int i = 0;i<2 ; i++)
            {
                keysCopy[i] = key[i];
            }

            this.availPermissions.Add(new Permission(name, keysCopy, additionalPermits));
            this.availPermissions.Sort();
        }

        /// <summary>
        /// Print data about each of the permissions in the list.
        /// </summary>
        public void PrintAll()
        {
            foreach (Permission p in this.availPermissions)
                p.Print();
        }

        /// <summary>
        /// Saves the list of permissions to an XML file.
        /// </summary>
        /// <param name="filename">Name of the file to save to. Not the full path.</param>
        /// <param name="isFileEdited">Whether the file already exists. If true, does not generate new file name.</param>
        public void SavePerm(string filename, bool isFileEdited = false)
        {
            // Determine full file name.
            string programDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Alphascan");
            if (!Directory.Exists(Path.Combine(programDirectory, "Permissions")))
            {
                Directory.CreateDirectory(Path.Combine(programDirectory, "Permissions"));
            }
            filename = Path.Combine(programDirectory, "Permissions", filename);

            // Create XML serializer.
            XmlSerializer serializer = new XmlSerializer(typeof(List<Permission>));

            string permFile;
            if (!isFileEdited)
            {
                // Add "_perm" before file extension.
                int index = filename.IndexOf(".");
                permFile = filename.Substring(0, index) + "_perm" + filename.Substring(index);
            }
            else
                permFile = filename;

            // Serialize list and write to file.
            TextWriter writer = new StreamWriter(permFile);
            serializer.Serialize(writer, this.availPermissions);
            writer.Close();
        }

        /// <summary>
        /// Loads saved permissions from an XML file into the workspace.
        /// </summary>
        /// <param name="filename">Name of the file to read permissions from.</param>
        public void LoadPerm(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<Permission>));
            
            serializer.UnknownNode += new XmlNodeEventHandler(Serializer_UnknownNode);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(Serializer_UnknownAttribute);

            FileStream fs = new FileStream(filename, FileMode.Open);
            this.availPermissions = (List<Permission>)serializer.Deserialize(fs);
            fs.Close();
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

        /// <summary>
        /// Removes all permissions from the list of possible permissions.
        /// </summary>
        public void RemoveAllPerms()
        {
            this.availPermissions.Clear();
        }
    }
}
