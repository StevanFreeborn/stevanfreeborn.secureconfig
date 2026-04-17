using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using StevanFreeborn.Extensions.Configuration.Secure;
using StevanFreeborn.Extensions.Configuration.Secure.Cli;
using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;
using StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cli.Json;

await Host.CreateDefaultBuilder(args)
  .ConfigureAppConfiguration(static config =>
  {
    config.SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile("appsettings.json", false);
  })
  .ConfigureLogging(static logging => logging.ClearProviders())
  .ConfigureServices((ctx, services) =>
  {
    services.AddSingleton(args);

    services.AddSecureConfig(b =>
    {
      var cliOptions = new SecureConfigCliOptions();
      ctx.Configuration.Bind(cliOptions);

      var profileName = ctx.Configuration.GetValue<string>("profile") ?? cliOptions.DefaultProfile;

      if (cliOptions.Profiles.TryGetValue(profileName, out var profile) is false)
      {
        throw new InvalidOperationException($"Secure configuration profile '{profileName}' not found.");
      }

      b.AddJsonAotContext(CliJsonContext.Default).ApplyProfile(profile);
    });

    services.AddSingleton<ITerminal, Terminal>();
    services.AddSingleton<ICommandFactory, GetCommandFactory>();
    services.AddSingleton<ICommandFactory, SetCommandFactory>();
    services.AddSingleton<ICommandFactory, DeleteCommandFactory>();
    services.AddHostedService<SecureConfigCli>();
  })
  .RunConsoleAsync();