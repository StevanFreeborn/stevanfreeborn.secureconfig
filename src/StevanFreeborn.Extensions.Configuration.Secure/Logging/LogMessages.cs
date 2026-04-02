using Microsoft.Extensions.Logging;

internal static partial class LogMessages
{
  [LoggerMessage(
      EventId = 1,
      Level = LogLevel.Warning,
      Message = "Failed to retrieve hardware-specific machine ID. Falling back to environment variables."
  )]
  public static partial void LogFailedRetrievingMachineId(this ILogger logger, Exception ex);

  [LoggerMessage(
      EventId = 2,
      Level = LogLevel.Information,
      Message = "Using fallback strategy for Machine ID generation."
  )]
  public static partial void LogUsingFallbackStrategy(this ILogger logger);

  [LoggerMessage(
      EventId = 3,
      Level = LogLevel.Warning,
      Message = "Failed to decrypt value for key {Key}"
  )]
  public static partial void LogDecryptionFailure(this ILogger logger, Exception ex, string key);
}