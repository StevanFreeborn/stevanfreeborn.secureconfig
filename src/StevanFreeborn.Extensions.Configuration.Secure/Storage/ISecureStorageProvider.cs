namespace StevanFreeborn.Extensions.Configuration.Secure.Storage;

/// <summary>
/// Defines a contract for a provider that stores and retrieves secure configuration data.
/// </summary>
public interface ISecureStorageProvider : IDisposable
{
  /// <summary>
  ///
  /// </summary>
  event EventHandler StorageChanged;

  /// <summary>
  /// Reads the value associated with the specified key asynchronously.
  /// </summary>
  /// <param name="key">The key of the configuration value to read.</param>
  /// <param name="ct">A cancellation token that can be used to cancel the read operation.</param>
  /// <returns>A task that represents the asynchronous read operation. The task result contains the value associated with the specified key, or an empty string if the key is not found.</returns>
  Task<string> ReadAsync(string key, CancellationToken ct = default);

  /// <summary>
  /// Reads all configuration values asynchronously.
  /// </summary>
  /// <param name="ct">A cancellation token that can be used to cancel the read operation.</param>
  /// <returns>A task that represents the asynchronous read operation. The task result contains a dictionary of all configuration keys and their encrypted values.</returns>
  Task<IDictionary<string, string>> ReadAllAsync(CancellationToken ct = default);

  /// <summary>
  /// Writes the specified key and encrypted data asynchronously.
  /// </summary>
  /// <param name="key">The key of the configuration value to write.</param>
  /// <param name="encryptedData">The encrypted configuration data to write.</param>
  /// <param name="ct">A cancellation token that can be used to cancel the write operation.</param>
  /// <returns>A task that represents the asynchronous write operation.</returns>
  Task WriteAsync(string key, string encryptedData, CancellationToken ct = default);

  /// <summary>
  /// Deletes the configuration value associated with the specified key asynchronously.
  /// </summary>
  /// <param name="key">The key of the configuration value to delete.</param>
  /// <param name="ct">A cancellation token that can be used to cancel the delete operation.</param>
  /// <returns>A task that represents the asynchronous delete operation. The task result contains <c>true</c> if the value was successfully deleted; otherwise, <c>false</c>.</returns>
  Task<bool> DeleteAsync(string key, CancellationToken ct = default);
}