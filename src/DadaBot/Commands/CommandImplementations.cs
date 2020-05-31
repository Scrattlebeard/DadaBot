using DadaBot.Configuration;
using DadaBot.Discord;
using DadaBot.Exceptions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace DadaBot.Commands
{
    public class CommandImplementations : ICommandImplementations
    {
        private readonly DebugLogger _log;
        private readonly DiscordSettings _discordSettings;
        private readonly DiscordManager _discordManager;
        private readonly SoundPlayer _soundPlayer;

        public CommandImplementations(DiscordManager discordManager, SoundPlayer soundPlayer, DiscordSettings discordSettings, DebugLogger log)
        {
            _log = log;
            _discordSettings = discordSettings;
            _discordManager = discordManager;
            _soundPlayer = soundPlayer;
        }

        public bool IsOwner(ulong userId)
        {
            _log.LogMessage(LogLevel.Debug, "DadaBot: CommandImplementation - IsOwner()", $"Checking user with id {userId}", DateTime.Now);

            return userId == _discordSettings.OwnerId;
        }

        public async Task<(bool success, string answer)> Greet(string serverName, string? channelName)
        {
            var isId = ulong.TryParse(serverName, out var id);
            DiscordGuild server;

            try
            {
                server = isId ? _discordManager.GetServerById(id) : _discordManager.GetServerByName(serverName);
            }
            catch (AmbiguousIdentifierException e)
            {
                _log.LogMessage(LogLevel.Warning, "DadaBot", e.Message, DateTime.Now, e);
                return (false, $"Sorry, but I know more than one server with name {serverName}. Please use the serverId instead.");
            }
            catch (UnmatchedIdentifierException e)
            {
                _log.LogMessage(LogLevel.Warning, "DadaBot", e.Message, DateTime.Now, e);
                return (false, $"Sorry, but I don't know any servers with the id {serverName}. Did you add me yet?");
            }

            DiscordChannel? channel = null;

            if (channelName != null)
            {
                isId = ulong.TryParse(channelName, out id);

                try
                {
                    channel = isId ? _discordManager.GetChannelById(id, server) : _discordManager.GetChannelByName(channelName, server);
                }
                catch (AmbiguousIdentifierException e)
                {
                    _log.LogMessage(LogLevel.Warning, "DadaBot", e.Message, DateTime.Now, e);
                    return (false, $"Sorry, but there's more than one channel with name {channelName}, on the server {server.Name}. Please use the channelId instead.");
                }
                catch (UnmatchedIdentifierException e)
                {
                    _log.LogMessage(LogLevel.Warning, "DadaBot", e.Message, DateTime.Now, e);
                    return (false, $"Sorry, but I couldn't find any channel with id {channelName} on the server {server.Name}.");
                }
            }

            //Default to system channel
            channel ??= server.SystemChannel;

            if (channel != null)
            {
                await channel.SendMessageAsync("Hello, world! Jeg er DadaBot og jeg kan streame lyd fra min ejers computer. Jeg håber I vil være søde ved mig :)");
                _log.LogMessage(LogLevel.Info, "DadaBot", $"Sucessfully greeted server {serverName} in channel {channelName ?? "The system channel"}.", DateTime.Now);
                return (true, "Done!");
            }
            else
            {
                _log.LogMessage(LogLevel.Warning, "DadaBot", $"Server {server.Name} has no system channel and no channel was explicitly specified.", DateTime.Now);
                return (false, $"Sorry, but server {server.Name} has no system channel. You'll have to tell me which channel to post my greeting in.");
            }
        }

        public async Task<(bool res, string output)> Play(CommandContext? ctx = null)
        {
            if(!_discordManager.VoiceChannelJoined())
            {
                if(_discordSettings.Autojoin)
                {
                    await AutoJoin(ctx);
                }
                else
                {
                    return (false, "I can't play, when I'm not in a channel. Please join a channel first, or enable autojoin.");
                }
            }

            var voiceConnection = _discordManager.GetVoiceConnection();                       

            if (voiceConnection != null)
            {                
                _soundPlayer.Start(voiceConnection);
                return (true, "Now playing.");
            }
            else
            {
                _log.LogMessage(LogLevel.Error, "DadaBot", "Failed to get voice stream, even though Bot should be in voice channel.", DateTime.Now);
                return (false, "Failed to get Voice output, so I can't play");
            }
        }

        public void StopPlayback()
        {
            _soundPlayer.Stop();
            _log.LogMessage(LogLevel.Info, "DadaBot", $"Playback stopped", DateTime.Now);
        }

        public async Task<(bool res, string output)> AutoJoin(CommandContext? ctx = null)
        {
            _log.LogMessage(LogLevel.Debug, "DadaBot", "Joining channel based on users currently active channel.", DateTime.Now);

            DiscordChannel? channel = null;

            if(ctx != null)
            {
                channel = _discordManager.GetActiveVoiceChannel(ctx);
            }

            channel ??= _discordManager.GetActiveVoiceChannelForUser(_discordSettings.OwnerId);

            if (channel == null)
            {
                return (false, $"I'm sorry, but I couldn't find you anywhere {ctx.User.Username}. Did you join a voice channel yet?");
            }

            _log.LogMessage(LogLevel.Debug, "DadaBot", $"Found channel {channel.Name} on server {channel.Guild.Name}.", DateTime.Now);
            await _discordManager.JoinChannel(channel);

            return (true, $"Joined channel {channel.Name} on server {channel.Guild.Name}.");
        }

        public async Task<(bool res, string output)> JoinVoiceChannel(string identifier)
        {
            _log.LogMessage(LogLevel.Debug, "DadaBot", $"Joining channel with identifier {identifier}.", DateTime.Now);

            var isId = ulong.TryParse(identifier, out var id);

            try
            {
                await (isId ? _discordManager.JoinChannelById(id) : _discordManager.JoinChannelByName(identifier));
                return (true, $"Joined channel with {(isId ? "id" : "name")} {identifier}.");
            }
            catch(AmbiguousIdentifierException e)
            {
                _log.LogMessage(LogLevel.Info, "DadaBot", $"Found more than one channel with identifier {identifier}. {e.Message}", DateTime.Now, e);
                return (false, $"I'm sorry but I found more than one channel matching {identifier}.");
            }
            catch(UnmatchedIdentifierException e)
            {
                _log.LogMessage(LogLevel.Info, "DadaBot", $"Couldn't find channel with identifier {identifier}. {e.Message}", DateTime.Now, e);
                return (false, $"I'm sorry, but I couldn't find any channel matching {identifier}.");
            }
            catch(WrongChannelTypeException e)
            {
                _log.LogMessage(LogLevel.Info, "DadaBot", $"Channel {identifier} was not a voice channel. {e.Message}", DateTime.Now, e);
                return (false, $"I'm sorry, but {identifier} doesn't seem to be a voice channel.");
            }
        }

        public void Disconnect()
        {
            StopPlayback();
            _discordManager.Disconnect();
        }
    }
}
