using System.Security.Cryptography;
using System.Text;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Cryptography;

public class MachineIdKeyProviderTests
{
  private readonly Mock<IMachineIdKeyGenerator> _mockKeyGenerator = new();
  private readonly MachineIdKeyProvider _sut;

  public MachineIdKeyProviderTests()
  {
    _sut = new(_mockKeyGenerator.Object);
  }

  [Fact]
  public void GetKey_WhenCalled_ItShouldReturn32ByteKey()
  {
    _mockKeyGenerator.Setup(static m => m.GetId()).Returns("random-stuff");

    var result = _sut.GetKey();

    result.Length.Should().Be(32);
  }

  [Fact]
  public void GetKey_WhenCalledForSameInput_ItShouldProduceConsistentHash()
  {
    var input = "some-machine-id";

    var mockInstanceOne = new Mock<IMachineIdKeyGenerator>();
    mockInstanceOne.Setup(static m => m.GetId()).Returns(input);

    var instanceOne = new MachineIdKeyProvider(mockInstanceOne.Object);
    var resultOne = instanceOne.GetKey();

    var mockInstanceTwo = new Mock<IMachineIdKeyGenerator>();
    mockInstanceTwo.Setup(static m => m.GetId()).Returns(input);

    var instanceTwo = new MachineIdKeyProvider(mockInstanceTwo.Object);
    var resultTwo = instanceTwo.GetKey();

    resultOne.Should().BeEquivalentTo(resultTwo);
  }

  [Fact]
  public void GetKey_WhenCalledWithDifferentInput_ItShouldProduceDifferentHashes()
  {
    var inputOne = "inputOne";
    var inputTwo = "inputTwo";

    _mockKeyGenerator.SetupSequence(static m => m.GetId())
      .Returns(inputOne)
      .Returns(inputTwo);

    var resultOne = _sut.GetKey();
    var resultTwo = _sut.GetKey();

    resultOne.Should().NotBeEquivalentTo(resultTwo);
  }

  [Fact]
  public void GetKey_WhenCalled_ItShouldUseSHA256Hash()
  {
    var rawId = "rawId";
    var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(rawId));

    _mockKeyGenerator.Setup(static m => m.GetId()).Returns(rawId);

    var result = _sut.GetKey();

    result.Should().BeEquivalentTo(expectedHash);
  }
}