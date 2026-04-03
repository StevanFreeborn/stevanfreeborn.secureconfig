using System.Security.Cryptography;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;
using StevanFreeborn.Extensions.Configuration.Secure.Tests.Common;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Integration;

public class SecureConfigOptionsIntegrationTests : IDisposable
{
  private readonly string _tempDir;
  private readonly string _base64Key;

  public SecureConfigOptionsIntegrationTests()
  {
    _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_tempDir);
    _base64Key = KeyGenerator.GetValidBase64Key();
  }

  [Fact]
  public async Task ConfigureSecureConfig_WithIOptions_ItShouldResolveOptions()
  {
    var host = await CreateHostAsync();

    using var scope = host.Services.CreateScope();
    var secureConfig = scope.ServiceProvider.GetRequiredService<ISecureConfig>();

    await secureConfig.SetAsync(nameof(TestApiOptions), new TestApiOptions { ApiKey = "initial-key" });

    var config = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
    config.Reload();

    var options = scope.ServiceProvider.GetRequiredService<IOptions<TestApiOptions>>();

    options.Value.Should().NotBeNull();
    options.Value.ApiKey.Should().Be("initial-key");
  }

  [Fact]
  public async Task ConfigureSecureConfig_WithIOptionsSnapshot_ItShouldResolveNewInstancePerScope()
  {
    var host = await CreateHostAsync();

    using var scope1 = host.Services.CreateScope();
    var snapshot1 = scope1.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestApiOptions>>();

    using var scope2 = host.Services.CreateScope();
    var snapshot2 = scope2.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestApiOptions>>();

    snapshot1.Should().NotBeSameAs(snapshot2);
    snapshot1.Value.Should().BeEquivalentTo(snapshot2.Value);
  }

  [Fact]
  public async Task ConfigureSecureConfig_WithIOptionsMonitor_ItShouldResolveMonitor()
  {
    var host = await CreateHostAsync();

    var monitor = host.Services.GetRequiredService<IOptionsMonitor<TestApiOptions>>();

    monitor.Should().NotBeNull();
    monitor.CurrentValue.Should().NotBeNull();
  }

  [Fact]
  public async Task ConfigureSecureConfig_WhenValueUpdatedAndReloaded_ItShouldReflectInIOptionsMonitor()
  {
    var host = await CreateHostAsync();

    var secureConfig = host.Services.GetRequiredService<ISecureConfig>();
    var monitor = host.Services.GetRequiredService<IOptionsMonitor<TestApiOptions>>();
    var config = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

    var originalValue = monitor.CurrentValue.ApiKey;

    await secureConfig.SetAsync(nameof(TestApiOptions), new TestApiOptions { ApiKey = "updated-key" });

    config.Reload();

    monitor.CurrentValue.ApiKey.Should().Be("updated-key");
    monitor.CurrentValue.ApiKey.Should().NotBe(originalValue);
  }

  [Fact]
  public async Task ConfigureSecureConfig_WhenValueUpdatedAndReloaded_ItShouldReflectInNewIOptionsSnapshot()
  {
    var host = await CreateHostAsync();

    using var scope1 = host.Services.CreateScope();
    var snapshotBefore = scope1.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestApiOptions>>();
    var originalValue = snapshotBefore.Value.ApiKey;

    var secureConfig = host.Services.GetRequiredService<ISecureConfig>();
    var config = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

    await secureConfig.SetAsync(nameof(TestApiOptions), new TestApiOptions { ApiKey = "snapshot-updated-key" });

    config.Reload();

    using var scope2 = host.Services.CreateScope();
    var snapshotAfter = scope2.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestApiOptions>>();

    snapshotAfter.Value.ApiKey.Should().Be("snapshot-updated-key");
    snapshotAfter.Value.ApiKey.Should().NotBe(originalValue);
  }

  [Fact]
  public async Task ConfigureSecureConfig_WhenValueUpdatedAndReloaded_ItShouldNotUpdateExistingIOptions()
  {
    var host = await CreateHostAsync();

    var secureConfig = host.Services.GetRequiredService<ISecureConfig>();
    var options = host.Services.GetRequiredService<IOptions<TestApiOptions>>();
    var config = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

    var originalValue = options.Value.ApiKey;

    await secureConfig.SetAsync(nameof(TestApiOptions), new TestApiOptions { ApiKey = "should-not-update" });

    config.Reload();

    options.Value.ApiKey.Should().Be(originalValue);
  }

  [Fact]
  public async Task ConfigureSecureConfig_WithNestedOptions_ItShouldBindCorrectly()
  {
    var host = await CreateHostAsync();

    var secureConfig = host.Services.GetRequiredService<ISecureConfig>();
    var config = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

    await secureConfig.SetAsync(nameof(TestApiOptions), new TestApiOptions { ApiKey = "nested-key" });

    config.Reload();

    var options = host.Services.GetRequiredService<IOptions<TestApiOptions>>();

    options.Value.ApiKey.Should().Be("nested-key");
  }

  [Fact]
  public async Task ConfigureSecureConfig_WithFullHost_ItShouldStartAndResolveAllServices()
  {
    var host = await CreateHostAsync();

    var secureConfig = host.Services.GetRequiredService<ISecureConfig>();
    var options = host.Services.GetRequiredService<IOptions<TestApiOptions>>();
    var snapshotFactory = host.Services.GetRequiredService<IServiceScopeFactory>();
    var monitor = host.Services.GetRequiredService<IOptionsMonitor<TestApiOptions>>();
    var configuration = host.Services.GetRequiredService<IConfiguration>();

    secureConfig.Should().NotBeNull();
    options.Should().NotBeNull();
    snapshotFactory.Should().NotBeNull();
    monitor.Should().NotBeNull();
    configuration.Should().NotBeNull();

    using var scope = snapshotFactory.CreateScope();
    var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestApiOptions>>();
    snapshot.Should().NotBeNull();
  }

  [Fact]
  public async Task ConfigureSecureConfig_WhenValueUpdated_ItShouldPropagateThroughEntirePipeline()
  {
    var host = await CreateHostAsync();

    var secureConfig = host.Services.GetRequiredService<ISecureConfig>();
    var options = host.Services.GetRequiredService<IOptions<TestApiOptions>>();
    var monitor = host.Services.GetRequiredService<IOptionsMonitor<TestApiOptions>>();
    var config = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();

    var originalOptionsValue = options.Value.ApiKey;
    var originalMonitorValue = monitor.CurrentValue.ApiKey;

    await secureConfig.SetAsync(nameof(TestApiOptions), new TestApiOptions { ApiKey = "pipeline-updated-key" });

    config.Reload();

    monitor.CurrentValue.ApiKey.Should().Be("pipeline-updated-key");

    using var scope = host.Services.CreateScope();
    var snapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<TestApiOptions>>();
    snapshot.Value.ApiKey.Should().Be("pipeline-updated-key");

    options.Value.ApiKey.Should().Be(originalOptionsValue);
  }

  public void Dispose()
  {
    if (Directory.Exists(_tempDir))
    {
      Directory.Delete(_tempDir, true);
    }

    GC.SuppressFinalize(this);
  }

  private Action<ISecureConfigBuilder> CreateConfigBuilder() => builder =>
  {
    builder
      .WithBase64EncryptionKey(_base64Key)
      .WithAesCryptoProvider()
      .UseJsonFileStorage(new JsonStorageOptions
      {
        DirectoryPath = _tempDir,
        FileName = "secure-config.json"
      })
      .AddJsonAotContext(OptionsTestJsonContext.Default);
  };

  private async Task<IHost> CreateHostAsync()
  {
    var configure = CreateConfigBuilder();

    var hostBuilder = Host.CreateDefaultBuilder()
      .ConfigureLogging(c => c.ClearProviders())
      .ConfigureAppConfiguration((_, builder) =>
      {
        builder.AddSecureConfig(configure);
      })
      .ConfigureServices((context, services) =>
      {
        services.Configure<TestApiOptions>(context.Configuration.GetSection(nameof(TestApiOptions)));
        services.AddSecureConfig(configure);
      });

    var host = hostBuilder.Build();
    await host.StartAsync();

    var secureConfig = host.Services.GetRequiredService<ISecureConfig>();

    if (string.IsNullOrEmpty((await secureConfig.GetAsync<TestApiOptions>(nameof(TestApiOptions)))?.ApiKey))
    {
      await secureConfig.SetAsync(nameof(TestApiOptions), new TestApiOptions { ApiKey = "default-key" });
      var config = (IConfigurationRoot)host.Services.GetRequiredService<IConfiguration>();
      config.Reload();
    }

    return host;
  }
}

internal sealed class TestApiOptions
{
  public string ApiKey { get; init; } = string.Empty;
}

[JsonSerializable(typeof(TestApiOptions))]
internal partial class OptionsTestJsonContext : JsonSerializerContext
{
}
