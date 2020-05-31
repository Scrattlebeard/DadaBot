namespace DadaBot.Configuration
{
    public class DiscordSettings
    {
        public string Token { get; set; } = string.Empty;
        public string LogLevel { get; set; } = "Debug";
        public ulong OwnerId { get; set; } = 128524271700934657; //Dada
        public bool Autojoin { get; set; } = true;
    }
}
