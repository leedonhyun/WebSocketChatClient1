using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

using WebSocketChatShared;

namespace WebSocketChatClient1.Client.Commands;

// Example: Connect Command
public class ConnectCommand : CommandBase
{
    public ConnectCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        var url = args.Length > 0 ? args[0] : ChatConstants.DefaultServerUrl;
        return _client.ConnectAsync(url);
    }
}