using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using WebSocketChatClient1.Interfaces;

namespace ChatSystem.Client.Commands;

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

// Example: Connect Command
public class ConnectCommand : CommandBase
{
    public ConnectCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        var url = args.Length > 0 ? args[0] : ClientConstants.DefaultServerUrl;
        return _client.ConnectAsync(url);
    }
}

// Example: Disconnect Command
public class DisconnectCommand : CommandBase
{
    public DisconnectCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        return _client.DisconnectAsync();
    }
}

public class SetUsernameCommand : CommandBase
{
    public SetUsernameCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length > 0)
            return _client.SetUsernameAsync(string.Join(" ", args));
        
        _statusChanged("Usage: /username <name>");
        return Task.CompletedTask;
    }
}

public class GetUserListCommand : CommandBase
{
    public GetUserListCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        return _client.GetUserListAsync();
    }
}

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

public class AcceptFileCommand : CommandBase
{
    public AcceptFileCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length > 0)
            return _client.AcceptFileAsync(args[0]);
        
        _statusChanged("Usage: /accept <fileId>");
        return Task.CompletedTask;
    }
}

public class RejectFileCommand : CommandBase
{
    public RejectFileCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length > 0)
            return _client.RejectFileAsync(args[0]);

        _statusChanged("Usage: /reject <fileId>");
        return Task.CompletedTask;
    }
}

// Example: Send Private Message Command
public class PrivateMessageCommand : CommandBase
{
    public PrivateMessageCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        if (args.Length >= 2)
        {
            var targetUser = args[0];
            var message = string.Join(" ", args.Skip(1));
            return _client.SendPrivateMessageAsync(message, targetUser);
        }
        
        _statusChanged?.Invoke(ClientConstants.UsageMessages.PrivateMessageUsage);
        _statusChanged?.Invoke(ClientConstants.UsageMessages.PrivateMessageExample);
        //_statusChanged?.Invoke(ClientConstants.UsageMessages.PrivateMessageAliases);
        return Task.CompletedTask;
    }
}

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
        
        _statusChanged(ClientConstants.UsageMessages.CreateRoomUsage);
        _statusChanged(ClientConstants.UsageMessages.CreateRoomExample);
        return Task.CompletedTask;
    }
}

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

public class LeaveRoomCommand : CommandBase
{
    public LeaveRoomCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        var targetRoomId = args.Length > 0 ? args[0] : null;
        return _client.LeaveRoomAsync(targetRoomId);
    }
}

public class GetRoomListCommand : CommandBase
{
    public GetRoomListCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        return _client.GetRoomListAsync();
    }
}

public class GetRoomMembersCommand : CommandBase
{
    public GetRoomMembersCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        var roomForMembers = args.Length > 0 ? args[0] : null;
        return _client.GetRoomMembersAsync(roomForMembers);
    }
}

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
        
        _statusChanged(ClientConstants.UsageMessages.KickUsage);
        return Task.CompletedTask;
    }
}

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
        
        _statusChanged(ClientConstants.UsageMessages.RoomMessageUsage);
        _statusChanged(ClientConstants.UsageMessages.RoomMessageAltUsage);
        _statusChanged(ClientConstants.UsageMessages.RoomMessageExample);
        if (!string.IsNullOrEmpty(_client.CurrentRoom))
        {
            _statusChanged(string.Format(ClientConstants.StatusMessages.CurrentRoomInfo, _client.CurrentRoom));
        }
        return Task.CompletedTask;
    }
}

// Example: Help Command
public class HelpCommand : CommandBase
{
    public HelpCommand(IChatClient client, Action<string> statusChanged) : base(client, statusChanged) { }

    public override Task ExecuteAsync(string[] args, Dictionary<string, object> options)
    {
        foreach (var line in ClientConstants.HelpText)
        {
            _statusChanged?.Invoke(line);
        }
        return Task.CompletedTask;
    }
}