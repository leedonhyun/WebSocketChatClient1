using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

using WebSocketChatShared;

namespace WebSocketChatClient1.Client.Commands;

public class SendFileCommand : CommandBase
{
    public SendFileCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        var autoAccept = options.ContainsKey("a");

        if (args.Length > 0)
        {
            var filePath = args[0];
            string? targetUser = null;
            string? roomId = null;

            if (args.Length > 1)
            {
                var target = args[1];
                if (IsRoomId(target))
                    roomId = target;
                else
                    targetUser = target;
            }
            return _client.SendFileAsync(filePath, targetUser, autoAccept, roomId);
        }

        _statusChanged(ChatConstants.UsageMessages.SendUsage);
        _statusChanged(ChatConstants.UsageMessages.SendExamplesHeader);
        _statusChanged(ChatConstants.UsageMessages.SendExamplePublic);
        _statusChanged(ChatConstants.UsageMessages.SendExampleUser);
        _statusChanged(ChatConstants.UsageMessages.SendExampleRoom);
        _statusChanged(ChatConstants.UsageMessages.SendExampleAuto);
        return Task.CompletedTask;
    }
}