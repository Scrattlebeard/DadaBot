using DadaBot.Configuration;
using DadaBot.Discord;
using DadaBot.Exceptions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using NLog;
using System;
using System.Threading.Tasks;

namespace DadaBot.Commands
{
    public class CommandImplementations : ICommandImplementations
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly DiscordSettings _discordSettings;
        private readonly DiscordManager _discordManager;
        private readonly SoundPlayer _soundPlayer;

        public CommandImplementations(DiscordManager discordManager, SoundPlayer soundPlayer, DiscordSettings discordSettings)
        {
            _discordSettings = discordSettings;
            _discordManager = discordManager;
            _soundPlayer = soundPlayer;
        }

        public bool IsOwner(ulong userId)
        {
            _log.Debug("IsOwner(): Checking user with id {userId}", userId);            
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
                _log.Warn(e, "Ambiguous Server identifier.");                
                return (false, $"Sorry, but I know more than one server with name {serverName}. Please use the serverId instead.");
            }
            catch (UnmatchedIdentifierException e)
            {
                _log.Warn(e, "Unknown server.");                
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
                    _log.Warn(e, "Ambiguous channel identifier.");
                    return (false, $"Sorry, but there's more than one channel with name {channelName}, on the server {server.Name}. Please use the channelId instead.");
                }
                catch (UnmatchedIdentifierException e)
                {
                    _log.Warn(e, "Unknown channel identifier.");                    
                    return (false, $"Sorry, but I couldn't find any channel with id {channelName} on the server {server.Name}.");
                }
            }

            //Default to system channel
            channel ??= server.SystemChannel;

            if (channel != null)
            {
                await channel.SendMessageAsync("Hello, world! Jeg er DadaBot og jeg kan streame lyd fra min ejers computer. Jeg håber I vil være søde ved mig :)");
                _log.Info("Sucessfully greeted server {serverName} in channel {channelName}", serverName, channelName ?? "The system channel");                
                return (true, "Done!");
            }
            else
            {
                _log.Warn("Server {serverName} has no system channel and no channel was explicitly specified.", serverName);                
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
                _log.Error("Failed to get voice stream, even though Bot should be in voice channel.");                
                return (false, "Failed to get Voice output, so I can't play");
            }
        }

        public void StopPlayback()
        {
            _soundPlayer.Stop();
            _log.Info("Playback stopped.");            
        }

        public async Task<(bool res, string output)> AutoJoin(CommandContext? ctx = null)
        {
            _log.Debug("Joining channel based on users currently active channel.");

            DiscordChannel? channel = null;

            if(ctx != null)
            {
                channel = _discordManager.GetActiveVoiceChannel(ctx);
            }

            channel ??= _discordManager.GetActiveVoiceChannelForUser(_discordSettings.OwnerId);

            if (channel == null)
            {
                return (false, $"I'm sorry, but I couldn't find you anywhere. Did you join a voice channel yet?");
            }

            _log.Debug("Found channel {channelName} on server {serverName}.", channel.Name, channel.Guild.Name);
            await _discordManager.JoinChannel(channel);

            return (true, $"Joined channel {channel.Name} on server {channel.Guild.Name}.");
        }

        public async Task<(bool res, string output)> JoinVoiceChannel(string identifier)
        {
            _log.Debug("Joining channel with identifier {identifier}.", identifier);

            var isId = ulong.TryParse(identifier, out var id);

            try
            {
                await (isId ? _discordManager.JoinChannelById(id) : _discordManager.JoinChannelByName(identifier));
                return (true, $"Joined channel with {(isId ? "id" : "name")} {identifier}.");
            }
            catch(AmbiguousIdentifierException e)
            {
                _log.Info(e, "Found more than one channel with identifier {identifier}", identifier);
                return (false, $"I'm sorry but I found more than one channel matching {identifier}.");
            }
            catch(UnmatchedIdentifierException e)
            {
                _log.Info(e, "Couldn't find channel with identifier {identifier}.", identifier);
                return (false, $"I'm sorry, but I couldn't find any channel matching {identifier}.");
            }
            catch(WrongChannelTypeException e)
            {
                _log.Info(e, "Channel {identifier} was not a voice channel.", identifier);
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
