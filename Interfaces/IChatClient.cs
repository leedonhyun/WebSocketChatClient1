using ChatSystem.Client.Models;
using ChatSystem.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebSocketChatClient1.Interfaces;

public interface IChatClient
{
    event Action<ChatMessage>? MessageReceived;
    event Action<string>? StatusChanged;
    event Action<FileTransferInfo>? FileOfferReceived;
    event Action<string, int, int>? FileTransferProgress;

    bool IsConnected { get; }
    string Username { get; }
    string? CurrentRoom { get; }

    Task<bool> ConnectAsync(string serverUrl);
    Task DisconnectAsync();
    Task SendMessageAsync(string message);
    Task SendPrivateMessageAsync(string message, string toUsername);
    Task SendRoomMessageAsync(string message, string roomId);
    Task SendFileAsync(string filePath, string? toUsername = null, bool autoAccept = false, string? roomId = null);
    Task AcceptFileAsync(string fileId);
    Task RejectFileAsync(string fileId);
    Task SetUsernameAsync(string username);
    Task GetUserListAsync();

    // 그룹 채팅 기능
    Task CreateRoomAsync(string roomName, string description = "", bool isPrivate = false, string? password = null);
    Task JoinRoomAsync(string roomId, string? password = null);
    Task LeaveRoomAsync(string? roomId = null);
    Task GetRoomListAsync();
    Task GetRoomMembersAsync(string? roomId = null);
    Task InviteToRoomAsync(string roomId, string username);
    Task KickFromRoomAsync(string roomId, string username);
}