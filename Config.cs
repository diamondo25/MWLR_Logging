using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using System.Xml;
using System.Xml.Serialization;

namespace MWLR_Logging
{
    public sealed class Config
    {

        internal static Config Instance { get; private set; }
        internal static bool CreateTable = false;
        internal static string ConfigName = "Config.xml";

        internal static void Load()
        {
            ConfigName = Environment.CurrentDirectory + Path.DirectorySeparatorChar + ConfigName;
            Logger.WriteLine("Loading configuration...");
            if (!File.Exists(ConfigName))
            {
                Logger.WriteLine("{0} doesn't exist... Making one lol", ConfigName);
                Instance = new Config();
                Instance.ConnectionData.Database = "mwlr";
                Instance.ConnectionData.Host = "127.0.0.1";
                Instance.ConnectionData.Port = 3306;
                Instance.ConnectionData.Username = "root";
                Instance.ConnectionData.Password = "";
                Instance.MapleStoryServerIP = "63.251.217.2";
                Instance.MapleStoryServerPort = 8484;
                Save();
                CreateTable = true;
            }
            else
            {
                using (XmlReader reader = XmlReader.Create(ConfigName)) Instance = (Config)(new XmlSerializer(typeof(Config))).Deserialize(reader);
                if (Instance.SkipWorlds == null)
                {
                    Instance.SkipWorlds = new List<byte>();
                    Environment.Exit(1);
                }
                if (Instance.SkipWorlds.Count == 0)
                    Instance.SkipWorlds.Add(133);
                Save();
                Logger.WriteLine("Loaded!");
            }
        }

        internal static void Save()
        {
            XmlWriterSettings xws = new XmlWriterSettings();
            xws.NewLineOnAttributes = true;
            xws.Indent = true;
            xws.IndentChars = "\t";
            new XmlSerializer(typeof(Config)).Serialize(XmlWriter.Create(ConfigName, xws), Instance);
        }

        public string MapleStoryServerIP { get; set; }
        public ushort MapleStoryServerPort { get; set; }
        public bool CRASHMODE_ENABLE { get; set; }
        public List<byte> SkipWorlds { get; set; }

        public Connection ConnectionData = new Connection();

        public sealed class Connection
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Database { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
        }


    }
}