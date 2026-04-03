using System.Security.Cryptography;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Cryptography;

public class AesCryptoProviderTests
{
  private readonly Mock<IEncryptionKeyProvider> _mockKeyProvider = new();
  private readonly AesCryptoProvider _sut;

  public AesCryptoProviderTests()
  {
    var key = new byte[32];
    RandomNumberGenerator.Fill(key);
    _mockKeyProvider.Setup(static m => m.GetKey()).Returns(key);

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

  [Fact]
  public void Encrypt_WhenCalledWithSameInputTwitch_ItShouldProduceDifferentCipherText()
  {
    var plainText = "identicalInput";

    var cipherTextOne = _sut.Encrypt(plainText);
    var cipherTextTwo = _sut.Encrypt(plainText);

    cipherTextOne.Should().NotBe(cipherTextTwo);
  }

  [Fact]
  public void Decrypt_WhenCalledWithTamperedData_ItShouldThrowCrytographicException()
  {
    var cipherText = _sut.Encrypt("some data");
    var rawBytes = Convert.FromBase64String(cipherText);
    rawBytes[^1] = (byte)(rawBytes[^1] ^ 0xFF);

    var tamperedText = Convert.ToBase64String(rawBytes);

    var act = () => _sut.Decrypt(tamperedText);

    act.Should().Throw<CryptographicException>();
  }
}