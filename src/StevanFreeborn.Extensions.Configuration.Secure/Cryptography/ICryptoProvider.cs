namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

internal interface ICryptoProvider
{
  string Encrypt(string plainText);
  string Decrypt(string cipherText);
}