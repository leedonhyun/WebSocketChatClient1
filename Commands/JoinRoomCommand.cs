using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

public class JoinRoomCommand : CommandBase
{
    public JoinRoomCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length > 0)
        {
            var roomId = args[0];
            var password = args.Length > 1 ? args[1] : null;
            return _client.JoinRoomAsync(roomId, password);
        }
        
        _statusChanged(ClientConstants.UsageMessages.JoinRoomUsage);
        _statusChanged(ClientConstants.UsageMessages.JoinRoomExample);
        return Task.CompletedTask;
    }
}