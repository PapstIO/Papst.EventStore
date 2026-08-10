#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Papst.EventStore.Testing;

/// <summary>
/// Configures Testcontainers to talk to a Podman engine when Docker is not the active
/// container runtime, so the integration tests run unchanged from:
/// <list type="bullet">
///   <item>Windows / Visual Studio with Podman Desktop (WSL backend),</item>
///   <item>macOS with a Podman machine,</item>
///   <item>a Linux GitHub Actions runner (Docker by default, or rootless Podman).</item>
/// </list>
/// The work runs once at assembly load - before Testcontainers reads its settings - and only
/// fills in configuration that has not been provided explicitly. An existing <c>DOCKER_HOST</c>
/// or a plain Docker environment is therefore left completely untouched, so nothing changes on
/// a Docker-based CI runner.
/// </summary>
/// <remarks>
/// This file is linked into every integration-test project that uses Testcontainers, so each
/// test assembly gets its own module initializer.
/// </remarks>
internal static class TestcontainersRuntime
{
  private static int _initialized;

  [ModuleInitializer]
  internal static void Initialize()
  {
    if (Interlocked.Exchange(ref _initialized, 1) != 0)
    {
      return;
    }

    try
    {
      Configure();
    }
    catch
    {
      // Detection is best-effort: on any failure fall back to the Testcontainers defaults
      // rather than breaking the test run.
    }
  }

  private static void Configure()
  {
    // Respect an explicitly configured endpoint (CI, custom developer setups): if the caller
    // already told Testcontainers where the engine is, do not second-guess it.
    if (HasValue("DOCKER_HOST"))
    {
      return;
    }

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      ConfigureWindows();
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      ConfigureMacOs();
    }
    else
    {
      ConfigureLinux();
    }
  }

  private static void ConfigureWindows()
  {
    // Podman Desktop runs the engine inside a WSL machine and exposes a Docker-compatible
    // named pipe. The presence of the podman-machine pipe tells us Podman is the active engine
    // (a plain Docker Desktop install would not create it), so we leave Docker Desktop alone.
    if (!WindowsPipeExists("podman-machine-default"))
    {
      return;
    }

    SetIfMissing("DOCKER_HOST", "npipe://./pipe/docker_engine");
    DisableRyuk();

    // Podman's WSL machine does not forward published container ports to the Windows host, so
    // point Testcontainers at the machine's IP address instead of localhost.
    string? ip = TryGetPodmanWslIp();
    if (ip is not null)
    {
      SetIfMissing("TESTCONTAINERS_HOST_OVERRIDE", ip);
    }
  }

  private static void ConfigureMacOs()
  {
    // Ask Podman for its (Docker-compatible) API socket. If Podman is not installed/running we
    // get nothing back and fall through to the Docker default.
    string? socket = FirstLine(RunProcess("podman", "machine inspect --format {{.ConnectionInfo.PodmanSocket.Path}}"));
    if (string.IsNullOrEmpty(socket) || !File.Exists(socket))
    {
      return;
    }

    SetIfMissing("DOCKER_HOST", "unix://" + socket);
    DisableRyuk();
    // A macOS Podman machine forwards published ports to localhost, so no host override needed.
  }

  private static void ConfigureLinux()
  {
    // Docker (the default on GitHub Actions Linux runners) works with no configuration.
    if (File.Exists("/var/run/docker.sock"))
    {
      return;
    }

    // Otherwise look for a rootless (then rootful) Podman socket.
    var candidates = new List<string>();
    string? runtimeDir = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
    if (!string.IsNullOrEmpty(runtimeDir))
    {
      candidates.Add(Path.Combine(runtimeDir, "podman", "podman.sock"));
    }

    candidates.Add("/run/podman/podman.sock");

    foreach (string socket in candidates)
    {
      if (File.Exists(socket))
      {
        SetIfMissing("DOCKER_HOST", "unix://" + socket);
        DisableRyuk();
        return;
      }
    }
  }

  /// <summary>
  /// The Podman machine on Windows is a WSL distribution; read the IP of its primary network
  /// interface so container ports can be reached directly.
  /// </summary>
  private static string? TryGetPodmanWslIp()
  {
    foreach (string distro in new[] { "podman-machine-default" })
    {
      string? output = RunProcess("wsl.exe", $"-d {distro} -- ip -4 -o addr show eth0");
      if (output is null)
      {
        continue;
      }

      Match match = Regex.Match(output, @"inet\s+(\d+\.\d+\.\d+\.\d+)");
      if (match.Success)
      {
        return match.Groups[1].Value;
      }
    }

    return null;
  }

  private static bool WindowsPipeExists(string name)
  {
    try
    {
      foreach (string pipe in Directory.EnumerateFiles(@"\\.\pipe\"))
      {
        if (string.Equals(Path.GetFileName(pipe), name, StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }
    }
    catch
    {
      // Enumerating the pipe filesystem can throw on some Windows configurations; treat as absent.
    }

    return false;
  }

  private static void DisableRyuk() =>
    // Ryuk, the Testcontainers resource reaper, needs privileges rootless Podman does not grant.
    // Fixtures clean up their own containers via WithAutoRemove()/DisposeAsync().
    SetIfMissing("TESTCONTAINERS_RYUK_DISABLED", "true");

  private static bool HasValue(string name) =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name));

  private static void SetIfMissing(string name, string value)
  {
    if (!HasValue(name))
    {
      Environment.SetEnvironmentVariable(name, value);
    }
  }

  private static string? FirstLine(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    foreach (string line in value.Split('\n'))
    {
      string trimmed = line.Trim();
      if (trimmed.Length > 0)
      {
        return trimmed;
      }
    }

    return null;
  }

  private static string? RunProcess(string fileName, string arguments)
  {
    string? executable = ResolveExecutable(fileName);
    if (executable is null)
    {
      return null;
    }

    try
    {
      using var process = new Process
      {
        StartInfo = new ProcessStartInfo(executable, arguments)
        {
          RedirectStandardOutput = true,
          RedirectStandardError = true,
          UseShellExecute = false,
          CreateNoWindow = true,
        },
      };

      process.Start();
      string stdout = process.StandardOutput.ReadToEnd();
      _ = process.StandardError.ReadToEnd(); // drain so the child cannot block on a full pipe
      if (!process.WaitForExit(15_000))
      {
        try
        {
          process.Kill(entireProcessTree: true);
        }
        catch
        {
          // ignored
        }

        return null;
      }

      return stdout;
    }
    catch
    {
      return null;
    }
  }

  private static string? ResolveExecutable(string name)
  {
    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    string[] extensions = isWindows ? new[] { string.Empty, ".exe" } : new[] { string.Empty };

    string pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    foreach (string dir in pathVariable.Split(Path.PathSeparator))
    {
      if (string.IsNullOrWhiteSpace(dir))
      {
        continue;
      }

      foreach (string extension in extensions)
      {
        string candidate = Path.Combine(dir, name + extension);
        if (File.Exists(candidate))
        {
          return candidate;
        }
      }
    }

    // wsl.exe always resolves by name on Windows even when it is not on PATH.
    if (isWindows && name.Equals("wsl.exe", StringComparison.OrdinalIgnoreCase))
    {
      return "wsl.exe";
    }

    // Podman Desktop does not always add podman to PATH; probe the well-known install locations.
    if (name.Equals("podman", StringComparison.OrdinalIgnoreCase))
    {
      foreach (string candidate in KnownPodmanLocations())
      {
        if (File.Exists(candidate))
        {
          return candidate;
        }
      }
    }

    return null;
  }

  private static IEnumerable<string> KnownPodmanLocations()
  {
    yield return "/opt/homebrew/bin/podman";
    yield return "/usr/local/bin/podman";

    string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    if (!string.IsNullOrEmpty(localAppData))
    {
      yield return Path.Combine(localAppData, "Programs", "Podman", "podman.exe");
    }

    string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
    if (!string.IsNullOrEmpty(programFiles))
    {
      yield return Path.Combine(programFiles, "RedHat", "Podman", "podman.exe");
    }
  }
}
