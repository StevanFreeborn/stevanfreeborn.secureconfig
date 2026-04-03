namespace StevanFreeborn.Extensions.Configuration.Secure.Sample;

public sealed record SmtpSettings
{
  public string Host { get; init; } = string.Empty;
  public int Port { get; init; }
  public string Username { get; init; } = string.Empty;
  public string Password { get; init; } = string.Empty;
  public bool UseSsl { get; init; }
}
