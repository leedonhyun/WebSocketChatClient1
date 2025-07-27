using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

using WebSocketChatShared;

namespace WebSocketChatClient1.Client.Commands;

public class KickFromRoomCommand : CommandBase
{
    public KickFromRoomCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length >= 2)
        {
            var roomId = args[0];
            var username = args[1];
            return _client.KickFromRoomAsync(roomId, username);
        }
        
        _statusChanged(ChatConstants.UsageMessages.KickUsage);
        return Task.CompletedTask;
    }
}