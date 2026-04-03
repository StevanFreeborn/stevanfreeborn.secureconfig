using System.Text.Json.Serialization.Metadata;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

/// <summary>
/// Provides secure storage, retrieval, and deletion of configuration values.
/// All values are automatically encrypted before storage and decrypted on retrieval.
/// </summary>
public interface ISecureConfig
{
  /// <summary>
  /// Serializes, encrypts, and stores a value for the specified key.
  /// Resolves <see cref="JsonTypeInfo{T}"/> from the registered serializer options.
  /// </summary>
  /// <typeparam name="T">The type of the value to store.</typeparam>
  /// <param name="key">The configuration key.</param>
  /// <param name="value">The value to store.</param>
  /// <param name="ct">A token to monitor for cancellation requests.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task SetAsync<T>(string key, T value, CancellationToken ct = default);

  /// <summary>
  /// Serializes, encrypts, and stores a value for the specified key using the provided type metadata.
  /// This overload supports Native AOT by accepting pre-compiled <see cref="JsonTypeInfo{T}"/>.
  /// </summary>
  /// <typeparam name="T">The type of the value to store.</typeparam>
  /// <param name="key">The configuration key.</param>
  /// <param name="value">The value to store.</param>
  /// <param name="typeInfo">The JSON type metadata for source-generated serialization.</param>
  /// <param name="ct">A token to monitor for cancellation requests.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  Task SetAsync<T>(string key, T value, JsonTypeInfo<T> typeInfo, CancellationToken ct = default);

  /// <summary>
  /// Reads, decrypts, and deserializes a value for the specified key.
  /// Resolves <see cref="JsonTypeInfo{T}"/> from the registered serializer options.
  /// </summary>
  /// <typeparam name="T">The type of the value to retrieve.</typeparam>
  /// <param name="key">The configuration key.</param>
  /// <param name="ct">A token to monitor for cancellation requests.</param>
  /// <returns>The deserialized value, or <c>default</c> if the key does not exist.</returns>
  Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

  /// <summary>
  /// Reads, decrypts, and deserializes a value for the specified key using the provided type metadata.
  /// This overload supports Native AOT by accepting pre-compiled <see cref="JsonTypeInfo{T}"/>.
  /// </summary>
  /// <typeparam name="T">The type of the value to retrieve.</typeparam>
  /// <param name="key">The configuration key.</param>
  /// <param name="typeInfo">The JSON type metadata for source-generated serialization.</param>
  /// <param name="ct">A token to monitor for cancellation requests.</param>
  /// <returns>The deserialized value, or <c>default</c> if the key does not exist.</returns>
  Task<T?> GetAsync<T>(string key, JsonTypeInfo<T> typeInfo, CancellationToken ct = default);

  /// <summary>
  /// Deletes the value associated with the specified key from secure storage.
  /// </summary>
  /// <param name="key">The configuration key.</param>
  /// <param name="ct">A token to monitor for cancellation requests.</param>
  /// <returns><c>true</c> if the value was deleted; otherwise, <c>false</c>.</returns>
  Task<bool> DeleteAsync(string key, CancellationToken ct = default);
}