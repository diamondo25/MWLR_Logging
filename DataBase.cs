﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;
using MySql.Data.MySqlClient.Properties;
using MySql.Data.Types;

using System.Timers;

namespace MWLR_Logging
{
    public class DataBase
    {
        private static bool ENABLE_DATABASE = false;
        public static string ConnectionString { get; private set; }

        private static MySqlConnection _connection;

        private static string _preparedInsertQuery = START_STRING;
        private const string START_STRING = "INSERT DELAYED INTO log VALUES ";

        public static void InitializeDataBase()
        {
            ENABLE_DATABASE = !System.IO.File.Exists("testing.txt");
            Logger.WriteLine("Using database: {0}", ENABLE_DATABASE.ToString());
            Connect(Config.Instance.ConnectionData.Username, Config.Instance.ConnectionData.Password, Config.Instance.ConnectionData.Database, Config.Instance.ConnectionData.Host);
        }
        public static void Connect(string pUsername, string pPassword, string pDatabase, string pHost, int pPort = 3306)
        {
            if (!ENABLE_DATABASE) return;
            ConnectionString = string.Format("Server={0};Database={1};Username={2};Password={3};Pooling=true;Min Pool Size=4;Max Pool Size=32;Port={4};CharSet=utf8;", pHost, pDatabase, pUsername, pPassword, pPort);
            Connect();
        }

        private static void Connect()
        {
            Logger.WriteLine("Connecting to database...");
            _connection = new MySqlConnection(ConnectionString);
            _connection.StateChange += new System.Data.StateChangeEventHandler(mConnection_StateChange);
            _connection.Open();
        }

        static void mConnection_StateChange(object sender, System.Data.StateChangeEventArgs e)
        {
            if (e.CurrentState == System.Data.ConnectionState.Open)
            {
                Logger.WriteLine("Connected to MySQL server! Changing timestamp to UTC");

                using (MySqlCommand cmd = new MySqlCommand("SET time_zone = '+00:00';", _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateWorldData(int pID, string pName, int pChannels, int pRibbon, string pMessage)
        {
            string query = string.Format("INSERT INTO world_data VALUES ({0}, '{1}', {2}, {4}, '{3}') ON DUPLICATE KEY UPDATE world_name = '{1}', channels = {2}, message = '{3}', `state` = {4};\r\n", pID, MySqlHelper.EscapeString(pName), pChannels, MySqlHelper.EscapeString(pMessage), pRibbon + 1);
            if (!ENABLE_DATABASE) return;
            pRibbon++;
            using (MySqlCommand cmd = new MySqlCommand(query, _connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public static void SaveChannels(List<Channel> pChannels)
        {
            if (!ENABLE_DATABASE) return;
            foreach (Channel channel in pChannels)
            {
                _preparedInsertQuery += "(" + channel.World + "," + channel.ID + "," + channel.Population + ", '" + Program.CurrentDate + "'),";
                //addition += string.Format("({0},{1},{2},'{3}'),", channel.World, channel.ID, channel.Population, Program.mCurrentDate);
            }
        }

        public static void Clear()
        {
            _preparedInsertQuery = START_STRING;
        }

        public static void SaveLawl()
        {
            if (!ENABLE_DATABASE) return;
            try
            {
                if (_preparedInsertQuery.EndsWith(","))
                    _preparedInsertQuery = _preparedInsertQuery.Remove(_preparedInsertQuery.Length - 1, 1);
                if (!_preparedInsertQuery.StartsWith(START_STRING))
                    _preparedInsertQuery = START_STRING + _preparedInsertQuery;

                using (MySqlCommand cmd = new MySqlCommand(_preparedInsertQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex.ToString());
                Logger.WriteLine("Exception while saving data: {0}", ex.ToString());
                if (ex.ToString().Contains("valid and open"))
                {
                    // MySQL error. Reconnect.
                    Connect();
                    
                }
            }
            Clear();
        }

        public static void AddCrash(byte world, byte channel, int load1, int load2)
        {
            if (!ENABLE_DATABASE) return;
            try
            {
                using (MySqlCommand cmd = new MySqlCommand("INSERT INTO crashes VALUES ('" + Program.CurrentDate + "'," + world + "," + channel + "," + load1 + "," + load2 + ")", _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex.ToString());
                Logger.WriteLine("Exception while saving data: {0}", ex.ToString());
            }
        }

        public static void CreateTable()
        {
            if (!ENABLE_DATABASE) return;
            string query = @"
CREATE TABLE `crashes` (
  `log` datetime NOT NULL,
  `worldid` tinyint(3) unsigned NOT NULL,
  `channelid` tinyint(4) NOT NULL,
  `loadfirst` smallint(6) NOT NULL,
  `loadlast` smallint(6) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `log` (
  `world_id` tinyint(2) unsigned NOT NULL,
  `channel_id` tinyint(2) unsigned NOT NULL,
  `current_load` mediumint(5) unsigned NOT NULL,
  `log_date` datetime NOT NULL,
  KEY `NewIndex1` (`channel_id`),
  KEY `NewIndex2` (`world_id`),
  KEY `NewIndex3` (`current_load`),
  KEY `NewIndex4` (`log_date`)
) ENGINE=MyISAM DEFAULT CHARSET=latin1;

CREATE TABLE `world_data` (
  `world_id` tinyint(2) unsigned NOT NULL,
  `world_name` varchar(12) NOT NULL,
  `channels` tinyint(2) unsigned NOT NULL,
  `state` enum('None','Event','New','Hot') NOT NULL DEFAULT 'None',
  `message` text NOT NULL,
  PRIMARY KEY (`world_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
";

            MySqlCommand cmd = new MySqlCommand(query, _connection);
            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }

    }
}
