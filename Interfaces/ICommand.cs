using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebSocketChatClient1.Interfaces
{
    /// <summary>
    /// Represents a command that can be executed.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Executes the command asynchronously.
        /// </summary>
        /// <param name="args">The arguments for the command.</param>
        /// <param name="options">The options for the command.</param>
        Task ExecuteAsync(string[] args, Dictionary<string, object> options);
    }
}