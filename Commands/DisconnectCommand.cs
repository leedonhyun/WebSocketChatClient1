using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

// Example: Disconnect Command
public class DisconnectCommand : CommandBase
{
    public DisconnectCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        return _client.DisconnectAsync();
    }
}