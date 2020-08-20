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
using System.Linq;
using System.Threading;
using EliteAPI.Event.Models;

namespace DiscordRPC_EliteDangerous
{
    class Program
    {
        static EliteDangerousAPI api;
        static DiscordRichPresence discord;

        //Adapted from sample code
        private static async Task Main(string[] args)
        {
            discord = new DiscordRichPresence("746041178603913227");

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
            //Navigation
            EventHandlers.Navigation nav = new EventHandlers.Navigation();
            api.Events.FSDJumpEvent += nav.FSDJumpEvent;
            api.Events.SupercruiseEntryEvent += nav.SupercruiseEntryEvent;
            api.Events.SupercruiseExitEvent += nav.SupercruiseExitEvent;
            //Exploration

            //Combat

            //Other
            EventHandlers.Other other = new EventHandlers.Other();
            api.Events.MusicEvent += other.MusicEvent;
            
            //Start API
            await api.StartAsync();

            //Turn on and off when the game is running and periodically update
            while (true)
            {
                Thread.Sleep(1500);
                if (Process.GetProcessesByName("EliteDangerous64").Count() > 0)
                {
                    discord.TurnOn();
                    discord.Update();
                }
                else
                {
                    discord.TurnOff();
                }
            }
        }

        //Will contain all event handlers, nested classes used for organisation
        class EventHandlers
        {
            public class Navigation
            {
                public void FSDJumpEvent(object sender, FSDJumpEvent e)
                {
                    //Occurs when a jump concludes
                    Debug.WriteLine(string.Format("FSD JUMP EVENT!!!! {0}, {1}, {2}, {3}", e.Event, e.Body, e.BoostUsed, e.JumpDist));
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "In Supercruise";
                }
                public void SupercruiseEntryEvent(object sender, SupercruiseEntryEvent e)
                {
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "In Supercruise";
                }
                public void SupercruiseExitEvent(object sender, SupercruiseExitEvent e)
                {
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "In normal space";
                }
            }
            public class Other
            {
                public void MusicEvent(object sender, MusicEvent e)
                {
                    switch (e.MusicTrack)
                    {
                        case "Main Menu":
                            discord.BottomText = "In Main Menu";
                            break;
                        case "Codex":
                            discord.BottomText = "Reading the Codex";
                            break;
                        default:
                            discord.BottomText = e.MusicTrack;
                            break;
                    }
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
        }
        public void TurnOn()
        {
            if (Online) return;
            rpc.Initialize();
            Online = true;
        }
        public void TurnOff()
        {
            if (!Online) return;
            rpc.ClearPresence();
            rpc.Deinitialize();
            Online = false;
        }

        public bool Online=false;

        //Presence text, updated from API events. Once the API supports reading the current player state on demand, we can replace default values with actual information in the constructor.
        public string TopText = "Waiting for data..."; //Should contain the star system name
        public string BottomText = ""; //Current action (docking, supercruise, jumping, fighting, using DSS, etc)

        //Called periodically to refresh the presence by the main loop
        public void Update()
        {
            if (!Online) return;

            RichPresence presence = new RichPresence
            {
                Details = TopText,
                State = BottomText,
                Assets = new Assets()
                {
                    LargeImageKey = "edlogo",
                    LargeImageText = "Fly Dangerously!",
                    SmallImageKey = "edlogo",
                    SmallImageText = ""
                }
            };
            rpc.SetPresence(presence);
        }
    }
}