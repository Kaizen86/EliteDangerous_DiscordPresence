using System;
using System.Diagnostics;
using System.Threading.Tasks;
using EliteAPI;
using EliteAPI.Event.Models.Travel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Somfic.Logging.Console;
using Somfic.Logging.Console.Themes;
using DiscordRPC;

namespace DiscordRPC_EliteDangerous
{
    class Program
    {
        static EliteDangerousAPI api;
        static DiscordRichPresence discord;

        //Adapted from sample code
        private static async Task Main(string[] args)
        {
            //Start Discord Rich Presence
            discord = new DiscordRichPresence("716464991326306355");

            IHost host = Host.CreateDefaultBuilder()
                .ConfigureLogging((context, logger) =>
                {
                    logger.ClearProviders();
                    logger.SetMinimumLevel(LogLevel.Trace);
                    logger.AddPrettyConsole(ConsoleThemes.Code);
                })
                .ConfigureServices((context, service) =>
                {
                    service.AddEliteAPI<EliteDangerousAPI>();
                })
                .Build();

            api = ActivatorUtilities.CreateInstance<EliteDangerousAPI>(host.Services);

            //Subscribe event handlers to API
            EventHandlers.Exploration exploration = new EventHandlers.Exploration();
            api.Events.FSDJumpEvent += exploration.FSDJumpEvent;

            await api.StartAsync();

            //await Task.Delay(-1);
        }


        //Will contain all event handlers, nested classes used for organisation
        class EventHandlers
        {
            public class Exploration
            {
                public void FSDJumpEvent(object sender, FSDJumpEvent e)
                {
                    Debug.WriteLine(string.Format("FSD JUMP EVENT!!!! {0}, {1}, {2}, {3}", e.Event, e.Body, e.BoostUsed, e.JumpDist));
                    Program.discord.PresenceBottomText = "fsd_jump";
                }
            }
        }
    }

    //Responsible for Discord RPC stuff
    class DiscordRichPresence
    {
        private DiscordRpcClient rpc;
        //Start/stop the client
        public DiscordRichPresence(string AppID)
        {
            rpc = new DiscordRpcClient(AppID);
            rpc.Initialize();
        }
        public void Stop()
        {
            rpc.ClearPresence();
            rpc.Deinitialize();
            rpc.Dispose();
        }

        //Presence text, updated from API events. Once the API supports reading the current player state on demand, we can replace default values with actual information in the constructor.
        public string PresenceTopText = "Unknown location"; //Should contain the star system name
        public string PresenceBottomText = "Unknown condition"; //Current action (docking, supercruise, jumping, fighting, using DSS, etc)

        //Called after an API event updates our state, refreshes the Rich Presence with new information
        public void Update()
        {
            RichPresence presence = new RichPresence
            {
                Details = "details",
                State = "state",
                Assets = new Assets()
                {
                    LargeImageKey = "largeimg",
                    LargeImageText = "largeimage_text",
                    SmallImageKey = "smallimg",
                    SmallImageText = "smallimage_text"
                }
            };
            rpc.SetPresence(presence);
        }
    }
}