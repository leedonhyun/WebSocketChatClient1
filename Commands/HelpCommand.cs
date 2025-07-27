using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

// Example: Help Command
public class HelpCommand : CommandBase
{
    public HelpCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        foreach (var line in ClientConstants.HelpText)
        {
            _statusChanged?.Invoke(line);
        }
        return Task.CompletedTask;
    }
}