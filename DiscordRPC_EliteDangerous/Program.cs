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

            //Combat - Interdiction, "Under attack", etc
            GameEventHandlers.Combat combat = new GameEventHandlers.Combat();
            api.Events.SelfDestructEvent += combat.SelfDestructEvent;

            //Meta - Being in the main menu, reading the Codex, logging on, etc
            GameEventHandlers.Meta meta = new GameEventHandlers.Meta();
            api.Events.LocationEvent += meta.LocationEvent;
            api.Events.MusicEvent += meta.MusicEvent;

            //Maintenance - Ship diagnostics and repairs
            GameEventHandlers.Maintenance repairs = new GameEventHandlers.Maintenance();
            api.Events.RebootRepairEvent += repairs.RebootRepairEvent;

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
                    //Occurs when a jump concludes
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "Just arrived";
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
                public void SelfDestructEvent(object s, SelfDestructEvent e)
                {
                    //"If I can't sell this cargo, then nobody can!"
                    discord.BottomText = "Self destructed";
                }
            }
            public class Maintenance
            {
                public void RebootRepairEvent(object s, RebootRepairEvent e)
                {
                    discord.BottomText = "Rebooted ship";
                }
                //Limpet event maybe - "Being saved by Fuel Rats"
                //Fuel scooping
            }
            public class Meta
            {
                public void LocationEvent(object s, LocationEvent e)
                {
                    discord.TopText = e.StarSystem;
                    discord.BottomText = "Just logged on";
                }
                string BeforeMusicEvent = "";
                public void MusicEvent(object s, MusicEvent e)
                {
                    if (BeforeMusicEvent.Length < 1) BeforeMusicEvent = discord.BottomText; //Only overwrite if it's empty - this prevents overwriting it before we read it back in the default case
                    switch (e.MusicTrack)
                    {
                        case "MainMenu":
                            discord.BottomText = "In Main Menu";
                            break;
                        case "Codex":
                            discord.BottomText = "Reading the Codex";
                            break;
                        case "GalaxyMap":
                            discord.BottomText = "Reading the Galaxy Map";
                            break;
                        default:
                            //Once we return to Exploration (flying the ship), we need to show what we were doing before that.
                            discord.BottomText = BeforeMusicEvent;
                            BeforeMusicEvent = ""; //Clear it again ready for next time we enter a menu
                            break;
                    }
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
        public string TopText = "Initialised."; //Should contain the star system name
        public string BottomText = "Waiting for data..."; //Current action (docking, supercruise, jumping, fighting, using DSS, etc)

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