using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TDLibCore;
using tdlib = Telegram.Td;
using tdapi = Telegram.Td.Api;
using System.IO;
using Newtonsoft.Json;

namespace TDLibCore_Example
{
    public class Program
    {
        [Obsolete]
        public static void Main(string[] args)
        {
            TDLibCore.Helper hpcore = new Helper()
            {
                debuglevel = TDLibCore.enums.DebugLevel.Normal,
                APIHASH = "eb36555066d17d1ae52f95ee8d62d81a",
                APIID = 191031,
            };
            TDLibCore.TDLibCore core = new TDLibCore.TDLibCore(hpcore)
            {
                phonenumber = "+989165203611",
            };
            core.OnVerificationCodeNeeded += Core_OnVerificationCodeNeeded;
            core.OnVerificationPasswordNeeded += Core_OnVerificationPasswordNeededAsync;
            core.OnReady += Core_OnReady;
            core.initializeclient();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Console.Title = core.connectionstate.ToString();
                    Thread.Sleep(100);
                }
            });
            Console.WriteLine("finished everything");
            Console.ReadKey();
        }

        private static async void Core_OnVerificationCodeNeeded(object sender, TDLibCoreEventArgs e)
        {
            string verificationcode = "";
            Console.WriteLine("Please enter verification code");
            verificationcode = Console.ReadLine();
            var response = await e.core.Authenticate(verificationcode);
            if (response.response == TDLibCore.enums.Response.Failed)
            {
                tdapi.Error error = response.responseobj as tdapi.Error;
                Console.WriteLine("verificaition code - " + error.Message);
            }
        }

        private static async void Core_OnVerificationPasswordNeededAsync(object sender, TDLibCoreEventArgs e)
        {
            string password = "";
            Console.WriteLine("Please enter your password");
            password = Console.ReadLine();
            var response = await e.core.Authenticate(password);
            if (response.response == TDLibCore.enums.Response.Failed)
            {
                tdapi.Error error = response.responseobj as tdapi.Error;
                Console.WriteLine("verification password - " + error.Message);
            }
        }

        private static async void Core_OnReady(object sender, TDLibCoreEventArgs e)
        {
            TDLibCore.TDLibCore core = e.core;
            Console.WriteLine("ready");
            Console.WriteLine("Gathering chatslist ...");
            List<tdapi.Chat> chatslist = await core.GetMainChatList();
			core.mainresponsehandlers.Add(new tdapi.UpdateNewMessage().GetType(), async (a, b) =>
            {
                TDLibCoreEventArgs args = b;
                Console.WriteLine(args.additionalobject);
            });
            if (chatslist.Count > 0)
            {
                Console.WriteLine(chatslist.Count);
            }
            else
            {
                Console.WriteLine("you have no chats in your main chatlist");
            }
        }
    }
}