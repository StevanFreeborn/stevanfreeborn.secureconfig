using Moq;


using StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Tests.Unit.Configuration;

public class SecureConfigBuilderExtensionsTests
{
  private readonly Mock<ISecureConfigBuilder> _mockBuilder = new();

  [Fact]
  public void ApplyProfile_WhenCalledWithValidOptions_ItShouldConfigureBuilder()
  {
    _mockBuilder.Setup(static m => m.UseJsonFileStorage(It.IsAny<Action<Storage.JsonStorageOptions>>()))
      .Callback<Action<Storage.JsonStorageOptions>>(static a => a(new Storage.JsonStorageOptions()))
      .Returns(_mockBuilder.Object);

    _mockBuilder.Setup(static m => m.WithMachineIdKey()).Returns(_mockBuilder.Object);
    _mockBuilder.Setup(static m => m.WithBase64EncryptionKey(It.IsAny<string>())).Returns(_mockBuilder.Object);
    _mockBuilder.Setup(static m => m.WithAesCryptoProvider()).Returns(_mockBuilder.Object);

    var profile = new SecureConfigProfile
    {
      Storage = new StorageProviderOptions
      {
        Type = "json",
        Json = new JsonStorageOptions { DirectoryPath = "dir", FileName = "file.json" }
      },
      Key = new KeyProviderOptions
      {
        Type = "machineid"
      },
      Crypto = new CryptoProviderOptions
      {
        Type = "aes"
      }
    };

    var result = _mockBuilder.Object.ApplyProfile(profile);

    result.Should().Be(_mockBuilder.Object);
    _mockBuilder.Verify(static m => m.UseJsonFileStorage(It.IsAny<Action<Storage.JsonStorageOptions>>()), Times.Once());
    _mockBuilder.Verify(static m => m.WithMachineIdKey(), Times.Once());
    _mockBuilder.Verify(static m => m.WithAesCryptoProvider(), Times.Once());
  }

  [Fact]
  public void ApplyProfile_WhenCalledWithBase64Key_ItShouldConfigureBuilder()
  {
    var builderMock = new Mock<ISecureConfigBuilder>();

    var profile = new SecureConfigProfile
    {
      Storage = new StorageProviderOptions { Type = "json", Json = new Cli.Configuration.JsonStorageOptions() },
      Key = new KeyProviderOptions { Type = "base64", Base64 = new Base64KeyOptions { Key = "test-key" } },
      Crypto = new CryptoProviderOptions { Type = "aes" }
    };

    builderMock.Object.ApplyProfile(profile);

    builderMock.Verify(m => m.WithBase64EncryptionKey("test-key"), Times.Once());
  }

  [Fact]
  public void ApplyProfile_WhenUnsupportedStorage_ItShouldThrowNotSupportedException()
  {
    var builderMock = new Mock<ISecureConfigBuilder>();
    var profile = new SecureConfigProfile
    {
      Storage = new StorageProviderOptions { Type = "unsupported" },
      Key = new KeyProviderOptions { Type = "machineid" },
      Crypto = new CryptoProviderOptions { Type = "aes" }
    };

    Action act = () => builderMock.Object.ApplyProfile(profile);

    act.Should().Throw<NotSupportedException>().WithMessage("Storage provider type 'unsupported' is not supported.");
  }

  [Fact]
  public void ApplyProfile_WhenUnsupportedKey_ItShouldThrowNotSupportedException()
  {
    var builderMock = new Mock<ISecureConfigBuilder>();
    var profile = new SecureConfigProfile
    {
      Storage = new StorageProviderOptions { Type = "json", Json = new Cli.Configuration.JsonStorageOptions() },
      Key = new KeyProviderOptions { Type = "unsupported" },
      Crypto = new CryptoProviderOptions { Type = "aes" }
    };

    Action act = () => builderMock.Object.ApplyProfile(profile);

    act.Should().Throw<NotSupportedException>().WithMessage("Key provider type 'unsupported' is not supported.");
  }

  [Fact]
  public void ApplyProfile_WhenUnsupportedCrypto_ItShouldThrowNotSupportedException()
  {
    var builderMock = new Mock<ISecureConfigBuilder>();
    var profile = new SecureConfigProfile
    {
      Storage = new StorageProviderOptions { Type = "json", Json = new Cli.Configuration.JsonStorageOptions() },
      Key = new KeyProviderOptions { Type = "machineid" },
      Crypto = new CryptoProviderOptions { Type = "unsupported" }
    };

    Action act = () => builderMock.Object.ApplyProfile(profile);

    act.Should().Throw<NotSupportedException>().WithMessage("Crypto provider type 'unsupported' is not supported.");
  }

  [Fact]
  public void SecureConfigCliOptions_ShouldBeCovered()
  {
    var options = new SecureConfigCliOptions
    {
      DefaultProfile = "default",
      Profiles = []
    };
    options.DefaultProfile.Should().Be("default");
    options.Profiles.Should().NotBeNull();
  }
}