using WebSocketChatClient1.Client.Models;

using System.Text;

using WebSocketChatClient1.Interfaces;
namespace WebSocketChatClient1.Client.Services;
public class CommandParser : ICommandParser
{
    public ParsedCommand Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input) || !input.StartsWith("/"))
        {
            return new ParsedCommand { IsValid = false, ErrorMessage = "Commands must start with '/'." };
        }

        var commandAndArgs = ParseCommandParts(input.Substring(1));
        if (commandAndArgs.Length == 0)
        {
            return new ParsedCommand { IsValid = false, ErrorMessage = "Empty command." };
        }

        var command = commandAndArgs[0].ToLower();
        var arguments = new List<string>();
        var options = new Dictionary<string, object>();

        for (int i = 1; i < commandAndArgs.Length; i++)
        {
            var part = commandAndArgs[i];
            if (part.StartsWith("-"))
            {
                var key = part.TrimStart('-');
                if (i + 1 < commandAndArgs.Length && !commandAndArgs[i + 1].StartsWith("-"))
                {
                    if (key.Equals("password", StringComparison.OrdinalIgnoreCase))
                    {
                        options[key] = commandAndArgs[i + 1];
                        i++;
                    }
                    else
                    {
                        options[key] = true;
                    }
                }
                else
                {
                    options[key] = true;
                }
            }
            else
            {
                arguments.Add(part);
            }
        }

        return new ParsedCommand
        {
            Command = command,
            Arguments = arguments.ToArray(),
            Options = options,
            IsValid = true
        };
    }

    private static string[] ParseCommandParts(string input)
    {
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        bool inQuotes = false;

        foreach (char c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (currentPart.Length > 0)
                {
                    parts.Add(currentPart.ToString());
                    currentPart.Clear();
                }
            }
            else
            {
                currentPart.Append(c);
            }
        }

        if (currentPart.Length > 0)
        {
            parts.Add(currentPart.ToString());
        }

        return parts.ToArray();
    }
}