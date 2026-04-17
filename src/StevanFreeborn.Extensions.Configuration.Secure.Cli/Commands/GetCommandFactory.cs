using System.CommandLine;
using System.Text.Json;

using StevanFreeborn.Extensions.Configuration.Secure.Cli.Json;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;

internal sealed class GetCommandFactory(
  ISecureConfig secureConfig,
  ITerminal terminal
) : ICommandFactory
{
  private const string CommandName = "get";
  private const string CommandDescription = "Retrieves the value for the given key";

  private readonly Argument<string> _keyArg = new("key")
  {
    Description = "The key whose value you want to retrieve."
  };

  private readonly Option<bool> _prettyOption = new("--pretty", "-p")
  {
    Description = "Indicates whether the value - if JSON - should be pretty printed or not"
  };

  private readonly ISecureConfig _secureConfig = secureConfig;
  private readonly ITerminal _terminal = terminal;

  public Command Create()
  {
    var command = new Command(CommandName, CommandDescription)
    {
      _keyArg,
      _prettyOption
    };

    command.SetAction(HandleAsync);

    return command;
  }

  private async Task<int> HandleAsync(ParseResult result, CancellationToken ct)
  {
    var key = result.GetRequiredValue(_keyArg);
    var shouldPrintPretty = result.GetValue(_prettyOption);
    var value = await _secureConfig.GetAsync<string>(key, ct);

    if (value is null)
    {
      _terminal.WriteLine($"Unable to retrieve value for {key}");
      return ExitCodes.KeyNotFound;
    }

    try
    {
      using var json = JsonDocument.Parse(value);

      if (shouldPrintPretty)
      {
        var options = new JsonSerializerOptions()
        {
          WriteIndented = true,
        };
        var context = new CliJsonContext(options);
        value = JsonSerializer.Serialize(json.RootElement, context.JsonElement);
      }
      else
      {
        value = JsonSerializer.Serialize(json.RootElement, CliJsonContext.Default.JsonElement);
      }
    }
    catch (JsonException)
    {
    }

    _terminal.WriteLine(value);
    return ExitCodes.Success;
  }
}