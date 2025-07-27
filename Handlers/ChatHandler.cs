using WebSocketChatClient1.Client.Connection;
using WebSocketChatClient1.Client.Interfaces;
using WebSocketChatShared.Models;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using WebSocketChatClient1.Interfaces;
using WebSocketChatShared;

namespace WebSocketChatClient1.Client.Handlers;

/// <summary>
/// Handles the logic for chat messages and room management.
/// </summary>
public class ChatHandler : IChatHandler
{
    private readonly IConnectionManager _connectionManager;
    private readonly Action<string> _statusChanged;
    private readonly Func<bool> _isConnected;
    private readonly Func<string> _getUsername;
    private readonly Func<string?> _getCurrentRoom;
    private readonly Action<string> _setUsername;
    private readonly Action<string?> _setCurrentRoom;

    public ChatHandler(
        IConnectionManager connectionManager,
        Action<string> statusChanged,
        Func<bool> isConnected,
        Func<string> getUsername,
        Func<string?> getCurrentRoom,
        Action<string> setUsername,
        Action<string?> setCurrentRoom)
    {
        _connectionManager = connectionManager;
        _statusChanged = statusChanged;
        _isConnected = isConnected;
        _getUsername = getUsername;
        _getCurrentRoom = getCurrentRoom;
        _setUsername = setUsername;
        _setCurrentRoom = setCurrentRoom;
    }

    public Task SendMessageAsync(string message)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var chatMessage = new ChatMessage { Type = ChatConstants.MessageTypes.Chat, Message = message, Timestamp = DateTime.UtcNow };
        return SendChatMessageAsync(chatMessage);
    }

    public Task SendPrivateMessageAsync(string message, string toUsername)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var chatMessage = new ChatMessage { Type = ChatConstants.MessageTypes.PrivateMessage,
            ToUsername = toUsername,
            Message = message,
            Timestamp = DateTime.UtcNow };
        _statusChanged(string.Format(ChatConstants.StatusMessages.PrivateMessageSent, toUsername));
        return SendChatMessageAsync(chatMessage);
    }

    public Task SendRoomMessageAsync(string message, string roomId)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var chatMessage = new ChatMessage { Type = ChatConstants.MessageTypes.RoomMessage,
            Message = $"roomMessage {roomId} {message}", Username = _getUsername(), Timestamp = DateTime.UtcNow };
        _statusChanged(string.Format(ChatConstants.StatusMessages.RoomMessageSent, roomId, message));
        return SendChatMessageAsync(chatMessage);
    }

    public Task SetUsernameAsync(string username)
    {
        if (!_isConnected()) return Task.CompletedTask;
        _setUsername(username);
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.SetUsername, Message = username, Timestamp = DateTime.UtcNow };
        _statusChanged(string.Format(ChatConstants.StatusMessages.UsernameSet, username));
        return SendChatMessageAsync(message);
    }

    public Task GetUserListAsync()
    {
        if (!_isConnected()) return Task.CompletedTask;
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.ListUsers, Timestamp = DateTime.UtcNow };
        return SendChatMessageAsync(message);
    }

    public Task CreateRoomAsync(string roomName, string description = "", bool isPrivate = false, string? password = null)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.CreateRoom, Message = $"{roomName}{ChatConstants.CommandArgSeparator}{description}{ChatConstants.CommandArgSeparator}{isPrivate}{ChatConstants.CommandArgSeparator}{password ?? ""}", Timestamp = DateTime.UtcNow };
        _statusChanged(string.Format(ChatConstants.StatusMessages.RoomCreating, roomName));
        return SendChatMessageAsync(message);
    }

    public Task JoinRoomAsync(string roomId, string? password = null)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.JoinRoom, Message = $"{roomId}{ChatConstants.CommandArgSeparator}{password ?? ""}", Timestamp = DateTime.UtcNow };
        _setCurrentRoom(roomId); // Temporarily set room, server response will confirm
        _statusChanged(string.Format(ChatConstants.StatusMessages.RoomJoining, roomId));
        return SendChatMessageAsync(message);
    }

    public Task LeaveRoomAsync(string? roomId = null)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var targetRoom = roomId ?? _getCurrentRoom();
        if (string.IsNullOrEmpty(targetRoom))
        {
            _statusChanged(ChatConstants.ErrorMessages.NoRoomToLeave);
            return Task.CompletedTask;
        }
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.LeaveRoom, Message = targetRoom, Timestamp = DateTime.UtcNow };
        if (targetRoom == _getCurrentRoom())
        {
            _setCurrentRoom(null);
        }
        _statusChanged(string.Format(ChatConstants.StatusMessages.RoomLeftTarget, targetRoom));
        return SendChatMessageAsync(message);
    }

    public Task GetRoomListAsync()
    {
        if (!_isConnected()) return Task.CompletedTask;
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.ListRooms, Timestamp = DateTime.UtcNow };
        return SendChatMessageAsync(message);
    }

    public Task GetRoomMembersAsync(string? roomId = null)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var targetRoom = roomId ?? _getCurrentRoom();
        if (string.IsNullOrEmpty(targetRoom))
        {
            _statusChanged(ChatConstants.ErrorMessages.NoRoomSpecified);
            return Task.CompletedTask;
        }
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.ListRoomMembers, Message = targetRoom, Timestamp = DateTime.UtcNow };
        return SendChatMessageAsync(message);
    }

    public Task InviteToRoomAsync(string roomId, string username)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.InviteToRoom, Message = $"{roomId}{ChatConstants.CommandArgSeparator}{username}", Timestamp = DateTime.UtcNow };
        _statusChanged(string.Format(ChatConstants.StatusMessages.InvitingUser, username, roomId));
        return SendChatMessageAsync(message);
    }

    public Task KickFromRoomAsync(string roomId, string username)
    {
        if (!_isConnected()) return Task.CompletedTask;
        var message = new ChatMessage { Type = ChatConstants.MessageTypes.KickFromRoom, Message = $"{roomId}{ChatConstants.CommandArgSeparator}{username}", Timestamp = DateTime.UtcNow };
        _statusChanged(string.Format(ChatConstants.StatusMessages.KickingUser, username, roomId));
        return SendChatMessageAsync(message);
    }

    private async Task SendChatMessageAsync(ChatMessage message)
    {
        if (_connectionManager is WebSocketConnectionManager wsManager && wsManager.MessageChannel != null)
        {
            var json = JsonSerializer.Serialize(message);
            var buffer = Encoding.UTF8.GetBytes(json + "\n");
            await wsManager.MessageChannel.Output.WriteAsync(buffer.AsMemory());
            await wsManager.MessageChannel.Output.FlushAsync();
        }
    }
}