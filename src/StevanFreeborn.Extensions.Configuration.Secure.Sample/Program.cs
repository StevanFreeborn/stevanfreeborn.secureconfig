using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using StevanFreeborn.Extensions.Configuration.Secure.Sample;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

var builder = Host.CreateApplicationBuilder();

const string configFileName = "appsettings.json";
var opts = new JsonStorageOptions()
{
  FileName = configFileName,
};

var storageProvider = new JsonFileStorageProvider(opts);

builder.Configuration.AddSecureConfig(storageProvider);

builder.Services.Configure<ApiOptions>(
  builder.Configuration.GetSection(nameof(ApiOptions))
);

builder.Services.AddSecureConfig()
  .UseJsonFileStorage(opt => opt.FileName = configFileName);

var app = builder.Build();

var options = app.Services.GetRequiredService<IOptions<ApiOptions>>();
var secureConfig = app.Services.GetRequiredService<ISecureConfig>();

Console.WriteLine(options.Value);

await secureConfig.SetAsync(
  nameof(ApiOptions),
  new ApiOptions { ApiKey = "apiKey" }
);

