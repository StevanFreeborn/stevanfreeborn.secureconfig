using System.Text.Json.Serialization.Metadata;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

internal interface ISecureConfig
{
  Task SetAsync<T>(string key, T value, CancellationToken ct = default);
  Task SetAsync<T>(string key, T value, JsonTypeInfo<T> typeInfo, CancellationToken ct = default);
  Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
  Task<T?> GetAsync<T>(string key, JsonTypeInfo<T> typeInfo, CancellationToken ct = default);
  Task<bool> DeleteAsync(string key, CancellationToken ct = default);
}