using Microsoft.Extensions.Logging;

using System;
using System.Threading;
using System.Threading.Tasks;

using WebSocketChatClient1.Client;
using WebSocketChatClient1.Interfaces;

using WebSocketChatShared.Models;

namespace WebSocketChatClient1
{
    public class ConsoleApplication
    {
        private readonly IChatClient _chatClient;
        private readonly ILogger<ConsoleApplication> _logger;

        public ConsoleApplication(IChatClient chatClient, ILogger<ConsoleApplication> logger)
        {
            _chatClient = chatClient;
            _logger = logger;

            if (_chatClient is ChatClient client)
            {
                client.MessageReceived += OnMessageReceived;
                client.StatusChanged += OnStatusChanged;
                client.FileOfferReceived += OnFileOfferReceived;
                client.FileTransferProgress += OnFileTransferProgress;
            }
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Chat Client started. Type '/help' for a list of commands.");

            while (!cancellationToken.IsCancellationRequested)
            {
                // Display prompt with username
                var prompt = GetPrompt();
                Console.Write(prompt);

                var input = await Task.Run(Console.ReadLine, cancellationToken);

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                if (input.StartsWith("/"))
                {
                    if (_chatClient is ChatClient client)
                    {
                        await client.ProcessCommandAsync(input);
                    }
                }
                else
                {
                    await _chatClient.SendMessageAsync(input);
                }
            }
        }

        private string GetPrompt()
        {
            var roomIndicator = string.IsNullOrEmpty(_chatClient.CurrentRoom) ? "public" : $"room:{_chatClient.CurrentRoom}";
            var username = string.IsNullOrEmpty(_chatClient.Username) ? "guest" : _chatClient.Username;
            return $"{roomIndicator}/{username}> ";
        }

        private void OnMessageReceived(ChatMessage message)
        {
            ClearCurrentConsoleLine();
            var displayMessage = "";
            switch (message.Type)
            {
                case "chat":
                    displayMessage = $"[{message.Timestamp:HH:mm:ss}] {message.Username}: {message.Message}";
                    break;
                case "privateChat":
                    displayMessage = $"[{message.Timestamp:HH:mm:ss}] (private) {message.Username} to {message.ToUsername}: {message.Message}";
                    break;
                case "groupChat":
                case "roomChat":
                    displayMessage = $"[{message.Timestamp:HH:mm:ss}] (room: {message.RoomId}) {message.Username}: {message.Message}";
                    break;
                case "system":
                    displayMessage = $"[{message.Timestamp:HH:mm:ss}] [SYSTEM] {message.Message}";
                    break;
                case "error":
                    displayMessage = $"[{message.Timestamp:HH:mm:ss}] [ERROR] {message.Message}";
                    break;
                default:
                    displayMessage = $"[{message.Timestamp:HH:mm:ss}] [UNHANDLED:{message.Type}] {message.Username}: {message.Message}";
                    break;
            }
            Console.WriteLine(displayMessage);
            Console.Write(GetPrompt());
        }

        private void OnStatusChanged(string status)
        {
            ClearCurrentConsoleLine();
            Console.WriteLine($"[STATUS] {status}");
            Console.Write(GetPrompt());
        }

        private void OnFileOfferReceived(FileTransferInfo offer)
        {
            ClearCurrentConsoleLine();
            Console.WriteLine($"File offer from {offer.FromUsername}: {offer.FileName} ({offer.FileSize} bytes).");
            Console.WriteLine($"Type '/accept {offer.Id}' or '/reject {offer.Id}'.");
            Console.Write(GetPrompt());
        }

        private void OnFileTransferProgress(string fileId, int sent, int total)
        {
            ClearCurrentConsoleLine();
            var percentage = total > 0 ? (int)((double)sent / total * 100) : 0;
            Console.Write($"File transfer progress for {fileId}: {sent}/{total} bytes ({percentage}%)");
            if (sent == total)
            {
                Console.WriteLine(); // New line on completion
            }
            Console.Write(GetPrompt());
        }

        private static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}