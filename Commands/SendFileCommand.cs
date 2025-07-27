using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

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

        _statusChanged(ClientConstants.UsageMessages.SendUsage);
        _statusChanged(ClientConstants.UsageMessages.SendExamplesHeader);
        _statusChanged(ClientConstants.UsageMessages.SendExamplePublic);
        _statusChanged(ClientConstants.UsageMessages.SendExampleUser);
        _statusChanged(ClientConstants.UsageMessages.SendExampleRoom);
        _statusChanged(ClientConstants.UsageMessages.SendExampleAuto);
        return Task.CompletedTask;
    }
}