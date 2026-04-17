using System.Text.Json.Serialization;

namespace StevanFreeborn.Extensions.Configuration.Secure.Sample;

[JsonSerializable(typeof(ApiOptions))]
[JsonSerializable(typeof(DatabaseSettings))]
[JsonSerializable(typeof(SmtpSettings))]
internal partial class AppJsonContext : JsonSerializerContext
{
}