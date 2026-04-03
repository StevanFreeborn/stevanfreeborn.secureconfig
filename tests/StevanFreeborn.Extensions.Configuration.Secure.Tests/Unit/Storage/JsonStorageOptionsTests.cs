using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Storage;

public class JsonStorageOptionsTests
{
  [Fact]
  public void Path_WhenCalledWithFileNameSet_ItShouldReturnExpectedPath()
  {
    var fileName = "appsettings.json";
    var expectedPath = Path.Combine(AppContext.BaseDirectory, fileName);

    var opts = new JsonStorageOptions()
    {
      FileName = fileName,
    };

    opts.FullPath.Should().Be(expectedPath);
  }

  [Fact]
  public void Path_WhenCalledWithFileNameAndDirectoryPathSet_ItShouldReturnExpectedPath()
  {
    var fileName = "appsettings.json";
    var directoryPath = @"C:\Path\To\Some\Directory";
    var expectedPath = Path.Combine(directoryPath, fileName);

    var opts = new JsonStorageOptions()
    {
      FileName = fileName,
      DirectoryPath = directoryPath,
    };

    opts.FullPath.Should().Be(expectedPath);
  }
}