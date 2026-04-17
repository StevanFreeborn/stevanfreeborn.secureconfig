namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;

internal sealed class Terminal : ITerminal
{
  public void WriteLine(string value)
  {
    Console.WriteLine(value);
  }
}