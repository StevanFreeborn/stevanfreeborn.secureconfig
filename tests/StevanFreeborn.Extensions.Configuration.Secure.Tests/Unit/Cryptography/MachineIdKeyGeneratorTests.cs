using Microsoft.Extensions.Logging;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Cryptography;

public class MachineIdKeyGeneratorTests
{
  private readonly Mock<ILogger<MachineIdKeyGenerator>> _mockLogger = new();
  private readonly MachineIdKeyGenerator _sut;

  public MachineIdKeyGeneratorTests()
  {
    _sut = new(_mockLogger.Object);
  }

  [Fact]
  public void GetId_WhenCalled_ItShouldReturnNonEmptyString()
  {
    _sut.GetId().Should().NotBeEmpty();
  }

  [Fact]
  public void GetId_WhenCalledMultipleTimes_ItShouldReturnConsistentId()
  {
    var resultOne = _sut.GetId();
    var resultTwo = _sut.GetId();

    resultTwo.Should().Be(resultOne);
  }
}