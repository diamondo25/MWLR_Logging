using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MWLR_Logging;
using WvsBeta.Common.Sessions;

namespace MWLR_Logging.MapleStories
{
    public abstract class AbstractMaple
    {
        public Dictionary<string, int> OldLoads = new Dictionary<string, int>();
        public int UsersOnline { get; set; }
        public Session Session { get; set; }
        public short HandlingVersion { get; set; }
        public short RequiresVersion { get; set; }
        public abstract void sendClientReady();
        public abstract void sendPong();
        public abstract void sendWorldListReRequest();
        public abstract void handlePackets(Packet packet);
        public string Name { get; set; }

        public AbstractMaple(string name)
        {
            Name = name;
            DataBase.Clear();
        }


        int last = 0;

        public void HandleData(List<Channel> pData)
        {

            if (pData == null)
            {
                // Last packet. so save
                if (!Program.IgnoreDataTemp)
                {
                    if (!Program.CRASHMODE)
                        DataBase.SaveLawl();
                    Program.mIlol++;
                }
                else
                {
                    Logger.WriteLine("Current Population: {0}", Program.Connection.AmountOnline);
                    
                    Program.IgnoreDataTemp = false;
                }
                if (Program.mIlol >= 10)
                {
                    int diff = 0;
                    if (last == 0)
                    {
                        diff = last = Program.Connection.AmountOnline;
                    }
                    else
                    {
                        diff = Program.Connection.AmountOnline - last;
                    }
                    var URL = string.Format("http://www.craftnet.nl/total_graph.png?from={0}", Program.CurrentTime);
                    string msg = string.Format("MapleStory Global has {0:N0} players online at the moment. ({1}) {2}", Program.Connection.AmountOnline, Math.Abs(diff).ToString("N0") + (diff < 0 ? " Left" : " Joined"), URL);

                    if (!Program.CRASHMODE)
                        TwitterClient.Instance.SendMessage(msg);
                    Logger.WriteLine(msg);
                    Program.mIlol = 0;
                    last = Program.Connection.AmountOnline;

                }
            }
            else
            {
                if (!Program.IgnoreDataTemp && !Program.CRASHMODE)
                {
                    DataBase.SaveWorldData(pData);
                }
            }
        }

        public void TweetCrash(Channel chnl)
        {
            var URL = string.Format("http://www.craftnet.nl/world_graph.png?worldid={0}&chid={1}&minutes=15&from={2}", chnl.World, chnl.ID, Program.CurrentTime);
            SendTweet("{0} crashed(?): from {1:N0} to {2:N0} players online #mscrash @MapleStory {3}", chnl.ChannelName, OldLoads[chnl.ChannelName], chnl.Population, URL);
        }

        public void SendTweet(string what, params object[] lulz)
        {
            TwitterClient.Instance.SendMessage(what, lulz);
        }
    }
}
