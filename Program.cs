using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.IO;
using System.Net;
using WvsBeta.Common.Sessions;

namespace MWLR_Logging
{
    class Program
    {
        public static string CurrentDate { get; set; }
        public static long CurrentTime { get; set; }
        public static long TweetCurrentTime { get; set; }
        public static int mTotalOnline { get; set; }
        public static byte mIlol { get; set; }
        public static bool IgnoreDataTemp { get; set; }
        public static bool CRASHMODE = true;

        public static MapleConnection Connection { get; private set; }

        public static void UpdateDate()
        {
            DateTime now = DateTime.Now;

            Program.CurrentDate = now.ToString("yyyy-MM-dd HH:mm:ss");
            TimeSpan span = (now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());

            //return the total seconds (which is a UNIX timestamp)
            Program.CurrentTime = (long)span.TotalSeconds;
            Program.TweetCurrentTime = Program.CurrentTime - (60 * 60); // 1 hour diff
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
                                pw.WriteHexString(string.Join(" ", lineargs, 1, lineargs.Length - 1));
                                Connection.SendPacket(pw);
                            }
                            break;
                        }
                    default: Logger.WriteLine("Unknown command. Use help. Command: {0}", line); break;
                }
            }
        }



        public static string Login(string pUsername, string pPassword)
        {
            string key = "";
            string input = "userID=" + pUsername + "&password=" + pPassword;
            byte[] content = Encoding.ASCII.GetBytes(input);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://www.nexon.net/api/v001/account/login");
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.UserAgent = "MWLR/1.0.0 by CraftNet";
            req.ContentLength = content.Length;

            req.GetRequestStream().Write(content, 0, content.Length);
            try
            {
                using (var response = (HttpWebResponse)req.GetResponse())
                {
                    if (response != null)
                    {
                        string herp = response.Headers["Set-Cookie"].ToString();
                        int len = herp.IndexOf("NPPv2=") + "NPPv2=".Length;
                        herp = herp.Substring(len);
                        herp = herp.Substring(0, herp.IndexOf(';', len));
                        key = herp;
                    }
                    else
                    {
                        Logger.WriteLine("Invalid credentials, I guess..");
                    }
                }
            }
            catch (System.Net.WebException ex)
            {
                StreamReader sr = new StreamReader(ex.Response.GetResponseStream());
                System.IO.File.WriteAllText("err.txt", sr.ReadToEnd());
                Logger.WriteLine("!!!!!! Invalid credentials!", ex.ToString());

            }
            return key;
        }
    }
}