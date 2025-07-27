using WebSocketChatClient1.Client.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Services;


public class ChatRoomManager : IChatRoomManager
{
    private readonly ILogger<ChatRoomManager> _logger;
    private string? _currentRoom;
    private readonly Dictionary<string, ChatRoomInfo> _roomCache = new();
    private readonly Dictionary<string, List<string>> _roomMembersCache = new();

    public event Action<string?>? CurrentRoomChanged;

    public ChatRoomManager(ILogger<ChatRoomManager> logger)
    {
        _logger = logger;
    }

    public async Task<List<ChatRoomInfo>> GetAvailableRoomsAsync()
    {
        // 실제 구현에서는 서버로부터 방 목록을 가져와야 함
        await Task.CompletedTask;
        return _roomCache.Values.ToList();
    }

    public async Task<ChatRoom?> GetRoomInfoAsync(string roomId)
    {
        // 실제 구현에서는 서버로부터 방 정보를 가져와야 함
        await Task.CompletedTask;

        if (_roomCache.TryGetValue(roomId, out var roomInfo))
        {
            return new ChatRoom
            {
                Id = roomInfo.Id,
                Name = roomInfo.Name,
                Description = roomInfo.Description,
                Type = roomInfo.Type,
                MaxMembers = roomInfo.MaxMembers,
                IsPrivate = roomInfo.IsPrivate
            };
        }

        return null;
    }

    public async Task<List<string>> GetRoomMembersAsync(string roomId)
    {
        await Task.CompletedTask;
        return _roomMembersCache.TryGetValue(roomId, out var members) ? members : new List<string>();
    }

    public void UpdateCurrentRoom(string? roomId)
    {
        if (_currentRoom != roomId)
        {
            var previousRoom = _currentRoom;
            _currentRoom = roomId;
            CurrentRoomChanged?.Invoke(_currentRoom);

            _logger.LogInformation($"Current room changed from '{previousRoom}' to '{_currentRoom}'");
        }
    }

    public string? GetCurrentRoom()
    {
        return _currentRoom;
    }

    public void UpdateRoomCache(string roomId, ChatRoomInfo roomInfo)
    {
        _roomCache[roomId] = roomInfo;
    }

    public void UpdateRoomMembersCache(string roomId, List<string> members)
    {
        _roomMembersCache[roomId] = members;
    }

    public void ClearRoomCache()
    {
        _roomCache.Clear();
        _roomMembersCache.Clear();
    }
}

