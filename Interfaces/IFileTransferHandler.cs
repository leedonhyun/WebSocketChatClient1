using System.Threading.Tasks;

namespace ChatSystem.Client.Interfaces
{
    /// <summary>
    /// Defines operations for handling file transfers.
    /// </summary>
    public interface IFileTransferHandler
    {
        /// <summary>
        /// Sends a file to a user or a room.
        /// </summary>
        Task SendFileAsync(string filePath, string? toUsername = null, bool autoAccept = false, string? roomId = null);

        /// <summary>
        /// Accepts an incoming file offer.
        /// </summary>
        Task AcceptFileAsync(string fileId);

        /// <summary>
        /// Rejects an incoming file offer.
        /// </summary>
        Task RejectFileAsync(string fileId);
    }
}