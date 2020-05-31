using DSharpPlus.CommandsNext;
using System.Threading.Tasks;

namespace DadaBot.Commands
{
    public interface ICommandImplementations
    {
        public bool IsOwner(ulong userId);
        public Task<(bool success, string answer)> Greet(string serverName, string? channelName);
        public Task<(bool res, string output)> Play(CommandContext? ctx = null);
        public void StopPlayback();
        public Task<(bool res, string output)> AutoJoin(CommandContext? ctx = null);
        public Task<(bool res, string output)> JoinVoiceChannel(string identifier);
        public void Disconnect();
    }
}
