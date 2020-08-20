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

namespace DiscordRPC_EliteDangerous
{
    class Program
    {
        static EliteDangerousAPI api;

        private static async Task Main(string[] args)
        {
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

            //Add events
            EventHandlers.Exploration exploration = new EventHandlers.Exploration();
            api.Events.FSDJumpEvent += exploration.FSDJumpEvent;

            await api.StartAsync();

            //await Task.Delay(-1);
        }
    }

    class EventHandlers
    {
        public class Exploration
        {
            public void FSDJumpEvent(object sender, FSDJumpEvent e)
            {
                Debug.WriteLine(string.Format("FSD JUMP EVENT!!!! {0}, {1}, {2}, {3}", e.Event, e.Body, e.BoostUsed, e.JumpDist));
            }
        }
    }
}