namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

internal sealed class AesCryptoProvider(IEncryptionKeyProvider keyProvider) : ICryptoProvider
{
  private readonly IEncryptionKeyProvider _keyProvider = keyProvider
    ?? throw new ArgumentNullException(nameof(keyProvider));

  // TODO: Implement this shit
  
  public string Decrypt(string cipherText)
  {
    _ = _keyProvider.GetKey();
    throw new NotImplementedException();
  }

  public string Encrypt(string plainText)
  {
    throw new NotImplementedException();
  }
}