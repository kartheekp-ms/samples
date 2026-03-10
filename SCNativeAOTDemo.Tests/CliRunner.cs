using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SCNativeAOTDemo.Tests;

/// <summary>
/// Result from running the CLI executable.
/// </summary>
public record CliResult(int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Runs the SystemCommandLineDemo executable as a child process and captures output.
/// </summary>
public static class CliRunner
{
    private static readonly Lazy<string> ExePath = new(() =>
    {
        var path = FindExecutable();
        Console.WriteLine($"[CliRunner] Using executable: {path}");
        return path;
    });

    private static string FindExecutable()
    {
        // Walk up from the test assembly's output directory to the repo root,
        // then into the app project's output matching the same configuration.
        var testDir = AppContext.BaseDirectory; // e.g. ...bin/Debug/net10.0/
        var dir = new DirectoryInfo(testDir);

        // Navigate up to the solution root (contains samples.slnx)
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "samples.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("Could not find solution root from " + testDir);
        }

        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "SCNativeAOTDemo.exe"
            : "SCNativeAOTDemo";

        // Match the same configuration/TFM that the test project was built with.
        // testDir looks like: <repo>\SystemCommandLineDemo.Tests\bin\Debug\net10.0\
        var parts = testDir.Replace('/', '\\').TrimEnd('\\').Split('\\');
        var tfm = parts[^1];           // net10.0
        var config = parts[^2];        // Debug or Release

        var projectBin = Path.Combine(dir.FullName, "SCNativeAOTDemo", "bin", config, tfm);

        // Prefer the Native AOT published binary over the managed build.
        // AOT publish output: bin/{config}/{tfm}/{rid}/publish/{exeName}
        var publishDir = new DirectoryInfo(projectBin);
        if (publishDir.Exists)
        {
            foreach (var ridDir in publishDir.GetDirectories())
            {
                var aotPath = Path.Combine(ridDir.FullName, "publish", exeName);
                if (File.Exists(aotPath))
                {
                    return aotPath;
                }
            }
        }

        // Fall back to the managed build output.
        var managedPath = Path.Combine(projectBin, exeName);
        if (File.Exists(managedPath))
        {
            return managedPath;
        }

        throw new FileNotFoundException(
            $"Executable not found. Publish the SCNativeAOTDemo project with 'dotnet publish' (Native AOT) " +
            $"or build with 'dotnet build'. Searched under {projectBin}");
    }

    /// <summary>
    /// Runs the CLI with the given arguments and returns the captured output.
    /// </summary>
    public static async Task<CliResult> RunAsync(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = ExePath.Value,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start process.");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CliResult(process.ExitCode, stdout, stderr);
    }
}
