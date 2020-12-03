using DadaBot.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DadaBot
{
    class Startup
    {
        static async Task Main(string[] args)
        {
            var listener = new ConsoleTraceListener();
            Trace.Listeners.Add(listener);

            Trace.WriteLine("Initializing DadaBot...");

            var token = GetArgument(args, "token");
            var configPath = GetArgument(args, "config") ?? "DadaBot.config.json";

            var config = await LoadConfig(configPath);

            if (config == null)
            {
                throw new Exception($"Could not load configuration file {configPath}");
            }

            //Token given in argument overrides config
            if (token != null)
            {
                config.DiscordSettings.Token = token;
            }

            var bot = new DadaBot(config);

            Trace.WriteLine("Starting DadaBot...");

            await bot.Run();
        }        

        private static string? GetArgument(string[] args, string argumentName)
        {
            return args.FirstOrDefault(a => a.StartsWith($"{argumentName}="))?.Replace($"{argumentName}=", "");
        }

        private static async Task<DadaBotConfiguration> LoadConfig(string path)
        {
            using var stream = new FileStream(path, FileMode.Open);
            using var reader = new StreamReader(stream);

            var contents = await reader.ReadToEndAsync();
            var res = JsonConvert.DeserializeObject<DadaBotConfiguration>(contents);

            return res;
        }
    }
}
