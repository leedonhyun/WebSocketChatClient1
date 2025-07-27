using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

public class RejectFileCommand : CommandBase
{
    public RejectFileCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length > 0)
            return _client.RejectFileAsync(args[0]);

        _statusChanged("Usage: /reject <fileId>");
        return Task.CompletedTask;
    }
}