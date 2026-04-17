using System.CommandLine;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;
using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Tests.Unit.Commands;

public class SetCommandFactoryTests
{
  private readonly Mock<ISecureConfig> _mockSecureConfig = new();
  private readonly Mock<ITerminal> _mockTerminal = new();
  private readonly SetCommandFactory _sut;

  public SetCommandFactoryTests()
  {
    _sut = new(_mockSecureConfig.Object, _mockTerminal.Object);
  }

  [Fact]
  public void Create_WhenCalled_ItShouldReturnExpectedCommand()
  {
    var expectedCommand = new Command("set", "Sets the value for the given key")
    {
      new Argument<string>("key")
      {
        Description = "The key whose value you want to set.",
      },
      new Argument<string>("value")
      {
        Description = "The value that you want to set for the key."
      },
    };

    var result = _sut.Create();

    result.Should().BeEquivalentTo(expectedCommand, static o => o.IgnoringCyclicReferences());
    result.Action.Should().NotBeNull();
  }
}