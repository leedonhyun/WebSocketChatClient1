using System.Threading.Tasks;

namespace WebSocketChatClient1.Client.Interfaces;

/// <summary>
/// Defines operations for handling chat messages and room management.
/// </summary>
public interface IChatHandler
{
    Task SendMessageAsync(string message);
    Task SendPrivateMessageAsync(string message, string toUsername);
    Task SendRoomMessageAsync(string message, string roomId);
    Task SetUsernameAsync(string username);
    Task GetUserListAsync();
    Task CreateRoomAsync(string roomName, string description = "", bool isPrivate = false, string? password = null);
    Task JoinRoomAsync(string roomId, string? password = null);
    Task LeaveRoomAsync(string? roomId = null);
    Task GetRoomListAsync();
    Task GetRoomMembersAsync(string? roomId = null);
    Task InviteToRoomAsync(string roomId, string username);
    Task KickFromRoomAsync(string roomId, string username);
}