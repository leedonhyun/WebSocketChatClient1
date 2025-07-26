using ChatSystem.Client.Interfaces;

using Nerdbank.Streams;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

// ==================== 연결 관리자 ====================
namespace ChatSystem.Client.Connection
{
    public class WebSocketConnectionManager : IConnectionManager
    {
        private ClientWebSocket? _webSocket;
        private MultiplexingStream? _multiplexingStream;
        private MultiplexingStream.Channel? _messageChannel;
        private MultiplexingStream.Channel? _fileChannel;
        private CancellationTokenSource _cancellationTokenSource = new();
        private bool _isConnected = false;

        public bool IsConnected => _isConnected;
        public event Action<string>? StatusChanged;

        public MultiplexingStream.Channel? MessageChannel => _messageChannel;
        public MultiplexingStream.Channel? FileChannel => _fileChannel;

        public async Task<bool> ConnectAsync(string serverUrl, CancellationToken cancellationToken)
        {
            try
            {
                _webSocket = new ClientWebSocket();
                StatusChanged?.Invoke("Connecting to server...");

                await _webSocket.ConnectAsync(new Uri(serverUrl), cancellationToken);

                var stream = _webSocket.AsStream();
                _multiplexingStream = await MultiplexingStream.CreateAsync(
                    stream,
                    new MultiplexingStream.Options
                    {
                        TraceSource = new System.Diagnostics.TraceSource("ChatClient")
                    },
                    cancellationToken);

                var messageChannelTask = _multiplexingStream.OfferChannelAsync("messages", cancellationToken);
                var fileChannelTask = _multiplexingStream.OfferChannelAsync("files", cancellationToken);

                await Task.WhenAll(messageChannelTask, fileChannelTask);

                _messageChannel = await messageChannelTask;
                _fileChannel = await fileChannelTask;

                _isConnected = true;
                StatusChanged?.Invoke("Connected to server");
                return true;
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _isConnected = false;
                _cancellationTokenSource.Cancel();

                if (_multiplexingStream != null)
                    await _multiplexingStream.DisposeAsync();

                if (_webSocket?.State == WebSocketState.Open)
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);

                StatusChanged?.Invoke("Disconnected from server");
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Disconnection error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _webSocket?.Dispose();
            _multiplexingStream?.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}

