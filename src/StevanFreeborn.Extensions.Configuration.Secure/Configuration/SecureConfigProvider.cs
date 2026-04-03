using System.Globalization;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

internal class SecureConfigProvider : ConfigurationProvider, IDisposable
{
  private readonly ISecureStorageProvider _storageProvider;
  private readonly ICryptoProvider _cryptoProvider;
  private readonly ILogger<SecureConfigProvider> _logger;

  public SecureConfigProvider(
    ISecureStorageProvider storageProvider,
    ICryptoProvider cryptoProvider,
    ILogger<SecureConfigProvider> logger
  )
  {
    _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider));
    _cryptoProvider = cryptoProvider ?? throw new ArgumentNullException(nameof(cryptoProvider));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    _storageProvider.StorageChanged += HandleStorageChangedAsync;
  }

  public override void Load()
  {
#pragma warning disable CA1849 // Call async methods when in an async method
    var encryptedData = _storageProvider.ReadAllAsync().GetAwaiter().GetResult();
#pragma warning restore CA1849 // Call async methods when in an async method
    Data = ProcessAndDecryptData(encryptedData);
  }

  public void Dispose()
  {
    _storageProvider.StorageChanged -= HandleStorageChangedAsync;
  }

  private async void HandleStorageChangedAsync(object? sender, EventArgs e)
  {
    try
    {
      var encryptedData = await _storageProvider.ReadAllAsync().ConfigureAwait(false);

      var newData = ProcessAndDecryptData(encryptedData);

      Data = newData;

      OnReload();
    }
#pragma warning disable CA1031 // Do not catch general exception types
    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
    {
      _logger.LogFailedToReloadSecureConfig(ex);
    }
  }

  private Dictionary<string, string?> ProcessAndDecryptData(IDictionary<string, string> encryptedData)
  {
    var flattenedData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    foreach (var kvp in encryptedData)
    {
      try
      {
        var decryptedJson = _cryptoProvider.Decrypt(kvp.Value);
        using var document = JsonDocument.Parse(decryptedJson);
        FlattenJsonElement(flattenedData, document.RootElement, kvp.Key);
      }
#pragma warning disable CA1031 // Do not catch general exception types
      catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
      {
        _logger.LogDecryptionFailure(ex, kvp.Key);
      }
    }

    return flattenedData;
  }

  private static void FlattenJsonElement(IDictionary<string, string?> data, JsonElement element, string currentKey)
  {
    switch (element.ValueKind)
    {
      case JsonValueKind.Object:
        foreach (var property in element.EnumerateObject())
        {
          var newKey = string.IsNullOrEmpty(currentKey) ? property.Name : ConfigurationPath.Combine(currentKey, property.Name);
          FlattenJsonElement(data, property.Value, newKey);
        }
        break;
      case JsonValueKind.Array:
        var index = 0;
        foreach (var arrayElement in element.EnumerateArray())
        {
          var newKey = ConfigurationPath.Combine(currentKey, index.ToString(CultureInfo.InvariantCulture));
          FlattenJsonElement(data, arrayElement, newKey);
          index++;
        }
        break;
      case JsonValueKind.String:
        data[currentKey] = element.GetString();
        break;
      case JsonValueKind.Number:
        data[currentKey] = element.GetRawText();
        break;
      case JsonValueKind.True:
        data[currentKey] = "true";
        break;
      case JsonValueKind.False:
        data[currentKey] = "false";
        break;
      case JsonValueKind.Null:
      case JsonValueKind.Undefined:
      default:
        data[currentKey] = null;
        break;
    }
  }
}