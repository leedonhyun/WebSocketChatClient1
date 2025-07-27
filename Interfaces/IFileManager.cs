using WebSocketChatClient1.Client.Models;
using System.Threading.Tasks;

namespace WebSocketChatClient1.Interfaces;

public interface IFileManager
{
    Task<string> SaveFileAsync(string fileName, byte[] data, string? senderUsername = null);
    Task<byte[]> ReadFileAsync(string filePath);
    Task<FileUploadResult> UploadFileAsync(string filePath);
    string GetDownloadPath(string? senderUsername = null);
}