using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Cryptography;

public class StaticKeyProviderTests
{
  [Fact]
  public void Constructor_ShouldThrowArgumentNullException_WhenKeyIsNull()
  {
    var act = () => new StaticKeyProvider(null!);

    act.Should().Throw<ArgumentException>();
  }

  [Fact]
  public void Constructor_WhenCalledAndKeyIsNot32Bytes_ItShouldThrowArgumentException()
  {
    var shortKey = Convert.ToBase64String(new byte[16]);

    var act = () => new StaticKeyProvider(shortKey);

    act.Should().Throw<ArgumentException>().WithMessage("*must be exactly 32 bytes*");
  }

  [Fact]
  public void GetKey_WhenCalled_ItShouldReturnValid32ByteArray()
  {
    var expectedBytes = new byte[32];
    Random.Shared.NextBytes(expectedBytes);
    var base64Key = Convert.ToBase64String(expectedBytes);

    var provider = new StaticKeyProvider(base64Key);

    var result = provider.GetKey();

    result.Should().BeEquivalentTo(expectedBytes);
    result.Length.Should().Be(32);
  }
}