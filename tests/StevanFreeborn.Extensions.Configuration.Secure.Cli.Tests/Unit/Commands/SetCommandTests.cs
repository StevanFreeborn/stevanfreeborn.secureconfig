using System.CommandLine;
using System.Text.Json;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Tests.Unit.Commands;

public class SetCommandTests
{
  private readonly Mock<ISecureConfig> _mockSecureConfig = new();
  private readonly Mock<ITerminal> _mockTerminal = new();
  private readonly Command _sut;

  public SetCommandTests()
  {
    _sut = new SetCommandFactory(_mockSecureConfig.Object, _mockTerminal.Object).Create();
  }

  [Fact]
  public async Task Set_WhenCalledWithoutKey_ItShouldReturnNonZeroExitCode()
  {
    var result = await _sut.Parse([]).InvokeAsync();

    result.Should().NotBe(0);
  }

  [Fact]
  public async Task Set_WhenCalledWithoutValue_ItShouldReturnNonZeroExitCode()
  {
    var result = await _sut.Parse(["key"]).InvokeAsync();

    result.Should().NotBe(0);
  }

  [Fact]
  public async Task Set_WhenCalledWithKeyAndValue_ItShouldReturnZeroExitCode()
  {
    var expectedKey = "key";
    var expectedValue = "value";

    var result = await _sut.Parse([expectedKey, expectedValue]).InvokeAsync();

    result.Should().Be(0);

    _mockSecureConfig.Verify(m => m.SetAsync(expectedKey, expectedValue, It.IsAny<CancellationToken>()), Times.Once());
  }

  [Fact]
  public async Task Set_WhenCalledWithKeyAndJsonValue_ItShouldReturnZeroExitCode()
  {
    var expectedKey = "key";
    var expectedValue = JsonSerializer.Serialize(new { ApiKey = "ApiKey" });

    var result = await _sut.Parse([expectedKey, expectedValue]).InvokeAsync();

    result.Should().Be(0);

    _mockSecureConfig.Verify(m => m.SetAsync(expectedKey, expectedValue, It.IsAny<CancellationToken>()), Times.Once());
  }
}