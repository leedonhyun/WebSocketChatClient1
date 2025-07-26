using ChatSystem.Client.Interfaces;
using ChatSystem.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatSystem.Client.Console;

public class ConsoleApplication
{
    private readonly IChatClient _chatClient;
    private readonly ChatClient _chatClientImpl; // 구체적인 타입 캐시
    private readonly Dictionary<string, FileTransferInfo> _pendingFiles = new();

    public ConsoleApplication(IChatClient chatClient)
    {
        _chatClient = chatClient;
        _chatClientImpl = (ChatClient)chatClient; // 한 번만 캐스팅

        _chatClient.MessageReceived += OnMessageReceived;
        _chatClient.StatusChanged += OnStatusChanged;
        _chatClient.FileOfferReceived += OnFileOfferReceived;
        _chatClient.FileTransferProgress += OnFileTransferProgress;
    }

    public async Task RunAsync()
    {
        System.Console.WriteLine("=== Advanced WebSocket Chat Client with Group Support ===");
        System.Console.WriteLine("Basic Commands:");
        System.Console.WriteLine("  /connect [url] - Connect to server");
        System.Console.WriteLine("  /disconnect - Disconnect from server");
        System.Console.WriteLine("  /username <name> - Set username");
        System.Console.WriteLine("  /users - List online users");
        System.Console.WriteLine("  /quit - Exit application");
        System.Console.WriteLine();
        System.Console.WriteLine("Chat Commands:");
        System.Console.WriteLine("  /msg <user> <message> - Send private message");
        System.Console.WriteLine("  /pm <user> <message> - Send private message (alias)");
        System.Console.WriteLine("  /private <user> <message> - Send private message (alias)");
        System.Console.WriteLine("  /privateMessage <user> <message> - Send private message (alias)");
        System.Console.WriteLine("  /room <roomid> <message> - Send message to specific room");
        System.Console.WriteLine("  /room <message> - Send message to current room (if joined)");
        System.Console.WriteLine();
        System.Console.WriteLine("Room Commands:");
        System.Console.WriteLine("  /create <name> [desc] [-private] [-password <pwd>] - Create room");
        System.Console.WriteLine("  /join <roomid> [password] - Join room");
        System.Console.WriteLine("  /leave [roomid] - Leave room");
        System.Console.WriteLine("  /rooms - List available rooms");
        System.Console.WriteLine("  /members [roomid] - List room members");
        System.Console.WriteLine("  /invite <roomid> <user> - Invite user to room");
        System.Console.WriteLine("  /kick <roomid> <user> - Kick user from room");
        System.Console.WriteLine();
        System.Console.WriteLine("File Commands:");
        System.Console.WriteLine("  /send [-a] <filepath> [username|roomid] - Send file to user or room");
        System.Console.WriteLine("  /accept <fileId> - Accept incoming file");
        System.Console.WriteLine("  /reject <fileId> - Reject incoming file");
        System.Console.WriteLine();
        System.Console.WriteLine("Note: Any message not starting with '/' will be sent as public chat");
        if (!string.IsNullOrEmpty(_chatClient.CurrentRoom))
        {
            System.Console.WriteLine($"       Currently in room: {_chatClient.CurrentRoom}");
        }
        System.Console.WriteLine();

        while (true)
        {
            // 현재 방 상태를 프롬프트에 표시
            var prompt = "";
            if (_chatClient.IsConnected)
            {
                if (!string.IsNullOrEmpty(_chatClient.CurrentRoom))
                {
                    prompt = $"[{_chatClient.CurrentRoom}] > ";
                }
                else
                {
                    prompt = "[Public] > ";
                }
            }
            else
            {
                prompt = "[Disconnected] > ";
            }

            System.Console.Write(prompt);
            var input = System.Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            if (input.StartsWith("/"))
            {
                if (input.Equals("/quit", StringComparison.OrdinalIgnoreCase))
                {
                    await _chatClient.DisconnectAsync();
                    break;
                }

                await _chatClientImpl.ProcessCommandAsync(input);
            }
            else
            {
                if (_chatClient.IsConnected)
                {
                    // Check if user is in a room and wants to send to current room
                    if (!string.IsNullOrEmpty(_chatClient.CurrentRoom))
                    {
                        await _chatClientImpl.SendRoomMessageAsync(input, _chatClient.CurrentRoom);
                    }
                    else
                    {
                        await _chatClient.SendMessageAsync(input);
                    }
                }
                else
                    System.Console.WriteLine("Not connected to server. Use /connect to connect.");
            }
        }
    }

    private static void OnMessageReceived(ChatMessage message)
    {
        var timestamp = message.Timestamp.ToString("HH:mm:ss");
        switch (message.Type)
        {
            case "system":
                System.Console.WriteLine($"[{timestamp}] * {message.Message}");
                break;
            case "chat":
                System.Console.WriteLine($"[{timestamp}] {message.Username}: {message.Message}");
                break;
            case "privateMessage":
                System.Console.WriteLine($"[{timestamp}] [PRIVATE] {message.Username}: {message.Message}");
                break;
            case "roomMessage":
                System.Console.WriteLine($"[{timestamp}] [ROOM] {message.Username}: {message.Message}");
                break;
            case "userList":
                System.Console.WriteLine($"[{timestamp}] Online users: {message.Message}");
                break;
            case "roomList":
                System.Console.WriteLine($"[{timestamp}] Available rooms: {message.Message}");
                break;
            case "roomMembers":
                System.Console.WriteLine($"[{timestamp}] Room members: {message.Message}");
                break;
            case "roomJoined":
                System.Console.WriteLine($"[{timestamp}] ✓ Joined room: {message.Message}");
                break;
            case "roomLeft":
                System.Console.WriteLine($"[{timestamp}] ← Left room: {message.Message}");
                break;
            case "roomCreated":
                System.Console.WriteLine($"[{timestamp}] ✓ Room created: {message.Message}");
                break;
            default:
                System.Console.WriteLine($"[{timestamp}] {message.Username}: {message.Message}");
                break;
        }
    }

    private static void OnStatusChanged(string status)
    {
        System.Console.WriteLine($"Status: {status}");
    }

    private void OnFileOfferReceived(FileTransferInfo fileInfo)
    {
        _pendingFiles[fileInfo.Id] = fileInfo;

        System.Console.WriteLine($"\n*** FILE OFFER RECEIVED ***");
        System.Console.WriteLine($"From: {fileInfo.FromUsername}");
        System.Console.WriteLine($"File: {fileInfo.FileName}");
        System.Console.WriteLine($"Size: {fileInfo.FileSize:N0} bytes");
        System.Console.WriteLine($"File ID: {fileInfo.Id}");
        System.Console.WriteLine($"*** READY FOR DOWNLOAD ***");
        System.Console.WriteLine($"Use '/accept {fileInfo.Id}' to accept or '/reject {fileInfo.Id}' to reject");
        System.Console.WriteLine();
    }

    private void OnFileTransferProgress(string fileId, int chunkIndex, int totalChunks)
    {
        if (_pendingFiles.TryGetValue(fileId, out var fileInfo))
        {
            var progress = (double)(chunkIndex + 1) / totalChunks * 100;
            System.Console.WriteLine($"File transfer progress: {fileInfo.FileName} - {progress:F1}% ({chunkIndex + 1}/{totalChunks})");
        }
    }
}