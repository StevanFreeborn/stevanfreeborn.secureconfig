using System.Text.Json;
using System.Text.Json.Serialization;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cli.Json;

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(JsonElement))]
internal sealed partial class CliJsonContext : JsonSerializerContext
{
}