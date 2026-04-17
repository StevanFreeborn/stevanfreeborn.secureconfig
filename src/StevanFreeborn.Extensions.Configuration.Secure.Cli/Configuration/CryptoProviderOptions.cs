namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;

internal sealed class CryptoProviderOptions
{
  public string Type { get; set; } = CryptoProviderTypes.Aes;
}