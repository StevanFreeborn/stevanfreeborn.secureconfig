namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;

internal sealed class SecureConfigProfile
{
  public StorageProviderOptions Storage { get; set; } = new();
  public KeyProviderOptions Key { get; set; } = new();
  public CryptoProviderOptions Crypto { get; set; } = new();
}