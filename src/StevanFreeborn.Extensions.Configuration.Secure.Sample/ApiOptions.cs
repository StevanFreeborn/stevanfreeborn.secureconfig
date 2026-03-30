namespace StevanFreeborn.Extensions.Configuration.Secure.Sample;

public sealed record ApiOptions
{
  public string ApiKey { get; init; } = string.Empty;
}