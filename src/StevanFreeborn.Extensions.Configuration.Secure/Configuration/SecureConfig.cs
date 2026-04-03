using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

internal sealed class SecureConfig(
  ISecureStorageProvider storageProvider,
  ICryptoProvider cryptoProvider,
  JsonSerializerOptions jsonOptions
) : ISecureConfig
{
  private readonly ISecureStorageProvider _storageProvider = storageProvider ??
    throw new ArgumentNullException(nameof(storageProvider));

  private readonly ICryptoProvider _cryptoProvider = cryptoProvider ??
    throw new ArgumentNullException(nameof(cryptoProvider));

  private readonly JsonSerializerOptions _jsonOptions = jsonOptions ??
    throw new ArgumentNullException(nameof(jsonOptions));

  public Task SetAsync<T>(string key, T value, CancellationToken ct = default)
  {
    var typeInfo = GetTypeInfo<T>();
    return SetAsync(key, value, typeInfo, ct);
  }

  public async Task SetAsync<T>(string key, T value, JsonTypeInfo<T> typeInfo, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      throw new ArgumentException("Cannot be null or whitespace.", nameof(key));
    }

    if (value is null)
    {
      throw new ArgumentNullException(nameof(value));
    }

    var json = JsonSerializer.Serialize(value, typeInfo);
    var encryptedValue = _cryptoProvider.Encrypt(json);

    await _storageProvider.WriteAsync(key, encryptedValue, ct).ConfigureAwait(false);
  }

  public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
  {
    var typeInfo = GetTypeInfo<T>();
    return GetAsync(key, typeInfo, ct);
  }

  public async Task<T?> GetAsync<T>(string key, JsonTypeInfo<T> typeInfo, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      throw new ArgumentException("Cannot be null or whitespace.", nameof(key));
    }

    var encryptedData = await _storageProvider.ReadAsync(key, ct).ConfigureAwait(false);

    if (string.IsNullOrWhiteSpace(encryptedData))
    {
      return default;
    }

    var data = _cryptoProvider.Decrypt(encryptedData);
    return JsonSerializer.Deserialize(data, typeInfo);
  }

  public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
  {
    return _storageProvider.DeleteAsync(key, ct);
  }

  private JsonTypeInfo<T> GetTypeInfo<T>()
  {
    try
    {
      return (JsonTypeInfo<T>?)_jsonOptions.GetTypeInfo(typeof(T))
        ?? throw new InvalidOperationException($"AOT metadata for type '{typeof(T).Name}' is missing. Did you forget to register it via {nameof(SecureConfigBuilder.AddJsonAotContext)}()?");
    }
    catch (NotSupportedException ex)
    {
      throw new InvalidOperationException($"AOT metadata for type '{typeof(T).Name}' is missing. Did you forget to register it via {nameof(SecureConfigBuilder.AddJsonAotContext)}()?", ex);
    }
  }
}