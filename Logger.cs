using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MWLR_Logging
{
    public static class Logger
    {
        public static string Logfile;

        public static void SetLogfile()
        {
            Logfile = "LogFile" + DateTime.Now.ToString("yyyy_M_d") + ".log";
            File.Create(Logfile).Close();
        }

        public static void WriteLine(string pInput, params object[] pParams)
        {
            string text = string.Format("[{0}] {1} {2}", DateTime.Now, string.Format(pInput, pParams), Environment.NewLine);
            File.AppendAllText(Logfile, text);
            Console.Write(text);
        }

        public static void Write(string pInput, params object[] pParams)
        {
            File.AppendAllText(Logfile, string.Format(pInput, pParams));
            Console.Write(pInput, pParams);
        }

        public static void ErrorLog(string pInput, params object[] pParams)
        {
            File.AppendAllText("EXCEPTIONS.txt", string.Format("{0}[{2}]{1}{0}", Environment.NewLine, "-----------------", DateTime.Now) + string.Format(pInput, pParams) + Environment.NewLine);
            TwitterClient.Instance.SendMessage("@Diamondo25 crap");
        }
    }
}