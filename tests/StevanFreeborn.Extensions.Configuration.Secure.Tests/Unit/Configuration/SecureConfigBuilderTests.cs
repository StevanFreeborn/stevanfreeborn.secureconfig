using System.Security.Cryptography;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Configuration;

public class SecureConfigBuilderTests
{
  private readonly SecureConfigBuilder _sut = new();

  [Fact]
  public void Constructor_WhenCalled_ItShouldInitializeWithDefaultValues()
  {
    _sut.StorageProvider.Should().BeNull();
    _sut.CryptoProviderFactory.Should().BeNull();
    _sut.KeyProvider.Should().BeNull();
    _sut.LoggerFactory.Should().Be(NullLoggerFactory.Instance);
    _sut.SerializerOptions.PropertyNameCaseInsensitive.Should().BeTrue();
  }

  [Fact]
  public void UseJsonFileStorage_WithOptions_ItShouldSetStorageProvider()
  {
    var options = new JsonStorageOptions
    {
      FileName = "test_config.json",
      DirectoryPath = "/tmp/config"
    };

    var result = _sut.UseJsonFileStorage(options);

    result.Should().BeSameAs(_sut);
    _sut.StorageProvider.Should().NotBeNull();
    _sut.StorageProvider.Should().BeOfType<JsonFileStorageProvider>();
  }

  [Fact]
  public void UseJsonFileStorage_WithOptions_ItShouldUseProvidedOptions()
  {
    var options = new JsonStorageOptions
    {
      FileName = "custom.json",
      DirectoryPath = "/custom/path"
    };

    _sut.UseJsonFileStorage(options);

    _sut.StorageProvider.Should().BeOfType<JsonFileStorageProvider>();
  }

  [Fact]
  public void UseJsonFileStorage_WithConfigureAction_ItShouldSetStorageProvider()
  {
    var result = _sut.UseJsonFileStorage(opt => opt.FileName = "test.json");

    result.Should().BeSameAs(_sut);
    _sut.StorageProvider.Should().NotBeNull();
    _sut.StorageProvider.Should().BeOfType<JsonFileStorageProvider>();
  }

  [Fact]
  public void UseJsonFileStorage_WithConfigureAction_ItShouldApplyConfiguration()
  {
    _sut.UseJsonFileStorage(opt =>
    {
      opt.FileName = "configured.json";
      opt.DirectoryPath = "/configured/path";
    });

    _sut.StorageProvider.Should().BeOfType<JsonFileStorageProvider>();
  }

  [Fact]
  public void UseJsonFileStorage_WithNullConfigureAction_ItShouldThrowArgumentNullException()
  {
    var act = () => _sut.UseJsonFileStorage((Action<JsonStorageOptions>)null!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("configure");
  }

  [Fact]
  public void AddJsonAotContext_WithContext_ItShouldAddToResolverChain()
  {
    var mockContext = new Mock<IJsonTypeInfoResolver>();

    var result = _sut.AddJsonAotContext(mockContext.Object);

    result.Should().BeSameAs(_sut);
    _sut.SerializerOptions.TypeInfoResolverChain.Should().Contain(mockContext.Object);
  }

  [Fact]
  public void AddJsonAotContext_WithNullContext_ItShouldThrowArgumentNullException()
  {
    var act = () => _sut.AddJsonAotContext(null!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("context");
  }

  [Fact]
  public void AddJsonAotContext_WhenCalledMultipleTimes_ItShouldAddAllToChain()
  {
    var mockContext1 = new Mock<IJsonTypeInfoResolver>();
    var mockContext2 = new Mock<IJsonTypeInfoResolver>();

    _sut.AddJsonAotContext(mockContext1.Object);
    _sut.AddJsonAotContext(mockContext2.Object);

    _sut.SerializerOptions.TypeInfoResolverChain.Should().Contain(mockContext1.Object);
    _sut.SerializerOptions.TypeInfoResolverChain.Should().Contain(mockContext2.Object);
  }

  [Fact]
  public void UseCustomStorage_WithProvider_ItShouldSetStorageProvider()
  {
    var mockProvider = new Mock<ISecureStorageProvider>();

    var result = _sut.UseCustomStorage(mockProvider.Object);

    result.Should().BeSameAs(_sut);
    _sut.StorageProvider.Should().BeSameAs(mockProvider.Object);
  }

  [Fact]
  public void UseCustomStorage_WithNullProvider_ItShouldThrowArgumentNullException()
  {
    var act = () => _sut.UseCustomStorage(null!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("provider");
  }

  [Fact]
  public void WithBase64EncryptionKey_WithValidKey_ItShouldSetKeyProvider()
  {
    var validKey = Convert.ToBase64String(new byte[32]);

    var result = _sut.WithBase64EncryptionKey(validKey);

    result.Should().BeSameAs(_sut);
    _sut.KeyProvider.Should().NotBeNull();
    _sut.KeyProvider.Should().BeOfType<StaticKeyProvider>();
  }

  [Fact]
  public void WithBase64EncryptionKey_WithValidKey_ItShouldReturnCorrectKey()
  {
    var keyBytes = new byte[32];
    RandomNumberGenerator.Fill(keyBytes);

    var validKey = Convert.ToBase64String(keyBytes);

    _sut.WithBase64EncryptionKey(validKey);

    _sut.KeyProvider!.GetKey().Should().Equal(keyBytes);
  }

  [Fact]
  public void WithMachineIdKey_WhenCalled_ItShouldSetKeyProvider()
  {
    var result = _sut.WithMachineIdKey();

    result.Should().BeSameAs(_sut);
    _sut.KeyProvider.Should().NotBeNull();
    _sut.KeyProvider.Should().BeOfType<MachineIdKeyProvider>();
  }

  [Fact]
  public void WithMachineIdKey_WhenCalled_ItShouldUseLoggerFactory()
  {
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    var mockLogger = new Mock<ILogger>();

    mockLoggerFactory.Setup(f => f.CreateLogger(typeof(MachineIdKeyGenerator).FullName!))
      .Returns(mockLogger.Object);

    _sut.WithLoggerFactory(mockLoggerFactory.Object);

    _sut.WithMachineIdKey();

    mockLoggerFactory.Verify(f => f.CreateLogger(typeof(MachineIdKeyGenerator).FullName!), Times.Once());
  }

  [Fact]
  public void WithCustomKeyProvider_WithProvider_ItShouldSetKeyProvider()
  {
    var mockProvider = new Mock<IEncryptionKeyProvider>();

    var result = _sut.WithCustomKeyProvider(mockProvider.Object);

    result.Should().BeSameAs(_sut);
    _sut.KeyProvider.Should().BeSameAs(mockProvider.Object);
  }

  [Fact]
  public void WithCustomKeyProvider_WithNullProvider_ItShouldThrowArgumentNullException()
  {
    var act = () => _sut.WithCustomKeyProvider(null!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("provider");
  }

  [Fact]
  public void WithLoggerFactory_WithFactory_ItShouldSetLoggerFactory()
  {
    var mockFactory = new Mock<ILoggerFactory>();

    var result = _sut.WithLoggerFactory(mockFactory.Object);

    result.Should().BeSameAs(_sut);
    _sut.LoggerFactory.Should().BeSameAs(mockFactory.Object);
  }

  [Fact]
  public void WithLoggerFactory_WithNullFactory_ItShouldSetNullLoggerFactory()
  {
    var mockFactory = new Mock<ILoggerFactory>();
    _sut.WithLoggerFactory(mockFactory.Object);

    _sut.WithLoggerFactory(null!);

    _sut.LoggerFactory.Should().Be(NullLoggerFactory.Instance);
  }

  [Fact]
  public void WithAesCryptoProvider_WhenCalled_ItShouldSetCryptoProviderFactory()
  {
    var mockKeyProvider = new Mock<IEncryptionKeyProvider>();

    var result = _sut.WithAesCryptoProvider();

    result.Should().BeSameAs(_sut);
    _sut.CryptoProviderFactory.Should().NotBeNull();

    var cryptoProvider = _sut.CryptoProviderFactory!(mockKeyProvider.Object);
    cryptoProvider.Should().BeOfType<AesCryptoProvider>();
  }

  [Fact]
  public void WithCustomCryptoProvider_WithFactory_ItShouldSetCryptoProviderFactory()
  {
    var mockCryptoProvider = new Mock<ICryptoProvider>();
    var mockKeyProvider = new Mock<IEncryptionKeyProvider>();
    Func<IEncryptionKeyProvider, ICryptoProvider> factory = (kp) => mockCryptoProvider.Object;

    var result = _sut.WithCustomCryptoProvider(factory);

    result.Should().BeSameAs(_sut);
    _sut.CryptoProviderFactory.Should().NotBeNull();

    var cryptoProvider = _sut.CryptoProviderFactory!(mockKeyProvider.Object);
    cryptoProvider.Should().BeSameAs(mockCryptoProvider.Object);
  }

  [Fact]
  public void WithCustomCryptoProvider_WithNullFactory_ItShouldThrowArgumentNullException()
  {
    var act = () => _sut.WithCustomCryptoProvider(null!);

    act.Should().Throw<ArgumentNullException>()
      .WithParameterName("cryptoProviderFactory");
  }

  [Fact]
  public void MethodChaining_WhenAllMethodsCalled_ItShouldConfigureAllProperties()
  {
    var mockStorageProvider = new Mock<ISecureStorageProvider>();
    var mockKeyProvider = new Mock<IEncryptionKeyProvider>();
    var mockLoggerFactory = new Mock<ILoggerFactory>();
    var mockCryptoProvider = new Mock<ICryptoProvider>();

    _sut.UseCustomStorage(mockStorageProvider.Object)
      .WithCustomKeyProvider(mockKeyProvider.Object)
      .WithLoggerFactory(mockLoggerFactory.Object)
      .WithCustomCryptoProvider((kp) => mockCryptoProvider.Object);

    _sut.StorageProvider.Should().BeSameAs(mockStorageProvider.Object);
    _sut.KeyProvider.Should().BeSameAs(mockKeyProvider.Object);
    _sut.LoggerFactory.Should().BeSameAs(mockLoggerFactory.Object);
    _sut.CryptoProviderFactory.Should().NotBeNull();

    var provider = _sut.CryptoProviderFactory!(mockKeyProvider.Object);
    provider.Should().BeSameAs(mockCryptoProvider.Object);
  }

  [Fact]
  public void MethodChaining_WhenOverridingStorage_ItShouldUseLastSetProvider()
  {
    var mockProvider1 = new Mock<ISecureStorageProvider>();
    var mockProvider2 = new Mock<ISecureStorageProvider>();

    _sut.UseCustomStorage(mockProvider1.Object)
      .UseCustomStorage(mockProvider2.Object);

    _sut.StorageProvider.Should().BeSameAs(mockProvider2.Object);
  }

  [Fact]
  public void MethodChaining_WhenOverridingKeyProvider_ItShouldUseLastSetProvider()
  {
    var mockProvider1 = new Mock<IEncryptionKeyProvider>();
    var mockProvider2 = new Mock<IEncryptionKeyProvider>();

    _sut.WithCustomKeyProvider(mockProvider1.Object)
      .WithCustomKeyProvider(mockProvider2.Object);

    _sut.KeyProvider.Should().BeSameAs(mockProvider2.Object);
  }

  [Fact]
  public void MethodChaining_WhenOverridingLoggerFactory_ItShouldUseLastSetFactory()
  {
    var mockFactory1 = new Mock<ILoggerFactory>();
    var mockFactory2 = new Mock<ILoggerFactory>();

    _sut.WithLoggerFactory(mockFactory1.Object)
      .WithLoggerFactory(mockFactory2.Object);

    _sut.LoggerFactory.Should().BeSameAs(mockFactory2.Object);
  }
}