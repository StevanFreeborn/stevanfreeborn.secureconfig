namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;

internal sealed class SecureConfigCliOptions
{
  public string DefaultProfile { get; set; } = "Default";
  public Dictionary<string, SecureConfigProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}