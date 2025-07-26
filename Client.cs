using ChatSystem.Client.Connection;
using ChatSystem.Client.Interfaces;
using ChatSystem.Client.Processors;
using ChatSystem.Models;

using Microsoft.Extensions.Logging;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


// ==================== 메인 클라이언트 클래스 ====================
namespace ChatSystem.Client
{
    public class ChatClient : IChatClient, IDisposable
    {
        // Constants
        private const string DEFAULT_SERVER_URL = "ws://localhost:5106/ws";
        private const string ROOM_PREFIX = "ROOM:";

        private readonly IConnectionManager _connectionManager;
        private readonly IFileManager _fileManager;
        private readonly IMessageProcessor<ChatMessage> _chatProcessor;
        private readonly IMessageProcessor<FileTransferMessage> _fileProcessor;
        private readonly ICommandParser _commandParser;
        private readonly ILogger<ChatClient> _logger;

        private string _username = "";
        private string? _currentRoom = null;
        private readonly Dictionary<string, FileTransferInfo> _pendingFiles = new();

        public event Action<ChatMessage>? MessageReceived;
        public event Action<string>? StatusChanged;
        public event Action<FileTransferInfo>? FileOfferReceived;
        public event Action<string, int, int>? FileTransferProgress;

        public bool IsConnected => _connectionManager.IsConnected;
        public string Username => _username;
        public string? CurrentRoom => _currentRoom;

        public ChatClient(
            IConnectionManager connectionManager,
            IFileManager fileManager,
            IMessageProcessor<ChatMessage> chatProcessor,
            IMessageProcessor<FileTransferMessage> fileProcessor,
            ICommandParser commandParser,
            ILogger<ChatClient> logger)
        {
            _connectionManager = connectionManager;
            _fileManager = fileManager;
            _chatProcessor = chatProcessor;
            _fileProcessor = fileProcessor;
            _commandParser = commandParser;
            _logger = logger;

            // 이벤트 연결
            _connectionManager.StatusChanged += status => StatusChanged?.Invoke(status);

            if (_chatProcessor is ChatMessageProcessor chatProc)
            {
                chatProc.MessageReceived += msg => MessageReceived?.Invoke(msg);
                chatProc.RoomJoined += roomId => {
                    _currentRoom = roomId;
                    StatusChanged?.Invoke($"Current room set to: {roomId}");
                };
                chatProc.RoomLeft += roomId => {
                    if (_currentRoom == roomId)
                    {
                        _currentRoom = null;
                        StatusChanged?.Invoke("Left current room");
                    }
                };
            }

            if (_fileProcessor is FileTransferProcessor fileProc)
            {
                fileProc.FileOfferReceived += offer => FileOfferReceived?.Invoke(offer);
                fileProc.FileTransferProgress += (id, current, total) => FileTransferProgress?.Invoke(id, current, total);
                fileProc.StatusChanged += status => StatusChanged?.Invoke(status);
            }
        }

        public async Task<bool> ConnectAsync(string serverUrl)
        {
            var result = await _connectionManager.ConnectAsync(serverUrl, CancellationToken.None);
            if (result)
            {
                // 메시지 수신 시작
                _ = Task.Run(ReceiveMessagesAsync);
                _ = Task.Run(ReceiveFileTransferAsync);
            }
            return result;
        }

        public async Task DisconnectAsync()
        {
            await _connectionManager.DisconnectAsync();
        }

        public async Task SendMessageAsync(string message)
        {
            if (!IsConnected) return;

            var chatMessage = new ChatMessage
            {
                Type = "chat",
                Message = message,
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(chatMessage);
        }

        public async Task SendPrivateMessageAsync(string message, string toUsername)
        {
            if (!IsConnected) return;

            var chatMessage = new ChatMessage
            {
                Type = "privateMessage",
                Message = $"@{toUsername}: {message}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(chatMessage);
            StatusChanged?.Invoke($"Private message sent to {toUsername}");
        }

        public async Task SendRoomMessageAsync(string message, string roomId)
        {
            if (!IsConnected) return;

            var chatMessage = new ChatMessage
            {
                Type = "chat",
                Message = $"roomMessage {roomId} {message}",
                Username = _username,
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(chatMessage);
            StatusChanged?.Invoke($"Message sent to room '{roomId}': {message}");
        }

        public async Task SendFileAsync(string filePath, string? toUsername = null, bool autoAccept = false, string? roomId = null)
        {
            if (!ValidateConnection()) return;

            try
            {
                var uploadResult = await _fileManager.UploadFileAsync(filePath);
                StatusChanged?.Invoke($"Uploading file to server: {uploadResult.FileName} (ID: {uploadResult.FileId})");

                // 파일 업로드
                for (int i = 0; i < uploadResult.Chunks.Count; i++)
                {
                    var uploadMessage = new FileTransferMessage
                    {
                        Type = "fileUpload",
                        FileId = uploadResult.FileId,
                        FileInfo = new FileTransferInfo
                        {
                            Id = uploadResult.FileId,
                            FileName = uploadResult.FileName,
                            FileSize = uploadResult.FileSize,
                            ContentType = uploadResult.ContentType,
                            ToUsername = GetFileTransferTarget(toUsername, roomId)
                        },
                        Data = uploadResult.Chunks[i],
                        ChunkIndex = i,
                        TotalChunks = uploadResult.Chunks.Count,
                        Timestamp = DateTime.UtcNow
                    };

                    await SendFileMessageAsync(uploadMessage);
                    var progress = (double)(i + 1) / uploadResult.Chunks.Count * 100;
                    StatusChanged?.Invoke($"Uploading: {uploadResult.FileName} - {progress:F1}%");
                }

                // 업로드 완료
                var completeMessage = new FileTransferMessage
                {
                    Type = "fileUploadComplete",
                    FileId = uploadResult.FileId,
                    FileInfo = new FileTransferInfo
                    {
                        Id = uploadResult.FileId,
                        FileName = uploadResult.FileName,
                        FileSize = uploadResult.FileSize,
                        ContentType = uploadResult.ContentType,
                        ToUsername = !string.IsNullOrEmpty(roomId) ? $"{ROOM_PREFIX}{roomId}" : (toUsername ?? "")
                    },
                    Timestamp = DateTime.UtcNow
                };

                await SendFileMessageAsync(completeMessage);
                StatusChanged?.Invoke($"File upload completed: {uploadResult.FileName} (ID: {uploadResult.FileId}). Sending offer...");

                // 파일 제안
                var offerMessage = new FileTransferMessage
                {
                    Type = autoAccept ? "fileOfferAuto" : "fileOffer",
                    FileId = uploadResult.FileId,
                    FileInfo = new FileTransferInfo
                    {
                        Id = uploadResult.FileId,
                        FileName = uploadResult.FileName,
                        FileSize = uploadResult.FileSize,
                        ContentType = uploadResult.ContentType,
                        ToUsername = GetFileTransferTarget(toUsername, roomId)
                    },
                    ToUsername = GetFileTransferTarget(toUsername, roomId),
                    Timestamp = DateTime.UtcNow
                };

                await SendFileMessageAsync(offerMessage);
                var target = !string.IsNullOrEmpty(roomId) ? $"room '{roomId}'" : $"user '{toUsername}'";
                StatusChanged?.Invoke($"{(autoAccept ? "Auto-accept " : "")}File offer sent to {target}: {uploadResult.FileName}");
                StatusChanged?.Invoke($"File ID: {uploadResult.FileId} - Recipients can use '/accept {uploadResult.FileId}' to download");
            }
            catch (FileNotFoundException ex)
            {
                StatusChanged?.Invoke($"File not found: {filePath}");
                _logger.LogError(ex, "File not found when sending file: {FilePath}", filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusChanged?.Invoke($"Access denied: Cannot read file {filePath}");
                _logger.LogError(ex, "Access denied when reading file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"File send error: {ex.Message}");
                _logger.LogError(ex, "Error sending file: {FilePath}", filePath);
            }
        }

        public async Task AcceptFileAsync(string fileId)
        {
            if (!IsConnected) return;

            var acceptMessage = new FileTransferMessage
            {
                Type = "fileAccept",
                FileId = fileId,
                Timestamp = DateTime.UtcNow
            };

            await SendFileMessageAsync(acceptMessage);
            StatusChanged?.Invoke($"File accepted: {fileId}");
        }

        public async Task RejectFileAsync(string fileId)
        {
            if (!IsConnected) return;

            var rejectMessage = new FileTransferMessage
            {
                Type = "fileReject",
                FileId = fileId,
                Timestamp = DateTime.UtcNow
            };

            await SendFileMessageAsync(rejectMessage);
            StatusChanged?.Invoke($"File rejected: {fileId}");
        }

        public async Task SetUsernameAsync(string username)
        {
            if (!IsConnected) return;

            _username = username;

            // FileTransferProcessor에도 현재 사용자명 설정
            if (_fileProcessor is FileTransferProcessor fileProc)
            {
                fileProc.SetCurrentUsername(username);
            }

            var message = new ChatMessage
            {
                Type = "setUsername",
                Message = username,
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
            StatusChanged?.Invoke($"Username set to: {username}");
        }

        public async Task GetUserListAsync()
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = "listUsers",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
        }

        // =============== 그룹 채팅 메서드 ===============

        public async Task CreateRoomAsync(string roomName, string description = "", bool isPrivate = false, string? password = null)
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = "createRoom",
                Message = $"{roomName}|{description}|{isPrivate}|{password ?? ""}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
            StatusChanged?.Invoke($"Creating room: {roomName}");
        }

        public async Task JoinRoomAsync(string roomId, string? password = null)
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = "joinRoom",
                Message = $"{roomId}|{password ?? ""}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);

            // 성공적인 조인은 서버 응답에서 처리하지만, 일단 임시로 설정
            _currentRoom = roomId;
            StatusChanged?.Invoke($"Joining room: {roomId}");
        }

        public async Task LeaveRoomAsync(string? roomId = null)
        {
            if (!IsConnected) return;

            var targetRoom = roomId ?? _currentRoom;
            if (string.IsNullOrEmpty(targetRoom))
            {
                StatusChanged?.Invoke("No room to leave");
                return;
            }

            var message = new ChatMessage
            {
                Type = "leaveRoom",
                Message = targetRoom,
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);

            if (targetRoom == _currentRoom)
            {
                _currentRoom = null;
            }

            StatusChanged?.Invoke($"Left room: {targetRoom}");
        }

        public async Task GetRoomListAsync()
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = "listRooms",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
        }

        public async Task GetRoomMembersAsync(string? roomId = null)
        {
            if (!IsConnected) return;

            var targetRoom = roomId ?? _currentRoom;
            if (string.IsNullOrEmpty(targetRoom))
            {
                StatusChanged?.Invoke("No room specified");
                return;
            }

            var message = new ChatMessage
            {
                Type = "listRoomMembers",
                Message = targetRoom,
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
        }

        public async Task InviteToRoomAsync(string roomId, string username)
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = "inviteToRoom",
                Message = $"{roomId}|{username}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
            StatusChanged?.Invoke($"Inviting {username} to room {roomId}");
        }

        public async Task KickFromRoomAsync(string roomId, string username)
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = "kickFromRoom",
                Message = $"{roomId}|{username}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
            StatusChanged?.Invoke($"Kicking {username} from room {roomId}");
        }

        // =============== Helper 메서드들 ===============

        private static bool IsRoomId(string input) => Guid.TryParse(input, out _);

        private static string FormatRoomTarget(string roomId) => $"{ROOM_PREFIX}{roomId}";

        private static string GetFileTransferTarget(string? toUsername, string? roomId)
        {
            return !string.IsNullOrEmpty(roomId) ? FormatRoomTarget(roomId) : (toUsername ?? "");
        }

        private bool ValidateConnection()
        {
            if (IsConnected) return true;
            StatusChanged?.Invoke("❌ Not connected to server. Use /connect to connect first.");
            return false;
        }

        // TODO: 향후 개선 사항
        // - Command Pattern을 사용해서 switch 문 리팩토링
        // - 각 명령어를 별도 클래스로 분리 (ICommand 인터페이스 구현)
        // - 명령어 등록을 Dictionary<string, ICommand>로 관리
        // - 이렇게 하면 새로운 명령어 추가가 쉬워지고 테스트도 용이해짐

        private void ShowHelp()
        {
            StatusChanged?.Invoke("=== Available Commands ===");
            StatusChanged?.Invoke("Basic Commands:");
            StatusChanged?.Invoke("  /connect [url] - Connect to server");
            StatusChanged?.Invoke("  /disconnect - Disconnect from server");
            StatusChanged?.Invoke("  /username <name> - Set username");
            StatusChanged?.Invoke("  /users - List online users");
            StatusChanged?.Invoke("  /help or /? - Show this help");
            StatusChanged?.Invoke("");
            StatusChanged?.Invoke("Chat Commands:");
            StatusChanged?.Invoke("  /msg <user> <message> - Send private message");
            StatusChanged?.Invoke("  /room <roomid> <message> - Send message to specific room");
            StatusChanged?.Invoke("");
            StatusChanged?.Invoke("Room Commands:");
            StatusChanged?.Invoke("  /create <name> [desc] [-private] [-password <pwd>] - Create room");
            StatusChanged?.Invoke("  /join <roomid> [password] - Join room");
            StatusChanged?.Invoke("  /leave [roomid] - Leave room");
            StatusChanged?.Invoke("  /rooms - List available rooms");
            StatusChanged?.Invoke("  /members [roomid] - List room members");
            StatusChanged?.Invoke("  /invite <roomid> <user> - Invite user to room");
            StatusChanged?.Invoke("  /kick <roomid> <user> - Kick user from room");
            StatusChanged?.Invoke("");
            StatusChanged?.Invoke("File Commands:");
            StatusChanged?.Invoke("  /send [-a] <filepath> [username|roomid] - Send file to user or room");
            StatusChanged?.Invoke("  /accept <fileId> - Accept incoming file");
            StatusChanged?.Invoke("  /reject <fileId> - Reject incoming file");
        }

        // 명령어 처리 메서드
        public async Task ProcessCommandAsync(string input)
        {
            var parsed = _commandParser.Parse(input);
            if (!parsed.IsValid)
            {
                StatusChanged?.Invoke($"Invalid command: {parsed.ErrorMessage}");
                return;
            }

            switch (parsed.Command)
            {
                case "connect":
                    var url = parsed.Arguments.Length > 0 ? parsed.Arguments[0] : DEFAULT_SERVER_URL;
                    await ConnectAsync(url);
                    break;

                case "disconnect":
                    await DisconnectAsync();
                    break;

                case "username":
                    if (parsed.Arguments.Length > 0)
                        await SetUsernameAsync(string.Join(" ", parsed.Arguments));
                    break;

                case "users":
                    await GetUserListAsync();
                    break;

                case "help":
                case "?":
                    ShowHelp();
                    break;

                case "send":
                    if (parsed.Arguments.Length > 0)
                    {
                        var filePath = parsed.Arguments[0];
                        var autoAccept = parsed.Options.ContainsKey("a");

                        // 두 번째 인자가 있으면 사용자명 또는 룸ID로 처리
                        string? targetUser = null;
                        string? roomId = null;

                        if (parsed.Arguments.Length > 1)
                        {
                            var target = parsed.Arguments[1];
                            // GUID 형태면 룸ID로 처리, 아니면 사용자명으로 처리
                            if (IsRoomId(target))
                            {
                                roomId = target;
                            }
                            else
                            {
                                targetUser = target;
                            }
                        }

                        await SendFileAsync(filePath, targetUser, autoAccept, roomId);
                    }
                    else
                    {
                        StatusChanged?.Invoke("Usage: /send [-a] <filepath> [username|roomid]");
                        StatusChanged?.Invoke("Examples:");
                        StatusChanged?.Invoke("  /send myfile.txt - Send to public (all users)");
                        StatusChanged?.Invoke("  /send myfile.txt john - Send to user 'john'");
                        StatusChanged?.Invoke("  /send myfile.txt room123 - Send to room 'room123'");
                        StatusChanged?.Invoke("  /send -a myfile.txt john - Auto-accept for user 'john'");
                    }
                    break;

                case "accept":
                    if (parsed.Arguments.Length > 0)
                        await AcceptFileAsync(parsed.Arguments[0]);
                    break;

                case "reject":
                    if (parsed.Arguments.Length > 0)
                        await RejectFileAsync(parsed.Arguments[0]);
                    break;

                // =============== 그룹 채팅 명령어 ===============

                case "msg":
                case "pm":
                case "private":
                case "privateMessage":
                    if (parsed.Arguments.Length >= 2)
                    {
                        var targetUser = parsed.Arguments[0];
                        var message = string.Join(" ", parsed.Arguments.Skip(1));
                        await SendPrivateMessageAsync(message, targetUser);
                    }
                    else
                    {
                        StatusChanged?.Invoke("Usage: /msg <username> <message>");
                        StatusChanged?.Invoke("  Example: /msg john Hello there!");
                        StatusChanged?.Invoke("  Aliases: /pm, /private, /privateMessage");
                    }
                    break;

                case "create":
                case "createroom":
                    if (parsed.Arguments.Length > 0)
                    {
                        var roomName = parsed.Arguments[0];
                        var description = parsed.Arguments.Length > 1 ? parsed.Arguments[1] : "";
                        var isPrivate = parsed.Options.ContainsKey("private") || parsed.Options.ContainsKey("p");
                        var password = parsed.Options.TryGetValue("password", out var pwd) ? pwd.ToString() : null;
                        await CreateRoomAsync(roomName, description, isPrivate, password);
                    }
                    else
                    {
                        StatusChanged?.Invoke("Usage: /create <roomname> [description] [-private] [-password <pwd>]");
                        StatusChanged?.Invoke("  Example: /create myroom \"My cool room\" -private -password secret123");
                    }
                    break;

                case "join":
                case "joinroom":
                    if (parsed.Arguments.Length > 0)
                    {
                        var roomId = parsed.Arguments[0];
                        var password = parsed.Arguments.Length > 1 ? parsed.Arguments[1] : null;
                        await JoinRoomAsync(roomId, password);
                    }
                    else
                    {
                        StatusChanged?.Invoke("Usage: /join <roomid> [password]");
                        StatusChanged?.Invoke("  Example: /join room123 mypassword");
                    }
                    break;

                case "leave":
                case "leaveroom":
                    var targetRoomId = parsed.Arguments.Length > 0 ? parsed.Arguments[0] : null;
                    await LeaveRoomAsync(targetRoomId);
                    break;

                case "rooms":
                case "listrooms":
                    await GetRoomListAsync();
                    break;

                case "members":
                case "roommembers":
                    var roomForMembers = parsed.Arguments.Length > 0 ? parsed.Arguments[0] : null;
                    await GetRoomMembersAsync(roomForMembers);
                    break;

                case "invite":
                    if (parsed.Arguments.Length >= 2)
                    {
                        var roomId = parsed.Arguments[0];
                        var username = parsed.Arguments[1];
                        await InviteToRoomAsync(roomId, username);
                    }
                    else
                    {
                        StatusChanged?.Invoke("Usage: /invite <roomid> <username>");
                    }
                    break;

                case "kick":
                    if (parsed.Arguments.Length >= 2)
                    {
                        var roomId = parsed.Arguments[0];
                        var username = parsed.Arguments[1];
                        await KickFromRoomAsync(roomId, username);
                    }
                    else
                    {
                        StatusChanged?.Invoke("Usage: /kick <roomid> <username>");
                    }
                    break;

                case "room":
                    if (parsed.Arguments.Length >= 2)
                    {
                        var roomId = parsed.Arguments[0];
                        var message = string.Join(" ", parsed.Arguments.Skip(1));
                        await SendRoomMessageAsync(message, roomId);
                    }
                    else if (parsed.Arguments.Length == 1 && !string.IsNullOrEmpty(_currentRoom))
                    {
                        // 현재 방에 메시지 보내기 (roomId 생략 시)
                        var message = parsed.Arguments[0];
                        await SendRoomMessageAsync(message, _currentRoom);
                    }
                    else
                    {
                        StatusChanged?.Invoke("Usage: /room <roomid> <message>");
                        StatusChanged?.Invoke("  Or if you're in a room: /room <message>");
                        StatusChanged?.Invoke("  Example: /room general Hello everyone!");
                        if (!string.IsNullOrEmpty(_currentRoom))
                        {
                            StatusChanged?.Invoke($"  Current room: {_currentRoom}");
                        }
                    }
                    break;

                default:
                    StatusChanged?.Invoke($"Unknown command: {parsed.Command}");
                    break;
            }
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

        private async Task SendFileMessageAsync(FileTransferMessage message)
        {
            if (_connectionManager is WebSocketConnectionManager wsManager && wsManager.FileChannel != null)
            {
                var json = JsonSerializer.Serialize(message);
                var buffer = Encoding.UTF8.GetBytes(json + "\n");
                await wsManager.FileChannel.Output.WriteAsync(buffer.AsMemory());
                await wsManager.FileChannel.Output.FlushAsync();
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            if (_connectionManager is not WebSocketConnectionManager wsManager || wsManager.MessageChannel == null)
                return;

            var messageBuffer = new StringBuilder();

            try
            {
                while (IsConnected)
                {
                    var result = await wsManager.MessageChannel.Input.ReadAsync();
                    if (result.IsCompleted) break;

                    var receivedText = Encoding.UTF8.GetString(result.Buffer.ToArray());
                    wsManager.MessageChannel.Input.AdvanceTo(result.Buffer.End);

                    messageBuffer.Append(receivedText);
                    var messages = messageBuffer.ToString().Split('\n');

                    messageBuffer.Clear();
                    if (!string.IsNullOrEmpty(messages[^1]))
                        messageBuffer.Append(messages[^1]);

                    for (int i = 0; i < messages.Length - 1; i++)
                    {
                        var messageText = messages[i].Trim();
                        if (!string.IsNullOrEmpty(messageText))
                        {
                            try
                            {
                                var chatMessage = JsonSerializer.Deserialize<ChatMessage>(messageText);
                                if (chatMessage != null)
                                    await _chatProcessor.ProcessAsync(chatMessage);
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Failed to parse chat message");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving messages");
            }
        }

        private async Task ReceiveFileTransferAsync()
        {
            if (_connectionManager is not WebSocketConnectionManager wsManager || wsManager.FileChannel == null)
                return;

            var messageBuffer = new StringBuilder();

            try
            {
                while (IsConnected)
                {
                    var result = await wsManager.FileChannel.Input.ReadAsync();
                    if (result.IsCompleted) break;

                    var receivedText = Encoding.UTF8.GetString(result.Buffer.ToArray());
                    wsManager.FileChannel.Input.AdvanceTo(result.Buffer.End);

                    messageBuffer.Append(receivedText);
                    var messages = messageBuffer.ToString().Split('\n');

                    messageBuffer.Clear();
                    if (!string.IsNullOrEmpty(messages[^1]))
                        messageBuffer.Append(messages[^1]);

                    for (int i = 0; i < messages.Length - 1; i++)
                    {
                        var messageText = messages[i].Trim();
                        if (!string.IsNullOrEmpty(messageText))
                        {
                            try
                            {
                                var fileMessage = JsonSerializer.Deserialize<FileTransferMessage>(messageText);
                                if (fileMessage != null)
                                    await _fileProcessor.ProcessAsync(fileMessage);
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Failed to parse file message");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving file transfers");
            }
        }

        public void Dispose()
        {
            _connectionManager?.Dispose();
        }
    }
}
