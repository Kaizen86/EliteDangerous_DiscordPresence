using DiscordRPC;
using EliteAPI;
using EliteAPI.Event.Models;
using EliteAPI.Event.Models.Travel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Somfic.Logging.Console;
using Somfic.Logging.Console.Themes;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordRPC_EliteDangerous
{
    class Program
    {
        static EliteDangerousAPI api;
        static DiscordRichPresence discord;

        //Adapted from sample code
        private static async Task Main()
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
            EventHandlers.Movement nav = new EventHandlers.Movement();
            api.Events.LocationEvent += nav.LocationEvent;
            api.Events.StartJumpEvent += nav.StartJumpEvent;
            api.Events.FSDJumpEvent += nav.FSDJumpEvent;
            api.Events.SupercruiseEntryEvent += nav.SupercruiseEntryEvent;
            api.Events.SupercruiseExitEvent += nav.SupercruiseExitEvent;
            //Exploration

            //Combat
            EventHandlers.Combat combat = new EventHandlers.Combat();

            //Other
            EventHandlers.Other other = new EventHandlers.Other();
            api.Events.MusicEvent += other.MusicEvent;
            api.Events.SelfDestructEvent += other.SelfDestructEvent;
            api.Events.RebootRepairEvent += other.RebootRepairEvent;

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
#pragma warning disable IDE0060 // Remove unused parameter 'object s'
            public class Movement
            {
                public void LocationEvent(object s, LocationEvent e)
                {
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "Logged on";
                }
                public void StartJumpEvent(object s, StartJumpEvent e)
                {
                    //Occurs when a jump commences
                    if (e.JumpType == "Hyperspace") 
                    {
                        discord.TopText = "Jumping to " + e.StarSystem;
                        discord.BottomText = "";
                    }
                    else
                        discord.BottomText = "Engaging Supercruise...";
                }
                public void FSDJumpEvent(object s, FSDJumpEvent e)
                {
                    //Occurs when a jump concludes
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "In Supercruise";
                }
                public void SupercruiseEntryEvent(object s, SupercruiseEntryEvent e)
                {
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "In Supercruise";
                }
                public void SupercruiseExitEvent(object s, SupercruiseExitEvent e)
                {
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "In normal space";
                }
            }
            public class Combat
            {
            }
            public class Other
            {
                public void MusicEvent(object s, MusicEvent e)
                {
                    switch (e.MusicTrack)
                    {
                        case "Main Menu":
                            discord.BottomText = "In Main Menu";
                            break;
                        case "Codex":
                            discord.BottomText = "Reading the Codex";
                            break;
                    }
                }
                public void SelfDestructEvent(object s, SelfDestructEvent e)
                {
                    discord.BottomText = "Self destructed";
                }
                public void RebootRepairEvent(object s, RebootRepairEvent e)
                {
                    discord.BottomText = "Rebooted ship";
                }
            }
#pragma warning restore IDE0060 // Remove unused parameter 'object s'
        }
    }

    //Responsible for Discord RPC stuff
    class DiscordRichPresence
    {
        private readonly DiscordRpcClient rpc;
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