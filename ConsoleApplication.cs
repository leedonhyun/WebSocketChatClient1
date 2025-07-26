using ChatSystem.Client.Interfaces;
using ChatSystem.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatSystem.Client.Console;

public class ConsoleApplication
{
    private readonly IChatClient _chatClient;
    private readonly ChatClient _chatClientImpl; // 구체적인 타입 캐시
    private readonly Dictionary<string, FileTransferInfo> _pendingFiles = new();

    public ConsoleApplication(IChatClient chatClient)
    {
        _chatClient = chatClient;
        _chatClientImpl = (ChatClient)chatClient; // 한 번만 캐스팅

        _chatClient.MessageReceived += OnMessageReceived;
        _chatClient.StatusChanged += OnStatusChanged;
        _chatClient.FileOfferReceived += OnFileOfferReceived;
        _chatClient.FileTransferProgress += OnFileTransferProgress;
    }

    public async Task RunAsync()
    {
        System.Console.WriteLine(ClientConstants.ConsoleUI.WelcomeHeader);
        foreach (var line in ClientConstants.ConsoleUI.HelpText)
        {
            System.Console.WriteLine(line);
        }
        System.Console.WriteLine();
        System.Console.WriteLine(ClientConstants.ConsoleUI.Note);
        if (!string.IsNullOrEmpty(_chatClient.CurrentRoom))
        {
            System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.CurrentRoomInfo, _chatClient.CurrentRoom));
        }
        System.Console.WriteLine();

        while (true)
        {
            // 현재 방 상태를 프롬프트에 표시
            var prompt = "";
            if (_chatClient.IsConnected)
            {
                if (!string.IsNullOrEmpty(_chatClient.CurrentRoom))
                {
                    prompt = string.Format(ClientConstants.ConsoleUI.PromptRoomFormat, _chatClient.CurrentRoom);
                }
                else
                {
                    prompt = ClientConstants.ConsoleUI.PromptPublic;
                }
            }
            else
            {
                prompt = ClientConstants.ConsoleUI.PromptDisconnected;
            }

            System.Console.Write(prompt);
            var input = System.Console.ReadLine();
            if (string.IsNullOrEmpty(input)) continue;

            if (input.StartsWith("/"))
            {
                if (input.Equals(ClientConstants.Commands.Quit, StringComparison.OrdinalIgnoreCase))
                {
                    await _chatClient.DisconnectAsync();
                    break;
                }

                await _chatClientImpl.ProcessCommandAsync(input);
            }
            else
            {
                if (_chatClient.IsConnected)
                {
                    // Check if user is in a room and wants to send to current room
                    if (!string.IsNullOrEmpty(_chatClient.CurrentRoom))
                    {
                        await _chatClientImpl.SendRoomMessageAsync(input, _chatClient.CurrentRoom);
                    }
                    else
                    {
                        await _chatClient.SendMessageAsync(input);
                    }
                }
                else
                    System.Console.WriteLine(ClientConstants.ErrorMessages.NotConnectedSimple);
            }
        }
    }

    private static void OnMessageReceived(ChatMessage message)
    {
        var timestamp = message.Timestamp.ToString("HH:mm:ss");
        switch (message.Type)
        {
            case ClientConstants.MessageTypes.System:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.System, timestamp, message.Message));
                break;
            case ClientConstants.MessageTypes.Chat:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.Chat, timestamp, message.Username, message.Message));
                break;
            case ClientConstants.MessageTypes.PrivateMessage:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.Private, timestamp, message.Username, message.Message));
                break;
            case ClientConstants.MessageTypes.RoomMessage:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.Room, timestamp, message.Username, message.Message));
                break;
            case ClientConstants.MessageTypes.UserList:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.UserList, timestamp, message.Message));
                break;
            case ClientConstants.MessageTypes.RoomList:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.RoomList, timestamp, message.Message));
                break;
            case ClientConstants.MessageTypes.RoomMembers:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.RoomMembers, timestamp, message.Message));
                break;
            case ClientConstants.MessageTypes.RoomJoined:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.RoomJoined, timestamp, message.Message));
                break;
            case ClientConstants.MessageTypes.RoomLeft:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.RoomLeft, timestamp, message.Message));
                break;
            case ClientConstants.MessageTypes.RoomCreated:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.RoomCreated, timestamp, message.Message));
                break;
            default:
                System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.MessageFormats.Chat, timestamp, message.Username, message.Message));
                break;
        }
    }

    private static void OnStatusChanged(string status)
    {
        System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.StatusFormat, status));
    }

    private void OnFileOfferReceived(FileTransferInfo fileInfo)
    {
        _pendingFiles[fileInfo.Id] = fileInfo;

        System.Console.WriteLine(ClientConstants.ConsoleUI.FileOfferHeader);
        System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.FileOfferFrom, fileInfo.FromUsername));
        System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.FileOfferFile, fileInfo.FileName));
        System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.FileOfferSize, fileInfo.FileSize));
        System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.FileOfferId, fileInfo.Id));
        System.Console.WriteLine(ClientConstants.ConsoleUI.FileOfferReady);
        System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.FileOfferInstructions, fileInfo.Id));
        System.Console.WriteLine();
    }

    private void OnFileTransferProgress(string fileId, int chunkIndex, int totalChunks)
    {
        if (_pendingFiles.TryGetValue(fileId, out var fileInfo))
        {
            var progress = (double)(chunkIndex + 1) / totalChunks * 100;
            System.Console.WriteLine(string.Format(ClientConstants.ConsoleUI.FileTransferProgress, fileInfo.FileName, progress, chunkIndex + 1, totalChunks));
        }
    }
}