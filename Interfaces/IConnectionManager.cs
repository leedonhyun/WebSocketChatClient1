using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketChatClient1.Interfaces;

public interface IConnectionManager : IDisposable
{
    Task<bool> ConnectAsync(string serverUrl, CancellationToken cancellationToken);
    Task DisconnectAsync();
    bool IsConnected { get; }
    event Action<string>? StatusChanged;
}