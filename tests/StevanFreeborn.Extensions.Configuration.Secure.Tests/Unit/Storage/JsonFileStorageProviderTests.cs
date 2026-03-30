using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Storage;

public class JsonFileStorageProviderTests : IDisposable
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
      FileName = "testsettings.json",
      DirectoryPath = _tmpDirectory,
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
    var configKey = "KeyA";
    var configValue = "Value";

    await _sut.WriteAsync(configKey, configValue);
    var result = await _sut.ReadAsync(configKey);

    result.Should().Be(configValue);
    File.Exists(_options.FullPath).Should().BeTrue();
  }

  [Fact]
  public async Task ReadAllAsync_WhenCalled_ItShouldReturnAllStoredKeys()
  {
    await _sut.WriteAsync("Key1", "Val1");
    await _sut.WriteAsync("Key2", "Val2");

    var allData = await _sut.ReadAllAsync();

    allData.Should().BeEquivalentTo(new Dictionary<string, string>()
    {
      ["Key1"] = "Val1",
      ["Key2"] = "Val2",
    });
  }

  [Fact]
  public async Task DeleteAsync_WhenCalled_ItShouldRemoveKey()
  {
    await _sut.WriteAsync("KeyToDelete", "SomeValue");

    var deleteResult = await _sut.DeleteAsync("KeyToDelete");
    var readResult = await _sut.ReadAsync("KeyToDelete");

    deleteResult.Should().BeTrue();
    readResult.Should().BeEmpty();
  }

  public void Dispose()
  {
    if (Directory.Exists(_tmpDirectory))
    {
      Directory.Delete(_tmpDirectory, true);
    }

    GC.SuppressFinalize(this);
  }
}