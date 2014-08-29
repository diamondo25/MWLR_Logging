using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using WvsBeta.Common.Sessions;
using MWLR_Logging.MapleStories;

namespace MWLR_Logging
{
    public class Channel
    {
        public String ChannelName { get; set; }
        public int Population { get; set; }
        public byte World { get; set; }
        public byte ID { get; set; }
        public byte SpecialValue { get; set; }
    };

    public enum MapleState
    {
        Login,
        LoggingIn,
        LoginIncorrectPin,
        LoginIncorrectCredentials,
        RequestWorld,
        WorldSelect,
        CharSelect
    }

    public class MapleServerConnector : AbstractConnection
    {
        public bool connected = false;
        public bool lostConnection = false;
        AbstractMaple MapleClient;
        public MapleState State = MapleState.Login;

        public MapleServerConnector(string pIP, ushort pPort)
            : base(pIP, pPort)
        {
        }

        public override void AC_OnPacketInbound(Packet pPacket)
        {
            if (pPacket.Length > 0)
                MapleClient.handlePackets(pPacket);
            else
                Logger.WriteLine("[WARNING] Got packet with length 0!");
        }

        public void Disconnect(bool shutdown)
        {
            Logger.WriteLine("Disconnecting from server... ");
            lostConnection = false;
            connected = false;
            if (!Disconnected)
            {
                Disconnect();
            }
        }

        public override void OnDisconnect()
        {
            Logger.WriteLine("MapleServerConnector disconnected from server!");

            if (connected)
            {
                lostConnection = true;
                connected = false;
                lastDisconnect = DateTime.Now;
            }
        }

        int oldVersion = 0, oldLocale = 0;
        string oldPatchLocation = "XD";
        public override void OnHandshakeInbound(Packet pPacket)
        {
            Logger.WriteLine("Received HandShake packet");
            Program.UpdateDate();

            short version = (short)MapleVersion;
            string patchLocation = MaplePatchLocation;
            byte locale = MapleLocale;

            if (oldPatchLocation == "XD" || (oldVersion == version && oldPatchLocation == patchLocation && oldLocale == locale))
            {
                TwitterClient.Instance.SendMessage("Connected with the {0} server. Version: V{1}.{2}", GetMapleStoryLocale(locale), version, patchLocation);
            }
            else
            {
                TwitterClient.Instance.SendMessage("Connected with the {0} server, and it seems to be updated! From V{1}.{2} to V{3}.{4} #MapleStory", GetMapleStoryLocale(locale), oldVersion, oldPatchLocation, version, patchLocation);
            }


            oldVersion = version;
            oldPatchLocation = patchLocation;
            oldLocale = locale;

            Logger.WriteLine("Version: {0}; Patch location: '{1}'; Locale: {2}", oldVersion, oldPatchLocation, oldLocale);

            switch (locale)
            {
                case 1: MapleClient = new MapleKorea(this, version, short.Parse(patchLocation)); break;
                case 7: MapleClient = new MapleSEA(this, version, short.Parse(patchLocation)); break;
                case 8:
                    GMSKeys.Initialize(); 
                    MapleClient = new MapleGlobal(this, version, short.Parse(patchLocation));
                    break;

            }
            

            State = MapleState.WorldSelect;
            Logger.WriteLine("MapleClient: {0}", MapleClient.ToString());
            MapleClient.HandlingVersion = version;
            MapleClient.sendClientReady();

            Logger.WriteLine("Done");
            MapleClient.sendWorldListReRequest();
            connected = true;

            if (lostConnection && lastDisconnect != DateTime.MinValue && (DateTime.Now - lastDisconnect).TotalMinutes > 5)
            {
                TwitterClient.Instance.SendMessage("{0} downtime: {1} minutes", GetMapleStoryLocale(locale), (DateTime.Now - lastDisconnect).TotalMinutes);
            }
            lastDisconnect = DateTime.MinValue;

            lostConnection = false;
        }

        DateTime lastDisconnect = DateTime.MinValue;
        bool didTweet = false;
        public void requestWorldList()
        {
            if (lostConnection)
            {
                Logger.WriteLine("We are (still) disconnected (requestWorldList())");
                if (!didTweet)
                {
                    TwitterClient.Instance.SendMessage("Disconnected from the server! Trying to reconnect.");
                    didTweet = true;
                }


                // Try connect.
                letsConnect();
            }
            else if (State == MapleState.WorldSelect && MapleClient != null && !Disconnected)
            {
                MapleClient.sendWorldListReRequest();
            }
        }

        public void letsConnect()
        {
            try
            {
                Connect();
                if (lostConnection)
                {
                    Logger.WriteLine("Done!");
                }
                didTweet = false;
                connected = true;
                lostConnection = false;
                Logger.WriteLine("Connected with MapleStory Server at " + base.IP + ":" + base.Port + "! Waiting for handshake packet..." + (lastDisconnect != DateTime.MinValue ? (DateTime.Now - lastDisconnect).TotalMinutes + " minutes downtime!" : ""));

            }
            catch (Exception ex)
            {
                Logger.WriteLine("Failed to connect: {0}", ex.Message);
            }
        }

        public static string GetMapleStoryLocale(byte type)
        {
            switch (type)
            {
                case 0x01: return "MapleStory Korea";
                case 0x02: return "MapleStory Korea Test";
                case 0x03: return "MapleStory Japan";
                case 0x05: return "Maplestory China/Global Test";
                case 0x06: return "MapleStory Taiwan";
                //case 0x07: return "MapleStory East Asia or Thailand";
                case 0x07: return "MapleStory SEA";
                case 0x08: return "MapleStory Global";
                case 0x09: return "MapleStory Europe or Brazil";
                default: return "Unknown Maplestory type (please send Diamondo25 the version name and type)";
            }

        }
    }
}