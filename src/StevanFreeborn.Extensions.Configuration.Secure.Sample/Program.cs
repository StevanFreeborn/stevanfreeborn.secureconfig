using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StevanFreeborn.Extensions.Configuration.Secure;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Sample;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║  StevanFreeborn.Extensions.Configuration.Secure Sample   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();

await Demo1_BasicFileStorageWithBase64Key();
await Demo2_MachineIdKeyDerivation();
await Demo3_HostAndIOptionsIntegration();
await Demo4_ConfigurationProviderPattern();
await Demo5_CustomStorageProvider();
await Demo6_CustomKeyProvider();
await Demo7_TypedAOTOverloads();

Console.WriteLine("All demos completed.");

static async Task Demo1_BasicFileStorageWithBase64Key()
{
  PrintHeader("Demo 1: Basic File Storage with Base64 Key");

  using var tempDir = new TempDirectory();
  var key = GenerateBase64Key();

  var services = new ServiceCollection();
  services.AddSecureConfig(builder =>
  {
    builder
      .WithBase64EncryptionKey(key)
      .WithAesCryptoProvider()
      .UseJsonFileStorage(new JsonStorageOptions
      {
        DirectoryPath = tempDir.Path,
        FileName = "demo1.json",
      })
      .AddJsonAotContext(AppJsonContext.Default);
  });

  var provider = services.BuildServiceProvider();
  var secureConfig = provider.GetRequiredService<ISecureConfig>();

  var dbSettings = new DatabaseSettings
  {
    ConnectionString = "Server=localhost;Database=MyDb;Trusted_Connection=True;",
    Timeout = 30,
    RetryCount = 3,
  };

  Console.WriteLine("  Storing DatabaseSettings...");
  await secureConfig.SetAsync("Database", dbSettings);

  var retrieved = await secureConfig.GetAsync<DatabaseSettings>("Database");
  Console.WriteLine($"  Retrieved: ConnectionString={retrieved!.ConnectionString}");
  Console.WriteLine($"  Retrieved: Timeout={retrieved.Timeout}, RetryCount={retrieved.RetryCount}");

  var smtpSettings = new SmtpSettings
  {
    Host = "smtp.example.com",
    Port = 587,
    Username = "user@example.com",
    Password = "s3cretP@ssw0rd!",
    UseSsl = true,
  };

  Console.WriteLine("  Storing SmtpSettings (sensitive data)...");
  await secureConfig.SetAsync("Smtp", smtpSettings);

  var smtp = await secureConfig.GetAsync<SmtpSettings>("Smtp");
  Console.WriteLine($"  Retrieved: Host={smtp!.Host}, Port={smtp.Port}, UseSsl={smtp.UseSsl}");
  Console.WriteLine($"  Retrieved: Username={smtp.Username}, Password={smtp.Password}");

  Console.WriteLine("  Deleting Smtp settings...");
  var deleted = await secureConfig.DeleteAsync("Smtp");
  Console.WriteLine($"  Deleted: {deleted}");

  var missing = await secureConfig.GetAsync<SmtpSettings>("Smtp");
  Console.WriteLine($"  After delete: {(missing is null ? "null (as expected)" : "still present")}");

  Console.WriteLine();
}

static async Task Demo2_MachineIdKeyDerivation()
{
  PrintHeader("Demo 2: Machine ID Key Derivation");

  using var tempDir = new TempDirectory();

  var services = new ServiceCollection();
  services.AddSecureConfig(builder =>
  {
    builder
      .WithMachineIdKey()
      .WithAesCryptoProvider()
      .UseJsonFileStorage(new JsonStorageOptions
      {
        DirectoryPath = tempDir.Path,
        FileName = "demo2.json",
      })
      .AddJsonAotContext(AppJsonContext.Default);
  });

  var provider = services.BuildServiceProvider();
  var secureConfig = provider.GetRequiredService<ISecureConfig>();

  Console.WriteLine("  Storing config encrypted with machine-derived key...");
  await secureConfig.SetAsync("MachineLocked", new ApiOptions { ApiKey = "machine-specific-secret" });

  var value = await secureConfig.GetAsync<ApiOptions>("MachineLocked");
  Console.WriteLine($"  Retrieved on same machine: ApiKey={value!.ApiKey}");
  Console.WriteLine("  (This data would NOT be decryptable on a different machine)");

  Console.WriteLine();
}

static async Task Demo3_HostAndIOptionsIntegration()
{
  PrintHeader("Demo 3: Host + IOptions / IOptionsSnapshot / IOptionsMonitor");

  using var tempDir = new TempDirectory();
  var key = GenerateBase64Key();

  Action<ISecureConfigBuilder> configure = builder =>
  {
    builder
      .WithBase64EncryptionKey(key)
      .WithAesCryptoProvider()
      .UseJsonFileStorage(new JsonStorageOptions
      {
        DirectoryPath = tempDir.Path,
        FileName = "demo3.json",
      })
      .AddJsonAotContext(AppJsonContext.Default);
  };

  var host = await Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration((_, b) => b.AddSecureConfig(configure))
    .ConfigureServices((ctx, s) =>
    {
      s.Configure<ApiOptions>(ctx.Configuration.GetSection(nameof(ApiOptions)));
      s.AddSecureConfig(configure);
    })
    .StartAsync();

  var secureConfig = host.Services.GetRequiredService<ISecureConfig>();
  var options = host.Services.GetRequiredService<IOptions<ApiOptions>>();
  var monitor = host.Services.GetRequiredService<IOptionsMonitor<ApiOptions>>();
  var config = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

  Console.WriteLine("  Setting initial ApiOptions...");
  await secureConfig.SetAsync(nameof(ApiOptions), new ApiOptions { ApiKey = "initial-key-001" });
  config.Reload();

  Console.WriteLine($"  IOptions:            {options.Value.ApiKey}");
  Console.WriteLine($"  IOptionsMonitor:     {monitor.CurrentValue.ApiKey}");

  Console.WriteLine("  Updating ApiOptions and reloading config...");
  await secureConfig.SetAsync(nameof(ApiOptions), new ApiOptions { ApiKey = "updated-key-002" });
  config.Reload();

  Console.WriteLine($"  IOptions:            {options.Value.ApiKey} (unchanged - singleton)");
  Console.WriteLine($"  IOptionsMonitor:     {monitor.CurrentValue.ApiKey} (updated)");

  using (var scope1 = host.Services.CreateScope())
  {
    var snapshot1 = scope1.ServiceProvider.GetRequiredService<IOptionsSnapshot<ApiOptions>>();
    Console.WriteLine($"  IOptionsSnapshot 1:  {snapshot1.Value.ApiKey} (new scope, sees update)");
  }

  await host.StopAsync();
  Console.WriteLine();
}

static async Task Demo4_ConfigurationProviderPattern()
{
  PrintHeader("Demo 4: IConfigurationBuilder Pattern (No DI)");

  using var tempDir = new TempDirectory();
  var key = GenerateBase64Key();

  var seedServices = new ServiceCollection();
  seedServices.AddSecureConfig(builder =>
  {
    builder
      .WithBase64EncryptionKey(key)
      .WithAesCryptoProvider()
      .UseJsonFileStorage(new JsonStorageOptions
      {
        DirectoryPath = tempDir.Path,
        FileName = "demo4.json",
      })
      .AddJsonAotContext(AppJsonContext.Default);
  });

  var seedProvider = seedServices.BuildServiceProvider();
  var secureConfig = seedProvider.GetRequiredService<ISecureConfig>();

  Console.WriteLine("  Seeding data via ISecureConfig...");
  await secureConfig.SetAsync("ApiOptions", new ApiOptions { ApiKey = "from-config-builder" });

  var configuration = new ConfigurationBuilder()
    .AddSecureConfig(builder =>
    {
      builder
        .WithBase64EncryptionKey(key)
        .WithAesCryptoProvider()
        .UseJsonFileStorage(new JsonStorageOptions
        {
          DirectoryPath = tempDir.Path,
          FileName = "demo4.json",
        });
    })
    .Build();

  var apiKey = configuration["ApiOptions:ApiKey"];
  Console.WriteLine($"  Read via IConfiguration: ApiOptions:ApiKey = {apiKey}");

  Console.WriteLine();
}

static async Task Demo5_CustomStorageProvider()
{
  PrintHeader("Demo 5: Custom Storage Provider (In-Memory)");

  var key = GenerateBase64Key();
  var memoryStore = new Dictionary<string, string>();

  var services = new ServiceCollection();
  services.AddSecureConfig(builder =>
  {
    builder
      .WithBase64EncryptionKey(key)
      .WithAesCryptoProvider()
      .UseCustomStorage(new InMemoryStorageProvider(memoryStore))
      .AddJsonAotContext(AppJsonContext.Default);
  });

  var provider = services.BuildServiceProvider();
  var secureConfig = provider.GetRequiredService<ISecureConfig>();

  Console.WriteLine("  Storing ApiOptions in in-memory storage...");
  await secureConfig.SetAsync("ApiOptions", new ApiOptions { ApiKey = "in-memory-secret" });

  Console.WriteLine($"  Memory store now has {memoryStore.Count} encrypted entry(ies)");
  Console.WriteLine($"  Encrypted value starts with: {memoryStore["ApiOptions"][..40]}...");

  var retrieved = await secureConfig.GetAsync<ApiOptions>("ApiOptions");
  Console.WriteLine($"  Retrieved: ApiKey={retrieved!.ApiKey}");

  Console.WriteLine();
}

static async Task Demo6_CustomKeyProvider()
{
  PrintHeader("Demo 6: Custom Key Provider");

  using var tempDir = new TempDirectory();

  var customKeyProvider = new EnvironmentVariableKeyProvider("MY_SECURE_CONFIG_KEY");

  var services = new ServiceCollection();
  services.AddSecureConfig(builder =>
  {
    builder
      .WithCustomKeyProvider(customKeyProvider)
      .WithAesCryptoProvider()
      .UseJsonFileStorage(new JsonStorageOptions
      {
        DirectoryPath = tempDir.Path,
        FileName = "demo6.json",
      })
      .AddJsonAotContext(AppJsonContext.Default);
  });

  var provider = services.BuildServiceProvider();
  var secureConfig = provider.GetRequiredService<ISecureConfig>();

  Console.WriteLine("  Storing ApiOptions with custom key from environment variable...");
  await secureConfig.SetAsync("ApiOptions", new ApiOptions { ApiKey = "env-var-protected" });

  var retrieved = await secureConfig.GetAsync<ApiOptions>("ApiOptions");
  Console.WriteLine($"  Retrieved: ApiKey={retrieved!.ApiKey}");

  Console.WriteLine();
}

static async Task Demo7_TypedAOTOverloads()
{
  PrintHeader("Demo 7: Typed AOT Overloads (Native AOT Support)");

  using var tempDir = new TempDirectory();
  var key = GenerateBase64Key();

  var services = new ServiceCollection();
  services.AddSecureConfig(builder =>
  {
    builder
      .WithBase64EncryptionKey(key)
      .WithAesCryptoProvider()
      .UseJsonFileStorage(new JsonStorageOptions
      {
        DirectoryPath = tempDir.Path,
        FileName = "demo7.json",
      })
      .AddJsonAotContext(AppJsonContext.Default);
  });

  var provider = services.BuildServiceProvider();
  var secureConfig = provider.GetRequiredService<ISecureConfig>();

  JsonTypeInfo<DatabaseSettings> dbTypeInfo = AppJsonContext.Default.DatabaseSettings;
  JsonTypeInfo<SmtpSettings> smtpTypeInfo = AppJsonContext.Default.SmtpSettings;

  Console.WriteLine("  Setting values using explicit JsonTypeInfo<T> overloads...");
  await secureConfig.SetAsync(
    "Database",
    new DatabaseSettings { ConnectionString = "Server=prod;Database=Main;", Timeout = 60, RetryCount = 5 },
    dbTypeInfo
  );

  await secureConfig.SetAsync(
    "Smtp",
    new SmtpSettings { Host = "smtp.prod.com", Port = 465, Username = "admin", Password = "prod!", UseSsl = true },
    smtpTypeInfo
  );

  Console.WriteLine("  Getting values using explicit JsonTypeInfo<T> overloads...");
  var db = await secureConfig.GetAsync("Database", dbTypeInfo);
  var smtp = await secureConfig.GetAsync("Smtp", smtpTypeInfo);

  Console.WriteLine($"  Database: ConnectionString={db!.ConnectionString}, Timeout={db.Timeout}");
  Console.WriteLine($"  Smtp:     Host={smtp!.Host}, Port={smtp.Port}, UseSsl={smtp.UseSsl}");

  Console.WriteLine();
}

static string GenerateBase64Key()
{
  var key = new byte[32];
  RandomNumberGenerator.Fill(key);
  return Convert.ToBase64String(key);
}

static void PrintHeader(string title)
{
  Console.WriteLine($"── {title} ──");
}

sealed class TempDirectory : IDisposable
{
  public string Path { get; }

  public TempDirectory()
  {
    Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(Path);
  }

  public void Dispose()
  {
    if (Directory.Exists(Path))
    {
      Directory.Delete(Path, true);
    }
  }
}

sealed class InMemoryStorageProvider : ISecureStorageProvider
{
  private readonly Dictionary<string, string> _store;

  public InMemoryStorageProvider(Dictionary<string, string> store)
  {
    _store = store;
  }

  public event EventHandler? StorageChanged;

  public Task<string> ReadAsync(string key, CancellationToken ct = default)
  {
    return _store.TryGetValue(key, out var value) ? Task.FromResult(value) : Task.FromResult(string.Empty);
  }

  public Task<IDictionary<string, string>> ReadAllAsync(CancellationToken ct = default)
  {
    return Task.FromResult<IDictionary<string, string>>(new Dictionary<string, string>(_store));
  }

  public Task WriteAsync(string key, string encryptedData, CancellationToken ct = default)
  {
    _store[key] = encryptedData;
    StorageChanged?.Invoke(this, EventArgs.Empty);
    return Task.CompletedTask;
  }

  public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
  {
    return Task.FromResult(_store.Remove(key));
  }

  public void Dispose()
  {
  }
}

sealed class EnvironmentVariableKeyProvider : IEncryptionKeyProvider
{
  private readonly string _variableName;

  public EnvironmentVariableKeyProvider(string variableName)
  {
    _variableName = variableName;
  }

  public byte[] GetKey()
  {
    var value = Environment.GetEnvironmentVariable(_variableName);

    if (string.IsNullOrWhiteSpace(value))
    {
      Console.WriteLine($"    [Warning] Environment variable '{_variableName}' not set. Using SHA256 hash of variable name as key.");
      value = _variableName;
    }

    return SHA256.HashData(Encoding.UTF8.GetBytes(value));
  }
}