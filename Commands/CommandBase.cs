using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

// Base class to provide access to the client and status updates
public abstract class CommandBase : ICommand
{
    protected readonly IChatClient _client;
    protected readonly Action<string> _statusChanged;

    protected CommandBase(IChatClient client, Action<string> statusChanged)
    {
        _client = client;
        _statusChanged = statusChanged;
    }

    public abstract Task ExecuteAsync(string[] args, Dictionary<string, object> options);

    protected static bool IsRoomId(string input) => Guid.TryParse(input, out _);
}