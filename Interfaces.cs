using ChatSystem.Client.Models;
using ChatSystem.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatSystem.Client.Interfaces;

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

public interface IConnectionManager : IDisposable
{
    Task<bool> ConnectAsync(string serverUrl, CancellationToken cancellationToken);
    Task DisconnectAsync();
    bool IsConnected { get; }
    event Action<string>? StatusChanged;
}

public interface IMessageProcessor<T> where T : BaseMessage
{
    Task ProcessAsync(T message);
}

public interface IFileManager
{
    Task<string> SaveFileAsync(string fileName, byte[] data, string? senderUsername = null);
    Task<byte[]> ReadFileAsync(string filePath);
    Task<FileUploadResult> UploadFileAsync(string filePath);
    string GetDownloadPath(string? senderUsername = null);
}

public interface ICommandParser
{
    ParsedCommand Parse(string input);
}

public interface IChatRoomManager
{
    Task<List<ChatRoomInfo>> GetAvailableRoomsAsync();
    Task<ChatRoom?> GetRoomInfoAsync(string roomId);
    Task<List<string>> GetRoomMembersAsync(string roomId);
    void UpdateCurrentRoom(string? roomId);
    string? GetCurrentRoom();
    event Action<string?>? CurrentRoomChanged;
}
