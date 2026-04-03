using System.Text.Json;

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace StevanFreeborn.Extensions.Configuration.Secure.Storage;

internal sealed class JsonFileStorageProvider : ISecureStorageProvider
{
  private readonly SemaphoreSlim _fileLock = new(1, 1);
  private readonly PhysicalFileProvider? _fileProvider;
  private readonly IDisposable? _changeTokenRegistration;
  private readonly JsonStorageOptions _options;

  public event EventHandler? StorageChanged;

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
        () => _ = NotifyStorageChangedAsync()
      );
    }
  }

  public async Task<string> ReadAsync(string key, CancellationToken ct = default)
  {
    var data = await AcquireLockAndLoadAsync(ct).ConfigureAwait(false);

    if (data is not null && data.TryGetValue(key, out var v))
    {
      return v;
    }

    return string.Empty;
  }

  public Task<IDictionary<string, string>> ReadAllAsync(CancellationToken ct = default)
  {
    return AcquireLockAndLoadAsync(ct);
  }

  public async Task WriteAsync(string key, string encryptedData, CancellationToken ct = default)
  {
    await _fileLock.WaitAsync(ct).ConfigureAwait(false);

    try
    {
      var data = await LoadAsync(ct).ConfigureAwait(false);
      data[key] = encryptedData;
      await SaveAsync(data, ct).ConfigureAwait(false);
    }
    finally
    {
      _fileLock.Release();
    }
  }

  public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
  {
    await _fileLock.WaitAsync(ct).ConfigureAwait(false);

    try
    {
      var data = await LoadAsync(ct).ConfigureAwait(false);
      var result = data.Remove(key);
      await SaveAsync(data, ct).ConfigureAwait(false);
      return result;
    }
    finally
    {
      _fileLock.Release();
    }
  }

  public void Dispose()
  {
    _changeTokenRegistration?.Dispose();
    _fileProvider?.Dispose();
    _fileLock.Dispose();
  }

  private async Task<IDictionary<string, string>> AcquireLockAndLoadAsync(CancellationToken ct)
  {
    await _fileLock.WaitAsync(ct).ConfigureAwait(false);

    try
    {
      return await LoadAsync(ct).ConfigureAwait(false);
    }
    finally
    {
      _fileLock.Release();
    }
  }

  private async Task NotifyStorageChangedAsync()
  {
    await Task.Delay(250).ConfigureAwait(false);
    StorageChanged?.Invoke(this, EventArgs.Empty);
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