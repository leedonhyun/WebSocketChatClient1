using WebSocketChatClient1.Client.Connection;
using WebSocketChatClient1.Client.Interfaces;
using WebSocketChatClient1.Models;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Handlers;

/// <summary>
/// Handles the logic for sending, accepting, and rejecting file transfers.
/// </summary>
public class FileTransferHandler : IFileTransferHandler
{
    private readonly IConnectionManager _connectionManager;
    private readonly IFileManager _fileManager;
    private readonly ILogger<FileTransferHandler> _logger;
    private readonly Action<string> _statusChanged;
    private readonly Func<bool> _isConnected;
    private readonly Func<string?, string?, string> _getFileTransferTarget;

    public FileTransferHandler(
        IConnectionManager connectionManager,
        IFileManager fileManager,
        ILogger<FileTransferHandler> logger,
        Action<string> statusChanged,
        Func<bool> isConnected,
        Func<string?, string?, string> getFileTransferTarget)
    {
        _connectionManager = connectionManager;
        _fileManager = fileManager;
        _logger = logger;
        _statusChanged = statusChanged;
        _isConnected = isConnected;
        _getFileTransferTarget = getFileTransferTarget;
    }

    public async Task SendFileAsync(string filePath, string? toUsername = null, bool autoAccept = false, string? roomId = null)
    {
        if (!_isConnected())
        {
            _statusChanged(ClientConstants.ErrorMessages.NotConnected);
            return;
        }

        try
        {
            var uploadResult = await _fileManager.UploadFileAsync(filePath);
            _statusChanged(string.Format(ClientConstants.StatusMessages.FileUploading, uploadResult.FileName, uploadResult.FileId));

            var targetIdentity = _getFileTransferTarget(toUsername, roomId);

            // Upload chunks
            for (int i = 0; i < uploadResult.Chunks.Count; i++)
            {
                var uploadMessage = new FileTransferMessage
                {
                    Type = ClientConstants.MessageTypes.FileUpload,
                    FileId = uploadResult.FileId,
                    FileInfo = new FileTransferInfo { Id = uploadResult.FileId, FileName = uploadResult.FileName, FileSize = uploadResult.FileSize, ContentType = uploadResult.ContentType, ToUsername = targetIdentity },
                    Data = uploadResult.Chunks[i],
                    ChunkIndex = i,
                    TotalChunks = uploadResult.Chunks.Count,
                    Timestamp = DateTime.UtcNow
                };
                await SendFileMessageAsync(uploadMessage);
                var progress = (double)(i + 1) / uploadResult.Chunks.Count * 100;
                _statusChanged(string.Format(ClientConstants.StatusMessages.FileUploadProgress, uploadResult.FileName, progress));
            }

            // Signal completion
            var completeMessage = new FileTransferMessage
            {
                Type = ClientConstants.MessageTypes.FileUploadComplete,
                FileId = uploadResult.FileId,
                FileInfo = new FileTransferInfo { Id = uploadResult.FileId, FileName = uploadResult.FileName, FileSize = uploadResult.FileSize, ContentType = uploadResult.ContentType, ToUsername = targetIdentity },
                Timestamp = DateTime.UtcNow
            };
            await SendFileMessageAsync(completeMessage);
            _statusChanged(string.Format(ClientConstants.StatusMessages.FileUploadComplete, uploadResult.FileName, uploadResult.FileId));

            // Send offer
            var offerMessage = new FileTransferMessage
            {
                Type = autoAccept ? ClientConstants.MessageTypes.FileOfferAuto : ClientConstants.MessageTypes.FileOffer,
                FileId = uploadResult.FileId,
                FileInfo = new FileTransferInfo { Id = uploadResult.FileId, FileName = uploadResult.FileName, FileSize = uploadResult.FileSize, ContentType = uploadResult.ContentType, ToUsername = targetIdentity },
                ToUsername = targetIdentity,
                Timestamp = DateTime.UtcNow
            };
            await SendFileMessageAsync(offerMessage);

            var targetDescription = !string.IsNullOrEmpty(roomId) ? $"room '{roomId}'" : $"user '{toUsername}'";
            var autoAcceptPrefix = autoAccept ? "Auto-accept " : "";
            _statusChanged(string.Format(ClientConstants.StatusMessages.FileOfferSent, autoAcceptPrefix, targetDescription, uploadResult.FileName));
            _statusChanged(string.Format(ClientConstants.StatusMessages.FileOfferInfo, uploadResult.FileId));
        }
        catch (FileNotFoundException ex)
        {
            _statusChanged(string.Format(ClientConstants.ErrorMessages.FileNotFound, filePath));
            _logger.LogError(ex, "File not found when sending file: {FilePath}", filePath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _statusChanged(string.Format(ClientConstants.ErrorMessages.AccessDenied, filePath));
            _logger.LogError(ex, "Access denied when reading file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _statusChanged(string.Format(ClientConstants.ErrorMessages.FileSendError, ex.Message));
            _logger.LogError(ex, "Error sending file: {FilePath}", filePath);
        }
    }

    public async Task AcceptFileAsync(string fileId)
    {
        if (!_isConnected()) return;

        var acceptMessage = new FileTransferMessage
        {
            Type = ClientConstants.MessageTypes.FileAccept,
            FileId = fileId,
            Timestamp = DateTime.UtcNow
        };
        await SendFileMessageAsync(acceptMessage);
        _statusChanged(string.Format(ClientConstants.StatusMessages.FileAccepted, fileId));
    }

    public async Task RejectFileAsync(string fileId)
    {
        if (!_isConnected()) return;

        var rejectMessage = new FileTransferMessage
        {
            Type = ClientConstants.MessageTypes.FileReject,
            FileId = fileId,
            Timestamp = DateTime.UtcNow
        };
        await SendFileMessageAsync(rejectMessage);
        _statusChanged(string.Format(ClientConstants.StatusMessages.FileRejected, fileId));
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
}