using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common.Sessions;

namespace MWLR_Logging.MapleStories
{
    class MapleSEA : AbstractMaple
    {
        private short version, subversion;
        private Session _session;

        public MapleSEA(Session pSession, short _v, short _sv)
            : base("SEA", 7)
        {
            _session = pSession;
            this.RequiresVersion = -1;
            version = _v;
            subversion = _sv;
            Program.mTotalOnline = 0;
            Program.Connection.AmountOnline = 0;
        }

        private void SendCheckedPacket(Packet pPacket)
        {
            while (true)
            {
                ushort idx = BitConverter.ToUInt16(_session.EncryptIV, 0);
                if ((idx % 31) == 0)
                    sendHSKey(CRC32.calcCrc32(_session.EncryptIV, 2));
                else
                    break;
            }
            _session.SendPacket(pPacket);
        }

        public override void handlePackets(Packet packet)
        {
            //Logger.WriteLine("Got packet: {0}", packet.ToString());
            switch (packet.ReadShort())
            {
                case 0x06: handleWorld(packet); break; // 0x0A
                case 0x0F: sendPong(); break;
            }
        }

        bool gotData = Program.CRASHMODE;//false;
        int iworlds = 0;
        int ichannels = 0;
        public void handleWorld(Packet packet)
        {
            byte ID = packet.ReadByte();
            if (ID == 0xFF)
            {
                HandleData(null);
                // Logger.WriteLine("Worlds: {0}; Channels: {1}; Channel load avg: {2}", iworlds, ichannels, (double)Program.mTotalOnline / (double)ichannels);
                gotData = true;
                iworlds = ichannels = 0;
                return;
            }
            iworlds++;

            string name = packet.ReadString();
            byte ribbon = packet.ReadByte(); // Ribbon
            string eventMsg = packet.ReadString(); // Event message
            packet.ReadShort(); // EXP rate
            packet.ReadShort(); // DROP rate

            int channels = packet.ReadByte();
            ichannels += channels;
            if (!gotData)
                DataBase.UpdateWorldData(ID, name, channels, ribbon, eventMsg);

            if (Config.Instance.SkipWorlds.Contains(ID)) return;

            List<Channel> channelList = new List<Channel>();
            Channel chan;
            for (int i = 0; i < channels; i++)
            {
                chan = new Channel();
                chan.ChannelName = packet.ReadString();
                chan.Population = packet.ReadInt();
                chan.World = packet.ReadByte();
                chan.ID = packet.ReadByte();
                chan.ID += 1;
                Program.Connection.AmountOnline += chan.Population;
                chan.SpecialValue = packet.ReadByte();
                channelList.Add(chan);

                if (OldLoads.ContainsKey(chan.ChannelName) && OldLoads[chan.ChannelName] >= 10 && (chan.Population * 100 / OldLoads[chan.ChannelName]) <= 20)
                {
                    Logger.WriteLine("{0} crashed, as it went from {1:N0} to {2:N0} people online.", chan.ChannelName, OldLoads[chan.ChannelName], chan.Population);
                    DataBase.AddCrash(chan.World, chan.ID, OldLoads[chan.ChannelName], chan.Population);
                    TweetCrash(chan);
                }

                if (chan.Population < 0)
                {
                    Logger.WriteLine("Seems like Nexon didn't fix the negative bug: Channel {0} has {1} players online.", chan.ChannelName, chan.Population);
                    chan.Population = Math.Abs(chan.Population); // Make it positive!
                }

                if (OldLoads.ContainsKey(chan.ChannelName))
                {
                    OldLoads[chan.ChannelName] = chan.Population;
                }
                else
                {
                    OldLoads.Add(chan.ChannelName, chan.Population);
                }
            }

            HandleData(channelList);
        }

        public override void sendClientReady()
        {
            sendHSInit();
            var packet = new Packet();
            if (version >= 139)
                packet.WriteShort(0x3D);
            else
                packet.WriteShort(0x3C);
            packet.WriteByte(_locale);
            packet.WriteShort(version);
            packet.WriteShort(subversion);
            SendCheckedPacket(packet);
        }

        public void sendHSInit()
        {
            var packet = new Packet();
            packet.WriteShort(0x2D);
            packet.WriteByte(1);
            packet.WriteLong(0);
            SendCheckedPacket(packet);
        }

        public void sendHSKey(int pValue)
        {
            var packet = new Packet();
            packet.WriteShort(0x2D);
            packet.WriteByte(0);
            packet.WriteInt(pValue);
            packet.WriteInt(0);
            _session.SendPacket(packet);
        }

        public override void sendWorldListReRequest()
        {
            Packet packet = new Packet();
            packet.WriteShort(0x19);
            SendCheckedPacket(packet);
        }

        public override void sendPong()
        {
            Packet packet = new Packet();
            if (version >= 139)
                packet.WriteShort(0x44);
            else
                packet.WriteShort(0x43);
            packet.WriteInt(0);
            packet.WriteShort(0);
            SendCheckedPacket(packet);
        }

    }
}
