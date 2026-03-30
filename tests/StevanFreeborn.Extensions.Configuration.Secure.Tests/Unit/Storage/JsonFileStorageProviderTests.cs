using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Storage;

public class JsonFileStorageProviderTests
{
  private readonly string _tmpDirectory;
  private readonly JsonStorageOptions _options;
  private readonly JsonFileStorageProvider _sut;

  public JsonFileStorageProviderTests()
  {
    _tmpDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(_tmpDirectory);

    _options = new()
    {
      FileName = "testsettings.json"
    };

    _sut = new(_options);
  }

  [Fact]
  public void Constructor_WhenCalledWithNullOptions_ItShouldThrowArgumentNullException()
  {
    var act = static () => new JsonFileStorageProvider(null!);

    act.Should().Throw<ArgumentNullException>();
  }

  [Fact]
  public async Task WriteAsync_And_ReadAsync_WhenCalled_ItShouldBeAbleToPersistAndRetrieveValues()
  {
    var configKey = "Key";
    var configValue = "Value";

    await _sut.WriteAsync(configKey, configValue);
    var result = await _sut.ReadAsync(configKey);

    result.Should().Be(configValue);
    File.Exists(_options.FullPath).Should().BeTrue();
  }
}
