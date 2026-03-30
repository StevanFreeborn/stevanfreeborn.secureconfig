namespace StevanFreeborn.Extensions.Configuration.Secure.Storage;

/// <summary>
/// Options for configuring the <see cref="JsonFileStorageProvider"/>.
/// </summary>
public sealed class JsonStorageOptions
{
  /// <summary>
  /// Gets or sets the name of the JSON file used for storage.
  /// </summary>
  public string FileName { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the directory path where the JSON file is located. Defaults to the base directory of the application.
  /// </summary>
  public string DirectoryPath { get; set; } = AppContext.BaseDirectory;

  /// <summary>
  /// Gets the full, combined path to the JSON file, including the directory and file name.
  /// </summary>
  public string FullPath => Path.Combine(DirectoryPath, FileName);
}