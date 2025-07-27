using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

public class InviteToRoomCommand : CommandBase
{
    public InviteToRoomCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length >= 2)
        {
            var roomId = args[0];
            var username = args[1];
            return _client.InviteToRoomAsync(roomId, username);
        }
        
        _statusChanged(ClientConstants.UsageMessages.InviteUsage);
        return Task.CompletedTask;
    }
}