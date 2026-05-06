using System.Diagnostics;
using System.Text.Json;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Cli.Tests;

public sealed class ProgramEndToEndTests
{
    [Fact]
    public async Task ProgramShouldRunScanThroughDefaultCompositionRoot()
    {
        using var temp = TemporaryFile.Create("hello");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var executablePath = Path.Combine(AppContext.BaseDirectory, "WinSafeClean.Cli.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("scan");
        startInfo.ArgumentList.Add("--path");
        startInfo.ArgumentList.Add(temp.Path);
        startInfo.ArgumentList.Add("--format");
        startInfo.ArgumentList.Add("json");

        using var process = Process.Start(startInfo)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

        await process.WaitForExitAsync(timeout.Token);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        Assert.Equal(0, process.ExitCode);
        Assert.Equal(string.Empty, stderr);

        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("1.3", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("Full", root.GetProperty("privacyMode").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal(temp.Path, item.GetProperty("path").GetString());
        Assert.Equal("File", item.GetProperty("itemKind").GetString());
        Assert.Equal(JsonValueKind.Array, item.GetProperty("evidence").ValueKind);
    }

    [Fact]
    public async Task ProgramShouldRunPlanThroughDefaultCompositionRoot()
    {
        using var temp = TemporaryFile.Create("hello");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var executablePath = Path.Combine(AppContext.BaseDirectory, "WinSafeClean.Cli.exe");

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("plan");
        startInfo.ArgumentList.Add("--path");
        startInfo.ArgumentList.Add(temp.Path);
        startInfo.ArgumentList.Add("--format");
        startInfo.ArgumentList.Add("json");

        using var process = Process.Start(startInfo)!;
        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);

        await process.WaitForExitAsync(timeout.Token);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        Assert.Equal(0, process.ExitCode);
        Assert.Equal(string.Empty, stderr);

        using var document = JsonDocument.Parse(stdout);
        var root = document.RootElement;
        Assert.Equal("0.1", root.GetProperty("schemaVersion").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal(temp.Path, item.GetProperty("path").GetString());
        Assert.True(Enum.TryParse<CleanupPlanAction>(item.GetProperty("action").GetString(), out _));
        Assert.True(Enum.TryParse<RiskLevel>(item.GetProperty("riskLevel").GetString(), out _));
        Assert.Equal(JsonValueKind.Array, item.GetProperty("reasons").ValueKind);
    }

    private sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public static TemporaryFile Create(string content)
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            File.WriteAllText(path, content);
            return new TemporaryFile(path);
        }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
