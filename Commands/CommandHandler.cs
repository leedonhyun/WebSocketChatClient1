using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using WebSocketChatClient1.Interfaces;

namespace WebSocketChatClient1.Client.Commands;

/// <summary>
/// Handles parsing and execution of commands.
/// </summary>
public class CommandHandler
{
    private readonly Dictionary<string, ICommand> _commands = new();
    private readonly ICommandParser _commandParser;
    private readonly Action<string> _statusChanged;

    public CommandHandler(ICommandParser commandParser, Action<string> statusChanged)
    {
        _commandParser = commandParser;
        _statusChanged = statusChanged;
    }

    /// <summary>
    /// Registers a command with its associated aliases.
    /// </summary>
    public void RegisterCommand(ICommand command, params string[] aliases)
    {
        foreach (var alias in aliases)
        {
            _commands[alias.ToLowerInvariant()] = command;
        }
    }

    /// <summary>
    /// Parses the input string and executes the corresponding command.
    /// </summary>
    public async Task ProcessCommandAsync(string input)
    {
        var parsed = _commandParser.Parse(input);
        //var (commandName, args) = _commandParser.Parse(input);
        if (!parsed.IsValid)
        {
            _statusChanged?.Invoke(string.Format(ClientConstants.ErrorMessages.InvalidCommand, parsed.ErrorMessage));
            return;
        }

        if (_commands.TryGetValue(parsed.Command.ToLowerInvariant(), out var command))
        {
            await command.ExecuteAsync(parsed.Arguments, parsed.Options);
        }
        else
        {
            _statusChanged?.Invoke(string.Format(ClientConstants.ErrorMessages.UnknownCommand, parsed.Command));
        }
    }
}