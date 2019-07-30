using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Reflection;

namespace DatabaseVersioningTool.DataAccess
{
    public class VersionTracker<VC, V>
        where VC : IDatabaseVersionCollection<V>, new()
        where V : IDatabaseVersion, new()
    {
        public List<VC> Versions { get; private set; }

        private List<PropertyInfo> properties { get; set; }

        public static VersionTracker<VC, V> Tracker
        {
            get
            {
                if (_tracker == null)
                {
                    _tracker = new VersionTracker<VC, V>();
                }
                return _tracker;
            }
        }
        private static VersionTracker<VC, V> _tracker { get; set; }

        public VersionTracker()
        {
            Versions = new List<VC>();
        }

        public void WriteFile()
        {
            using (XmlWriter writer = XmlWriter.Create(FileManager.Manager.VersionFilePath))
            {
                writer.WriteStartElement("Versions");
                for (int x = 0; x< Versions.Count; x++)
                {
                    VC collection = Versions[x];
                    writer.WriteStartElement(collection.GetType().Name);
                    foreach (PropertyInfo info in collection.GetType().GetProperties())
                    {
                        if (info.CanRead && info.CanWrite)
                        {
                            writer.WriteAttributeString(info.Name, collection.GetType().GetProperty(info.Name).GetValue(collection, null).ToString());
                        }
                    }

                    for(int v = 0; v < collection.Versions.Count; v++)
                    {
                        V item = collection.Versions[v];
                        writer.WriteStartElement(item.GetType().Name);

                        foreach (PropertyInfo info in properties)
                        {
                            if (info.CanRead && info.CanWrite)
                            {
                                writer.WriteAttributeString(info.Name, item.GetType().GetProperty(info.Name).GetValue(item, null).ToString());
                            }
                        }    

                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
            }

        }

        public void ReadFile()
        {
            Versions = new List<VC>();

            using (XmlReader reader = XmlReader.Create(FileManager.Manager.VersionFilePath))
            {
                bool readyToAdd = false;
                VC collection = default(VC);

                while (reader.Read())
                {
                    readyToAdd = false;
                    if (reader.IsStartElement())
                    {
                        if (reader.Depth == 1)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                collection = new VC();
                                if (reader.HasAttributes)
                                {
                                    for (int x = 0; x < reader.AttributeCount; x++)
                                    {
                                        reader.MoveToAttribute(x);

                                        foreach (PropertyInfo info in collection.GetType().GetProperties())
                                        {
                                            if (info.Name == reader.Name)
                                            {
                                                if (info.CanRead && info.CanWrite)
                                                {
                                                    info.SetValue(collection, Convert.ChangeType(reader.Value, info.PropertyType), null);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (reader.Depth == 2)
                        {
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                if (reader.HasAttributes)
                                {
                                    //string version = string.Empty, from = string.Empty, to = string.Empty, path = string.Empty, release = string.Empty;
                                    V item = new V();
                                    for (int x = 0; x < reader.AttributeCount; x++)
                                    {
                                        reader.MoveToAttribute(x);

                                        foreach (PropertyInfo info in properties)
                                        {
                                            if (info.Name == reader.Name)
                                            {
                                                if (info.CanRead && info.CanWrite)
                                                {
                                                    info.SetValue(item, Convert.ChangeType(reader.Value, info.PropertyType), null);
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (collection != null)
                                    {
                                        var method = collection.GetType().GetMethod("AddVersion");
                                        method.Invoke(collection, new object[1] { item });
                                        readyToAdd = true;
                                    }
                                }
                            }
                        }

                        if (readyToAdd && collection != null)
                        {
                            Versions.Add(collection);
                            collection = default(VC);
                        }
                    }
                }
            }
        }


        public void Load()
        {
            V obj = new V();
            properties = new List<PropertyInfo>();
            properties.AddRange(obj.GetType().GetProperties());

            ReadFile();
        }

        internal VC GetDatabaseVersion(string dbName)
        {
            if(!Versions.Any(x => x.Name == dbName))
            {
                Versions.Add(new VC() { Name = dbName });
            }
            return Versions.Single(x => x.Name == dbName);
        }
    }
}
