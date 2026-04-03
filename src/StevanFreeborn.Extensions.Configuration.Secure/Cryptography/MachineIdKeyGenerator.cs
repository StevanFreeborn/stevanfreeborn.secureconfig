using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Extensions.Logging;

namespace StevanFreeborn.Extensions.Configuration.Secure.Cryptography;

internal sealed class MachineIdKeyGenerator : IMachineIdKeyGenerator
{
  private const string IOPlatformUUID = nameof(IOPlatformUUID);
  private const string MachineGuid = nameof(MachineGuid);
  private const string WinRegistryPath = @"SOFTWARE\Microsoft\Cryptography";
  private readonly ILogger<MachineIdKeyGenerator> _logger;
  private readonly Lazy<string> _machineId;

  public MachineIdKeyGenerator(ILogger<MachineIdKeyGenerator> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _machineId = new(GenerateMachineId);
  }

  public string GetId()
  {
    return _machineId.Value;
  }

  private string GenerateMachineId()
  {
    try
    {
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
        using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(WinRegistryPath);
        var guid = key?.GetValue(MachineGuid)?.ToString();

        if (string.IsNullOrWhiteSpace(guid) is false)
        {
          return guid;
        }
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        const string machineIdPath = "/etc/machine-id";
        if (File.Exists(machineIdPath))
        {
          return File.ReadAllText(machineIdPath).Trim();
        }
      }

      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        var startInfo = new ProcessStartInfo
        {
          FileName = "ioreg",
          Arguments = "-rd1 -c IOPlatformExpertDevice",
          RedirectStandardOutput = true,
          UseShellExecute = false,
          CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        using var reader = process?.StandardOutput;
        var output = reader?.ReadToEnd();

        if (output != null && output.Contains(IOPlatformUUID, StringComparison.OrdinalIgnoreCase))
        {
          var parts = output.Split([IOPlatformUUID], StringSplitOptions.None);

          if (parts.Length > 1)
          {
            var idPart = parts[1].Split('\"');

            if (idPart.Length > 3)
            {
              return idPart[3];
            }
          }
        }
      }
    }
#pragma warning disable CA1031
    catch (Exception ex)
#pragma warning restore CA1031
    {
      _logger.LogFailedRetrievingMachineId(ex);
    }

    _logger.LogUsingFallbackStrategy();
    return $"{Environment.MachineName}_{Environment.UserName}";
  }
}