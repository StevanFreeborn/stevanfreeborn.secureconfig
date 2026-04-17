using System.CommandLine;
using System.Text.Json;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Tests.Unit.Commands;

public class GetCommandTests
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
  };
  private readonly Mock<ISecureConfig> _mockSecureConfig = new();
  private readonly Mock<ITerminal> _mockTerminal = new();
  private readonly Command _sut;

  public GetCommandTests()
  {
    _sut = new GetCommandFactory(_mockSecureConfig.Object, _mockTerminal.Object).Create();
  }

  [Fact]
  public async Task Get_WhenCalledWithoutKeyArgument_ItShouldReturnNonZeroExitCode()
  {
    var result = await _sut.Parse([]).InvokeAsync();

    result.Should().NotBe(0);
  }

  [Fact]
  public async Task Get_WhenCalledWithNonExistentKey_ItShouldReturnNonZeroExitCode()
  {
    var expectedKey = "key";

    _mockSecureConfig
      .Setup(m => m.GetAsync<string>(expectedKey, It.IsAny<CancellationToken>()))
      .ReturnsAsync((string?)null);

    var result = await _sut.Parse([expectedKey]).InvokeAsync();

    result.Should().NotBe(0);
  }

  [Fact]
  public async Task Get_WhenCalledWithKeyForNonJsonValue_ItShouldReturnZeroExitCode()
  {
    var expectedKey = "key";
    var expectedValue = "value";
    
    _mockSecureConfig
      .Setup(m => m.GetAsync<string>(expectedKey, It.IsAny<CancellationToken>()))
      .ReturnsAsync(expectedValue);

    var result = await _sut.Parse([expectedKey]).InvokeAsync();

    result.Should().Be(0);
  }

  [Fact]
  public async Task Get_WhenCalledWithKeyForJsonValue_ItShouldReturnZeroExitCode()
  {
    var expectedKey = "key";
    var expectedValue = JsonSerializer.Serialize(new { ApiKey = "Key" });
    
    _mockSecureConfig
      .Setup(m => m.GetAsync<string>(expectedKey, It.IsAny<CancellationToken>()))
      .ReturnsAsync(expectedValue);

    var result = await _sut.Parse([expectedKey]).InvokeAsync();

    result.Should().Be(0);
  }

  [Fact]
  public async Task Get_WhenCalledWithKeyForJsonValueAndPrettyPrint_ItShouldReturnZeroExitCode()
  {
    var expectedKey = "key";
    var testValue = new { ApiKey = "Key" };
    var expectedValue = JsonSerializer.Serialize(testValue);
    var expectedPrintedValue = JsonSerializer.Serialize(testValue, JsonOptions);
    
    _mockSecureConfig
      .Setup(m => m.GetAsync<string>(expectedKey, It.IsAny<CancellationToken>()))
      .ReturnsAsync(expectedValue);

    var result = await _sut.Parse([expectedKey, "-p"]).InvokeAsync();

    result.Should().Be(0);

    _mockTerminal.Verify(m => m.WriteLine(expectedPrintedValue), Times.Once());
  }
}