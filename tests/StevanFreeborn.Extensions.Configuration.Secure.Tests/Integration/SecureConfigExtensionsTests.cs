using System.Security.Cryptography;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;
using StevanFreeborn.Extensions.Configuration.Secure.Tests.Common;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Integration;

public class SecureConfigExtensionsTests : IDisposable
{
  private readonly TempDirectory _tempDir = new();

  public void Dispose()
  {
    _tempDir.Dispose();
    GC.SuppressFinalize(this);
  }

  [Fact]
  public void AddSecureConfig_ToConfigurationBuilder_ItShouldReturnConfigurationBuilder()
  {
    var builder = new ConfigurationBuilder();

    var result = builder.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    result.Should().BeSameAs(builder);
  }

  [Fact]
  public void AddSecureConfig_ToConfigurationBuilder_ItShouldAddSecureConfigSource()
  {
    var builder = new ConfigurationBuilder();

    builder.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    builder.Sources.Should().ContainSingle(s => s is SecureConfigSource);
  }

  [Fact]
  public void AddSecureConfig_ToServiceCollection_ItShouldReturnServiceCollection()
  {
    var services = new ServiceCollection();

    var result = services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    result.Should().BeSameAs(services);
  }

  [Fact]
  public void AddSecureConfig_ToServiceCollection_ItShouldRegisterISecureConfig()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    var descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(ISecureConfig)).Subject;
    descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
  }

  [Fact]
  public void AddSecureConfig_ToServiceCollection_ItShouldRegisterAllDependencies()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    services.Should().Contain(d => d.ServiceType == typeof(ISecureStorageProvider));
    services.Should().Contain(d => d.ServiceType == typeof(IEncryptionKeyProvider));
    services.Should().Contain(d => d.ServiceType == typeof(ICryptoProvider));
    services.Should().Contain(d => d.ServiceType == typeof(ISecureConfig));
  }

  [Fact]
  public void AddSecureConfig_ToServiceCollection_ItShouldNotDuplicateExistingRegistrations()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));
    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    services.Count(d => d.ServiceType == typeof(ISecureConfig)).Should().Be(1);
    services.Count(d => d.ServiceType == typeof(ISecureStorageProvider)).Should().Be(1);
    services.Count(d => d.ServiceType == typeof(IEncryptionKeyProvider)).Should().Be(1);
    services.Count(d => d.ServiceType == typeof(ICryptoProvider)).Should().Be(1);
  }

  [Fact]
  public async Task AddSecureConfig_WithRealProviders_ItShouldResolveISecureConfig()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    var provider = services.BuildServiceProvider();
    var secureConfig = provider.GetRequiredService<ISecureConfig>();

    secureConfig.Should().NotBeNull();
    secureConfig.Should().BeOfType<SecureConfig>();
  }

  [Fact]
  public async Task AddSecureConfig_WithRealProviders_ItShouldSetAndGetValue()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    var provider = services.BuildServiceProvider();
    var secureConfig = provider.GetRequiredService<ISecureConfig>();

    var testValue = new IntegrationTestConfig { Name = "TestName", Value = 42 };
    await secureConfig.SetAsync("test-key", testValue);

    var result = await secureConfig.GetAsync<IntegrationTestConfig>("test-key");

    result.Should().NotBeNull();
    result!.Name.Should().Be("TestName");
    result.Value.Should().Be(42);
  }

  [Fact]
  public async Task AddSecureConfig_WithRealProviders_ItShouldDeleteValue()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    var provider = services.BuildServiceProvider();
    var secureConfig = provider.GetRequiredService<ISecureConfig>();

    await secureConfig.SetAsync("delete-key", new IntegrationTestConfig { Name = "ToDelete", Value = 1 });

    var deleted = await secureConfig.DeleteAsync("delete-key");

    deleted.Should().BeTrue();

    var result = await secureConfig.GetAsync<IntegrationTestConfig>("delete-key");
    result.Should().BeNull();
  }

  [Fact]
  public async Task AddSecureConfig_WithRealProviders_ItShouldReturnDefaultForMissingKey()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    var provider = services.BuildServiceProvider();
    var secureConfig = provider.GetRequiredService<ISecureConfig>();

    var result = await secureConfig.GetAsync<IntegrationTestConfig>("nonexistent-key");

    result.Should().BeNull();
  }

  [Fact]
  public async Task AddSecureConfig_WithRealProviders_ItShouldResolveAllServicesFromContainer()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    var provider = services.BuildServiceProvider();

    var storageProvider = provider.GetRequiredService<ISecureStorageProvider>();
    var cryptoProvider = provider.GetRequiredService<ICryptoProvider>();
    var secureConfig = provider.GetRequiredService<ISecureConfig>();

    storageProvider.Should().NotBeNull();
    cryptoProvider.Should().NotBeNull();
    secureConfig.Should().NotBeNull();

    storageProvider.Should().BeOfType<JsonFileStorageProvider>();
    cryptoProvider.Should().BeOfType<AesCryptoProvider>();
  }

  [Fact]
  public async Task AddSecureConfig_WithMachineIdKey_ItShouldWorkEndToEnd()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(builder =>
    {
      builder
        .WithMachineIdKey()
        .WithAesCryptoProvider()
        .UseJsonFileStorage(new JsonStorageOptions
        {
          DirectoryPath = _tempDir.Path,
          FileName = "secure-config.json",
        })
        .AddJsonAotContext(IntegrationTestJsonContext.Default);
    });

    var provider = services.BuildServiceProvider();
    var secureConfig = provider.GetRequiredService<ISecureConfig>();

    var testValue = new IntegrationTestConfig { Name = "MachineIdTest", Value = 99 };
    await secureConfig.SetAsync("machine-key", testValue);

    var result = await secureConfig.GetAsync<IntegrationTestConfig>("machine-key");

    result.Should().NotBeNull();
    result!.Name.Should().Be("MachineIdTest");
    result.Value.Should().Be(99);
  }

  [Fact]
  public async Task AddSecureConfig_WithTypedOverload_ItShouldSerializeAndDeserializeCorrectly()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(CreateDefaultConfig(_tempDir.Path));

    var provider = services.BuildServiceProvider();
    var secureConfig = provider.GetRequiredService<ISecureConfig>();

    var testValue = new IntegrationTestConfig { Name = "TypedTest", Value = 777 };
    await secureConfig.SetAsync("typed-key", testValue, IntegrationTestJsonContext.Default.IntegrationTestConfig);

    var result = await secureConfig.GetAsync("typed-key", IntegrationTestJsonContext.Default.IntegrationTestConfig);

    result.Should().NotBeNull();
    result!.Name.Should().Be("TypedTest");
    result.Value.Should().Be(777);
  }

  private static Action<ISecureConfigBuilder> CreateDefaultConfig(string tempDir) => builder =>
  {
    builder
      .WithBase64EncryptionKey(KeyGenerator.GetValidBase64Key())
      .WithAesCryptoProvider()
      .UseJsonFileStorage(new JsonStorageOptions { DirectoryPath = tempDir, FileName = "secure-config.json" })
      .AddJsonAotContext(IntegrationTestJsonContext.Default);
  };
}

internal sealed class IntegrationTestConfig
{
  public string Name { get; init; } = string.Empty;
  public int Value { get; init; }
}

[JsonSerializable(typeof(IntegrationTestConfig))]
internal partial class IntegrationTestJsonContext : JsonSerializerContext
{
}