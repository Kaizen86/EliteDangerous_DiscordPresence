using DiscordRPC;
using EliteAPI;
using EliteAPI.Event.Models;
using EliteAPI.Event.Models.Travel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Somfic.Logging.Console;
using Somfic.Logging.Console.Themes;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            discord = new DiscordRichPresence("746041178603913227"); //Replace with your Discord App ID

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

            //Subscribe various event handlers to API
            //Hyperspace
            GameEventHandlers.FSD fsd = new GameEventHandlers.FSD();
            api.Events.StartJumpEvent += fsd.StartJumpEvent;
            api.Events.FSDJumpEvent += fsd.FSDJumpEvent;
            api.Events.SupercruiseEntryEvent += fsd.SupercruiseEntryEvent;
            api.Events.SupercruiseExitEvent += fsd.SupercruiseExitEvent;

            //Exploration - Star/planet scanning

            //Mission events
            GameEventHandlers.Mission tasks = new GameEventHandlers.Mission();
            api.Events.MissionAbandonedEvent += tasks.MissionAbandonedEvent;
            api.Events.MissionAcceptedEvent += tasks.MissionAcceptedEvent;
            api.Events.MissionCompletedEvent += tasks.MissionCompletedEvent;
            api.Events.MissionFailedEvent += tasks.MissionFailedEvent;
            api.Events.MissionRedirectedEvent += tasks.MissionRedirectedEvent;

            //Combat - Interdiction, "Under attack", etc
            GameEventHandlers.Combat combat = new GameEventHandlers.Combat();
            api.Events.SelfDestructEvent += combat.SelfDestructEvent;

            //Ship specific things like reboots, AFMUs, limpets, fuel scooping, etc
            GameEventHandlers.Ship ship = new GameEventHandlers.Ship();
            api.Events.RebootRepairEvent += ship.RebootRepairEvent;
            api.Events.FuelScoopEvent += ship.FuelScoopEvent;
            api.Events.AfmuRepairsEvent += ship.AfmuRepairsEvent;

            GameEventHandlers.Station station = new GameEventHandlers.Station();
            api.Events.DockingGrantedEvent += station.DockingGrantedEvent;
            api.Events.DockedEvent += station.DockedEvent;
            api.Events.UndockedEvent += station.UndockedEvent;
            api.Events.MusicEvent += station.MusicEvent;

            //UI Events - Being in the main menu, reading the Codex, logging on, etc
            GameEventHandlers.UIEvents ui = new GameEventHandlers.UIEvents();
            api.Events.LocationEvent += ui.LocationEvent;
            api.Events.MusicEvent += ui.MusicEvent;

            //Start the API
            await api.StartAsync();

            //Turn rich presence on and off when the game is running and periodically update
            while (true)
            {
                Thread.Sleep(500);
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
        class GameEventHandlers
        {
#pragma warning disable IDE0060 // Remove unused parameter 'object s'
            public class FSD
            {
                public void StartJumpEvent(object s, StartJumpEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;

                    //Occurs when a jump commences
                    if (e.JumpType == "Hyperspace")
                    {
                        discord.TopText = "Jumping to system:";
                        discord.BottomText = e.StarSystem;
                    }
                    else
                        discord.BottomText = "Engaging Supercruise";
                }
                public void FSDJumpEvent(object s, FSDJumpEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    //Occurs when a jump concludes
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "Just arrived";
                }
                public void SupercruiseEntryEvent(object s, SupercruiseEntryEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "In Supercruise";
                }
                public void SupercruiseExitEvent(object s, SupercruiseExitEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.TopText = e.StarSystem;
                    //If we drop near something (star, planet, space station), say what it is
                    if (e.Body == "") discord.BottomText = "In normal space"; //Otherwise, 
                    else discord.BottomText = "Near " + e.Body;
                }
            }
            public class Combat
            {
                public void SelfDestructEvent(object s, SelfDestructEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    //"If I can't sell this cargo, then nobody can!"
                    discord.BottomText = "Self destructed";
                }
            }
            public class Mission
            {
                public void MissionAbandonedEvent(object s, MissionAbandonedEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.BottomText = "Abandoned a mission";
                }
                public void MissionAcceptedEvent(object s, MissionAcceptedEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.BottomText = "Accepted a mission";
                }
                public void MissionCompletedEvent(object s, MissionCompletedEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.BottomText = "Completed a mission";
                }
                public void MissionFailedEvent(object s, MissionFailedEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.BottomText = "Failed a mission";
                }
                public void MissionRedirectedEvent(object s, MissionRedirectedEvent e)
                {
                    /*
                    if (HasEventExpired(e.Timestamp)) return;
                    //"Incoming mission critical message"
                    */
                }
            }
            public class Ship
            {
                public void RebootRepairEvent(object s, RebootRepairEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.BottomText = "Rebooted ship";
                }
                //Limpet event maybe - "Being saved by Fuel Rats"
                public void FuelScoopEvent(object s, FuelScoopEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.BottomText = "Finished scooping fuel";
                }
                public void AfmuRepairsEvent(object s, AfmuRepairsEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.BottomText = string.Format("Repaired {0} to {1}%", e.ModuleLocalised, System.Math.Round(e.Health * 100, 0));
                }
                public void DiedEvent(object s, DiedEvent e) { }
            }
            public class Station
            {
                bool docked = false;
                public void DockingGrantedEvent(object s, DockingGrantedEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    discord.BottomText = "Docking at " + e.StationName;
                }
                public void DockedEvent(object s, DockedEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    docked = true;
                    discord.BottomText = "Docked at " + e.StationName;
                }
                public void UndockedEvent(object s, UndockedEvent e)
                {
                    if (HasEventExpired(e.Timestamp)) return;
                    docked = false;
                    discord.BottomText = "Leaving " + e.StationName;
                }
                public void MusicEvent(object s, MusicEvent e)
                {
                    //If using an autopilot, detect it with the music cue
                    if (e.MusicTrack == "DockingComputer")
                    {
                        if (docked)
                        {
                            discord.BottomText = "Autopilot docking";
                        }
                        else
                        {
                            discord.BottomText = "Autopilot undocking";
                        }
                    }
                }
            }
            public class UIEvents
            {
                public void LocationEvent(object s, LocationEvent e)
                {
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "Just logged on";
                }
                string BeforeMusicEvent = "";
                public void MusicEvent(object s, MusicEvent e)
                {
                    switch (e.MusicTrack)
                    {
                        case "MainMenu":
                            BeforeMusicEvent = discord.BottomText;
                            discord.BottomText = "In Main Menu";
                            break;
                        case "Codex":
                            BeforeMusicEvent = discord.BottomText;
                            discord.BottomText = "Reading the Codex";
                            break;
                        case "GalaxyMap":
                            BeforeMusicEvent = discord.BottomText;
                            discord.BottomText = "Reading the Galaxy Map";
                            break;
                        case "SystemMap":
                            BeforeMusicEvent = discord.BottomText;
                            discord.BottomText = "Reading the System Map";
                            break;

                        case "Supercruise": //Also respond to supercruise music cues
                        case "Exploration":
                            if (BeforeMusicEvent.Length > 0) //Check if the cause of the music event was a menu exit
                            {
                                discord.BottomText = BeforeMusicEvent; //Restore previous activity
                                BeforeMusicEvent = ""; //Clear it again, ready for next time we enter a menu
                            }
                            break;
                    }
                }
            }
            static bool HasEventExpired(DateTime timestamp)
            {
                //Check if the provided timestamp is older than 24 hours, returning true if it is
                return DateTime.Now.Subtract(timestamp).Hours > 24;
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
        public string TopText = "Initialised."; //Normally contains the star system name, but I'm not strict on that.
        public string BottomText = "Waiting for data..."; //Current action (docking, supercruise, jumping, fighting, using DSS, etc). This will be updated most often

        private string _top;
        private string _bottom;
        //Called periodically by the main loop to refresh the presence text
        public void Update()
        {
            if (!Online) return;

            if (_top != TopText || _bottom != BottomText)
            {
                //To prevent spamming Discord's servers, only send data when it updates. 
                _top = TopText;
                _bottom = BottomText;

                RichPresence presence = new RichPresence
                {
                    Details = TopText,
                    State = BottomText,
                    Assets = new Assets()
                    {
                        //You can customise these however you want
                        LargeImageKey = "edlogo",
                        LargeImageText = "Fly Dangerously!",
                        SmallImageKey = "edlogo" /*,
                        SmallImageText = "" */
            }
        };
                rpc.SetPresence(presence);
            }
        }
    }
}