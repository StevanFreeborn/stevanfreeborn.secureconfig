using System.Text.Json.Serialization;

namespace StevanFreeborn.Extensions.Configuration.Secure.Storage;

[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class SecureConfigJsonContext : JsonSerializerContext;