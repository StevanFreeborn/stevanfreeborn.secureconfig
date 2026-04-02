using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

internal sealed class SecureConfigBuilder : ISecureConfigBuilder
{
  internal ISecureStorageProvider? StorageProvider { get; private set; }
  internal Func<IEncryptionKeyProvider, ICryptoProvider>? CryptoProviderFactory { get; private set; }
  internal IEncryptionKeyProvider? KeyProvider { get; private set; }
  internal ILoggerFactory LoggerFactory { get; private set; } = NullLoggerFactory.Instance;
  internal JsonSerializerOptions SerializerOptions { get; } = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  public ISecureConfigBuilder UseJsonFileStorage(JsonStorageOptions options)
  {
    StorageProvider = new JsonFileStorageProvider(options);
    return this;
  }

  public ISecureConfigBuilder UseJsonFileStorage(Action<JsonStorageOptions> configure)
  {
#if NET6_0_OR_GREATER
    ArgumentNullException.ThrowIfNull(configure);
#else
    if (configure is null)
    {
      throw new ArgumentNullException(nameof(configure));
    }
#endif

    var options = new JsonStorageOptions();
    configure.Invoke(options);
    StorageProvider = new JsonFileStorageProvider(options);
    return this;
  }

  public ISecureConfigBuilder AddJsonAotContext(IJsonTypeInfoResolver context)
  {
#if NET6_0_OR_GREATER
    ArgumentNullException.ThrowIfNull(context);
#else
    if (context is null)
    {
      throw new ArgumentNullException(nameof(context));
    }
#endif

    SerializerOptions.TypeInfoResolverChain.Insert(0, context);
    return this;
  }

  public ISecureConfigBuilder UseCustomStorage(ISecureStorageProvider provider)
  {
    StorageProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    return this;
  }

  public ISecureConfigBuilder WithBase64EncryptionKey(string key)
  {
    KeyProvider = new StaticKeyProvider(key);
    return this;
  }

  public ISecureConfigBuilder WithMachineIdKey()
  {
    var logger = LoggerFactory.CreateLogger<MachineIdKeyGenerator>();
    KeyProvider = new MachineIdKeyProvider(new MachineIdKeyGenerator(logger));
    return this;
  }

  public ISecureConfigBuilder WithCustomKeyProvider(IEncryptionKeyProvider provider)
  {
    KeyProvider = provider ?? throw new ArgumentNullException(nameof(provider));
    return this;
  }

  public ISecureConfigBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
  {
    LoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    return this;
  }

  public ISecureConfigBuilder WithAesCryptoProvider()
  {
    CryptoProviderFactory = (kp) => new AesCryptoProvider(kp);
    return this;
  }

  public ISecureConfigBuilder WithCustomCryptoProvider(Func<IEncryptionKeyProvider, ICryptoProvider> cryptoProviderFactory)
  {
    CryptoProviderFactory = cryptoProviderFactory;
    return this;
  }
}