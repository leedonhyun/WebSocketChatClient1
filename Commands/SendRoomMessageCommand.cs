using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

using WebSocketChatShared;

namespace WebSocketChatClient1.Client.Commands;

public class SendRoomMessageCommand : CommandBase
{
    public SendRoomMessageCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length >= 2)
        {
            var roomId = args[0];
            var message = string.Join(" ", args.Skip(1));
            return _client.SendRoomMessageAsync(message, roomId);
        }
        else if (args.Length == 1 && !string.IsNullOrEmpty(_client.CurrentRoom))
        {
            var message = args[0];
            return _client.SendRoomMessageAsync(message, _client.CurrentRoom);
        }
        
        _statusChanged(ChatConstants.UsageMessages.RoomMessageUsage);
        _statusChanged(ChatConstants.UsageMessages.RoomMessageAltUsage);
        _statusChanged(ChatConstants.UsageMessages.RoomMessageExample);
        if (!string.IsNullOrEmpty(_client.CurrentRoom))
        {
            _statusChanged(string.Format(ChatConstants.StatusMessages.CurrentRoomInfo, _client.CurrentRoom));
        }
        return Task.CompletedTask;
    }
}