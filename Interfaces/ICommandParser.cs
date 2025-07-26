using ChatSystem.Client.Models;

namespace WebSocketChatClient1.Interfaces;

public interface ICommandParser
{
    ParsedCommand Parse(string input);
}