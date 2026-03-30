namespace StevanFreeborn.Extensions.Configuration.Secure.Storage;

public sealed class JsonStorageOptions
{
  public string FileName { get; set; } = string.Empty;
  public string DirectoryPath { get; set; } = AppContext.BaseDirectory;
  public string FullPath => Path.Combine(DirectoryPath, FileName);
}