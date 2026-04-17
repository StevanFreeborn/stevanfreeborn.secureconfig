namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;

internal sealed class StorageProviderOptions
{
  public string Type { get; set; } = StorageProviderTypes.Json;
  public JsonStorageOptions Json { get; set; } = new();
}