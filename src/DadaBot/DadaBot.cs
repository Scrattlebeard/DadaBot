using DadaBot.Commands;
using DadaBot.Configuration;
using DadaBot.Discord;
using DadaBot.Sound;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.VoiceNext;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace DadaBot
{
    public class DadaBot
    {
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
            serviceProvider.AddService(typeof(DebugLogger), client.DebugLogger);

            using var soundPlayer = new SoundPlayer(new SoundDevice(_soundSettings.InputDeviceId, _soundSettings.InputDeviceName), _soundSettings, client.DebugLogger);
            serviceProvider.AddService(typeof(ISoundPlayer), soundPlayer);

            var discordManager = new DiscordManager(client, _discordSettings, client.DebugLogger);
            serviceProvider.AddService(typeof(DiscordManager), discordManager);

            var commandImplementations = new CommandImplementations(discordManager, soundPlayer, _discordSettings, client.DebugLogger);
            serviceProvider.AddService(typeof(ICommandImplementations), commandImplementations);                                   

            SetupCommands(client, serviceProvider);
            SetupVoice(client);                       

            await client.ConnectAsync();

            await Task.Run(() => new ConsoleCommands(commandImplementations, client.DebugLogger).Listen());
        }

        private DiscordClient GetDiscordClient()
        {
            Enum.TryParse(typeof(LogLevel), _discordSettings.LogLevel, true, out var logLevel);

            var conf = new DiscordConfiguration
            {
                AutoReconnect = true,
                Token = _discordSettings.Token,
                TokenType = TokenType.Bot,
                UseInternalLogHandler = true,
                LogLevel = (LogLevel?) logLevel ?? LogLevel.Debug
            };

            var client = new DiscordClient(conf);                       

            client.Ready += Client_Ready;
            client.GuildAvailable += Client_GuildAvailable;
            client.ClientErrored += Client_ClientError;

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

            commands.CommandExecuted += Client_CommandExecuted;
            commands.CommandErrored += Client_CommandErrored;
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

        private Task Client_Ready(ReadyEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "DadaBot", "I'm ready to process events.", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Info, "DadaBot", $"Server found: {e.Guild.Name}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_ClientError(ClientErrorEventArgs e)
        {
            e.Client.DebugLogger.LogMessage(LogLevel.Error, "DadaBot", $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_CommandExecuted(CommandExecutionEventArgs e)
        {            
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, "DadaBot", $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);
            return Task.CompletedTask;
        }

        private Task Client_CommandErrored(CommandErrorEventArgs e)
        {            
            e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, "DadaBot", $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);
            return Task.CompletedTask;
        }
    }
}
