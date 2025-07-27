using WebSocketChatClient1.Models;

using Microsoft.Extensions.Logging;

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Processors;

public class ChatMessageProcessor : IMessageProcessor<ChatMessage>
{
    public event Action<ChatMessage>? MessageReceived;
    public event Action<string>? RoomJoined;
    public event Action<string>? RoomLeft;

    public async Task ProcessAsync(ChatMessage message)
    {
        // 방 관련 상태 변경 처리
        switch (message.Type)
        {
            case ClientConstants.MessageTypes.RoomJoined:
                // 메시지에서 방 ID 추출 (예: "Successfully joined room: roomId")
                var joinedRoomId = ExtractRoomIdFromMessage(message.Message);
                if (!string.IsNullOrEmpty(joinedRoomId))
                {
                    RoomJoined?.Invoke(joinedRoomId);
                }
                break;

            case ClientConstants.MessageTypes.RoomLeft:
                // 메시지에서 방 ID 추출
                var leftRoomId = ExtractRoomIdFromMessage(message.Message);
                if (!string.IsNullOrEmpty(leftRoomId))
                {
                    RoomLeft?.Invoke(leftRoomId);
                }
                break;
        }

        MessageReceived?.Invoke(message);
        await Task.CompletedTask;
    }

    private string ExtractRoomIdFromMessage(string message)
    {
        // 간단한 파싱 로직 - 실제로는 서버 응답 형식에 맞춰 수정 필요
        if (message.Contains(ClientConstants.MessagePartSeparator))
        {
            var parts = message.Split(ClientConstants.MessagePartSeparator);
            if (parts.Length > 1)
            {
                return parts[1].Trim();
            }
        }
        return "";
    }
}

public class FileTransferProcessor : IMessageProcessor<FileTransferMessage>
{
    private readonly IFileManager _fileManager;
    private readonly ILogger<FileTransferProcessor> _logger;
    private readonly Dictionary<string, FileTransferInfo> _incomingFiles = new();
    private string _currentUsername = "";

    public event Action<FileTransferInfo>? FileOfferReceived;
    public event Action<string, int, int>? FileTransferProgress;
    public event Action<string>? StatusChanged;

    public FileTransferProcessor(IFileManager fileManager, ILogger<FileTransferProcessor> logger)
    {
        _fileManager = fileManager;
        _logger = logger;
    }

    public void SetCurrentUsername(string username)
    {
        _currentUsername = username;
    }

    public async Task ProcessAsync(FileTransferMessage message)
    {
        try
        {
            _logger.LogDebug(ClientConstants.ProcessorMessages.LogProcessingFileMessage, message.Type, message.FileId);

            switch (message.Type)
            {
                case ClientConstants.MessageTypes.FileOffer:
                    await HandleFileOfferAsync(message);
                    break;
                case ClientConstants.MessageTypes.FileAccept:
                    StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusFileAccepted, message.FromUsername));
                    break;
                case ClientConstants.MessageTypes.FileReject:
                    StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusFileRejected, message.FromUsername));
                    break;
                case ClientConstants.MessageTypes.FileError:
                    StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusFileError, message.ToUsername));
                    _incomingFiles.Remove(message.FileId);
                    break;
                case ClientConstants.MessageTypes.FileData:
                    await HandleFileDataAsync(message);
                    break;
                case ClientConstants.MessageTypes.FileComplete:
                    await HandleFileCompleteAsync(message);
                    break;
                default:
                    _logger.LogWarning(ClientConstants.ProcessorMessages.LogUnknownFileType, message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ClientConstants.ProcessorMessages.LogProcessingError);
            StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusProcessingError, ex.Message));
        }
    }

    private async Task HandleFileOfferAsync(FileTransferMessage message)
    {
        if (message.FileInfo != null)
        {
            _incomingFiles[message.FileId] = message.FileInfo;
            StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusFileOfferReceived, message.FileInfo.FileName));
            FileOfferReceived?.Invoke(message.FileInfo);
        }
        await Task.CompletedTask;
    }

    private async Task HandleFileDataAsync(FileTransferMessage message)
    {
        if (message.FileInfo != null && !_incomingFiles.ContainsKey(message.FileId))
        {
            _incomingFiles[message.FileId] = message.FileInfo;
            StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusAutoDownloading, message.FileInfo.FileName, message.FileInfo.FromUsername));
        }
        else if (!string.IsNullOrEmpty(message.FromUsername) && _incomingFiles.TryGetValue(message.FileId, out var existingFileInfo))
        {
            existingFileInfo.FromUsername = message.FromUsername;
        }

        if (_incomingFiles.TryGetValue(message.FileId, out var fileInfo) && message.Data != null)
        {
            // 보낸 사람의 사용자명으로 폴더 생성
            await _fileManager.SaveFileAsync(fileInfo.FileName, message.Data, fileInfo.FromUsername);

            StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusChunkSaved, message.ChunkIndex + 1, message.TotalChunks));
            FileTransferProgress?.Invoke(message.FileId, message.ChunkIndex + 1, message.TotalChunks);
        }
    }

    private async Task HandleFileCompleteAsync(FileTransferMessage message)
    {
        if (message.FileInfo != null && _incomingFiles.ContainsKey(message.FileId))
        {
            _incomingFiles[message.FileId] = message.FileInfo;
        }
        else if (!string.IsNullOrEmpty(message.FromUsername) && _incomingFiles.TryGetValue(message.FileId, out var existingFileInfo))
        {
            existingFileInfo.FromUsername = message.FromUsername;
        }

        if (_incomingFiles.TryGetValue(message.FileId, out var fileInfo))
        {
            var downloadPath = _fileManager.GetDownloadPath(fileInfo.FromUsername); // 보낸 사람의 폴더
            var filePath = Path.Combine(downloadPath, fileInfo.FileName);

            StatusChanged?.Invoke(ClientConstants.ProcessorMessages.StatusDownloadComplete);
            StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusDownloadFrom, fileInfo.FromUsername));
            StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusDownloadFile, fileInfo.FileName));
            StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusDownloadLocation, Path.GetFullPath(filePath)));

            if (File.Exists(filePath))
            {
                var actualSize = new FileInfo(filePath).Length;
                StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusDownloadSize, actualSize));

                if (actualSize == fileInfo.FileSize)
                {
                    StatusChanged?.Invoke(ClientConstants.ProcessorMessages.StatusIntegrityVerified);
                }
                else
                {
                    StatusChanged?.Invoke(string.Format(ClientConstants.ProcessorMessages.StatusIntegrityWarning, fileInfo.FileSize, actualSize));
                }
            }

            _incomingFiles.Remove(message.FileId);
        }

        await Task.CompletedTask;
    }
}
