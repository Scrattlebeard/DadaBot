using DadaBot.Configuration;
using DadaBot.Exceptions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DadaBot.Discord
{
    public class DiscordManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly DiscordClient _client;
        private readonly DiscordSettings _discordSettings;        

        private DiscordGuild? _currentServer;

        public DiscordManager(DiscordClient client, DiscordSettings discordSettings)
        {
            _client = client;
            _discordSettings = discordSettings;
        }

        public DiscordGuild GetServerByName(string serverName)
        {
            _log.Debug("Getting server by name {serverName}.", serverName);

            DiscordGuild res;

            try
            {
                 res = _client.Guilds.Values.SingleOrDefault(g => g.Name == serverName);
            }
            catch (InvalidOperationException e)
            {                
                throw new AmbiguousIdentifierException($"Could not uniquely identify server with name {serverName}.", e);
            }

            if(res != null)
            {
                return res;                
            }
            else
            {
                throw new UnmatchedIdentifierException($"Could not find server with name {serverName}.");
            }
        }

        public DiscordGuild GetServerById(ulong serverId)
        {
            _log.Debug("Getting server by id {serverId}.", serverId);

            if (_client.Guilds.ContainsKey(serverId))
            {
                return _client.Guilds[serverId];
            }
            else
            {
                throw new UnmatchedIdentifierException($"No known server has id {serverId}.");
            }
        }

        public DiscordChannel GetChannelByName(string channelName, DiscordGuild server)
        {
            _log.Debug("Getting channel by name {channelName} on server {serverName}.", channelName, server.Name);

            DiscordChannel res;

            try
            {
                res = server.Channels.Values.FirstOrDefault(c => c.Name == channelName);
            }
            catch (InvalidOperationException e)
            {
                throw new AmbiguousIdentifierException($"Could not uniquely identify channel with name {channelName} in server {server.Name}.", e);
            }

            if (res != null)
            {
                return res;
            }
            else
            {
                throw new UnmatchedIdentifierException($"Could not find any channel with name {channelName} in server {server.Name}.");
            }
        }

        public DiscordChannel GetChannelByName(string channelName)
        {
            _log.Debug("Getting channel by name {channelName}.", channelName);

            List<DiscordChannel> res = new List<DiscordChannel>();

            foreach(var server in _client.Guilds.Values)
            {
                res.AddRange(server.Channels.Values.Where(c => c.Name == channelName));
            }

            if(res.Count > 1)
            {
                throw new AmbiguousIdentifierException($"Could not uniquely identify channel with name {channelName}.");
            }

            if(res.Count == 0)
            {
                throw new UnmatchedIdentifierException($"Could not find any channel with name {channelName}.");
            }

            return res.First();
        }

        public DiscordChannel GetChannelById(ulong channelId)
        {
            _log.Debug("Getting channel by id {channelId}.", channelId);

            foreach (var server in _client.Guilds.Values)
            {
                if(server.Channels.ContainsKey(channelId))
                {
                    return server.Channels[channelId];
                }
            }

            throw new UnmatchedIdentifierException($"Could not find channel with id {channelId} in any known server.");
        }

        public DiscordChannel GetChannelById(ulong channelId, DiscordGuild server)
        {
            _log.Debug("Getting channel by id {channelId} on server {serverName}.", channelId, server.Name);

            if (server.Channels.ContainsKey(channelId))
            {
                return server.Channels[channelId];
            }
            else
            {
                throw new UnmatchedIdentifierException($"Could not find channel with id {channelId} in server {server.Name}.");
            }
        }

        public DiscordChannel? GetActiveVoiceChannel(CommandContext ctx)
        {
            //Did we receive command on a server where the user is currently in a channel?
            if (ctx.Member?.VoiceState.Channel != null)
            {
                return ctx.Member.VoiceState.Channel;
            }

            var userId = ctx.User.Id;
            return GetActiveVoiceChannelForUser(userId);
        }

        public DiscordChannel? GetActiveVoiceChannelForUser(ulong userId)
        {
            var servers = _client.Guilds?.Values ?? new List<DiscordGuild>();

            //Check all voice channels on all known servers
            foreach (var server in servers)
            {
                var voiceChannels = server.Channels.Values.Where(c => c.Type == ChannelType.Voice);

                foreach (var channel in voiceChannels)
                {
                    if (channel.Users.Any(u => u.Id == userId))
                    {
                        return channel;
                    }
                }
            }

            return null;
        }

        public async Task JoinChannel(DiscordChannel channel)
        {
            if (!(channel.Type == ChannelType.Voice))
            {
                throw new WrongChannelTypeException($"Channel {channel.Name} on server {channel.Guild.Name} is not a voice channel.");
            }

            _currentServer = channel.Guild;

            var voiceClient = _client.GetVoiceNext();
            var currentConnection = voiceClient.GetConnection(_currentServer);

            if(currentConnection != null)
            {
                _log.Info("Already connected to {channelName} on server {serverName}. Disconnecting to join {channelName} instead.", currentConnection.TargetChannel.Name, _currentServer?.Name, channel.Name);
                currentConnection.Disconnect();
            }

            await voiceClient.ConnectAsync(channel);
        }

        public async Task JoinChannelById(ulong id)
        {
            var channel = GetChannelById(id);        
            await JoinChannel(channel);
        }

        public async Task JoinChannelByName(string channelName)
        {
            var channel = GetChannelByName(channelName);
            await JoinChannel(channel);
        }

        public void Disconnect()
        {
            if (_currentServer != null)
            {
                var voiceClient = _client.GetVoiceNext();
                var currentConnection = voiceClient?.GetConnection(_currentServer);

                _log.Debug("Disconnecting from channel {channelName} current server {serverName}", currentConnection?.TargetChannel.Name ?? "null", _currentServer?.Name ?? "null");

                currentConnection?.Disconnect();
            }
        }

        public VoiceNextConnection? GetVoiceConnection()
        {
            var voiceClient = _client.GetVoiceNext();

            if(_currentServer == null || voiceClient.GetConnection(_currentServer) == null)
            {
                _log.Info("Failed to find voice connection on server {serverName}", _currentServer?.Name ?? "null");
                return null;
            }

            return voiceClient.GetConnection(_currentServer);
        }

        public bool VoiceChannelJoined()
        {
            var voiceClient = _client.GetVoiceNext();
            return _currentServer != null && voiceClient.GetConnection(_currentServer) != null;        
        }
    }
}
