using System.Text.Json.Serialization;

namespace StevanFreeborn.Extensions.Configuration.Secure.Sample;

[JsonSerializable(typeof(ApiOptions))]
internal partial class AppJsonContext : JsonSerializerContext
{
}