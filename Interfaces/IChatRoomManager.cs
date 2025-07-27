using WebSocketChatClient1.Client.Models;
using WebSocketChatShared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebSocketChatClient1.Interfaces;

public interface IChatRoomManager
{
    Task<List<ChatRoomInfo>> GetAvailableRoomsAsync();
    Task<ChatRoom?> GetRoomInfoAsync(string roomId);
    Task<List<string>> GetRoomMembersAsync(string roomId);
    void UpdateCurrentRoom(string? roomId);
    string? GetCurrentRoom();
    event Action<string?>? CurrentRoomChanged;
}