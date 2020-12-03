using DadaBot.Commands;
using DadaBot.Configuration;
using DadaBot.Discord;
using DadaBot.Sound;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.VoiceNext;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace DadaBot
{
    public class DadaBot
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly DiscordSettings _discordSettings;
        private readonly SoundSettings _soundSettings;

        public DadaBot(DadaBotConfiguration config)
        {
            _discordSettings = config.DiscordSettings;
            _soundSettings = config.SoundSettings;
        }

        public async Task Run()
        {
            using var serviceProvider = new ServiceContainer();            
            
            using var client = GetDiscordClient();

            if (_discordSettings.LogLevel == "Debug")
            {
                try
                {
                    SoundDevice.DetectDevices();
                }
                catch (Exception e)
                {
                    logger.Error(e, "Sound device detection failed: {exception}");
                        
                }
            }

            using var soundPlayer = new SoundPlayer(new SoundDevice(_soundSettings.InputDeviceName, _soundSettings.InputDeviceId), _soundSettings);
            serviceProvider.AddService(typeof(ISoundPlayer), soundPlayer);

            var discordManager = new DiscordManager(client, _discordSettings);
            serviceProvider.AddService(typeof(DiscordManager), discordManager);

            var commandImplementations = new CommandImplementations(discordManager, soundPlayer, _discordSettings);
            serviceProvider.AddService(typeof(ICommandImplementations), commandImplementations);                                   

            SetupCommands(client, serviceProvider);
            SetupVoice(client);                       

            await client.ConnectAsync();

            await Task.Run(() => new ConsoleCommands(commandImplementations).Listen());
        }

        private DiscordClient GetDiscordClient()
        {
            Enum.TryParse(typeof(Microsoft.Extensions.Logging.LogLevel), _discordSettings.LogLevel, true, out var logLevel);
            logLevel ??= Microsoft.Extensions.Logging.LogLevel.Debug;

            var conf = new DiscordConfiguration
            {
                AutoReconnect = true,
                Token = _discordSettings.Token,
                TokenType = TokenType.Bot,
                LoggerFactory = GetLoggerFactory(),
                MinimumLogLevel = (Microsoft.Extensions.Logging.LogLevel) logLevel,
                LogTimestampFormat = "dd MM yyyy - HH:mm:ss.fff"
            };

            var client = new DiscordClient(conf);                       
            return client;
        }

        private void SetupCommands(DiscordClient client, IServiceProvider serviceProvider)
        {
            var commandConf = new CommandsNextConfiguration
            {
                CaseSensitive = false,
                StringPrefixes = new string[] { "!" },
                EnableDefaultHelp = false,
                EnableMentionPrefix = false,
                EnableDms = true,
                Services = serviceProvider
            };

            var commands = client.UseCommandsNext(commandConf);
            commands.RegisterCommands<DiscordCommands>();
        }

        private void SetupVoice(DiscordClient client)
        {
            var voiceConf = new VoiceNextConfiguration
            {
                EnableIncoming = false,
                AudioFormat = AudioFormat.Default
            };

            client.UseVoiceNext(voiceConf);
        }

        private ILoggerFactory GetLoggerFactory()
        {
            return LoggerFactory.Create(builder => builder.AddNLog());
        }
    }
}
