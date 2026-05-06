using System.Text.Json;
using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;
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
        Assert.Equal("1.3", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("Full", root.GetProperty("privacyMode").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal(temp.Path, item.GetProperty("path").GetString());
        Assert.Equal("File", item.GetProperty("itemKind").GetString());
        Assert.Equal(0, item.GetProperty("evidence").GetArrayLength());
        Assert.NotEqual(JsonValueKind.Null, item.GetProperty("lastWriteTimeUtc").ValueKind);
        Assert.Equal("Unknown", item.GetProperty("risk").GetProperty("level").GetString());
        Assert.Equal("ReportOnly", item.GetProperty("risk").GetProperty("suggestedAction").GetString());
    }

    [Fact]
    public void ScanShouldWriteRedactedJsonWhenRequested()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--privacy", "redacted"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;
        Assert.Equal("Redacted", root.GetProperty("privacyMode").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal("[redacted-path-0001]", item.GetProperty("path").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("lastWriteTimeUtc").ValueKind);
        Assert.DoesNotContain(temp.Path, stdout.ToString());
    }

    [Fact]
    public void ScanShouldIncludeEvidenceFromInjectedProvider()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var evidenceProvider = new StubEvidenceProvider(
        [
            new EvidenceRecord(
                Type: EvidenceType.RunningProcessReference,
                Source: "example (PID 1234)",
                Confidence: 1.0,
                Message: $"Running process image path matches this file: {temp.Path}")
        ]);

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch,
            evidenceProvider);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var evidence = document.RootElement.GetProperty("items")[0].GetProperty("evidence");
        var item = Assert.Single(evidence.EnumerateArray());
        Assert.Equal("RunningProcessReference", item.GetProperty("type").GetString());
        Assert.Equal("example (PID 1234)", item.GetProperty("source").GetString());
        Assert.Equal(1.0, item.GetProperty("confidence").GetDouble());
    }

    [Fact]
    public void ScanShouldWriteRedactedMarkdownWhenRequested()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--format", "markdown", "--privacy", "redacted"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.Contains("Privacy mode: `Redacted`", stdout.ToString());
        Assert.Contains("[redacted-path-0001]", stdout.ToString());
        Assert.DoesNotContain(temp.Path, stdout.ToString());
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
    public void PlanShouldWriteJsonCleanupPlanToStdout()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;
        Assert.Equal("0.1", root.GetProperty("schemaVersion").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal(temp.Path, item.GetProperty("path").GetString());
        Assert.Equal("ReportOnly", item.GetProperty("action").GetString());
    }

    [Fact]
    public void PlanShouldWriteMarkdownCleanupPlanWhenRequested()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path, "--format", "markdown"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.Contains("# WinSafeClean Cleanup Plan", stdout.ToString());
        Assert.Contains(temp.Path, stdout.ToString());
    }

    [Fact]
    public void PlanShouldWriteRedactedJsonWhenRequested()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path, "--privacy", "redacted"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.DoesNotContain(temp.Path, stdout.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;
        Assert.False(root.TryGetProperty("privacyMode", out _));
        var item = root.GetProperty("items")[0];
        Assert.Equal("[redacted-path-0001]", item.GetProperty("path").GetString());
    }

    [Fact]
    public void PlanShouldUseExplicitCleanerMlRuleFileAsKnownCleanupEvidence()
    {
        using var temp = TemporaryFile.Create("hello");
        using var rules = TemporaryFile.Create($"""
            <cleaner id="example">
              <option id="cache">
                <action command="delete" search="file" path="{temp.Path}"/>
              </option>
            </cleaner>
            """);
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path, "--cleanerml", rules.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var item = document.RootElement.GetProperty("items")[0];
        Assert.Equal("ReportOnly", item.GetProperty("action").GetString());
        Assert.Contains(
            item.GetProperty("reasons").EnumerateArray(),
            reason => reason.GetString()!.Contains("Known cleanup rule matched", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PlanShouldRejectMissingCleanerMlRulePath()
    {
        using var temp = TemporaryFile.Create("hello");
        var missingRules = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path, "--cleanerml", missingRules],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("--cleanerml", stderr.ToString());
    }


    [Fact]
    public void PlanShouldWriteRedactedMarkdownWhenRequested()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path, "--format", "markdown", "--privacy", "redacted"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.Contains("[redacted-path-0001]", stdout.ToString());
        Assert.DoesNotContain(temp.Path, stdout.ToString());
    }

    [Fact]
    public void PlanShouldWritePlanOnlyToExplicitOutputPath()
    {
        using var temp = TemporaryFile.Create("hello");
        var outputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        try
        {
            var exitCode = CommandLineApp.Run(
                ["plan", "--path", temp.Path, "--output", outputPath],
                stdout,
                stderr,
                DateTimeOffset.UnixEpoch);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, stdout.ToString());
            Assert.Equal(string.Empty, stderr.ToString());
            Assert.True(File.Exists(outputPath));

            using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
            Assert.Equal("0.1", document.RootElement.GetProperty("schemaVersion").GetString());
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
    public void PlanShouldRejectExistingOutputFile()
    {
        using var temp = TemporaryFile.Create("scan target");
        using var output = TemporaryFile.Create("existing plan");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path, "--output", output.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("must not overwrite", stderr.ToString());
        Assert.Equal("existing plan", File.ReadAllText(output.Path));
    }

    [Fact]
    public void PlanShouldRejectOutputPathMatchingExistingDirectory()
    {
        using var temp = TemporaryFile.Create("scan target");
        using var sandbox = TemporarySandbox.Create();
        var outputDirectory = sandbox.CreateDirectory("existing-plan-dir");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path, "--output", outputDirectory],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("must not overwrite", stderr.ToString());
        Assert.True(Directory.Exists(outputDirectory));
    }


    [Fact]
    public void PlanShouldRejectMissingOutputParentDirectory()
    {
        using var temp = TemporaryFile.Create("scan target");
        var missingParent = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        var outputPath = System.IO.Path.Combine(missingParent, "plan.json");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["plan", "--path", temp.Path, "--output", outputPath],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("parent directory does not exist", stderr.ToString());
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void PlanShouldRejectProtectedOutputPath()
    {
        using var temp = TemporaryFile.Create("scan target");
        const string outputPath = @"C:\Windows\Installer\plan.json";
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        try
        {
            var exitCode = CommandLineApp.Run(
                ["plan", "--path", temp.Path, "--output", outputPath],
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
    public void PlanShouldNotModifyInputFile()
    {
        using var temp = TemporaryFile.Create("stable content");
        var beforeContent = File.ReadAllText(temp.Path);
        var beforeWriteTime = File.GetLastWriteTimeUtc(temp.Path);
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(["plan", "--path", temp.Path], stdout, stderr, DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(beforeContent, File.ReadAllText(temp.Path));
        Assert.Equal(beforeWriteTime, File.GetLastWriteTimeUtc(temp.Path));
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

    [Theory]
    [InlineData("--path")]
    [InlineData("--format")]
    [InlineData("--output")]
    [InlineData("--max-items")]
    [InlineData("--privacy")]
    public void ScanShouldRejectOptionsMissingValues(string option)
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(["scan", option], stdout, stderr, DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains($"{option} requires a value", stderr.ToString());
    }

    [Fact]
    public void ScanShouldRejectUnknownOption()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--unknown"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("Unknown option '--unknown'", stderr.ToString());
    }

    [Fact]
    public void ScanShouldAcceptRecursiveAndReportNestedChildren()
    {
        using var sandbox = TemporarySandbox.Create();
        var nestedDirectory = sandbox.CreateDirectory("nested");
        var hidden = sandbox.WriteFile(Path.Combine("nested", "hidden.txt"), "hidden");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", sandbox.RootPath, "--recursive"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        using var document = JsonDocument.Parse(stdout.ToString());
        var items = document.RootElement.GetProperty("items");
        Assert.Contains(items.EnumerateArray(), item => item.GetProperty("path").GetString() == nestedDirectory);
        Assert.Contains(items.EnumerateArray(), item => item.GetProperty("path").GetString() == hidden);
    }

    [Fact]
    public void ScanShouldRejectInvalidPrivacyMode()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--privacy", "public"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("--privacy must be either 'full' or 'redacted'", stderr.ToString());
    }

    [Fact]
    public void ScanShouldReturnCancelledWhenCancellationIsRequested()
    {
        using var temp = TemporaryFile.Create("hello");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch,
            cancellationToken: cancellation.Token);

        Assert.Equal(130, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("cancelled", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
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
    public void ScanShouldRejectOutputPathMatchingExistingDirectory()
    {
        using var temp = TemporaryFile.Create("scan target");
        using var sandbox = TemporarySandbox.Create();
        var outputDirectory = sandbox.CreateDirectory("existing-report-dir");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--output", outputDirectory],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("must not overwrite", stderr.ToString());
        Assert.True(Directory.Exists(outputDirectory));
    }

    [Fact]
    public void ScanShouldRejectMissingOutputParentDirectory()
    {
        using var temp = TemporaryFile.Create("scan target");
        var missingParent = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        var outputPath = System.IO.Path.Combine(missingParent, "report.json");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", temp.Path, "--output", outputPath],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("parent directory does not exist", stderr.ToString());
        Assert.False(File.Exists(outputPath));
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

    [Fact]
    public void ScanShouldReportDirectoryChildren()
    {
        using var sandbox = TemporarySandbox.Create();
        var alpha = sandbox.WriteFile("alpha.txt", "a");
        var beta = sandbox.WriteFile("beta.txt", "bb");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", sandbox.RootPath],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(stdout.ToString());
        var items = document.RootElement.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(alpha, items[0].GetProperty("path").GetString());
        Assert.Equal("File", items[0].GetProperty("itemKind").GetString());
        Assert.Equal(1, items[0].GetProperty("sizeBytes").GetInt64());
        Assert.NotEqual(JsonValueKind.Null, items[0].GetProperty("lastWriteTimeUtc").ValueKind);
        Assert.Equal(beta, items[1].GetProperty("path").GetString());
        Assert.Equal("File", items[1].GetProperty("itemKind").GetString());
        Assert.Equal(2, items[1].GetProperty("sizeBytes").GetInt64());
        Assert.NotEqual(JsonValueKind.Null, items[1].GetProperty("lastWriteTimeUtc").ValueKind);
    }

    [Fact]
    public void ScanShouldHonorMaxItems()
    {
        using var sandbox = TemporarySandbox.Create();
        sandbox.WriteFile("a.txt", "a");
        sandbox.WriteFile("b.txt", "b");
        sandbox.WriteFile("c.txt", "c");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", sandbox.RootPath, "--max-items", "2"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.Equal(2, document.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public void ScanShouldAcceptNoRecursiveAndSkipNestedChildren()
    {
        using var sandbox = TemporarySandbox.Create();
        var nestedDirectory = sandbox.CreateDirectory("nested");
        sandbox.WriteFile(Path.Combine("nested", "hidden.txt"), "hidden");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", sandbox.RootPath, "--no-recursive"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(stdout.ToString());
        var items = document.RootElement.GetProperty("items");
        var item = Assert.Single(items.EnumerateArray());
        Assert.Equal(nestedDirectory, item.GetProperty("path").GetString());
        Assert.Equal("Directory", item.GetProperty("itemKind").GetString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void ScanShouldRejectInvalidMaxItems(string maxItems)
    {
        using var sandbox = TemporarySandbox.Create();
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", sandbox.RootPath, "--max-items", maxItems],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("--max-items", stderr.ToString());
    }

    [Fact]
    public void ScanShouldReturnUnknownReportForInvalidPathSyntax()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", "bad\0path"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var item = document.RootElement.GetProperty("items")[0];
        Assert.Equal("bad\0path", item.GetProperty("path").GetString());
        Assert.Equal("Unknown", item.GetProperty("risk").GetProperty("level").GetString());
        Assert.Contains("invalid", item.GetProperty("risk").GetProperty("reasons")[0].GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("clean")]
    [InlineData("quarantine")]
    [InlineData("restore")]
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

    private sealed class StubEvidenceProvider(IReadOnlyList<EvidenceRecord> evidence) : IFileEvidenceProvider
    {
        public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return evidence;
        }
    }

    private sealed class TemporarySandbox : IDisposable
    {
        private TemporarySandbox(string rootPath)
        {
            RootPath = rootPath;
        }

        public string RootPath { get; }

        public static TemporarySandbox Create()
        {
            var rootPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WinSafeClean.Cli.Tests", System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
        }

        public string WriteFile(string relativePath, string content)
        {
            var path = System.IO.Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return path;
        }

        public string CreateDirectory(string relativePath)
        {
            var path = System.IO.Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
