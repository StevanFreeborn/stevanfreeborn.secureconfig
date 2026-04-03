using System.Security.Cryptography;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Common;

internal static class KeyGenerator
{
  public static string GetValidBase64Key()
  {
    var key = new byte[32];
    RandomNumberGenerator.Fill(key);
    return Convert.ToBase64String(key);
  }
}