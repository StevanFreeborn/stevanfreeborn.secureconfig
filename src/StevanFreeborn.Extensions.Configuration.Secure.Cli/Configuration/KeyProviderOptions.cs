namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;

internal sealed class KeyProviderOptions
{
  public string Type { get; set; } = KeyProviderTypes.MachineId;
  public Base64KeyOptions Base64 { get; set; } = new();
}