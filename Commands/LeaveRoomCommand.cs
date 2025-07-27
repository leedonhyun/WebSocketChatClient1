using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

public class LeaveRoomCommand : CommandBase
{
    public LeaveRoomCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        var targetRoomId = args.Length > 0 ? args[0] : null;
        return _client.LeaveRoomAsync(targetRoomId);
    }
}