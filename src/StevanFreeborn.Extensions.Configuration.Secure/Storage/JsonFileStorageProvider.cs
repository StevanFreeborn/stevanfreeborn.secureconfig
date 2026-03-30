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
    if (File.Exists(_options.FullPath) is false)
    {
      return string.Empty;
    }

    using var stream = new FileStream(_options.FullPath, FileMode.Open, FileAccess.Read);

    if (stream.Length is 0)
    {
      return string.Empty;
    }

    var data = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: ct);

    if (data is not null && data.TryGetValue(key, out var v))
    {
      return v;
    }

    return string.Empty;
  }

  public async Task WriteAsync(string key, string encryptedData, CancellationToken ct = default)
  {
    Directory.CreateDirectory(_options.DirectoryPath);

    var data = new Dictionary<string, string>
    {
      [key] = encryptedData
    };

    using var stream = new FileStream(
      _options.FullPath,
      FileMode.Create,
      FileAccess.Write,
      FileShare.None
    );

    await JsonSerializer.SerializeAsync(stream, data, JsonOptions, ct);
  }
}