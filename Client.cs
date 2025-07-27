using WebSocketChatClient1.Client.Commands;
using WebSocketChatClient1.Client.Connection;
using WebSocketChatClient1.Client.Handlers;
using WebSocketChatClient1.Client.Interfaces;
using WebSocketChatClient1.Client.Processors;
using WebSocketChatClient1.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using WebSocketChatClient1.Interfaces;



// ==================== 메인 클라이언트 클래스 ====================
namespace WebSocketChatClient1.Client
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
        private readonly CommandHandler _commandHandler;
        private readonly IFileTransferHandler _fileTransferHandler;
        private readonly IChatHandler _chatHandler;

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
            ILogger<ChatClient> logger,
            ILogger<FileTransferHandler> fileTransferHandlerLogger)
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

            _commandHandler = new CommandHandler(_commandParser, status => StatusChanged?.Invoke(status));
            _fileTransferHandler = new FileTransferHandler(
                _connectionManager,
                _fileManager,
                fileTransferHandlerLogger,
                status => StatusChanged?.Invoke(status),
                () => IsConnected,
                GetFileTransferTarget
            );
            _chatHandler = new ChatHandler(
                _connectionManager,
                status => StatusChanged?.Invoke(status),
                () => IsConnected,
                () => _username,
                () => _currentRoom,
                (newUsername) => {
                    _username = newUsername;
                    if (_fileProcessor is FileTransferProcessor fileProc)
                    {
                        fileProc.SetCurrentUsername(newUsername);
                    }
                },
                (newRoom) => _currentRoom = newRoom
            );
            RegisterCommands();
        }

        // Fix for CS1503 errors in RegisterCommands method

        private void RegisterCommands()
        {
            Action<string> statusChange = (status) => StatusChanged?.Invoke(status);

            // Fix 1: Ensure ConnectCommand implements ICommand from the correct namespace.
            // Fix 2: Pass aliases as string array, not a single string.
            _commandHandler.RegisterCommand(new ConnectCommand(this, statusChange), new string[] { ClientConstants.Commands.Connect });
            _commandHandler.RegisterCommand(new DisconnectCommand(this, statusChange), new string[] { ClientConstants.Commands.Disconnect });
            _commandHandler.RegisterCommand(new SetUsernameCommand(this, statusChange), new string[] { ClientConstants.Commands.Username });
            _commandHandler.RegisterCommand(new GetUserListCommand(this, statusChange), new string[] { ClientConstants.Commands.Users });
            _commandHandler.RegisterCommand(new HelpCommand(this, statusChange), new string[] { ClientConstants.Commands.Help, ClientConstants.Commands.HelpAlt });

            _commandHandler.RegisterCommand(new SendFileCommand(this, statusChange), new string[] { ClientConstants.Commands.Send });
            _commandHandler.RegisterCommand(new AcceptFileCommand(this, statusChange), new string[] { ClientConstants.Commands.Accept });
            _commandHandler.RegisterCommand(new RejectFileCommand(this, statusChange), new string[] { ClientConstants.Commands.Reject });

            _commandHandler.RegisterCommand(new PrivateMessageCommand(this, statusChange), new string[] { ClientConstants.Commands.Msg,  ClientConstants.Commands.PrivateMessage });
            _commandHandler.RegisterCommand(new SendRoomMessageCommand(this, statusChange), new string[] { ClientConstants.Commands.Room });

            _commandHandler.RegisterCommand(new CreateRoomCommand(this, statusChange), new string[] { ClientConstants.Commands.Create, ClientConstants.Commands.CreateRoom });
            _commandHandler.RegisterCommand(new JoinRoomCommand(this, statusChange), new string[] { ClientConstants.Commands.Join, ClientConstants.Commands.JoinRoom });
            _commandHandler.RegisterCommand(new LeaveRoomCommand(this, statusChange), new string[] { ClientConstants.Commands.Leave, ClientConstants.Commands.LeaveRoom });
            _commandHandler.RegisterCommand(new GetRoomListCommand(this, statusChange), new string[] { ClientConstants.Commands.Rooms, ClientConstants.Commands.ListRooms });
            _commandHandler.RegisterCommand(new GetRoomMembersCommand(this, statusChange), new string[] { ClientConstants.Commands.Members, ClientConstants.Commands.RoomMembers });
            _commandHandler.RegisterCommand(new InviteToRoomCommand(this, statusChange), new string[] { ClientConstants.Commands.Invite });
            _commandHandler.RegisterCommand(new KickFromRoomCommand(this, statusChange), new string[] { ClientConstants.Commands.Kick });
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

        public Task DisconnectAsync()
        {
            return _connectionManager.DisconnectAsync();
        }

        public Task SendMessageAsync(string message) => _chatHandler.SendMessageAsync(message);

        public Task SendPrivateMessageAsync(string message, string toUsername) => _chatHandler.SendPrivateMessageAsync(message, toUsername);

        public Task SendRoomMessageAsync(string message, string roomId) => _chatHandler.SendRoomMessageAsync(message, roomId);

        public Task SendFileAsync(string filePath, string? toUsername = null, bool autoAccept = false, string? roomId = null)
        {
            return _fileTransferHandler.SendFileAsync(filePath, toUsername, autoAccept, roomId);
        }

        public Task AcceptFileAsync(string fileId)
        {
            return _fileTransferHandler.AcceptFileAsync(fileId);
        }

        public Task RejectFileAsync(string fileId)
        {
            return _fileTransferHandler.RejectFileAsync(fileId);
        }

        public Task SetUsernameAsync(string username) => _chatHandler.SetUsernameAsync(username);

        public Task GetUserListAsync() => _chatHandler.GetUserListAsync();

        // =============== 그룹 채팅 메서드 ===============

        public Task CreateRoomAsync(string roomName, string description = "", bool isPrivate = false, string? password = null) => _chatHandler.CreateRoomAsync(roomName, description, isPrivate, password);

        public Task JoinRoomAsync(string roomId, string? password = null) => _chatHandler.JoinRoomAsync(roomId, password);

        public Task LeaveRoomAsync(string? roomId = null) => _chatHandler.LeaveRoomAsync(roomId);

        public Task GetRoomListAsync() => _chatHandler.GetRoomListAsync();

        public Task GetRoomMembersAsync(string? roomId = null) => _chatHandler.GetRoomMembersAsync(roomId);

        public Task InviteToRoomAsync(string roomId, string username) => _chatHandler.InviteToRoomAsync(roomId, username);

        public Task KickFromRoomAsync(string roomId, string username) => _chatHandler.KickFromRoomAsync(roomId, username);

        // =============== Helper 메서드들 ===============

        //private static bool IsRoomId(string input) => Guid.TryParse(input, out _);

        private static string FormatRoomTarget(string roomId) => $"{ROOM_PREFIX}{roomId}";

        private static string GetFileTransferTarget(string? toUsername, string? roomId)
        {
            return !string.IsNullOrEmpty(roomId) ? FormatRoomTarget(roomId) : (toUsername ?? "");
        }

        //private bool ValidateConnection()
        //{
        //    if (IsConnected) return true;
        //    StatusChanged?.Invoke(ClientConstants.ErrorMessages.NotConnected);
        //    return false;
        //}

        // 명령어 처리 메서드
        public async Task ProcessCommandAsync(string input)
        {
            await _commandHandler.ProcessCommandAsync(input);
        }

        //private async Task SendChatMessageAsync(ChatMessage message)
        //{
        //    if (_connectionManager is WebSocketConnectionManager wsManager && wsManager.MessageChannel != null)
        //    {
        //        var json = JsonSerializer.Serialize(message);
        //        var buffer = Encoding.UTF8.GetBytes(json + "\n");
        //        await wsManager.MessageChannel.Output.WriteAsync(buffer.AsMemory());
        //        await wsManager.MessageChannel.Output.FlushAsync();
        //    }
        //}

        //private async Task SendFileMessageAsync(FileTransferMessage message)
        //{
        //    if (_connectionManager is WebSocketConnectionManager wsManager && wsManager.FileChannel != null)
        //    {
        //        var json = JsonSerializer.Serialize(message);
        //        var buffer = Encoding.UTF8.GetBytes(json + "\n");
        //        await wsManager.FileChannel.Output.WriteAsync(buffer.AsMemory());
        //        await wsManager.FileChannel.Output.FlushAsync();
        //    }
        //}

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
