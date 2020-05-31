using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using DSharpPlus;
using System;

namespace DadaBot.Commands
{
    public class DiscordCommands : BaseCommandModule
    {
        [Command("Greet"), RequireOwner]
        [Aliases("Sayhi")]
        public async Task Greet(CommandContext ctx, string serverName, string? channelName = null)
        {
            await ctx.TriggerTypingAsync();

            var log = (DebugLogger)ctx.Services.GetService(typeof(DebugLogger));
            var impl = (ICommandImplementations) ctx.Services.GetService(typeof(ICommandImplementations));            

            (var res, var output) = await impl.Greet(serverName, channelName);

            await ctx.RespondAsync(output);

            if(!res)
            { 
                log.LogMessage(LogLevel.Info, "DadaBot", $"Failed to greet server {serverName} in channel {channelName ?? "The system channel"}.", DateTime.Now);
            }
        }

        [Command("Join"), RequireOwner]
        public async Task Join(CommandContext ctx, string? identifier = null)
        {
            await ctx.TriggerTypingAsync();

            var log = (DebugLogger)ctx.Services.GetService(typeof(DebugLogger));
            var impl = (ICommandImplementations)ctx.Services.GetService(typeof(ICommandImplementations));

            if (identifier == null)
            {
                var (res, output) = await impl.AutoJoin(ctx);
                await ctx.RespondAsync(output);

                if (!res)
                {
                    log.LogMessage(LogLevel.Error, "DadaBot", $"Failed to join caller: {output}.", DateTime.Now);
                }
            }
            else
            {
                var (res, output) = await impl.JoinVoiceChannel(identifier);
                await ctx.RespondAsync(output);

                if (!res)
                {
                    log.LogMessage(LogLevel.Info, "DadaBot", $"Could not join channel {identifier}. Output: {output}.", DateTime.Now);
                }
            }
        }

        [Command("LeaveChannel"), RequireOwner]
        [Aliases("Quit", "Leave", "Scram", "Go")]
        public async Task LeaveChannel(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();
                        
            var impl = (ICommandImplementations)ctx.Services.GetService(typeof(ICommandImplementations));
            impl.Disconnect();

            await ctx.RespondAsync("See you later!");
        }        

        [Command("Play"), RequireOwner]
        public async Task Play(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            var impl = (ICommandImplementations)ctx.Services.GetService(typeof(ICommandImplementations));
            var log = (DebugLogger)ctx.Services.GetService(typeof(DebugLogger));

            var (res, output) = await impl.Play(ctx);

            await ctx.RespondAsync(output);

            if(!res)
            {
                log.LogMessage(LogLevel.Info, "DadaBot", $"Could not start playback: {output}", DateTime.Now);
            }
        }

        [Command("Stop"), RequireOwner]
        public async Task Stop(CommandContext ctx)
        {            
            var impl = (ICommandImplementations)ctx.Services.GetService(typeof(ICommandImplementations));

            impl.StopPlayback();
                        
            await ctx.RespondAsync("Stopped.");
        }      
    }
}
