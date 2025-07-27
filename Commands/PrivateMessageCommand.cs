using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

using WebSocketChatShared;

namespace WebSocketChatClient1.Client.Commands;

// Example: Send Private Message Command
public class PrivateMessageCommand : CommandBase
{
    public PrivateMessageCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length >= 2)
        {
            var targetUser = args[0];
            //var message = string.Join(" ", args.Skip(1));
            var message = string.Join(" ", args);
            return _client.SendPrivateMessageAsync(message, targetUser);
        }
        
        _statusChanged?.Invoke(ChatConstants.UsageMessages.PrivateMessageUsage);
        _statusChanged?.Invoke(ChatConstants.UsageMessages.PrivateMessageExample);
        //_statusChanged?.Invoke(ChatConstants.UsageMessages.PrivateMessageAliases);
        return Task.CompletedTask;
    }
}