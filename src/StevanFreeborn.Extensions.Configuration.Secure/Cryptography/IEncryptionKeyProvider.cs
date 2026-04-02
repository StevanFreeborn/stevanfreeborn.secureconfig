namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

/// <summary>
/// Defines a contract for providing encryption keys used to secure configuration data.
/// </summary>
public interface IEncryptionKeyProvider
{
  /// <summary>
  /// Retrieves the encryption key as a byte array.
  /// </summary>
  /// <returns>A byte array containing the encryption key.</returns>
  byte[] GetKey();
}