using System.CommandLine;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;

internal sealed class DeleteCommandFactory(
  ISecureConfig secureConfig,
  ITerminal terminal
) : ICommandFactory
{
  private const string CommandName = "delete";
  private const string CommandDescription = "Delete the value for the given key";

  private readonly Argument<string> _keyArg = new("key")
  {
    Description = "The key whose value you want to remove."
  };

  private readonly ISecureConfig _secureConfig = secureConfig;
  private readonly ITerminal _terminal = terminal;

  public Command Create()
  {
    var command = new Command(CommandName, CommandDescription)
    {
      _keyArg,
    };

    command.SetAction(HandleAsync);

    return command;
  }

  private async Task<int> HandleAsync(ParseResult result, CancellationToken ct)
  {
    var key = result.GetRequiredValue(_keyArg);
    var isDeleted = await _secureConfig.DeleteAsync(key, ct);

    if (isDeleted)
    {
      _terminal.WriteLine($"Successfully removed value for {key}");
      return ExitCodes.Success;
    }
    else
    {
      _terminal.WriteLine($"Unable to remove value for {key}");
      return ExitCodes.KeyNotRemoved;
    }
  }
}