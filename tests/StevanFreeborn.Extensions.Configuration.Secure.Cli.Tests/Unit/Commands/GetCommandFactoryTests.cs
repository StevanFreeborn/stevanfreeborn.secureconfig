using System.CommandLine;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Tests.Unit.Commands;

public class GetCommandFactoryTests
{
  private readonly Mock<ISecureConfig> _mockSecureConfig = new();
  private readonly Mock<ITerminal> _mockTerminal = new();
  private readonly GetCommandFactory _sut;

  public GetCommandFactoryTests()
  {
    _sut = new(_mockSecureConfig.Object, _mockTerminal.Object);
  }

  [Fact]
  public void Create_WhenCalled_ItShouldReturnExpectedCommand()
  {
    var expectedCommand = new Command("get", "Retrieves the value for the given key")
    {
      new Argument<string>("key")
      {
        Description = "The key whose value you want to retrieve."
      },
      new Option<bool>("--pretty", "-p")
      {
        Description = "Indicates whether the value - if JSON - should be pretty printed or not"
      },
    };

    var result = _sut.Create();

    result.Should().BeEquivalentTo(expectedCommand, static o => o.IgnoringCyclicReferences());
    result.Action.Should().NotBeNull();
  }
}