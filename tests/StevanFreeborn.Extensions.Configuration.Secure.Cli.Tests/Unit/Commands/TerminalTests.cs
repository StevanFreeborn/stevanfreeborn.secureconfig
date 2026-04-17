using StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Tests.Unit.Commands;

public class TerminalTests : IDisposable
{
  private readonly StringWriter _stringWriter = new();
  private readonly Terminal _sut = new();

  public TerminalTests()
  {
    Console.SetOut(_stringWriter);
  }

  [Fact]
  public void WriteLine_WhenCalled_ItShouldWriteToConsole()
  {
    var message = "test message";

    _sut.WriteLine(message);

    _stringWriter.ToString().Should().Be($"test message{Environment.NewLine}");
  }

  public void Dispose()
  {
    var standardOutput = new StreamWriter(Console.OpenStandardOutput())
    {
      AutoFlush = true
    };

    Console.SetOut(standardOutput);

    GC.SuppressFinalize(this);
  }
}