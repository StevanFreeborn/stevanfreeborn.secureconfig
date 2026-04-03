namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

/// <summary>
/// Defines the contract for cryptographic operations.
/// </summary>
public interface ICryptoProvider
{
  /// <summary>
  /// Encrypts the provided plain text.
  /// </summary>
  /// <param name="plainText">The unencrypted string to be encrypted.</param>
  /// <returns>The encrypted cipher text. If <paramref name="plainText"/> is null, empty, or whitespace, it is returned unchanged.</returns>
  string Encrypt(string plainText);

  /// <summary>
  /// Decrypts the provided cipher text.
  /// </summary>
  /// <param name="cipherText">The encrypted string to be decrypted.</param>
  /// <returns>The decrypted plain text. If <paramref name="cipherText"/> is null, empty, or whitespace, it is returned unchanged.</returns>
  string Decrypt(string cipherText);
}