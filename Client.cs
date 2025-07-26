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
        // ClientConstants
        private const string DEFAULT_SERVER_URL = ClientConstants.DefaultServerUrl;
        private const string ROOM_PREFIX = ClientConstants.RoomPrefix;

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
                    StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.RoomJoined, roomId));
                };
                chatProc.RoomLeft += roomId => {
                    if (_currentRoom == roomId)
                    {
                        _currentRoom = null;
                        StatusChanged?.Invoke(ClientConstants.StatusMessages.RoomLeft);
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
                Type = ClientConstants.MessageTypes.Chat,
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
                Type = ClientConstants.MessageTypes.PrivateMessage,
                Message = $"@{toUsername}: {message}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(chatMessage);
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.PrivateMessageSent, toUsername));
        }

        public async Task SendRoomMessageAsync(string message, string roomId)
        {
            if (!IsConnected) return;

            var chatMessage = new ChatMessage
            {
                Type = ClientConstants.MessageTypes.Chat,
                Message = $"roomMessage {roomId} {message}",
                Username = _username,
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(chatMessage);
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.RoomMessageSent, roomId, message));
        }

        public async Task SendFileAsync(string filePath, string? toUsername = null, bool autoAccept = false, string? roomId = null)
        {
            if (!ValidateConnection()) return;

            try
            {
                var uploadResult = await _fileManager.UploadFileAsync(filePath);
                StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.FileUploading, uploadResult.FileName, uploadResult.FileId));

                // 파일 업로드
                for (int i = 0; i < uploadResult.Chunks.Count; i++)
                {
                    var uploadMessage = new FileTransferMessage
                    {
                        Type = ClientConstants.MessageTypes.FileUpload,
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
                    StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.FileUploadProgress, uploadResult.FileName, progress));
                }

                // 업로드 완료
                var completeMessage = new FileTransferMessage
                {
                    Type = ClientConstants.MessageTypes.FileUploadComplete,
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
                StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.FileUploadComplete, uploadResult.FileName, uploadResult.FileId));

                // 파일 제안
                var offerMessage = new FileTransferMessage
                {
                    Type = autoAccept ? ClientConstants.MessageTypes.FileOfferAuto : ClientConstants.MessageTypes.FileOffer,
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
                var autoAcceptPrefix = autoAccept ? "Auto-accept " : "";
                StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.FileOfferSent, autoAcceptPrefix, target, uploadResult.FileName));
                StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.FileOfferInfo, uploadResult.FileId));
            }
            catch (FileNotFoundException ex)
            {
                StatusChanged?.Invoke(string.Format(ClientConstants.ErrorMessages.FileNotFound, filePath));
                _logger.LogError(ex, "File not found when sending file: {FilePath}", filePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusChanged?.Invoke(string.Format(ClientConstants.ErrorMessages.AccessDenied, filePath));
                _logger.LogError(ex, "Access denied when reading file: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(string.Format(ClientConstants.ErrorMessages.FileSendError, ex.Message));
                _logger.LogError(ex, "Error sending file: {FilePath}", filePath);
            }
        }

        public async Task AcceptFileAsync(string fileId)
        {
            if (!IsConnected) return;

            var acceptMessage = new FileTransferMessage
            {
                Type = ClientConstants.MessageTypes.FileAccept,
                FileId = fileId,
                Timestamp = DateTime.UtcNow
            };

            await SendFileMessageAsync(acceptMessage);
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.FileAccepted, fileId));
        }

        public async Task RejectFileAsync(string fileId)
        {
            if (!IsConnected) return;

            var rejectMessage = new FileTransferMessage
            {
                Type = ClientConstants.MessageTypes.FileReject,
                FileId = fileId,
                Timestamp = DateTime.UtcNow
            };

            await SendFileMessageAsync(rejectMessage);
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.FileRejected, fileId));
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
                Type = ClientConstants.MessageTypes.SetUsername,
                Message = username,
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.UsernameSet, username));
        }

        public async Task GetUserListAsync()
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = ClientConstants.MessageTypes.ListUsers,
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
                Type = ClientConstants.MessageTypes.CreateRoom,
                Message = $"{roomName}{ClientConstants.CommandArgSeparator}{description}{ClientConstants.CommandArgSeparator}{isPrivate}{ClientConstants.CommandArgSeparator}{password ?? ""}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.RoomCreating, roomName));
        }

        public async Task JoinRoomAsync(string roomId, string? password = null)
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = ClientConstants.MessageTypes.JoinRoom,
                Message = $"{roomId}{ClientConstants.CommandArgSeparator}{password ?? ""}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);

            // 성공적인 조인은 서버 응답에서 처리하지만, 일단 임시로 설정
            _currentRoom = roomId;
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.RoomJoining, roomId));
        }

        public async Task LeaveRoomAsync(string? roomId = null)
        {
            if (!IsConnected) return;

            var targetRoom = roomId ?? _currentRoom;
            if (string.IsNullOrEmpty(targetRoom))
            {
                StatusChanged?.Invoke(ClientConstants.ErrorMessages.NoRoomToLeave);
                return;
            }

            var message = new ChatMessage
            {
                Type = ClientConstants.MessageTypes.LeaveRoom,
                Message = targetRoom,
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);

            if (targetRoom == _currentRoom)
            {
                _currentRoom = null;
            }

            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.RoomLeftTarget, targetRoom));
        }

        public async Task GetRoomListAsync()
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = ClientConstants.MessageTypes.ListRooms,
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
                StatusChanged?.Invoke(ClientConstants.ErrorMessages.NoRoomSpecified);
                return;
            }

            var message = new ChatMessage
            {
                Type = ClientConstants.MessageTypes.ListRoomMembers,
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
                Type = ClientConstants.MessageTypes.InviteToRoom,
                Message = $"{roomId}{ClientConstants.CommandArgSeparator}{username}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.InvitingUser, username, roomId));
        }

        public async Task KickFromRoomAsync(string roomId, string username)
        {
            if (!IsConnected) return;

            var message = new ChatMessage
            {
                Type = ClientConstants.MessageTypes.KickFromRoom,
                Message = $"{roomId}{ClientConstants.CommandArgSeparator}{username}",
                Timestamp = DateTime.UtcNow
            };

            await SendChatMessageAsync(message);
            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.KickingUser, username, roomId));
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
            StatusChanged?.Invoke(ClientConstants.ErrorMessages.NotConnected);
            return false;
        }

        // TODO: 향후 개선 사항
        // - Command Pattern을 사용해서 switch 문 리팩토링
        // - 각 명령어를 별도 클래스로 분리 (ICommand 인터페이스 구현)
        // - 명령어 등록을 Dictionary<string, ICommand>로 관리
        // - 이렇게 하면 새로운 명령어 추가가 쉬워지고 테스트도 용이해짐

        private void ShowHelp()
        {
            foreach (var line in ClientConstants.HelpText)
            {
                StatusChanged?.Invoke(line);
            }
        }

        // 명령어 처리 메서드
        public async Task ProcessCommandAsync(string input)
        {
            var parsed = _commandParser.Parse(input);
            if (!parsed.IsValid)
            {
                StatusChanged?.Invoke(string.Format(ClientConstants.ErrorMessages.InvalidCommand, parsed.ErrorMessage));
                return;
            }

            switch (parsed.Command)
            {
                case ClientConstants.Commands.Connect:
                    var url = parsed.Arguments.Length > 0 ? parsed.Arguments[0] : DEFAULT_SERVER_URL;
                    await ConnectAsync(url);
                    break;

                case ClientConstants.Commands.Disconnect:
                    await DisconnectAsync();
                    break;

                case ClientConstants.Commands.Username:
                    if (parsed.Arguments.Length > 0)
                        await SetUsernameAsync(string.Join(" ", parsed.Arguments));
                    break;

                case ClientConstants.Commands.Users:
                    await GetUserListAsync();
                    break;

                case ClientConstants.Commands.Help:
                case ClientConstants.Commands.HelpAlt:
                    ShowHelp();
                    break;

                case ClientConstants.Commands.Send:
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
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.SendUsage);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.SendExamplesHeader);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.SendExamplePublic);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.SendExampleUser);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.SendExampleRoom);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.SendExampleAuto);
                    }
                    break;

                case ClientConstants.Commands.Accept:
                    if (parsed.Arguments.Length > 0)
                        await AcceptFileAsync(parsed.Arguments[0]);
                    break;

                case ClientConstants.Commands.Reject:
                    if (parsed.Arguments.Length > 0)
                        await RejectFileAsync(parsed.Arguments[0]);
                    break;

                // =============== 그룹 채팅 명령어 ===============

                case ClientConstants.Commands.Msg:
                case ClientConstants.Commands.Pm:
                case ClientConstants.Commands.Private:
                case ClientConstants.Commands.PrivateMessage:
                    if (parsed.Arguments.Length >= 2)
                    {
                        var targetUser = parsed.Arguments[0];
                        var message = string.Join(" ", parsed.Arguments.Skip(1));
                        await SendPrivateMessageAsync(message, targetUser);
                    }
                    else
                    {
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.PrivateMessageUsage);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.PrivateMessageExample);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.PrivateMessageAliases);
                    }
                    break;

                case ClientConstants.Commands.Create:
                case ClientConstants.Commands.CreateRoom:
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
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.CreateRoomUsage);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.CreateRoomExample);
                    }
                    break;

                case ClientConstants.Commands.Join:
                case ClientConstants.Commands.JoinRoom:
                    if (parsed.Arguments.Length > 0)
                    {
                        var roomId = parsed.Arguments[0];
                        var password = parsed.Arguments.Length > 1 ? parsed.Arguments[1] : null;
                        await JoinRoomAsync(roomId, password);
                    }
                    else
                    {
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.JoinRoomUsage);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.JoinRoomExample);
                    }
                    break;

                case ClientConstants.Commands.Leave:
                case ClientConstants.Commands.LeaveRoom:
                    var targetRoomId = parsed.Arguments.Length > 0 ? parsed.Arguments[0] : null;
                    await LeaveRoomAsync(targetRoomId);
                    break;

                case ClientConstants.Commands.Rooms:
                case ClientConstants.Commands.ListRooms:
                    await GetRoomListAsync();
                    break;

                case ClientConstants.Commands.Members:
                case ClientConstants.Commands.RoomMembers:
                    var roomForMembers = parsed.Arguments.Length > 0 ? parsed.Arguments[0] : null;
                    await GetRoomMembersAsync(roomForMembers);
                    break;

                case ClientConstants.Commands.Invite:
                    if (parsed.Arguments.Length >= 2)
                    {
                        var roomId = parsed.Arguments[0];
                        var username = parsed.Arguments[1];
                        await InviteToRoomAsync(roomId, username);
                    }
                    else
                    {
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.InviteUsage);
                    }
                    break;

                case ClientConstants.Commands.Kick:
                    if (parsed.Arguments.Length >= 2)
                    {
                        var roomId = parsed.Arguments[0];
                        var username = parsed.Arguments[1];
                        await KickFromRoomAsync(roomId, username);
                    }
                    else
                    {
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.KickUsage);
                    }
                    break;

                case ClientConstants.Commands.Room:
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
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.RoomMessageUsage);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.RoomMessageAltUsage);
                        StatusChanged?.Invoke(ClientConstants.UsageMessages.RoomMessageExample);
                        if (!string.IsNullOrEmpty(_currentRoom))
                        {
                            StatusChanged?.Invoke(string.Format(ClientConstants.StatusMessages.CurrentRoomInfo, _currentRoom));
                        }
                    }
                    break;

                default:
                    StatusChanged?.Invoke(string.Format(ClientConstants.ErrorMessages.UnknownCommand, parsed.Command));
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
                                _logger.LogError(ex, ClientConstants.ErrorMessages.LogParseChatMessageFailed);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ClientConstants.ErrorMessages.LogReceiveMessageError);
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
                                _logger.LogError(ex, ClientConstants.ErrorMessages.LogParseFileMessageFailed);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ClientConstants.ErrorMessages.LogReceiveFileError);
            }
        }

        public void Dispose()
        {
            _connectionManager?.Dispose();
        }
    }
}
