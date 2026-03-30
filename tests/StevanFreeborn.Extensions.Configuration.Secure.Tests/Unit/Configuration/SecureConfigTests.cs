using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

using System.Text.Json;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Configuration;

public class SecureConfigTests
{
  private readonly Mock<ICryptoProvider> _mockCryptoProvider = new();
  private readonly Mock<ISecureStorageProvider> _mockStorageProvider = new();
  private readonly SecureConfig _sut;

  public SecureConfigTests()
  {
    _sut = new(_mockStorageProvider.Object, _mockCryptoProvider.Object);
  }

  [Fact]
  public void Constructor_WhenCalledWithNullStorageProvider_ItShouldThrowArgumentNullException()
  {
    var act = () => new SecureConfig(null!, _mockCryptoProvider.Object);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public void Constructor_WhenCalledWithNullCryptoProvider_ItShouldThrowArgumentNullException()
  {
    var act = () => new SecureConfig(_mockStorageProvider.Object, null!);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public async Task SetAsync_WhenCalledWithNullKey_ItShouldThrowArgumentNullException()
  {
    var act = async () => await _sut.SetAsync(null!, string.Empty);

    await act.Should().ThrowAsync<ArgumentNullException>();
  }


  [Fact]
  public async Task SetAsync_WhenCalledWithNullValue_ItShouldThrowArgumentNullException()
  {
    var act = async () => await _sut.SetAsync<string>("Key", null!);

    await act.Should().ThrowAsync<ArgumentNullException>();
  }

  [Fact]
  public async Task SetAsync_WhenCalled_ItShouldSerializeGivenValueAndEncryptIt()
  {
    var key = "Database";
    var config = new DummyConfig("localhost", 9999);
    var encryptedString = "encryptedString";
    var json = JsonSerializer.Serialize(config);

    _mockCryptoProvider.Setup(m => m.Encrypt(json)).Returns(encryptedString);

    await _sut.SetAsync(key, config);

    _mockStorageProvider.Verify(m => m.WriteAsync(key, encryptedString), Times.Once());
  }

  [Fact]
  public async Task GetAsync_WhenCalledWithNullKey_ItShouldThrowArgumentNullException()
  {
    var act = async () => await _sut.GetAsync<DummyConfig>(null!);

    await act.Should().ThrowAsync<ArgumentNullException>();
  }

  [Fact]
  public async Task GetAsync_WhenKeyExists_ItShouldReadDecryptAndDeserializeTheValue()
  {
    var key = "Database";
    var expectedConfig = new DummyConfig("localhost", 9999);
    var encryptedString = "encryptedString";
    var json = JsonSerializer.Serialize(expectedConfig);

    _mockStorageProvider.Setup(m => m.ReadAsync(key)).ReturnsAsync(encryptedString);
    _mockCryptoProvider.Setup(m => m.Decrypt(encryptedString)).Returns(json);

    var result = await _sut.GetAsync<DummyConfig>(key);

    result.Should().BeEquivalentTo(expectedConfig);
  }

  [Fact]
  public async Task GetAsync_WhenKeyDoesNotExist_ItShouldReturnDefaultValue()
  {
    var key = "Database";
    var expectedConfig = new DummyConfig("localhost", 9999);
    var json = JsonSerializer.Serialize(expectedConfig);

    _mockStorageProvider.Setup(m => m.ReadAsync(key)).ReturnsAsync(string.Empty);

    var result = await _sut.GetAsync<DummyConfig>(key);

    result.Should().BeNull();
  }

  [Fact]
  public async Task DeleteAsync_WhenCalled_ItShouldRemoveValue()
  {
    var key = "Database";

    _mockStorageProvider.Setup(m => m.DeleteAsync(key)).ReturnsAsync(true);

    var result = await _sut.DeleteAsync(key);

    result.Should().BeTrue();
    _mockStorageProvider.Verify(m => m.DeleteAsync(key), Times.Once());
  }

  private sealed record DummyConfig(string Host, int Port);
}