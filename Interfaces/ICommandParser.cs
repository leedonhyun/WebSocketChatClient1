using WebSocketChatClient1.Client.Models;

namespace WebSocketChatClient1.Interfaces;

public interface ICommandParser
{
    ParsedCommand Parse(string input);
}