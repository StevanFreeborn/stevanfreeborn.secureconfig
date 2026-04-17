using System.CommandLine;


using Microsoft.Extensions.Hosting;


using Moq;


using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Tests.Unit;

public class SecureConfigCliTests
{
  private readonly Mock<IHostApplicationLifetime> _mockLifetime = new();
  private readonly Mock<ICommandFactory> _mockCommandFactory = new();

  public SecureConfigCliTests()
  {
    _mockCommandFactory.Setup(m => m.Create()).Returns(new Command("test-command"));
  }

  [Fact]
  public void Constructor_WhenCalled_ItShouldCallCreateOnAllFactoriesPassedIn()
  {
    var sut = new SecureConfigCli(["secure-config"], _mockLifetime.Object, [_mockCommandFactory.Object]);
    _mockCommandFactory.Verify(static m => m.Create(), Times.Once());
  }

  [Fact]
  public async Task StartAsync_WhenCalled_ItShouldRunTheCLI()
  {
    // Arrange
    var sut = new SecureConfigCli(["secure-config", "--help"], _mockLifetime.Object, [_mockCommandFactory.Object]);

    // Act
    await sut.StartAsync(CancellationToken.None);

    // Assert
    _mockLifetime.Verify(m => m.StopApplication(), Times.Once());
  }

  [Fact]
  public async Task StopAsync_WhenCalled_ItShouldNotThrowException()
  {
    var sut = new SecureConfigCli(["secure-config"], _mockLifetime.Object, [_mockCommandFactory.Object]);
    var act = async () => await sut.StopAsync(CancellationToken.None);
    await act.Should().NotThrowAsync();
  }
}