namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

internal interface ISecureConfig
{
  Task<bool> DeleteAsync(string key, CancellationToken ct = default);
  Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
  Task SetAsync<T>(string key, T value, CancellationToken ct = default);
}
