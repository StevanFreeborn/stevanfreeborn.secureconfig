using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using StevanFreeborn.Extensions.Configuration.Secure;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Sample;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

Action<ISecureConfigBuilder> configure = builder =>
{
  builder
    .WithMachineIdKey()
    .WithAesCryptoProvider()
    .UseJsonFileStorage(new JsonStorageOptions())
    .AddJsonAotContext(AppJsonContext.Default);
};

var builder = Host.CreateDefaultBuilder()
  .ConfigureAppConfiguration((_, b) =>
  {
    b.AddSecureConfig(configure);
  })
  .ConfigureServices((ctx, s) =>
  {
    s.Configure<ApiOptions>(ctx.Configuration.GetSection(nameof(ApiOptions)));
    s.AddSecureConfig(configure);
  });

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<ApiOptions>>();
var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<ApiOptions>>();
var secureConfig = app.Services.GetRequiredService<ISecureConfig>();
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

var firstScope = scopeFactory.CreateScope();
var firstSnapshot = firstScope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ApiOptions>>();

var originalValue = await secureConfig.GetAsync<ApiOptions>(nameof(ApiOptions));
Console.WriteLine($"Config: {originalValue}");
Console.WriteLine($"IOptions: {options.Value}");
Console.WriteLine($"IOptionsSnapshot 1: {firstSnapshot.Value}");
Console.WriteLine($"IOptionsMonitor: {optionsMonitor.CurrentValue}");

await secureConfig.SetAsync(
  nameof(ApiOptions),
  new ApiOptions { ApiKey = Guid.NewGuid().ToString() }
);

var config = (IConfigurationRoot)app.Services.GetRequiredService<IConfiguration>();
config.Reload();

var updatedValue = await secureConfig.GetAsync<ApiOptions>(nameof(ApiOptions));
var secondScope = scopeFactory.CreateScope();
var secondSnapshot = secondScope.ServiceProvider.GetRequiredService<IOptionsSnapshot<ApiOptions>>();

Console.WriteLine($"Config: {updatedValue}");
Console.WriteLine($"IOptions: {options.Value}");
Console.WriteLine($"IOptionsSnapshot 2: {secondSnapshot.Value}");
Console.WriteLine($"IOptionsMonitor: {optionsMonitor.CurrentValue}");
