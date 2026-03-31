using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Cryptography;

public class AesCryptoProviderTests
{
  private readonly Mock<IEncryptionKeyProvider> _mockKeyProvider = new();
  private readonly AesCryptoProvider _sut;

  public AesCryptoProviderTests()
  {
    _sut = new(_mockKeyProvider.Object);
  }

  [Fact]
  public void EncryptAndDecrypt_WhenCalled_ItShouldReturnOriginalString()
  {
    var originalText = "SuperDuperSecret";

    var encryptedText = _sut.Encrypt(originalText);
    var decryptedText = _sut.Decrypt(encryptedText);

    encryptedText.Should().NotBe(originalText);
    decryptedText.Should().Be(originalText);
  }
}