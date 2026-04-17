
using System.CommandLine;
using System.Text.Json;

using StevanFreeborn.Extensions.Configuration.Secure.Cli.Json;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;

internal sealed class SetCommandFactory(
  ISecureConfig secureConfig,
  ITerminal terminal
) : ICommandFactory
{
  private const string CommandName = "set";
  private const string CommandDescription = "Sets the value for the given key";

  private readonly Argument<string> _keyArg = new("key")
  {
    Description = "The key whose value you want to set."
  };

  private readonly Argument<string> _valueArg = new("value")
  {
    Description = "The value that you want to set for the key."
  };

  private readonly ISecureConfig _secureConfig = secureConfig;
  private readonly ITerminal _terminal = terminal;

  public Command Create()
  {
    var command = new Command(CommandName, CommandDescription)
    {
      _keyArg,
      _valueArg,
    };

    command.SetAction(HandleAsync);

    return command;
  }

  private async Task<int> HandleAsync(ParseResult result, CancellationToken ct)
  {
    var key = result.GetRequiredValue(_keyArg);
    var value = result.GetRequiredValue(_valueArg);

    try
    {
      using var json = JsonDocument.Parse(value);
      value = JsonSerializer.Serialize(json.RootElement, CliJsonContext.Default.JsonElement);
    }
    catch (JsonException)
    {
    }

    await _secureConfig.SetAsync(key, value, ct);

    _terminal.WriteLine($"Successfully set value of {key}");
    return ExitCodes.Success;
  }
}