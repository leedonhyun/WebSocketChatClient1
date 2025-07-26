using ChatSystem.Client.Interfaces;
using ChatSystem.Client.Models;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatSystem.Client.Services;

public class FileManager : IFileManager
{
    private readonly string _downloadsPath;
    private readonly ILogger<FileManager> _logger;

    public FileManager(IConfiguration configuration, ILogger<FileManager> logger)
    {
        _downloadsPath = configuration.GetValue<string>("FileStorage:DownloadsPath") ??
                       Path.Combine(Directory.GetCurrentDirectory(), "downloads");
        _logger = logger;
        Directory.CreateDirectory(_downloadsPath);
    }

    public async Task<string> SaveFileAsync(string fileName, byte[] data, string? senderUsername = null)
    {
        var sanitizedFileName = SanitizeFileName(fileName);
        var userFolder = string.IsNullOrEmpty(senderUsername) ? "unknown" : SanitizeFileName(senderUsername);
        var userPath = Path.Combine(_downloadsPath, userFolder);

        Directory.CreateDirectory(userPath);

        var filePath = Path.Combine(userPath, sanitizedFileName);

        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write);
            await fileStream.WriteAsync(data);
            await fileStream.FlushAsync();

            _logger.LogDebug($"File chunk saved: {filePath} ({data.Length} bytes)");
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error saving file: {filePath}");
            throw;
        }
    }

    public async Task<byte[]> ReadFileAsync(string filePath)
    {
        try
        {
            return await File.ReadAllBytesAsync(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading file: {filePath}");
            throw;
        }
    }

    public async Task<FileUploadResult> UploadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileData = await File.ReadAllBytesAsync(filePath);

            const int chunkSize = 4096;
            var chunks = new List<byte[]>();

            for (int i = 0; i < fileData.Length; i += chunkSize)
            {
                var length = Math.Min(chunkSize, fileData.Length - i);
                var chunk = new byte[length];
                Array.Copy(fileData, i, chunk, 0, length);
                chunks.Add(chunk);
            }

            return new FileUploadResult
            {
                FileId = Guid.NewGuid().ToString(),
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                ContentType = GetContentType(fileInfo.Extension),
                Chunks = chunks
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading file: {filePath}");
            throw;
        }
    }

    public string GetDownloadPath(string? senderUsername = null)
    {
        var userFolder = string.IsNullOrEmpty(senderUsername) ? "unknown" : SanitizeFileName(senderUsername);
        return Path.Combine(_downloadsPath, userFolder);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).Distinct();
        return invalidChars.Aggregate(fileName, (current, c) => current.Replace(c, '_'));
    }

    private static string GetContentType(string extension)
    {
        return extension.ToLower() switch
        {
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}

public class CommandParser : ICommandParser
{
    public ParsedCommand Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
        {
            return new ParsedCommand { IsValid = false, ErrorMessage = "Commands must start with '/'." };
        }

        var commandAndArgs = ParseCommandParts(input.Substring(1));
        if (commandAndArgs.Length == 0)
        {
            return new ParsedCommand { IsValid = false, ErrorMessage = "Empty command." };
        }

        var command = commandAndArgs[0].ToLower();
        var arguments = new List<string>();
        var options = new Dictionary<string, object>();

        for (int i = 1; i < commandAndArgs.Length; i++)
        {
            var part = commandAndArgs[i];
            if (part.StartsWith("-"))
            {
                var key = part.TrimStart('-');
                if (i + 1 < commandAndArgs.Length && !commandAndArgs[i + 1].StartsWith("-"))
                {
                    if (key.Equals("password", StringComparison.OrdinalIgnoreCase))
                    {
                        options[key] = commandAndArgs[i + 1];
                        i++;
                    }
                    else
                    {
                        options[key] = true;
                    }
                }
                else
                {
                    options[key] = true;
                }
            }
            else
            {
                arguments.Add(part);
            }
        }

        return new ParsedCommand
        {
            Command = command,
            Arguments = arguments.ToArray(),
            Options = options,
            IsValid = true
        };
    }

    private static string[] ParseCommandParts(string input)
    {
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                }
            }
            else
            {
                currentPart.Append(c);
            }
        }

        if (currentPart.Length > 0)
        {
            parts.Add(currentPart.ToString());
        }

        return parts.ToArray();
    }
}

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

