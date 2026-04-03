using System.Text.Json;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace StevanFreeborn.Extensions.Configuration.Secure.Storage;

/// <summary>
/// Provides a mechanism to store and retrieve secure configuration data in a JSON file.
/// </summary>
public sealed class JsonFileStorageProvider : ISecureStorageProvider
{
  private static readonly SemaphoreSlim FileLock = new(1, 1);
  private readonly PhysicalFileProvider? _fileProvider;
  private readonly IDisposable? _changeTokenRegistration;
  private readonly JsonStorageOptions _options;

  /// <inheritdoc/>
  public event EventHandler? StorageChanged;

  /// <summary>
  /// Provides a mechanism to store and retrieve secure configuration data in a JSON file.
  /// </summary>
  /// <param name="options">The <see cref="JsonStorageOptions"/> configuring the storage provider, including file paths.</param>
  public JsonFileStorageProvider(JsonStorageOptions options)
  {
    _options = options
      ?? throw new ArgumentNullException(nameof(options));

    var directory = Path.GetDirectoryName(_options.FullPath);

    if (string.IsNullOrWhiteSpace(directory) is false && Directory.Exists(directory))
    {
      _fileProvider = new PhysicalFileProvider(directory)
      {
        UseActivePolling = true,
        UsePollingFileWatcher = true,
      };

      _changeTokenRegistration = ChangeToken.OnChange(
        () => _fileProvider.Watch(_options.FileName),
        () =>
        {
          Thread.Sleep(250);
          StorageChanged?.Invoke(this, EventArgs.Empty);
        }
      );
    }
  }

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
  public Task<IDictionary<string, string>> ReadAllAsync(CancellationToken ct = default)
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
  ///
  /// </summary>
  public void Dispose()
  {
    _changeTokenRegistration?.Dispose();
    _fileProvider?.Dispose();
  }

  private async Task<IDictionary<string, string>> AcquireLockAndLoadAsync(CancellationToken ct)
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

  private async Task<Dictionary<string, string>> LoadAsync(CancellationToken ct)
  {
    if (File.Exists(_options.FullPath) is false)
    {
      return [];
    }

    var stream = new FileStream(
      _options.FullPath,
      FileMode.Open,
      FileAccess.Read,
      FileShare.ReadWrite,
      bufferSize: 4096,
      FileOptions.Asynchronous | FileOptions.SequentialScan
    );

    await using var _ = stream.ConfigureAwait(false);

    if (stream.Length is 0)
    {
      return [];
    }

    var data = await JsonSerializer.DeserializeAsync(stream, SecureConfigJsonContext.Default.DictionaryStringString, ct)
      .ConfigureAwait(false);

    return data ?? [];
  }

  private async Task SaveAsync(Dictionary<string, string> data, CancellationToken ct)
  {
    Directory.CreateDirectory(_options.DirectoryPath);

    var stream = new FileStream(
      _options.FullPath,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None,
      bufferSize: 4096,
      FileOptions.Asynchronous
    );

    await using var _ = stream.ConfigureAwait(false);

    await JsonSerializer.SerializeAsync(stream, data, SecureConfigJsonContext.Default.DictionaryStringString, ct)
      .ConfigureAwait(false);
  }
}