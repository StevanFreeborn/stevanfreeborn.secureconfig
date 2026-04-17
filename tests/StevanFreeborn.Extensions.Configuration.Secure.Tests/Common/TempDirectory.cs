namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Common;

internal sealed class TempDirectory : IDisposable
{
  public string Path { get; }

  public TempDirectory()
  {
    Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(Path);
  }

  public void Dispose()
  {
    if (Directory.Exists(Path))
    {
      Directory.Delete(Path, true);
    }
  }
}