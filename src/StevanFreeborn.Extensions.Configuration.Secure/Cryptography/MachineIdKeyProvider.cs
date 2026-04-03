using System.Security.Cryptography;
using System.Text;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

internal sealed class MachineIdKeyProvider(IMachineIdKeyGenerator generator) : IEncryptionKeyProvider
{
  private readonly IMachineIdKeyGenerator _generator = generator;

  public byte[] GetKey()
  {
    var machineId = _generator.GetId();
    var bytes = Encoding.UTF8.GetBytes(machineId);
#if NET5_0_OR_GREATER
    return SHA256.HashData(bytes);
#else
    using var sha256 = SHA256.Create();
    return sha256.ComputeHash(bytes);
#endif
  }
}