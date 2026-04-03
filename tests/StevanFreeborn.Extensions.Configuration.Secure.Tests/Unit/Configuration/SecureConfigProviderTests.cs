using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using StevanFreeborn.Extensions.Configuration.Secure.Configuration;
using StevanFreeborn.Extensions.Configuration.Secure.Cryptography;
using StevanFreeborn.Extensions.Configuration.Secure.Storage;

namespace StevanFreeborn.Extensions.Configuration.Secure.Tests.Unit.Configuration;

public class SecureConfigProviderTests
{
  private readonly Mock<ISecureStorageProvider> _mockStorage = new();
  private readonly Mock<ICryptoProvider> _mockCrypto = new();
  private readonly Mock<ILogger<SecureConfigProvider>> _mockLogger = new();
  private readonly SecureConfigProvider _sut;

  public SecureConfigProviderTests()
  {
    _sut = new(_mockStorage.Object, _mockCrypto.Object, _mockLogger.Object);
  }

  private static string Key(params string[] segments) => ConfigurationPath.Combine(segments);

  [Fact]
  public void Load_WhenCalled_ItShouldReadDecryptAndFlattenJsonData()
  {
    var rootKey = "DatabaseOptions";
    var rawJson = @"{ ""Host"": ""localhost"", ""Port"": 5432 }";
    var encryptedString = "encrypted_payload";

    var storedData = new Dictionary<string, string>
    {
      { rootKey, encryptedString },
    };

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(storedData);
    _mockCrypto.Setup(m => m.Decrypt(encryptedString)).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("DatabaseOptions", "Host"), out var hostVal).Should().BeTrue();
    hostVal.Should().Be("localhost");

    _sut.TryGet(Key("DatabaseOptions", "Port"), out var portVal).Should().BeTrue();
    portVal.Should().Be("5432");
  }

  [Fact]
  public void Load_WhenStorageIsEmpty_ItShouldNotThrow()
  {
    _mockStorage.Setup(s => s.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string>());

    var act = _sut.Load;
    act.Should().NotThrow();
  }

  [Fact]
  public void Load_WhenDecryptionFails_ItShouldLogErrorAndContinue()
  {
    var storedData = new Dictionary<string, string>
    {
      { "ValidKey", "encrypted_valid" },
      { "BadKey", "encrypted_bad" },
    };

    _mockLogger.Setup(m => m.IsEnabled(LogLevel.Warning)).Returns(true);
    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(storedData);
    _mockCrypto.Setup(m => m.Decrypt("encrypted_valid")).Returns(@"{ ""Name"": ""test"" }");
    _mockCrypto.Setup(m => m.Decrypt("encrypted_bad")).Throws(new InvalidOperationException("Decryption failed"));

    _sut.Load();

    _sut.TryGet(Key("ValidKey", "Name"), out var val).Should().BeTrue();
    val.Should().Be("test");

    _mockLogger.Verify(logger => logger.Log(
        LogLevel.Warning,
        It.Is<EventId>(id => id.Id == 3),
        It.Is<It.IsAnyType>((state, type) => state.ToString()!.Contains("Failed to decrypt value for key")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
      ),
      Times.Once()
    );
  }

  [Fact]
  public void Load_WhenCalledWithDeeplyNestedObject_ItShouldFlattenCorrectly()
  {
    var rawJson = @"{
      ""Level1"": {
        ""Level2"": {
          ""Level3"": {
            ""Value"": ""deep_value""
          }
        }
      }
    }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Root", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Root", "Level1", "Level2", "Level3", "Value"), out var val).Should().BeTrue();
    val.Should().Be("deep_value");
  }

  [Fact]
  public void Load_WhenCalledWithArrayOfPrimitives_ItShouldFlattenWithIndices()
  {
    var rawJson = @"{ ""Tags"": [""alpha"", ""beta"", ""gamma""] }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Config", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Config", "Tags", "0"), out var v0).Should().BeTrue();
    v0.Should().Be("alpha");

    _sut.TryGet(Key("Config", "Tags", "1"), out var v1).Should().BeTrue();
    v1.Should().Be("beta");

    _sut.TryGet(Key("Config", "Tags", "2"), out var v2).Should().BeTrue();
    v2.Should().Be("gamma");
  }

  [Fact]
  public void Load_WhenCalledWithArrayOfObjects_ItShouldFlattenCorrectly()
  {
    var rawJson = @"{
      ""Servers"": [
        { ""Host"": ""srv1"", ""Port"": 8080 },
        { ""Host"": ""srv2"", ""Port"": 9090 }
      ]
    }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "App", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("App", "Servers", "0", "Host"), out var h0).Should().BeTrue();
    h0.Should().Be("srv1");

    _sut.TryGet(Key("App", "Servers", "0", "Port"), out var p0).Should().BeTrue();
    p0.Should().Be("8080");

    _sut.TryGet(Key("App", "Servers", "1", "Host"), out var h1).Should().BeTrue();
    h1.Should().Be("srv2");

    _sut.TryGet(Key("App", "Servers", "1", "Port"), out var p1).Should().BeTrue();
    p1.Should().Be("9090");
  }

  [Fact]
  public void Load_WhenCalledWithNestedArrays_ItShouldFlattenCorrectly()
  {
    var rawJson = @"{ ""Matrix"": [[1, 2], [3, 4]] }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Data", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Data", "Matrix", "0", "0"), out var v00).Should().BeTrue();
    v00.Should().Be("1");

    _sut.TryGet(Key("Data", "Matrix", "0", "1"), out var v01).Should().BeTrue();
    v01.Should().Be("2");

    _sut.TryGet(Key("Data", "Matrix", "1", "0"), out var v10).Should().BeTrue();
    v10.Should().Be("3");

    _sut.TryGet(Key("Data", "Matrix", "1", "1"), out var v11).Should().BeTrue();
    v11.Should().Be("4");
  }

  [Fact]
  public void Load_WhenCalledWithMixedArrayTypes_ItShouldFlattenCorrectly()
  {
    var rawJson = @"{
      ""Mixed"": [
        ""string_val"",
        42,
        true,
        null,
        { ""Nested"": ""obj"" }
      ]
    }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Cfg", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Cfg", "Mixed", "0"), out var s).Should().BeTrue();
    s.Should().Be("string_val");

    _sut.TryGet(Key("Cfg", "Mixed", "1"), out var n).Should().BeTrue();
    n.Should().Be("42");

    _sut.TryGet(Key("Cfg", "Mixed", "2"), out var b).Should().BeTrue();
    b.Should().Be("true");

    _sut.TryGet(Key("Cfg", "Mixed", "3"), out var nl).Should().BeTrue();
    nl.Should().BeNull();

    _sut.TryGet(Key("Cfg", "Mixed", "4", "Nested"), out var o).Should().BeTrue();
    o.Should().Be("obj");
  }

  [Fact]
  public void Load_WhenCalledWithBooleanValues_ItShouldFlattenAsStrings()
  {
    var rawJson = @"{ ""Enabled"": true, ""Disabled"": false }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Flags", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Flags", "Enabled"), out var en).Should().BeTrue();
    en.Should().Be("true");

    _sut.TryGet(Key("Flags", "Disabled"), out var dis).Should().BeTrue();
    dis.Should().Be("false");
  }

  [Fact]
  public void Load_WhenCalledWithNullValue_ItShouldFlattenAsNull()
  {
    var rawJson = @"{ ""NullableField"": null }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Opts", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Opts", "NullableField"), out var val).Should().BeTrue();
    val.Should().BeNull();
  }

  [Fact]
  public void Load_WhenCalledWithNumberFormats_ItShouldPreserveRawText()
  {
    var rawJson = @"{
      ""Integer"": 42,
      ""Float"": 3.14,
      ""Negative"": -7,
      ""Scientific"": 1.5e10,
      ""Zero"": 0
    }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Nums", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Nums", "Integer"), out var i).Should().BeTrue();
    i.Should().Be("42");

    _sut.TryGet(Key("Nums", "Float"), out var f).Should().BeTrue();
    f.Should().Be("3.14");

    _sut.TryGet(Key("Nums", "Negative"), out var n).Should().BeTrue();
    n.Should().Be("-7");

    _sut.TryGet(Key("Nums", "Scientific"), out var s).Should().BeTrue();
    s.Should().Be("1.5e10");

    _sut.TryGet(Key("Nums", "Zero"), out var z).Should().BeTrue();
    z.Should().Be("0");
  }

  [Fact]
  public void Load_WhenCalledWithEmptyObject_ItShouldNotAddKeys()
  {
    var rawJson = @"{ ""Empty"": {} }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Cfg", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Cfg", "Empty"), out _).Should().BeFalse();
  }

  [Fact]
  public void Load_WhenCalledWithEmptyArray_ItShouldNotAddKeys()
  {
    var rawJson = @"{ ""Empty"": [] }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Cfg", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Cfg", "Empty"), out _).Should().BeFalse();
  }

  [Fact]
  public void Load_WhenCalledWithEmptyString_ItShouldFlattenAsEmptyString()
  {
    var rawJson = @"{ ""Blank"": """" }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Cfg", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Cfg", "Blank"), out var val).Should().BeTrue();
    val.Should().Be("");
  }

  [Fact]
  public void Load_WhenCalledWithMultipleEncryptedKeys_ItShouldMergeData()
  {
    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string>
    {
      { "Db", "enc1" },
      { "Cache", "enc2" },
    });
    _mockCrypto.Setup(m => m.Decrypt("enc1")).Returns(@"{ ""Host"": ""db.local"" }");
    _mockCrypto.Setup(m => m.Decrypt("enc2")).Returns(@"{ ""Host"": ""cache.local"", ""Ttl"": 300 }");

    _sut.Load();

    _sut.TryGet(Key("Db", "Host"), out var dbHost).Should().BeTrue();
    dbHost.Should().Be("db.local");

    _sut.TryGet(Key("Cache", "Host"), out var cacheHost).Should().BeTrue();
    cacheHost.Should().Be("cache.local");

    _sut.TryGet(Key("Cache", "Ttl"), out var ttl).Should().BeTrue();
    ttl.Should().Be("300");
  }

  [Fact]
  public void Load_WhenCalledWithCaseInsensitiveKeys_ItShouldRetrieveCorrectly()
  {
    var rawJson = @"{ ""MyKey"": ""value"" }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Cfg", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("cfg", "mykey"), out var val).Should().BeTrue();
    val.Should().Be("value");

    _sut.TryGet(Key("CFG", "MYKEY"), out val).Should().BeTrue();
    val.Should().Be("value");
  }

  [Fact]
  public void Load_WhenCalledWithComplexNestedStructure_ItShouldFlattenCorrectly()
  {
    var rawJson = @"{
      ""App"": {
        ""Name"": ""MyApp"",
        ""Version"": ""1.0.0"",
        ""Features"": {
          ""EnableLogging"": true,
          ""LogLevel"": ""Debug"",
          ""Targets"": [""Console"", ""File""]
        },
        ""Database"": {
          ""Primary"": {
            ""ConnectionString"": ""Server=db1;Database=app"",
            ""PoolSize"": 10,
            ""Replicas"": [
              { ""Host"": ""replica1"", ""Port"": 5432, ""Active"": true },
              { ""Host"": ""replica2"", ""Port"": 5433, ""Active"": false }
            ]
          },
          ""ReadOnly"": {
            ""ConnectionString"": ""Server=db2;Database=app_ro"",
            ""PoolSize"": 5
          }
        },
        ""Metadata"": null,
        ""Tags"": [""prod"", ""v1"", { ""Region"": ""us-east"" }]
      }
    }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Root", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Root", "App", "Name"), out var name).Should().BeTrue();
    name.Should().Be("MyApp");

    _sut.TryGet(Key("Root", "App", "Version"), out var ver).Should().BeTrue();
    ver.Should().Be("1.0.0");

    _sut.TryGet(Key("Root", "App", "Features", "EnableLogging"), out var log).Should().BeTrue();
    log.Should().Be("true");

    _sut.TryGet(Key("Root", "App", "Features", "LogLevel"), out var lvl).Should().BeTrue();
    lvl.Should().Be("Debug");

    _sut.TryGet(Key("Root", "App", "Features", "Targets", "0"), out var t0).Should().BeTrue();
    t0.Should().Be("Console");

    _sut.TryGet(Key("Root", "App", "Features", "Targets", "1"), out var t1).Should().BeTrue();
    t1.Should().Be("File");

    _sut.TryGet(Key("Root", "App", "Database", "Primary", "ConnectionString"), out var cs).Should().BeTrue();
    cs.Should().Be("Server=db1;Database=app");

    _sut.TryGet(Key("Root", "App", "Database", "Primary", "PoolSize"), out var ps).Should().BeTrue();
    ps.Should().Be("10");

    _sut.TryGet(Key("Root", "App", "Database", "Primary", "Replicas", "0", "Host"), out var rh0).Should().BeTrue();
    rh0.Should().Be("replica1");

    _sut.TryGet(Key("Root", "App", "Database", "Primary", "Replicas", "0", "Port"), out var rp0).Should().BeTrue();
    rp0.Should().Be("5432");

    _sut.TryGet(Key("Root", "App", "Database", "Primary", "Replicas", "0", "Active"), out var ra0).Should().BeTrue();
    ra0.Should().Be("true");

    _sut.TryGet(Key("Root", "App", "Database", "Primary", "Replicas", "1", "Host"), out var rh1).Should().BeTrue();
    rh1.Should().Be("replica2");

    _sut.TryGet(Key("Root", "App", "Database", "Primary", "Replicas", "1", "Active"), out var ra1).Should().BeTrue();
    ra1.Should().Be("false");

    _sut.TryGet(Key("Root", "App", "Database", "ReadOnly", "ConnectionString"), out var csRo).Should().BeTrue();
    csRo.Should().Be("Server=db2;Database=app_ro");

    _sut.TryGet(Key("Root", "App", "Database", "ReadOnly", "PoolSize"), out var psRo).Should().BeTrue();
    psRo.Should().Be("5");

    _sut.TryGet(Key("Root", "App", "Metadata"), out var meta).Should().BeTrue();
    meta.Should().BeNull();

    _sut.TryGet(Key("Root", "App", "Tags", "0"), out var tag0).Should().BeTrue();
    tag0.Should().Be("prod");

    _sut.TryGet(Key("Root", "App", "Tags", "1"), out var tag1).Should().BeTrue();
    tag1.Should().Be("v1");

    _sut.TryGet(Key("Root", "App", "Tags", "2", "Region"), out var region).Should().BeTrue();
    region.Should().Be("us-east");
  }

  [Fact]
  public void Load_WhenCalledWithSpecialCharactersInStrings_ItShouldPreserveContent()
  {
    var rawJson = @"{
      ""Connection"": ""Server=localhost;Database=test;User=admin;Password=p@$$w0rd!"",
      ""Path"": ""C:\\Program Files\\App\\config.json"",
      ""JsonInString"": ""{\""inner\"": \""value\""}"",
      ""Unicode"": ""Hello \u4e16\u754c""
    }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Cfg", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Cfg", "Connection"), out var conn).Should().BeTrue();
    conn.Should().Be("Server=localhost;Database=test;User=admin;Password=p@$$w0rd!");

    _sut.TryGet(Key("Cfg", "Path"), out var path).Should().BeTrue();
    path.Should().Be("C:\\Program Files\\App\\config.json");

    _sut.TryGet(Key("Cfg", "JsonInString"), out var jis).Should().BeTrue();
    jis.Should().Be("{\"inner\": \"value\"}");

    _sut.TryGet(Key("Cfg", "Unicode"), out var uni).Should().BeTrue();
    uni.Should().Be("Hello 世界");
  }

  [Fact]
  public void Load_WhenCalledWithArrayOfEmptyObjects_ItShouldNotAddKeys()
  {
    var rawJson = @"{ ""Items"": [{}, {}] }";

    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Cfg", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns(rawJson);

    _sut.Load();

    _sut.TryGet(Key("Cfg", "Items", "0"), out _).Should().BeFalse();
    _sut.TryGet(Key("Cfg", "Items", "1"), out _).Should().BeFalse();
  }

  [Fact]
  public void Load_WhenCalledWithInvalidJson_ItShouldLogErrorAndContinue()
  {
    _mockLogger.Setup(m => m.IsEnabled(LogLevel.Warning)).Returns(true);
    _mockStorage.Setup(m => m.ReadAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<string, string> { { "Bad", "enc" } });
    _mockCrypto.Setup(m => m.Decrypt("enc")).Returns("not valid json{{{");

    _sut.Load();

    _mockLogger.Verify(x => x.Log(
        LogLevel.Warning,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Bad")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()
      ),
      Times.Once()
    );
  }
}