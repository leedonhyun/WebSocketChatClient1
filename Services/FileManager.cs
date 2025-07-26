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

