using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Configuration;

public class SecureConfigTests
{
  private readonly Mock<ICryptoProvider> _mockCryptoProvider = new();
  private readonly Mock<ISecureStorageProvider> _mockStorageProvider = new();
  private readonly JsonSerializerOptions _jsonSerializerOptions = new();
  private readonly SecureConfig _sut;

  public SecureConfigTests()
  {
    _sut = new(_mockStorageProvider.Object, _mockCryptoProvider.Object, _jsonSerializerOptions);
  }

  [Fact]
  public void Constructor_WhenCalledWithNullStorageProvider_ItShouldThrowArgumentNullException()
  {
    var act = () => new SecureConfig(null!, _mockCryptoProvider.Object, _jsonSerializerOptions);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WhenCalledWithNullCryptoProvider_ItShouldThrowArgumentNullException()
  {
    var act = () => new SecureConfig(_mockStorageProvider.Object, null!, _jsonSerializerOptions);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WhenCalledWithNullJsonOptions_ItShouldThrowArgumentNullException()
  {
    var act = () => new SecureConfig(_mockStorageProvider.Object, _mockCryptoProvider.Object, null!);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public async Task SetAsync_WhenCalledWithNullKey_ItShouldThrowArgumentNullException()
  {
    var act = async () => await _sut.SetAsync(null!, string.Empty, SecureConfigTestsJsonContext.Default.String);

    await act.Should().ThrowAsync<ArgumentException>();
  }


  [Fact]
  public async Task SetAsync_WhenCalledWithNullValue_ItShouldThrowArgumentNullException()
  {
    var act = async () => await _sut.SetAsync("Key", null!, SecureConfigTestsJsonContext.Default.String);

    await act.Should().ThrowAsync<ArgumentNullException>();
  }

  [Fact]
  public async Task SetAsync_WhenCalledWithoutJsonContextSet_ItShouldSerializeGivenValueAndEncryptIt()
  {
    var key = "Database";
    var config = new DummyConfig("localhost", 9999);
    var encryptedString = "encryptedString";
    var json = JsonSerializer.Serialize(config);

    _mockCryptoProvider.Setup(m => m.Encrypt(json)).Returns(encryptedString);

    var act = async () => await _sut.SetAsync(key, config);

    await act.Should().ThrowAsync<InvalidOperationException>();
  }

  [Fact]
  public async Task SetAsync_WhenCalledWithJsonContextSet_ItShouldSerializeGivenValueAndEncryptIt()
  {
    var key = "Database";
    var config = new DummyConfig("localhost", 9999);
    var encryptedString = "encryptedString";
    var json = JsonSerializer.Serialize(config);

    _jsonSerializerOptions.TypeInfoResolverChain.Insert(0, SecureConfigTestsJsonContext.Default);
    _mockCryptoProvider.Setup(m => m.Encrypt(json)).Returns(encryptedString);

    await _sut.SetAsync(key, config);

    _mockStorageProvider.Verify(m => m.WriteAsync(key, encryptedString, It.IsAny<CancellationToken>()), Times.Once());
  }

  [Fact]
  public async Task SetAsync_WhenCalledWithTypeInfo_ItShouldSerializeGivenValueAndEncryptIt()
  {
    var key = "Database";
    var config = new DummyConfig("localhost", 9999);
    var encryptedString = "encryptedString";
    var json = JsonSerializer.Serialize(config);

    _mockCryptoProvider.Setup(m => m.Encrypt(json)).Returns(encryptedString);

    await _sut.SetAsync(key, config, SecureConfigTestsJsonContext.Default.DummyConfig);

    _mockStorageProvider.Verify(m => m.WriteAsync(key, encryptedString, It.IsAny<CancellationToken>()), Times.Once());
  }

  [Fact]
  public async Task GetAsync_WhenCalledWithNullKey_ItShouldThrowArgumentNullException()
  {
    var act = async () => await _sut.GetAsync(null!, SecureConfigTestsJsonContext.Default.DummyConfig);

    await act.Should().ThrowAsync<ArgumentException>();
  }

  [Fact]
  public async Task GetAsync_WhenKeyExistsAndJsonContextNotSet_ItShouldReadDecryptAndDeserializeTheValue()
  {
    var key = "Database";
    var expectedConfig = new DummyConfig("localhost", 9999);
    var encryptedString = "encryptedString";
    var json = JsonSerializer.Serialize(expectedConfig);

    _mockStorageProvider.Setup(m => m.ReadAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(encryptedString);
    _mockCryptoProvider.Setup(m => m.Decrypt(encryptedString)).Returns(json);

    var act = async () => await _sut.GetAsync<DummyConfig>(key);

    await act.Should().ThrowAsync<InvalidOperationException>();
  }

  [Fact]
  public async Task GetAsync_WhenKeyExistsAndJsonContextIsSet_ItShouldReadDecryptAndDeserializeTheValue()
  {
    var key = "Database";
    var expectedConfig = new DummyConfig("localhost", 9999);
    var encryptedString = "encryptedString";
    var json = JsonSerializer.Serialize(expectedConfig);

    _jsonSerializerOptions.TypeInfoResolverChain.Insert(0, SecureConfigTestsJsonContext.Default);
    _mockStorageProvider.Setup(m => m.ReadAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(encryptedString);
    _mockCryptoProvider.Setup(m => m.Decrypt(encryptedString)).Returns(json);

    var result = await _sut.GetAsync(key, SecureConfigTestsJsonContext.Default.DummyConfig);

    result.Should().BeEquivalentTo(expectedConfig);
  }

  [Fact]
  public async Task GetAsync_WhenKeyExists_ItShouldReadDecryptAndDeserializeTheValue()
  {
    var key = "Database";
    var expectedConfig = new DummyConfig("localhost", 9999);
    var encryptedString = "encryptedString";
    var json = JsonSerializer.Serialize(expectedConfig);

    _mockStorageProvider.Setup(m => m.ReadAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(encryptedString);
    _mockCryptoProvider.Setup(m => m.Decrypt(encryptedString)).Returns(json);

    var result = await _sut.GetAsync(key, SecureConfigTestsJsonContext.Default.DummyConfig);

    result.Should().BeEquivalentTo(expectedConfig);
  }

  [Fact]
  public async Task GetAsync_WhenKeyDoesNotExist_ItShouldReturnDefaultValue()
  {
    var key = "Database";
    var expectedConfig = new DummyConfig("localhost", 9999);
    var json = JsonSerializer.Serialize(expectedConfig);

    _mockStorageProvider.Setup(m => m.ReadAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(string.Empty);

    var result = await _sut.GetAsync(key, SecureConfigTestsJsonContext.Default.DummyConfig);

    result.Should().BeNull();
  }

  [Fact]
  public async Task DeleteAsync_WhenCalled_ItShouldRemoveValue()
  {
    var key = "Database";

    _mockStorageProvider.Setup(m => m.DeleteAsync(key, It.IsAny<CancellationToken>())).ReturnsAsync(true);

    var result = await _sut.DeleteAsync(key);

    result.Should().BeTrue();
    _mockStorageProvider.Verify(m => m.DeleteAsync(key, It.IsAny<CancellationToken>()), Times.Once());
  }
}

internal sealed record DummyConfig(string Host, int Port);

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(DummyConfig))]
internal partial class SecureConfigTestsJsonContext : JsonSerializerContext
{
}