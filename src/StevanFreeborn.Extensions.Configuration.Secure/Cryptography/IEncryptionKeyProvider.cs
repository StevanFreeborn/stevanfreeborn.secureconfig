namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

internal interface IEncryptionKeyProvider
{
  byte[] GetKey();
}