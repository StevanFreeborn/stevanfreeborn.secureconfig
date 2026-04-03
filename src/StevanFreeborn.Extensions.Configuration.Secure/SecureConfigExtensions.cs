using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure;

/// <summary>
/// Provides extension methods for configuring secure configuration in .NET applications.
/// </summary>
public static class SecureConfigExtensions
{
  private const string JsonSerializerOptionsKey = "SecureConfigJsonSerializerOptions";

  /// <summary>
  /// Adds secure configuration to the configuration builder.
  /// </summary>
  /// <param name="builder">The configuration builder to add secure configuration to.</param>
  /// <param name="configure">An action to configure the secure configuration builder.</param>
  /// <returns>The configuration builder with secure configuration added.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="configure"/> is null.</exception>
  /// <exception cref="InvalidOperationException">Thrown when a required provider is not configured.</exception>
  public static IConfigurationBuilder AddSecureConfig(
    this IConfigurationBuilder builder,
    Action<ISecureConfigBuilder> configure
  )
  {
#if NET6_0_OR_GREATER
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(configure);
#else
    if (builder is null)
    {
      throw new ArgumentNullException(nameof(builder));
    }

    if (configure is null)
    {
      throw new ArgumentNullException(nameof(configure));
    }
#endif

    var configBuilder = new SecureConfigBuilder();

    configure.Invoke(configBuilder);

    if (configBuilder.StorageProvider is null)
    {
      throw new InvalidOperationException("A storage provider must be configured.");
    }

    if (configBuilder.KeyProvider is null)
    {
      throw new InvalidOperationException("A key provider must be configured");
    }

    if (configBuilder.CryptoProviderFactory is null)
    {
      throw new InvalidOperationException("A crypto provider must be configured.");
    }

    var cryptoProvider = configBuilder.CryptoProviderFactory.Invoke(configBuilder.KeyProvider);

    var source = new SecureConfigSource(configBuilder.StorageProvider, cryptoProvider, configBuilder.LoggerFactory);
    return builder.Add(source);
  }

  /// <summary>
  /// Adds secure configuration services to the service collection.
  /// </summary>
  /// <param name="services">The service collection to add secure configuration services to.</param>
  /// <param name="configure">An action to configure the secure configuration builder.</param>
  /// <returns>The service collection with secure configuration services added.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="configure"/> is null.</exception>
  /// <exception cref="InvalidOperationException">Thrown when a required provider is not configured.</exception>
  public static IServiceCollection AddSecureConfig(
    this IServiceCollection services,
    Action<ISecureConfigBuilder> configure
  )
  {
#if NET6_0_OR_GREATER
    ArgumentNullException.ThrowIfNull(services);
    ArgumentNullException.ThrowIfNull(configure);
#else
    if (services is null)
    {
      throw new ArgumentNullException(nameof(services));
    }

    if (configure is null)
    {
      throw new ArgumentNullException(nameof(configure));
    }
#endif

    var configBuilder = new SecureConfigBuilder();

    configure.Invoke(configBuilder);

    if (configBuilder.StorageProvider is null)
    {
      throw new InvalidOperationException("A storage provider must be configured.");
    }

    if (configBuilder.KeyProvider is null)
    {
      throw new InvalidOperationException("A key provider must be configured");
    }

    if (configBuilder.CryptoProviderFactory is null)
    {
      throw new InvalidOperationException("A crypto provider must be configured.");
    }

    services.TryAddKeyedSingleton(JsonSerializerOptionsKey, configBuilder.SerializerOptions);
    services.TryAddSingleton(configBuilder.StorageProvider);
    services.TryAddSingleton(configBuilder.KeyProvider);
    services.TryAddSingleton(configBuilder.CryptoProviderFactory.Invoke(configBuilder.KeyProvider));
    services.TryAddSingleton<ISecureConfig>(sp =>
    {
      var storageProvider = sp.GetRequiredService<ISecureStorageProvider>();
      var cryptoProvider = sp.GetRequiredService<ICryptoProvider>();
      var serializerOptions = sp.GetRequiredKeyedService<JsonSerializerOptions>(JsonSerializerOptionsKey);
      return new SecureConfig(storageProvider, cryptoProvider, serializerOptions);
    });

    return services;
  }
}