using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

using WebSocketChatShared;

namespace WebSocketChatClient1.Client.Commands;

public class CreateRoomCommand : CommandBase
{
    public CreateRoomCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length > 0)
        {
            var roomName = args[0];
            var description = args.Length > 1 ? args[1] : "";
            var isPrivate = options.ContainsKey("private") || options.ContainsKey("p");
            var password = options.TryGetValue("password", out var pwd) ? pwd?.ToString() : null;
            return _client.CreateRoomAsync(roomName, description, isPrivate, password);
        }
        
        _statusChanged(ChatConstants.UsageMessages.CreateRoomUsage);
        _statusChanged(ChatConstants.UsageMessages.CreateRoomExample);
        return Task.CompletedTask;
    }
}