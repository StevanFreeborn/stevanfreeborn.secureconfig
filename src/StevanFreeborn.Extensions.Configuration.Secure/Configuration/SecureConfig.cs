using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

internal interface ISecureConfig
{
  Task DeleteAsync<t>(string key, CancellationToken ct = default);
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
    throw new NotImplementedException();
  }

  public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
  {
    throw new NotImplementedException();
  }

  public async Task DeleteAsync<t>(string key, CancellationToken ct = default)
  {
    throw new NotImplementedException();
  }
}