using System.CommandLine;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Commands;

internal interface ICommandFactory
{
  Command Create();
}