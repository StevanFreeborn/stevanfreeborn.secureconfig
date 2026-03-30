using System.Text.Json;

namespace StevanFreeborn.Extensions.Configuration.Secure.Storage;

public sealed class JsonFileStorageProvider(JsonStorageOptions options)
{
  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    WriteIndented = true,
  };
  private readonly JsonStorageOptions _options = options
    ?? throw new ArgumentNullException(nameof(options));

  public async Task<string> ReadAsync(string key, CancellationToken ct = default)
  {
    var data = await LoadAsync(ct);

    if (data is not null && data.TryGetValue(key, out var v))
    {
      return v;
    }

    return string.Empty;
  }

  public Task<Dictionary<string, string>> ReadAllAsync(CancellationToken ct = default)
  {
    return LoadAsync(ct);
  }

  public async Task WriteAsync(string key, string encryptedData, CancellationToken ct = default)
  {
    var data = await LoadAsync(ct);
    data[key] = encryptedData;
    await SaveAsync(data, ct);
  }

  public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
  {
    var data = await LoadAsync(ct);
    var result = data.Remove(key);
    await SaveAsync(data, ct);
    return result;
  }

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

    var data = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: ct);

    return data ?? [];
  }

  private async Task SaveAsync(Dictionary<string, string> data, CancellationToken ct)
  {
    Directory.CreateDirectory(_options.DirectoryPath);

    using var stream = new FileStream(
      _options.FullPath,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None
    );

    await JsonSerializer.SerializeAsync(stream, data, JsonOptions, ct);
  }
}