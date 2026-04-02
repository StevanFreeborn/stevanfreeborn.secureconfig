using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Logging;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

/// <summary>
/// Provides a fluent builder interface for configuring secure configuration storage and encryption.
/// </summary>
public interface ISecureConfigBuilder
{
  /// <summary>
  /// Configures JSON file-based storage using the provided options instance.
  /// </summary>
  /// <param name="options">The JSON storage configuration options.</param>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder UseJsonFileStorage(JsonStorageOptions options);

  /// <summary>
  /// Configures JSON file-based storage using an action to configure the options.
  /// </summary>
  /// <param name="configure">An action to configure the <see cref="JsonStorageOptions"/>.</param>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder UseJsonFileStorage(Action<JsonStorageOptions> configure);

  /// <summary>
  /// Registers a JSON AOT source-generated context for serializing complex types.
  /// </summary>
  /// <param name="context">A context that implements <see cref="IJsonTypeInfoResolver"/> that will be used for JSON serialization.</param>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder AddJsonAotContext(IJsonTypeInfoResolver context);

  /// <summary>
  /// Configures a custom storage provider for secure configuration data.
  /// </summary>
  /// <param name="provider">The custom storage provider implementation.</param>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder UseCustomStorage(ISecureStorageProvider provider);

  /// <summary>
  /// Configures encryption using a Base64-encoded encryption key.
  /// </summary>
  /// <param name="key">The Base64-encoded encryption key string.</param>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder WithBase64EncryptionKey(string key);

  /// <summary>
  /// Configures encryption using a key derived from the machine id.
  /// </summary>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder WithMachineIdKey();


  /// <summary>
  /// Configures a custom encryption key provider.
  /// </summary>
  /// <param name="keyProvider">The custom encryption key provider implementation.</param>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder WithCustomKeyProvider(IEncryptionKeyProvider keyProvider);

  /// <summary>
  /// Configures logging using the provided logger factory.
  /// </summary>
  /// <param name="loggerFactory">The logger factory to use for logging operations.</param>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder WithLoggerFactory(ILoggerFactory loggerFactory);

  /// <summary>
  /// Configures AES crypto provider for encryption and decryption
  /// </summary>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder WithAesCryptoProvider();

  /// <summary>
  /// Configures the factory function that will be used to create the crypto provider for encryption and decryption
  /// </summary>
  /// <param name="cryptoProviderFactory">The crypto provider factor to use for encryption and decryption operations.</param>
  /// <returns>The current <see cref="ISecureConfigBuilder"/> instance for method chaining.</returns>
  ISecureConfigBuilder WithCustomCryptoProvider(Func<IEncryptionKeyProvider, ICryptoProvider> cryptoProviderFactory);
}