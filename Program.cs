using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using WvsBeta.Common.Sessions;

namespace MWLR_Logging
{
    class Program
    {
        public static string mCurrentDate { get; set; }
        public static long CurrentTime { get; set; }
        public static int mTotalOnline { get; set; }
        public static byte mIlol { get; set; }
        public static bool IgnoreDataTemp { get; set; }
        public static bool CRASHMODE = true;

        public static MapleConnection Connection { get; private set; }

        public static void UpdateDate()
        {
            DateTime now = DateTime.Now;

            Program.mCurrentDate = now.ToString("yyyy-MM-dd HH:mm:ss");
            now = now.AddHours(-1);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Program.CurrentTime = (long)((now - epoch).TotalSeconds);
        }

        static void Main(string[] args)
        {
            Logger.SetLogfile();
            TwitterClient.Load();
            mIlol = 60;
            IgnoreDataTemp = false;

            if (args.Length >= 1)
            {
                Config.ConfigName = args[0];
            }
            GMSKeys.Initialize();

            Config.Load();
            try
            {
                DataBase.InitializeDataBase();
                if (Config.CreateTable)
                {
                    DataBase.CreateTable();
                    Config.CreateTable = false;
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex.ToString());
                Logger.WriteLine("Incorrect database settings. {0}", ex.ToString());
                Console.ReadLine();
                Environment.Exit(1);
            }

            CRASHMODE = Config.Instance.CRASHMODE_ENABLE;

            //Config.Save();

            Connection = new MapleConnection(CRASHMODE ? 1000 : 60000);
            while (true)
            {
                string line = Console.ReadLine();
                string[] lineargs = line.Split(' ');
                switch (lineargs[0])
                {
                    case "load": Logger.WriteLine("Current Load: {0}", Connection.AmountOnline); break;
                    case "reconnect":
                        Logger.WriteLine("Starting reconnect...");
                        Connection.Disconnect();
                        Thread.Sleep(1000);
                        Connection.Connect();
                        break;
                    case "forceload":
                        Logger.WriteLine("Forcing request...");
                        IgnoreDataTemp = true;
                        Connection.ForceRequest();
                        break;
                    case "help":
                        Console.WriteLine("load");
                        Console.WriteLine("reconnect - Reconnecting to server");
                        Console.WriteLine("forceload - Forcing load request");
                        Console.WriteLine("sp [data] - Sends a packet with [data]");
                        Console.WriteLine("tweet [text] - Tweet a message with [text]");
                        Console.WriteLine("help - This stuff");
                        break;
                    case "tweet":
                        TwitterClient.Instance.SendMessage(string.Join(" ", lineargs, 1, lineargs.Length - 1));
                        break;
                    case "stop":
                    case "exit":
                    case "bye":
                    case "close":
                        Environment.Exit(0);
                        break;
                    case "reloadkeys":
                        GMSKeys.Initialize();
                        Logger.WriteLine("Reloaded keys");
                        break;
                    case "sp":
                        {
                            if (lineargs.Length > 1)
                            {
                                Packet pw = new Packet();
                                pw.WriteHexString(string.Join(" ", lineargs));
                                Connection.SendPacket(pw);
                            }
                            break;
                        }
                    default: Logger.WriteLine("Unknown command. Use help. Command: {0}", line); break;
                }
            }
        }
    }
}