namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

internal sealed class StaticKeyProvider : IEncryptionKeyProvider
{
  private readonly byte[] _key;

  public StaticKeyProvider(string base64Key)
  {
    if (string.IsNullOrWhiteSpace(base64Key))
    {
      throw new ArgumentNullException(nameof(base64Key));
    }

    byte[] decodedKey;

    try
    {
      decodedKey = Convert.FromBase64String(base64Key);
    }
    catch (FormatException ex)
    {
      throw new ArgumentException("The provided key is not a valid Base64 string.", nameof(base64Key), ex);
    }

    if (decodedKey.Length != 32)
    {
      throw new ArgumentException("The encryption key must be exactly 32 bytes (256 bits) for AES-256 encryption.", nameof(base64Key));
    }

    _key = decodedKey;
  }

  public byte[] GetKey()
  {
    return _key;
  }
}