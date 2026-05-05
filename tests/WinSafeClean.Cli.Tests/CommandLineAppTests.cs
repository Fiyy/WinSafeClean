using System.Text.Json;
using WinSafeClean.Cli;

namespace WinSafeClean.Cli.Tests;

public sealed class CommandLineAppTests
{
    [Fact]
    public void ScanShouldWriteJsonReportToStdout()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path],
            stdout,
            stderr,
            new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;
        Assert.Equal("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.Equal(temp.Path, root.GetProperty("items")[0].GetProperty("path").GetString());
        Assert.Equal("Unknown", root.GetProperty("items")[0].GetProperty("risk").GetProperty("level").GetString());
        Assert.Equal("ReportOnly", root.GetProperty("items")[0].GetProperty("risk").GetProperty("suggestedAction").GetString());
    }

    [Fact]
    public void ScanShouldWriteMarkdownWhenRequested()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--format", "markdown"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Contains("# WinSafeClean Scan Report", stdout.ToString());
        Assert.Contains(temp.Path, stdout.ToString());
    }

    [Fact]
    public void ScanShouldRequireExplicitPath()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(["scan"], stdout, stderr, DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("--path is required", stderr.ToString());
    }

    [Fact]
    public void ScanShouldRejectInvalidFormat()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--format", "html"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("--format must be either 'json' or 'markdown'", stderr.ToString());
    }

    [Fact]
    public void ScanShouldWriteReportOnlyToExplicitOutputPath()
    {
        using var temp = TemporaryFile.Create("hello");
        var outputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        try
        {
            var exitCode = CommandLineApp.Run(
                ["scan", "--path", temp.Path, "--output", outputPath],
                stdout,
                stderr,
                DateTimeOffset.UnixEpoch);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, stdout.ToString());
            Assert.Equal(string.Empty, stderr.ToString());
            Assert.True(File.Exists(outputPath));
            using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
            Assert.Equal(temp.Path, document.RootElement.GetProperty("items")[0].GetProperty("path").GetString());
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void ScanShouldRejectOutputPathMatchingInputFile()
    {
        using var temp = TemporaryFile.Create("original content");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--output", temp.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("must not overwrite", stderr.ToString());
        Assert.Equal("original content", File.ReadAllText(temp.Path));
    }

    [Fact]
    public void ScanShouldRejectExistingOutputFile()
    {
        using var temp = TemporaryFile.Create("scan target");
        using var output = TemporaryFile.Create("existing report");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--output", output.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("must not overwrite", stderr.ToString());
        Assert.Equal("existing report", File.ReadAllText(output.Path));
    }

    [Fact]
    public void ScanShouldRejectProtectedOutputPath()
    {
        using var temp = TemporaryFile.Create("scan target");
        const string outputPath = @"C:\Windows\Installer\scan-report.json";
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        try
        {
            var exitCode = CommandLineApp.Run(
                ["scan", "--path", temp.Path, "--output", outputPath],
                stdout,
                stderr,
                DateTimeOffset.UnixEpoch);

            Assert.Equal(2, exitCode);
            Assert.Equal(string.Empty, stdout.ToString());
            Assert.Contains("protected Windows path", stderr.ToString());
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void ScanShouldExposeProtectedPathRiskFromCore()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", @"\\?\C:\Windows\Installer\abc.msi"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Contains(@"""level"": ""Blocked""", stdout.ToString());
        Assert.Contains(@"""suggestedAction"": ""Keep""", stdout.ToString());
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("clean")]
    [InlineData("quarantine")]
    [InlineData("restore")]
    [InlineData("plan")]
    public void ShouldRejectExecutableCommandsDuringReadOnlyPhase(string command)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run([command, "--path", @"C:\Temp"], stdout, stderr, DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("read-only", stderr.ToString());
    }

    [Theory]
    [InlineData("--delete")]
    [InlineData("--fix")]
    [InlineData("--quarantine")]
    public void ScanShouldRejectExecutableOptionsDuringReadOnlyPhase(string option)
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(["scan", "--path", temp.Path, option], stdout, stderr, DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("read-only", stderr.ToString());
    }

    [Fact]
    public void ScanShouldNotModifyInputFile()
    {
        using var temp = TemporaryFile.Create("stable content");
        var beforeContent = File.ReadAllText(temp.Path);
        var beforeWriteTime = File.GetLastWriteTimeUtc(temp.Path);
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(["scan", "--path", temp.Path], stdout, stderr, DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(beforeContent, File.ReadAllText(temp.Path));
        Assert.Equal(beforeWriteTime, File.GetLastWriteTimeUtc(temp.Path));
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
