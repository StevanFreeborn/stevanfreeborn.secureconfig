using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit;

public class SecureConfigExtensionsTests
{
  private readonly Mock<ISecureStorageProvider> _mockStorageProvider = new();
  private readonly Mock<IEncryptionKeyProvider> _mockKeyProvider = new();
  private readonly Mock<ICryptoProvider> _mockCryptoProvider = new();
  private readonly Mock<ILoggerFactory> _mockLoggerFactory = new();

  [Fact]
  public void AddSecureConfig_WithNullBuilder_ItShouldThrowArgumentNullException()
  {
    IConfigurationBuilder builder = null!;

    var act = () => builder.AddSecureConfig(config => { });

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("builder");
  }

  [Fact]
  public void AddSecureConfig_WithNullConfigure_ItShouldThrowArgumentNullException()
  {
    var builder = new ConfigurationBuilder();

    var act = () => builder.AddSecureConfig(null!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("configure");
  }

  [Fact]
  public void AddSecureConfig_WhenStorageProviderNotConfigured_ItShouldThrowInvalidOperationException()
  {
    var builder = new ConfigurationBuilder();

    var act = () => builder.AddSecureConfig(config =>
    {
      config.WithBase64EncryptionKey(GetValidBase64Key());
      config.WithAesCryptoProvider();
    });

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("A storage provider must be configured.");
  }

  [Fact]
  public void AddSecureConfig_WhenKeyProviderNotConfigured_ItShouldThrowInvalidOperationException()
  {
    var builder = new ConfigurationBuilder();

    var act = () => builder.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithAesCryptoProvider();
    });

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("A key provider must be configured");
  }

  [Fact]
  public void AddSecureConfig_WhenCryptoProviderNotConfigured_ItShouldThrowInvalidOperationException()
  {
    var builder = new ConfigurationBuilder();

    var act = () => builder.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithBase64EncryptionKey(GetValidBase64Key());
    });

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("A crypto provider must be configured.");
  }

  [Fact]
  public void AddSecureConfig_WhenProperlyConfigured_ItShouldAddSecureConfigSource()
  {
    var builder = new ConfigurationBuilder();

    var result = builder.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithBase64EncryptionKey(GetValidBase64Key());
      config.WithAesCryptoProvider();
    });

    result.Should().BeSameAs(builder);
    builder.Sources.Should().ContainSingle(s => s is SecureConfigSource);
  }

  [Fact]
  public void AddSecureConfig_WhenProperlyConfigured_ItShouldBuildConfigurationProvider()
  {
    var encryptedValue = "encrypted_value";
    var decryptedJson = @"{ ""Setting"": ""Value"", ""Number"": 42 }";

    _mockStorageProvider
      .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new Dictionary<string, string> { { "MySection", encryptedValue } });

    _mockCryptoProvider
      .Setup(c => c.Decrypt(encryptedValue))
      .Returns(decryptedJson);

    var configuration = new ConfigurationBuilder()
      .AddSecureConfig(config =>
      {
        config.UseCustomStorage(_mockStorageProvider.Object);
        config.WithCustomKeyProvider(_mockKeyProvider.Object);
        config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
      })
      .Build();

    configuration.GetSection("MySection")["Setting"].Should().Be("Value");
    configuration.GetSection("MySection")["Number"].Should().Be("42");
  }

  [Fact]
  public void AddSecureConfig_WithJsonFileStorage_ItShouldWorkEndToEnd()
  {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var filePath = Path.Combine(tempDir, "test_config.json");

    try
    {
      Directory.CreateDirectory(tempDir);

      var keyBytes = new byte[32];
      RandomNumberGenerator.Fill(keyBytes);
      var keyProvider = new StaticKeyProvider(Convert.ToBase64String(keyBytes));
      var cryptoProvider = new AesCryptoProvider(keyProvider);

      var originalData = @"{ ""AppName"": ""TestApp"", ""Version"": ""1.0.0"" }";
      var encryptedData = cryptoProvider.Encrypt(originalData);

      File.WriteAllText(filePath, $"{{\"Settings\":\"{encryptedData}\"}}");

      var configuration = new ConfigurationBuilder()
        .AddSecureConfig(config =>
        {
          config.UseJsonFileStorage(options =>
          {
            options.DirectoryPath = tempDir;
            options.FileName = "test_config.json";
          });
          config.WithBase64EncryptionKey(Convert.ToBase64String(keyBytes));
          config.WithAesCryptoProvider();
        })
        .Build();

      configuration["Settings:AppName"].Should().Be("TestApp");
      configuration["Settings:Version"].Should().Be("1.0.0");
    }
    finally
    {
      if (File.Exists(filePath)) File.Delete(filePath);
      if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void AddSecureConfig_WithLoggerFactory_ItShouldUseProvidedLoggerFactory()
  {
    _mockStorageProvider
      .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new Dictionary<string, string>());

    var mockLogger = new Mock<ILogger<SecureConfigProvider>>();

    _mockLoggerFactory
      .Setup(f => f.CreateLogger(typeof(SecureConfigProvider).FullName!))
      .Returns(mockLogger.Object);

    var builder = new ConfigurationBuilder();

    builder.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithBase64EncryptionKey(GetValidBase64Key());
      config.WithAesCryptoProvider();
      config.WithLoggerFactory(_mockLoggerFactory.Object);
    })
    .Build();

    _mockLoggerFactory.Verify(
      f => f.CreateLogger(typeof(SecureConfigProvider).FullName!),
      Times.Once()
    );
  }

  [Fact]
  public void AddSecureConfig_WithMultipleCalls_ItShouldAddMultipleSources()
  {
    var builder = new ConfigurationBuilder();

    builder.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithBase64EncryptionKey(GetValidBase64Key());
      config.WithAesCryptoProvider();
    });

    builder.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithBase64EncryptionKey(GetValidBase64Key());
      config.WithAesCryptoProvider();
    });

    builder.Sources.Should().HaveCount(2);
    builder.Sources.Should().AllBeOfType<SecureConfigSource>();
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_WithNullServices_ItShouldThrowArgumentNullException()
  {
    IServiceCollection services = null!;

    var act = () => services.AddSecureConfig(config => { });

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("services");
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_WithNullConfigure_ItShouldThrowArgumentNullException()
  {
    var services = new ServiceCollection();

    var act = () => services.AddSecureConfig(null!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("configure");
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_WhenStorageProviderNotConfigured_ItShouldThrowInvalidOperationException()
  {
    var services = new ServiceCollection();

    var act = () => services.AddSecureConfig(config =>
    {
      config.WithBase64EncryptionKey(GetValidBase64Key());
      config.WithAesCryptoProvider();
    });

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("A storage provider must be configured.");
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_WhenKeyProviderNotConfigured_ItShouldThrowInvalidOperationException()
  {
    var services = new ServiceCollection();

    var act = () => services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithAesCryptoProvider();
    });

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("A key provider must be configured");
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_WhenCryptoProviderNotConfigured_ItShouldThrowInvalidOperationException()
  {
    var services = new ServiceCollection();

    var act = () => services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithBase64EncryptionKey(GetValidBase64Key());
    });

    act.Should().Throw<InvalidOperationException>()
      .WithMessage("A crypto provider must be configured.");
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_WhenProperlyConfigured_ItShouldRegisterServices()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    services.Should().Contain(s => s.ServiceType == typeof(ISecureStorageProvider));
    services.Should().Contain(s => s.ServiceType == typeof(IEncryptionKeyProvider));
    services.Should().Contain(s => s.ServiceType == typeof(ICryptoProvider));
    services.Should().Contain(s => s.ServiceType == typeof(ISecureConfig));
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldReturnServiceCollection()
  {
    var services = new ServiceCollection();

    var result = services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    result.Should().BeSameAs(services);
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldRegisterStorageProviderAsSingleton()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var storageDescriptor = services.First(s => s.ServiceType == typeof(ISecureStorageProvider));
    storageDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldRegisterKeyProviderAsSingleton()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var keyDescriptor = services.First(s => s.ServiceType == typeof(IEncryptionKeyProvider));
    keyDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldRegisterCryptoProviderAsSingleton()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var cryptoDescriptor = services.First(s => s.ServiceType == typeof(ICryptoProvider));
    cryptoDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldRegisterSecureConfigAsSingleton()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var secureConfigDescriptor = services.First(s => s.ServiceType == typeof(ISecureConfig));
    secureConfigDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldResolveSecureConfigFromDI()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var serviceProvider = services.BuildServiceProvider();
    var secureConfig = serviceProvider.GetService<ISecureConfig>();

    secureConfig.Should().NotBeNull();
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldResolveSameSecureConfigInstance()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var serviceProvider = services.BuildServiceProvider();
    var instance1 = serviceProvider.GetRequiredService<ISecureConfig>();
    var instance2 = serviceProvider.GetRequiredService<ISecureConfig>();

    instance1.Should().BeSameAs(instance2);
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldResolveSameStorageProviderInstance()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var serviceProvider = services.BuildServiceProvider();
    var instance1 = serviceProvider.GetRequiredService<ISecureStorageProvider>();
    var instance2 = serviceProvider.GetRequiredService<ISecureStorageProvider>();

    instance1.Should().BeSameAs(_mockStorageProvider.Object);
    instance2.Should().BeSameAs(_mockStorageProvider.Object);
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldResolveSameKeyProviderInstance()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var serviceProvider = services.BuildServiceProvider();
    var instance1 = serviceProvider.GetRequiredService<IEncryptionKeyProvider>();
    var instance2 = serviceProvider.GetRequiredService<IEncryptionKeyProvider>();

    instance1.Should().BeSameAs(_mockKeyProvider.Object);
    instance2.Should().BeSameAs(_mockKeyProvider.Object);
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldResolveSameCryptoProviderInstance()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var serviceProvider = services.BuildServiceProvider();
    var instance1 = serviceProvider.GetRequiredService<ICryptoProvider>();
    var instance2 = serviceProvider.GetRequiredService<ICryptoProvider>();

    instance1.Should().BeSameAs(_mockCryptoProvider.Object);
    instance2.Should().BeSameAs(_mockCryptoProvider.Object);
  }

  [Fact]
  public async Task AddSecureConfig_ServiceCollection_WithRealAesCryptoProvider_ItShouldWorkEndToEnd()
  {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    var filePath = Path.Combine(tempDir, "test_config.json");

    try
    {
      Directory.CreateDirectory(tempDir);

      var keyBytes = new byte[32];
      RandomNumberGenerator.Fill(keyBytes);
      var base64Key = Convert.ToBase64String(keyBytes);

      var services = new ServiceCollection();

      services.AddSecureConfig(config =>
      {
        config.AddJsonAotContext(SecureConfigExtensionsTestsJsonContext.Default);
        config.UseJsonFileStorage(options =>
        {
          options.DirectoryPath = tempDir;
          options.FileName = "test_config.json";
        });
        config.WithBase64EncryptionKey(base64Key);
        config.WithAesCryptoProvider();
      });

      var serviceProvider = services.BuildServiceProvider();
      var secureConfig = serviceProvider.GetRequiredService<ISecureConfig>();
      var storageProvider = serviceProvider.GetRequiredService<ISecureStorageProvider>();

      var testObject = new TestConfig { Name = "TestName", Value = 123 };
      await secureConfig.SetAsync("TestKey", testObject);

      var retrievedObject = await secureConfig.GetAsync<TestConfig>("TestKey");

      retrievedObject.Should().NotBeNull();
      retrievedObject!.Name.Should().Be("TestName");
      retrievedObject.Value.Should().Be(123);

      File.Exists(filePath).Should().BeTrue();
    }
    finally
    {
      if (File.Exists(filePath)) File.Delete(filePath);
      if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public async Task AddSecureConfig_ServiceCollection_WithMachineIdKey_ItShouldWorkEndToEnd()
  {
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    try
    {
      Directory.CreateDirectory(tempDir);

      var services = new ServiceCollection();

      services.AddSecureConfig(config =>
      {
        config.AddJsonAotContext(SecureConfigExtensionsTestsJsonContext.Default);
        config.UseJsonFileStorage(options =>
        {
          options.DirectoryPath = tempDir;
          options.FileName = "test_config.json";
        });
        config.WithMachineIdKey();
        config.WithAesCryptoProvider();
      });

      var serviceProvider = services.BuildServiceProvider();
      var secureConfig = serviceProvider.GetRequiredService<ISecureConfig>();

      var testObject = new TestConfig { Name = "MachineIdTest", Value = 456 };
      await secureConfig.SetAsync("MachineTest", testObject);

      var retrievedObject = await secureConfig.GetAsync<TestConfig>("MachineTest");

      retrievedObject.Should().NotBeNull();
      retrievedObject!.Name.Should().Be("MachineIdTest");
      retrievedObject.Value.Should().Be(456);
    }
    finally
    {
      if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void AddSecureConfig_ServiceCollection_ItShouldRegisterJsonSerializerOptions()
  {
    var services = new ServiceCollection();

    services.AddSecureConfig(config =>
    {
      config.UseCustomStorage(_mockStorageProvider.Object);
      config.WithCustomKeyProvider(_mockKeyProvider.Object);
      config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
    });

    var serviceProvider = services.BuildServiceProvider();
    var keyedService = serviceProvider.GetKeyedService<JsonSerializerOptions>("SecureConfigJsonSerializerOptions");

    keyedService.Should().NotBeNull();
    keyedService!.PropertyNameCaseInsensitive.Should().BeTrue();
  }

  [Fact]
  public void AddSecureConfig_CombinedWithOtherProviders_ItShouldMergeConfiguration()
  {
    var encryptedValue = "encrypted_value";
    var decryptedJson = @"{ ""SecureSetting"": ""SecureValue"" }";

    _mockStorageProvider
      .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new Dictionary<string, string> { { "SecureSection", encryptedValue } });

    _mockCryptoProvider
      .Setup(c => c.Decrypt(encryptedValue))
      .Returns(decryptedJson);

    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["RegularSetting"] = "RegularValue",
        ["AnotherSetting"] = "AnotherValue"
      })
      .AddSecureConfig(config =>
      {
        config.UseCustomStorage(_mockStorageProvider.Object);
        config.WithCustomKeyProvider(_mockKeyProvider.Object);
        config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
      })
      .Build();

    configuration["RegularSetting"].Should().Be("RegularValue");
    configuration["AnotherSetting"].Should().Be("AnotherValue");
    configuration["SecureSection:SecureSetting"].Should().Be("SecureValue");
  }

  [Fact]
  public void AddSecureConfig_WithNestedConfiguration_ItShouldFlattenCorrectly()
  {
    var encryptedValue = "encrypted_nested";
    var decryptedJson = @"{
      ""Level1"": {
        ""Level2"": {
          ""Setting"": ""NestedValue""
        }
      }
    }";

    _mockStorageProvider
      .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new Dictionary<string, string> { { "Nested", encryptedValue } });

    _mockCryptoProvider
      .Setup(c => c.Decrypt(encryptedValue))
      .Returns(decryptedJson);

    var configuration = new ConfigurationBuilder()
      .AddSecureConfig(config =>
      {
        config.UseCustomStorage(_mockStorageProvider.Object);
        config.WithCustomKeyProvider(_mockKeyProvider.Object);
        config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
      })
      .Build();

    configuration["Nested:Level1:Level2:Setting"].Should().Be("NestedValue");
  }

  [Fact]
  public void AddSecureConfig_WithArrays_ItShouldIndexCorrectly()
  {
    var encryptedValue = "encrypted_array";
    var decryptedJson = @"{ ""Items"": [""First"", ""Second"", ""Third""] }";

    _mockStorageProvider
      .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new Dictionary<string, string> { { "ArraySection", encryptedValue } });

    _mockCryptoProvider
      .Setup(c => c.Decrypt(encryptedValue))
      .Returns(decryptedJson);

    var configuration = new ConfigurationBuilder()
      .AddSecureConfig(config =>
      {
        config.UseCustomStorage(_mockStorageProvider.Object);
        config.WithCustomKeyProvider(_mockKeyProvider.Object);
        config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
      })
      .Build();

    configuration["ArraySection:Items:0"].Should().Be("First");
    configuration["ArraySection:Items:1"].Should().Be("Second");
    configuration["ArraySection:Items:2"].Should().Be("Third");
  }

  [Fact]
  public void AddSecureConfig_WithMultipleSections_ItShouldLoadAllSections()
  {
    var encrypted1 = "encrypted1";
    var encrypted2 = "encrypted2";
    var decrypted1 = @"{ ""Setting1"": ""Value1"" }";
    var decrypted2 = @"{ ""Setting2"": ""Value2"" }";

    _mockStorageProvider
      .Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>()))
      .ReturnsAsync(new Dictionary<string, string>
      {
        { "Section1", encrypted1 },
        { "Section2", encrypted2 }
      });

    _mockCryptoProvider
      .Setup(c => c.Decrypt("encrypted1"))
      .Returns(decrypted1);

    _mockCryptoProvider
      .Setup(c => c.Decrypt("encrypted2"))
      .Returns(decrypted2);

    var configuration = new ConfigurationBuilder()
      .AddSecureConfig(config =>
      {
        config.UseCustomStorage(_mockStorageProvider.Object);
        config.WithCustomKeyProvider(_mockKeyProvider.Object);
        config.WithCustomCryptoProvider((kp) => _mockCryptoProvider.Object);
      })
      .Build();

    configuration["Section1:Setting1"].Should().Be("Value1");
    configuration["Section2:Setting2"].Should().Be("Value2");
  }

  private static string GetValidBase64Key()
  {
    var keyBytes = new byte[32];
    RandomNumberGenerator.Fill(keyBytes);
    return Convert.ToBase64String(keyBytes);
  }
}

internal sealed class TestConfig
{
  public string Name { get; set; } = string.Empty;
  public int Value { get; set; }
}

[JsonSerializable(typeof(TestConfig))]
internal partial class SecureConfigExtensionsTestsJsonContext : JsonSerializerContext
{
}