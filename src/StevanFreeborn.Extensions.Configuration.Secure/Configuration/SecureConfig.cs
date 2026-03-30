using System.Text.Json;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

internal interface ISecureConfig
{
  Task<bool> DeleteAsync(string key, CancellationToken ct = default);
  Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
  Task SetAsync<T>(string key, T value, CancellationToken ct = default);
}

internal sealed class SecureConfig(
  ISecureStorageProvider storageProvider,
  ICryptoProvider cryptoProvider
) : ISecureConfig
{
  private readonly ISecureStorageProvider _storageProvider = storageProvider ??
    throw new ArgumentNullException(nameof(storageProvider));

  private readonly ICryptoProvider _cryptoProvider = cryptoProvider ??
    throw new ArgumentNullException(nameof(cryptoProvider));

  public async Task SetAsync<T>(string key, T value, CancellationToken ct = default)
  {
    if (string.IsNullOrWhiteSpace(key))
    {
      throw new ArgumentNullException(nameof(key));
    }

    if (value is null)
    {
      throw new ArgumentNullException(nameof(value));
    }

    var json = JsonSerializer.Serialize(value);
    var encryptedValue = _cryptoProvider.Encrypt(json);

    await _storageProvider.WriteAsync(key, encryptedValue, ct).ConfigureAwait(false);
  }

  public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
  {
    if (string.IsNullOrEmpty(key))
    {
      throw new ArgumentNullException(nameof(key));
    }

    var encryptedData = await _storageProvider.ReadAsync(key, ct).ConfigureAwait(false);

    if (string.IsNullOrWhiteSpace(encryptedData))
    {
      return default;
    }

    var data = _cryptoProvider.Decrypt(encryptedData);
    return JsonSerializer.Deserialize<T>(data);
  }

  public Task<bool> DeleteAsync(string key, CancellationToken ct = default)
  {
    return _storageProvider.DeleteAsync(key, ct);
  }
}