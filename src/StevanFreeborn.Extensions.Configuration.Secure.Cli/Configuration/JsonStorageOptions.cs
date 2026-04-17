namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;

internal sealed class JsonStorageOptions
{
  public string DirectoryPath { get; set; } = AppContext.BaseDirectory;
  public string FileName { get; set; } = "secure_config.json";
}