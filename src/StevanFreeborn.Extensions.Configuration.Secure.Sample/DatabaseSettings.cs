namespace StevanFreeborn.Extensions.Configuration.Secure.Sample;

public sealed record DatabaseSettings
{
  public string ConnectionString { get; init; } = string.Empty;
  public int Timeout { get; init; }
  public int RetryCount { get; init; }
}
