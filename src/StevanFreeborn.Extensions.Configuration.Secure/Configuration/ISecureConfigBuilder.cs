using Microsoft.Extensions.DependencyInjection;

namespace StevanFreeborn.Extensions.Configuration.Secure.Configuration;

/// <summary>
/// Provides a builder interface for configuring secure configuration services.
/// </summary>
public interface ISecureConfigBuilder
{
  /// <summary>
  /// Gets the <see cref="IServiceCollection"/> used to register secure configuration services.
  /// </summary>
  IServiceCollection Services { get; }
}