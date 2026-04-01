using System.Security.Cryptography;
using System.Text;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

internal sealed class AesCryptoProvider(IEncryptionKeyProvider keyProvider) : ICryptoProvider
{
  private const int NonceSize = 12;
  private const int TagSize = 16;

  private readonly IEncryptionKeyProvider _keyProvider = keyProvider
    ?? throw new ArgumentNullException(nameof(keyProvider));

  public string Encrypt(string plainText)
  {
    if (string.IsNullOrWhiteSpace(plainText))
    {
      return plainText;
    }

    var key = _keyProvider.GetKey();
    var plainBytes = Encoding.UTF8.GetBytes(plainText);

    var nonce = new byte[NonceSize].AsSpan();
    RandomNumberGenerator.Fill(nonce);

    var tag = new byte[TagSize].AsSpan();

    var cipherBytes = new byte[plainBytes.Length];

    using var aesGcm = new AesGcm(key);
    aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

    var combinedBytes = new byte[NonceSize + TagSize + plainBytes.Length];
    nonce.CopyTo(combinedBytes.AsSpan(0, NonceSize));
    tag.CopyTo(combinedBytes.AsSpan(NonceSize, TagSize));
    cipherBytes.CopyTo(combinedBytes.AsSpan(NonceSize + TagSize));

    return Convert.ToBase64String(combinedBytes);
  }

  public string Decrypt(string cipherText)
  {
    if (string.IsNullOrWhiteSpace(cipherText))
    {
      return cipherText;
    }

    var key = _keyProvider.GetKey();
    var combinedBytes = Convert.FromBase64String(cipherText).AsSpan();

    if (combinedBytes.Length < NonceSize + TagSize)
    {
      throw new CryptographicException($"Invalid playload. {nameof(cipherText)} is not of expected length");
    }

    var nonce = combinedBytes[..NonceSize];
    var tag = combinedBytes.Slice(NonceSize, TagSize);
    var cipherBytes = combinedBytes[(NonceSize + TagSize)..];
    var plainBytes = new byte[cipherBytes.Length];

    using var aesGcm = new AesGcm(key);
    aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

    return Encoding.UTF8.GetString(plainBytes);
  }
}