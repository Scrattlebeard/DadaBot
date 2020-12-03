using System;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NLog;

namespace DadaBot.Commands
{
    public class ConsoleCommands
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly ICommandImplementations _impl;

        public ConsoleCommands(ICommandImplementations impl)
        {
            _impl = impl;
        }

        public async Task Listen()
        {
            while(true)
            {
                if (Console.KeyAvailable)
                {
                    var data = Console.ReadLine();

                    try
                    {                        
                        var words = data.Split(' ');
                        var command = words[0].ToLower();
                        var args = words.Skip(1).ToArray();

                        var exit = await ExecuteCommand(command, args);

                        if (exit)
                        {
                            break;
                        }
                    }
                    catch(Exception e)
                    {
                        Trace.WriteLine($"Failed to execute command: \"{data}\". Error: {e}");
                    }
                }

                await Task.Delay(200);
            }
        }

        public async Task<bool> ExecuteCommand(string command, string[] args)
        {        
            switch (command)
            {
                case "exit":
                {
                    _log.Info("Received exit command, terminating main loop. See you next time!");
                    _impl.Disconnect();
                    return true;
                }
                case "j":
                case "join":
                {
                    if (args.Any())
                    {
                        var (res, output) = await _impl.JoinVoiceChannel(args.First());
                        HandleCommandResult(res, output);
                    }
                    else
                    {
                        var(res, output) = await _impl.AutoJoin();
                        HandleCommandResult(res, output);
                    }
                    return false;
                }
                case "l":
                case "leave":
                case "q":
                case "quit":
                {
                    _impl.Disconnect();
                    HandleCommandResult(true, "Disconnected.");
                    return false;
                }
                case "p":
                case "play":
                {
                    var (res, output) = await _impl.Play();
                    HandleCommandResult(res, output);
                    return false;
                }
                case "s":
                case "stop":
                {
                    _impl.StopPlayback();
                    HandleCommandResult(true, "Stopped.");
                    return false;
                }
                case "greet":
                {
                    if (args.Any())
                    {
                        var channelName = args.Length > 1 ? args[1] : null;
                        var (res, output) = await _impl.Greet(args[0], channelName);
                        HandleCommandResult(res, output);
                    }
                    else
                    {
                        HandleCommandResult(false, "You didn't specify which server to greet.");
                    }
                    return false;
                }
                default:
                {
                    _log.Info("Unrecognized console command: {command}.", command);
                    return false;
                }
            }
        }

        private void HandleCommandResult(bool res, string output)
        {
            _log.Info($"{(res ? "Success" : "Failed to execute command")}: {output}");
        }
    }    
}
