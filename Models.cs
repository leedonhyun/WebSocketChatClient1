using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatSystem.Client.Models;

public class FileUploadResult
{
    public string FileId { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string ContentType { get; set; } = "";
    public List<byte[]> Chunks { get; set; } = new();
}

public class ParsedCommand
{
    public string Command { get; set; } = "";
    public string[] Arguments { get; set; } = Array.Empty<string>();
    public Dictionary<string, object> Options { get; set; } = new();
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = "";
}

public class ConnectionSettings
{
    public string ServerUrl { get; set; } = "ws://localhost:5106/ws";
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
    public int MaxReconnectAttempts { get; set; } = 3;
}

// =============== 확장된 채팅 메시지 모델 ===============

public class ExtendedChatMessage
{
    public string Type { get; set; } = "";
    public string Username { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ToUsername { get; set; }    // 1:1 개인 메시지용
    public string? RoomId { get; set; }        // 그룹 방 메시지용
    public string? RoomName { get; set; }      // 방 이름
    public ChatMessageType MessageType { get; set; } = ChatMessageType.Public;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // 방 관련 추가 정보
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// =============== 그룹 채팅 관련 모델 ===============

public class ChatRoom
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ChatRoomType Type { get; set; } = ChatRoomType.Public;
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<string> Members { get; set; } = new();
    public int MaxMembers { get; set; } = 100;
    public bool IsPrivate { get; set; } = false;
    public string? Password { get; set; }
}

public class ChatRoomInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public ChatRoomType Type { get; set; } = ChatRoomType.Public;
    public int MemberCount { get; set; }
    public int MaxMembers { get; set; } = 100;
    public bool IsPrivate { get; set; } = false;
    public bool RequiresPassword { get; set; } = false;
}

public class PrivateConversation
{
    public string Id { get; set; } = "";
    public string User1 { get; set; } = "";
    public string User2 { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
}

public enum ChatRoomType
{
    Public = 0,
    Private = 1,
    Group = 2,
    DirectMessage = 3
}

public enum ChatMessageType
{
    Public = 0,      // 전체 공개 메시지
    Room = 1,        // 특정 방 메시지
    Private = 2,     // 1:1 개인 메시지
    System = 3,      // 시스템 메시지
    RoomJoin = 4,    // 방 입장 알림
    RoomLeave = 5,   // 방 퇴장 알림
    UserList = 6,    // 사용자 목록
    RoomList = 7     // 방 목록
}
