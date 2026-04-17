using System.CommandLine;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Tests.Unit.Commands;

public class DeleteCommandTests
{
  private readonly Mock<ISecureConfig> _mockSecureConfig = new();
  private readonly Mock<ITerminal> _mockTerminal = new();
  private readonly Command _sut;

  public DeleteCommandTests()
  {
    _sut = new DeleteCommandFactory(_mockSecureConfig.Object, _mockTerminal.Object).Create();
  }

  [Fact]
  public async Task Delete_WhenCalledWithoutKey_ItShouldReturnNonZeroExitCode()
  {
    var result = await _sut.Parse([]).InvokeAsync();

    result.Should().NotBe(0);
  }

  [Fact]
  public async Task Delete_WhenCalledWithNonExistentKey_ItShouldReturnNonZeroExitCode()
  {
    var expectedKey = "key";

    _mockSecureConfig.Setup(m => m.DeleteAsync(expectedKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);

    var result = await _sut.Parse([expectedKey]).InvokeAsync();

    result.Should().NotBe(0);
  }

  [Fact]
  public async Task Delete_WhenCalledWithExistingKey_ItShouldReturnZeroExitCode()
  {
    var expectedKey = "key";

    _mockSecureConfig.Setup(m => m.DeleteAsync(expectedKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

    var result = await _sut.Parse([expectedKey]).InvokeAsync();

    result.Should().Be(0);
  }
}