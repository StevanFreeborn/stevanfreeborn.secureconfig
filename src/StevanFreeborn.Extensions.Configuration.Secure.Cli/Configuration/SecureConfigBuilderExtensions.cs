using StevanFreeborn.Extensions.Configuration.Secure.Configuration;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Configuration;

internal static class SecureConfigBuilderExtensions
{
  public static ISecureConfigBuilder ApplyProfile(
    this ISecureConfigBuilder builder,
    SecureConfigProfile profile
  )
  {
    ConfigureStorage(builder, profile.Storage);
    ConfigureKey(builder, profile.Key);
    ConfigureCrypto(builder, profile.Crypto);

    return builder;
  }

  private static void ConfigureStorage(ISecureConfigBuilder builder, StorageProviderOptions options)
  {
#pragma warning disable CA1308 // Normalize strings to uppercase
    switch (options.Type.ToLowerInvariant())
#pragma warning restore CA1308 // Normalize strings to uppercase
    {
      case StorageProviderTypes.Json:
        builder.UseJsonFileStorage(opt =>
        {
          opt.DirectoryPath = options.Json.DirectoryPath;
          opt.FileName = options.Json.FileName;
        });
        break;
      default:
        throw new NotSupportedException($"Storage provider type '{options.Type}' is not supported.");
    }
  }

  private static void ConfigureKey(ISecureConfigBuilder builder, KeyProviderOptions options)
  {
#pragma warning disable CA1308 // Normalize strings to uppercase
    switch (options.Type.ToLowerInvariant())
#pragma warning restore CA1308 // Normalize strings to uppercase
    {
      case KeyProviderTypes.MachineId:
        builder.WithMachineIdKey();
        break;
      case KeyProviderTypes.Base64:
        builder.WithBase64EncryptionKey(options.Base64.Key);
        break;
      default:
        throw new NotSupportedException($"Key provider type '{options.Type}' is not supported.");
    }
  }

  private static void ConfigureCrypto(ISecureConfigBuilder builder, CryptoProviderOptions options)
  {
#pragma warning disable CA1308 // Normalize strings to uppercase
    switch (options.Type.ToLowerInvariant())
#pragma warning restore CA1308 // Normalize strings to uppercase
    {
      case CryptoProviderTypes.Aes:
        builder.WithAesCryptoProvider();
        break;
      default:
        throw new NotSupportedException($"Crypto provider type '{options.Type}' is not supported.");
    }
  }
}