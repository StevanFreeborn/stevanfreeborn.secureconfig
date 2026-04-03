using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

internal sealed class SecureConfigSource(
  ISecureStorageProvider storageProvider,
  ICryptoProvider cryptoProvider,
  ILoggerFactory loggerFactory
) : IConfigurationSource
{
  private readonly ISecureStorageProvider _storageProvider = storageProvider
    ?? throw new ArgumentNullException(nameof(storageProvider));

  private readonly ICryptoProvider _cryptoProvider = cryptoProvider
    ?? throw new ArgumentNullException(nameof(cryptoProvider));

  private readonly ILoggerFactory _loggerFactory = loggerFactory
    ?? throw new ArgumentNullException(nameof(loggerFactory));

  public IConfigurationProvider Build(IConfigurationBuilder builder)
  {
    var logger = _loggerFactory.CreateLogger<SecureConfigProvider>();
    return new SecureConfigProvider(_storageProvider, _cryptoProvider, logger);
  }
}