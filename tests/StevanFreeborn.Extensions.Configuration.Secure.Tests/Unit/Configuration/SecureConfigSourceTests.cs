using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Configuration;

public class SecureConfigSourceTests
{
  private readonly Mock<ISecureStorageProvider> _mockStorage = new();
  private readonly Mock<ICryptoProvider> _mockCrypto = new();
  private readonly Mock<ILoggerFactory> _mockLoggerFactory = new();

  [Fact]
  public void Constructor_WhenStorageProviderIsNull_ItShouldThrowArgumentNullException()
  {
    var act = () => new SecureConfigSource(
      storageProvider: null!,
      cryptoProvider: _mockCrypto.Object,
      loggerFactory: _mockLoggerFactory.Object
    );

    act.Should().Throw<ArgumentNullException>().WithParameterName("storageProvider");
  }

  [Fact]
  public void Constructor_WhenCryptoProviderIsNull_ItShouldThrowArgumentNullException()
  {
    var act = () => new SecureConfigSource(
      storageProvider: _mockStorage.Object,
      cryptoProvider: null!,
      loggerFactory: _mockLoggerFactory.Object
    );

    act.Should().Throw<ArgumentNullException>().WithParameterName("cryptoProvider");
  }

  [Fact]
  public void Constructor_WhenLoggerFactoryIsNull_ItShouldThrowArgumentNullException()
  {
    var act = () => new SecureConfigSource(
      storageProvider: _mockStorage.Object,
      cryptoProvider: _mockCrypto.Object,
      loggerFactory: null!
    );

    act.Should().Throw<ArgumentNullException>().WithParameterName("loggerFactory");
  }

  [Fact]
  public void Constructor_WhenAllDependenciesAreProvided_ItShouldNotThrow()
  {
    var act = () => new SecureConfigSource(
      storageProvider: _mockStorage.Object,
      cryptoProvider: _mockCrypto.Object,
      loggerFactory: _mockLoggerFactory.Object
    );

    act.Should().NotThrow();
  }

  [Fact]
  public void Build_WhenCalled_ItShouldReturnSecureConfigProvider()
  {
    var sut = new SecureConfigSource(
      storageProvider: _mockStorage.Object,
      cryptoProvider: _mockCrypto.Object,
      loggerFactory: _mockLoggerFactory.Object
    );

    var mockBuilder = new Mock<IConfigurationBuilder>();

    var result = sut.Build(mockBuilder.Object);

    result.Should().NotBeNull();
    result.Should().BeOfType<SecureConfigProvider>();
  }

  [Fact]
  public void Build_WhenCalled_ItShouldCreateLoggerFromFactory()
  {
    var mockLogger = new Mock<ILogger<SecureConfigProvider>>();

    _mockLoggerFactory
      .Setup(f => f.CreateLogger(typeof(SecureConfigProvider).FullName!))
      .Returns(mockLogger.Object);

    var sut = new SecureConfigSource(
      storageProvider: _mockStorage.Object,
      cryptoProvider: _mockCrypto.Object,
      loggerFactory: _mockLoggerFactory.Object
    );

    var mockBuilder = new Mock<IConfigurationBuilder>();
    sut.Build(mockBuilder.Object);

    _mockLoggerFactory.Verify(
      f => f.CreateLogger(typeof(SecureConfigProvider).FullName!),
      Times.Once()
    );
  }

  [Fact]
  public void Build_WhenCalledMultipleTimes_ItShouldReturnNewProviderInstance()
  {
    var sut = new SecureConfigSource(
      storageProvider: _mockStorage.Object,
      cryptoProvider: _mockCrypto.Object,
      loggerFactory: _mockLoggerFactory.Object
    );

    var mockBuilder = new Mock<IConfigurationBuilder>();

    var result1 = sut.Build(mockBuilder.Object);
    var result2 = sut.Build(mockBuilder.Object);

    result1.Should().NotBeSameAs(result2);
  }

  [Fact]
  public void Build_WhenCalledWithNullBuilder_ItShouldStillReturnProvider()
  {
    var sut = new SecureConfigSource(
      storageProvider: _mockStorage.Object,
      cryptoProvider: _mockCrypto.Object,
      loggerFactory: _mockLoggerFactory.Object
    );

    var result = sut.Build(builder: null!);

    result.Should().NotBeNull();
    result.Should().BeOfType<SecureConfigProvider>();
  }
}