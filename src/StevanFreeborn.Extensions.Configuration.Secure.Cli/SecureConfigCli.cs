using System.CommandLine;

using Microsoft.Extensions.Hosting;

using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli;

internal sealed class SecureConfigCli : IHostedService
{
  private readonly string[] _args;
  private readonly IHostApplicationLifetime _lifetime;
  private readonly RootCommand _rootCommand = new("secure-config is a command line utility used to manage your encrypted configuration")
  {
    new Option<string>("--profile")
    {
      Description = "Allows specifying which profile to use from the appsettings.json file.",
    },
  };

  public SecureConfigCli(
    string[] args,
    IHostApplicationLifetime lifetime,
    IEnumerable<ICommandFactory> commandFactories
  )
  {
    _args = args;
    _lifetime = lifetime;

    foreach (var factory in commandFactories)
    {
      _rootCommand.Add(factory.Create());
    }
  }

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    var exitCode = await _rootCommand.Parse(_args[1..]).InvokeAsync(cancellationToken: cancellationToken);
    Environment.ExitCode = exitCode;

    _lifetime.StopApplication();
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    return Task.CompletedTask;
  }
}