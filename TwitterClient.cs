using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.IO;

using Twitterizer;

namespace MWLR_Logging
{
    public class TwitterClient
    {
        public static TwitterClient Instance { get; private set; }
        
        OAuthTokens tokens = new OAuthTokens();
        bool k = false;

        public TwitterClient()
        {
            string[] keys = File.ReadAllLines("c");

            tokens.ConsumerKey = keys[0];
            tokens.ConsumerSecret = keys[1];
            if (!File.Exists("d"))
            {
                var response = OAuthUtility.GetRequestToken(keys[0], keys[1], "oob");
                Console.WriteLine(response.Token);

                Console.WriteLine(string.Format("Go to: http://twitter.com/oauth/authorize?oauth_token={0}", response.Token));

                Console.Write("PIN: ");
                string pin = Console.ReadLine();

                var access = OAuthUtility.GetAccessToken(keys[0], keys[1], response.Token, pin);

                File.WriteAllLines("d", new string[] { access.Token, access.TokenSecret });
                Console.WriteLine("Done?");
            }
            string[] lines = File.ReadAllLines("d");

            tokens.AccessToken = lines[0];
            tokens.AccessTokenSecret = lines[1];
            k = true;
        }

        public static void Load()
        {
            Logger.WriteLine("Init Twitter");
            try
            {
                Instance = new TwitterClient();
            }
            catch (Exception ex)
            {
                Logger.ErrorLog(ex.ToString());
                Logger.WriteLine(ex.ToString());
            }
            Logger.WriteLine("Done!");
        }

        public void SendMessage(string pMSG, params object[] pVals)
        {
            //Logger.WriteLine("Ignored Tweet");
            //return;
            if (k)
            {
                TwitterResponse<TwitterStatus> tweetResponse = TwitterStatus.Update(tokens, string.Format("[{0}] #mwlr {1}", DateTime.Now.ToShortTimeString(), string.Format(pMSG, pVals)));
                if (tweetResponse.Result == RequestResult.Success)
                {
                    Logger.WriteLine("Tweet Sent Successfully");
                }
                else
                {
                    Logger.WriteLine("Could not post tweet.: {0}", tweetResponse.ErrorMessage);
                }
            }
        }
    }
}
