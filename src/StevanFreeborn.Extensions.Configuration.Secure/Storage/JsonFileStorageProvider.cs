using System.Text.Json;

namespace StevanFreeborn.Extensions.Configuration.Secure.Storage;

/// <summary>
/// Provides a mechanism to store and retrieve secure configuration data in a JSON file.
/// </summary>
/// <param name="options">The <see cref="JsonStorageOptions"/> configuring the storage provider, including file paths.</param>
public sealed class JsonFileStorageProvider(JsonStorageOptions options)
{
  private static readonly SemaphoreSlim FileLock = new(1, 1);
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
  };
  private readonly JsonStorageOptions _options = options
    ?? throw new ArgumentNullException(nameof(options));

  /// <summary>
  /// Reads the value associated with the specified key from the JSON file asynchronously.
  /// </summary>
  /// <param name="key">The key of the configuration value to read.</param>
  /// <param name="ct">A cancellation token that can be used to cancel the read operation.</param>
  /// <returns>A task that represents the asynchronous read operation. The task result contains the value associated with the specified key, or an empty string if the key is not found.</returns>
  public async Task<string> ReadAsync(string key, CancellationToken ct = default)
  {
    var data = await AcquireLockAndLoadAsync(ct).ConfigureAwait(false);

    if (data is not null && data.TryGetValue(key, out var v))
    {
      return v;
    }

    return string.Empty;
  }

  /// <summary>
  /// Reads all configuration values from the JSON file asynchronously.
  /// </summary>
  /// <param name="ct">A cancellation token that can be used to cancel the read operation.</param>
  /// <returns>A task that represents the asynchronous read operation. The task result contains a dictionary of all configuration keys and their values.</returns>
  public Task<Dictionary<string, string>> ReadAllAsync(CancellationToken ct = default)
  {
    return AcquireLockAndLoadAsync(ct);
  }

  /// <summary>
  /// Writes the specified key and encrypted data to the JSON file asynchronously.
  /// </summary>
  /// <param name="key">The key of the configuration value to write.</param>
  /// <param name="encryptedData">The encrypted configuration data to write.</param>
  /// <param name="ct">A cancellation token that can be used to cancel the write operation.</param>
  /// <returns>A task that represents the asynchronous write operation.</returns>
  public async Task WriteAsync(string key, string encryptedData, CancellationToken ct = default)
  {
    await FileLock.WaitAsync(ct).ConfigureAwait(false);

    try
    {
      var data = await LoadAsync(ct).ConfigureAwait(false);
      data[key] = encryptedData;
      await SaveAsync(data, ct).ConfigureAwait(false);
    }
    finally
    {
      FileLock.Release();
    }
  }

  /// <summary>
  /// Deletes the configuration value associated with the specified key from the JSON file asynchronously.
  /// </summary>
  /// <param name="key">The key of the configuration value to delete.</param>
  /// <param name="ct">A cancellation token that can be used to cancel the delete operation.</param>
  /// <returns>A task that represents the asynchronous delete operation. The task result contains <c>true</c> if the value was successfully deleted; otherwise, <c>false</c>.</returns>
  public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
  {
    await FileLock.WaitAsync(ct).ConfigureAwait(false);

    try
    {
      var data = await LoadAsync(ct).ConfigureAwait(false);
      var result = data.Remove(key);
      await SaveAsync(data, ct).ConfigureAwait(false);
      return result;
    }
    finally
    {
      FileLock.Release();
    }
  }

  /// <summary>
  /// Acquires an exclusive lock and loads the configuration data from the JSON file.
  /// </summary>
  /// <param name="ct">A cancellation token to observe while waiting for the lock or during the load operation.</param>
  /// <returns>A dictionary containing the loaded configuration data.</returns>
  private async Task<Dictionary<string, string>> AcquireLockAndLoadAsync(CancellationToken ct)
  {
    await FileLock.WaitAsync(ct).ConfigureAwait(false);

    try
    {
      return await LoadAsync(ct).ConfigureAwait(false);
    }
    finally
    {
      FileLock.Release();
    }
  }

  /// <summary>
  /// Loads the configuration data from the JSON file.
  /// </summary>
  /// <param name="ct">A cancellation token to observe while loading the data.</param>
  /// <returns>A dictionary containing the loaded configuration data, or an empty dictionary if the file does not exist or is empty.</returns>
  private async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct)
  {
    if (File.Exists(_options.FullPath) is false)
    {
      return [];
    }

    using var stream = new FileStream(_options.FullPath, FileMode.Open, FileAccess.Read);

    if (stream.Length is 0)
    {
      return [];
    }

    var data = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: ct)
      .ConfigureAwait(false);

    return data ?? [];
  }

  /// <summary>
  /// Saves the configuration data to the JSON file.
  /// </summary>
  /// <param name="data">The configuration data to save.</param>
  /// <param name="ct">A cancellation token to observe while saving the data.</param>
  /// <returns>A task that represents the asynchronous save operation.</returns>
  private async Task SaveAsync(Dictionary<string, string> data, CancellationToken ct)
  {
    Directory.CreateDirectory(_options.DirectoryPath);

    using var stream = new FileStream(
      _options.FullPath,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None
    );

    await JsonSerializer.SerializeAsync(stream, data, JsonOptions, ct)
      .ConfigureAwait(false);
  }
}