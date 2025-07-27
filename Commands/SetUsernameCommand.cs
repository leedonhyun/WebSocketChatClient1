using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

public class SetUsernameCommand : CommandBase
{
    public SetUsernameCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length > 0)
            return _client.SetUsernameAsync(string.Join(" ", args));
        
        _statusChanged("Usage: /username <name>");
        return Task.CompletedTask;
    }
}