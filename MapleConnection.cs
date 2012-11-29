using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;

using WvsBeta.Common.Sessions;

namespace MWLR_Logging
{
    public class MapleConnection
    {

        private MapleServerConnector Connector;
        private Timer RefreshTimer;
        public int AmountOnline { get; set; }

        public MapleConnection(double msecs)
        {
            Connector = new MapleServerConnector(Config.Instance.MapleStoryServerIP, Config.Instance.MapleStoryServerPort);
            Connect();
            RefreshTimer = new Timer(msecs);
            RefreshTimer.Elapsed += (a, b) => ForceRequest();
            RefreshTimer.Start();
        }

        public void ForceRequest()
        {
            if (Connector == null) return;
            AmountOnline = 0;
            try
            {
                Program.UpdateDate();
                Connector.requestWorldList();
            }
            catch (Exception ex)
            {
                // Try reconnect after displaying error
                Logger.WriteLine("Couldn't retrieve data ({0}). Reconnecting.", ex.Message);
                //Logger.ErrorLog(ex.ToString());
                Connect();
            }
        }

        public void Connect()
        {
            if (Connector == null) return;
            Logger.WriteLine("Connecting to server @ {0}:{1}...", Config.Instance.MapleStoryServerIP, Config.Instance.MapleStoryServerPort);
            AmountOnline = 0;
            Program.UpdateDate();
            Program.mIlol = 60;
            try
            {
                Connector.Connect();
            }
            catch
            {
                Logger.WriteLine("Couldn't connect!");
            }
        }

        public void Disconnect()
        {
            RefreshTimer.Stop();
            Connector.Disconnect(false);
        }

        public void SendPacket(Packet pPacket)
        {
            if (Connector.connected)
            {
                Connector.SendPacket(pPacket);
            }
        }

    }
}
