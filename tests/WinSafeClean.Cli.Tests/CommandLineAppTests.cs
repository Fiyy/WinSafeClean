using System.Text.Json;
using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;
using WinSafeClean.Cli;

namespace WinSafeClean.Cli.Tests;

public sealed class CommandLineAppTests
{
    [Fact]
    public void ShouldWriteVersion()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(["--version"], stdout, stderr, DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.StartsWith("WinSafeClean 0.2.1", stdout.ToString(), StringComparison.Ordinal);
        Assert.EndsWith(Environment.NewLine, stdout.ToString(), StringComparison.Ordinal);
    }

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
        Assert.Equal("0.2", root.GetProperty("schemaVersion").GetString());
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
        Assert.Equal("[redacted-quarantine-root]", root.GetProperty("quarantineRoot").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal("[redacted-path-0001]", item.GetProperty("path").GetString());
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localApplicationData))
        {
            Assert.DoesNotContain(localApplicationData, stdout.ToString(), StringComparison.OrdinalIgnoreCase);
        }
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
            Assert.Equal("0.2", document.RootElement.GetProperty("schemaVersion").GetString());
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
    public void PreflightShouldReadPlanAndMetadataAndWriteJsonChecklist()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path, "--manual-confirmation"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        var root = document.RootElement;
        Assert.Equal("1.0", root.GetProperty("schemaVersion").GetString());
        Assert.True(root.GetProperty("isExecutable").GetBoolean());
        Assert.Contains(
            root.GetProperty("checks").EnumerateArray(),
            check => check.GetProperty("code").GetString() == "ManualConfirmation"
                && check.GetProperty("status").GetString() == "Passed");
    }

    [Fact]
    public void PreflightShouldWriteMarkdownWhenRequested()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path, "--manual-confirmation", "--format", "markdown"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.Contains("# WinSafeClean Preflight Checklist", stdout.ToString());
        Assert.Contains("ManualConfirmation", stdout.ToString());
    }

    [Fact]
    public void PreflightShouldReturnChecklistFailureWithoutManualConfirmation()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());

        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.False(document.RootElement.GetProperty("isExecutable").GetBoolean());
    }

    [Fact]
    public void PreflightShouldRejectMissingPlanFile()
    {
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        var missingPlan = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", missingPlan, "--metadata", metadataFile.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Equal(string.Empty, stdout.ToString());
        Assert.Contains("--plan", stderr.ToString());
    }

    [Fact]
    public void PreflightShouldRequirePlanAndMetadata()
    {
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(["preflight"], stdout, stderr, DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("--plan is required", stderr.ToString());
    }

    [Fact]
    public void PreflightShouldRejectMissingMetadataFile()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        var missingMetadata = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", missingMetadata],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("--metadata", stderr.ToString());
    }

    [Fact]
    public void PreflightShouldRejectInvalidPlanJson()
    {
        using var planFile = TemporaryFile.Create("{not valid json");
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("could not be read", stderr.ToString());
    }

    [Fact]
    public void PreflightShouldRejectInvalidFormat()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path, "--format", "html"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("--format", stderr.ToString());
    }

    [Fact]
    public void PreflightShouldRejectUnknownOption()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path, "--unknown"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option", stderr.ToString());
    }

    [Fact]
    public void PreflightShouldWriteChecklistOnlyToExplicitOutputPath()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        var outputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        try
        {
            var exitCode = CommandLineApp.Run(
                ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path, "--manual-confirmation", "--output", outputPath],
                stdout,
                stderr,
                DateTimeOffset.UnixEpoch);

            Assert.Equal(0, exitCode);
            Assert.Equal(string.Empty, stdout.ToString());
            Assert.True(File.Exists(outputPath));
            using var document = JsonDocument.Parse(File.ReadAllText(outputPath));
            Assert.Equal("1.0", document.RootElement.GetProperty("schemaVersion").GetString());
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
    public void PreflightShouldRejectExistingOutputFile()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        using var output = TemporaryFile.Create("existing");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path, "--output", output.Path],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("must not overwrite", stderr.ToString());
        Assert.Equal("existing", File.ReadAllText(output.Path));
    }

    [Fact]
    public void PreflightShouldNotModifyPlanOrMetadataFiles()
    {
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan()));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata()));
        var planBefore = File.ReadAllText(planFile.Path);
        var metadataBefore = File.ReadAllText(metadataFile.Path);
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["preflight", "--plan", planFile.Path, "--metadata", metadataFile.Path, "--manual-confirmation"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(planBefore, File.ReadAllText(planFile.Path));
        Assert.Equal(metadataBefore, File.ReadAllText(metadataFile.Path));
    }

    [Fact]
    public void QuarantineShouldRequireExplicitDangerConfirmation()
    {
        using var sandbox = TemporarySandbox.Create();
        var sourcePath = sandbox.WriteFile("cache.tmp", "cache");
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan(sourcePath, quarantineRoot)));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(CreateRestoreMetadata(sourcePath, quarantineRoot)));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["quarantine", "--plan", planFile.Path, "--metadata", metadataFile.Path, "--manual-confirmation"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("i-understand", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(sourcePath));
    }

    [Fact]
    public void QuarantineShouldMoveFileWithExplicitConfirmation()
    {
        using var sandbox = TemporarySandbox.Create();
        var sourcePath = sandbox.WriteFile("cache.tmp", "cache");
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        var metadata = CreateRestoreMetadata(sourcePath, quarantineRoot);
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan(sourcePath, quarantineRoot)));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(metadata));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            [
                "quarantine",
                "--plan", planFile.Path,
                "--metadata", metadataFile.Path,
                "--manual-confirmation",
                "--i-understand-this-moves-files"
            ],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.False(File.Exists(sourcePath));
        Assert.True(File.Exists(metadata.QuarantinePath));
        Assert.True(File.Exists(metadata.RestoreMetadataPath));
        var writtenMetadata = RestoreMetadataJsonSerializer.Deserialize(File.ReadAllText(metadata.RestoreMetadataPath));
        Assert.Equal("1.1", writtenMetadata.SchemaVersion);
        Assert.Equal("SHA256", writtenMetadata.ContentHashAlgorithm);
        Assert.False(string.IsNullOrWhiteSpace(writtenMetadata.ContentHash));

        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.True(document.RootElement.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public void QuarantineShouldAppendOperationLogWhenRequested()
    {
        using var sandbox = TemporarySandbox.Create();
        var sourcePath = sandbox.WriteFile("cache.tmp", "cache");
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        var metadata = CreateRestoreMetadata(sourcePath, quarantineRoot);
        var operationLogPath = System.IO.Path.Combine(quarantineRoot, "logs", "operations.jsonl");
        using var planFile = TemporaryFile.Create(CleanupPlanJsonSerializer.Serialize(CreatePreflightPlan(sourcePath, quarantineRoot)));
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(metadata));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            [
                "quarantine",
                "--plan", planFile.Path,
                "--metadata", metadataFile.Path,
                "--manual-confirmation",
                "--i-understand-this-moves-files",
                "--operation-log", operationLogPath
            ],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(operationLogPath));
        var log = File.ReadAllText(operationLogPath);
        Assert.Contains("QuarantineStarted", log);
        Assert.Contains("QuarantineCompleted", log);
    }

    [Fact]
    public void RestoreShouldRequireExplicitDangerConfirmation()
    {
        using var sandbox = TemporarySandbox.Create();
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        var metadata = CreateRestoreMetadata(System.IO.Path.Combine(sandbox.RootPath, "cache.tmp"), quarantineRoot);
        sandbox.WriteFile(Path.GetRelativePath(sandbox.RootPath, metadata.QuarantinePath), "cache");
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(metadata));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["restore", "--metadata", metadataFile.Path, "--manual-confirmation"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, exitCode);
        Assert.Contains("i-understand", stderr.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(metadata.QuarantinePath));
    }

    [Fact]
    public void RestoreShouldMoveFileWithExplicitConfirmation()
    {
        using var sandbox = TemporarySandbox.Create();
        var originalPath = System.IO.Path.Combine(sandbox.RootPath, "cache.tmp");
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        var metadata = CreateRestoreMetadata(originalPath, quarantineRoot);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(metadata.QuarantinePath)!);
        File.WriteAllText(metadata.QuarantinePath, "cache");
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(metadata));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            [
                "restore",
                "--metadata", metadataFile.Path,
                "--manual-confirmation",
                "--i-understand-this-moves-files"
            ],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.True(File.Exists(originalPath));
        Assert.False(File.Exists(metadata.QuarantinePath));
        Assert.Equal("cache", File.ReadAllText(originalPath));

        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.True(document.RootElement.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public void RestoreShouldRejectRedactedMetadataWithoutMovingFile()
    {
        using var sandbox = TemporarySandbox.Create();
        var originalPath = System.IO.Path.Combine(sandbox.RootPath, "cache.tmp");
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        var metadata = CreateRestoreMetadata(originalPath, quarantineRoot, redacted: true);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(metadata.QuarantinePath)!);
        File.WriteAllText(metadata.QuarantinePath, "cache");
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(metadata));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            [
                "restore",
                "--metadata", metadataFile.Path,
                "--manual-confirmation",
                "--i-understand-this-moves-files"
            ],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.False(File.Exists(originalPath));
        Assert.True(File.Exists(metadata.QuarantinePath));

        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.False(document.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Contains("redacted", document.RootElement.GetProperty("errorMessage").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestoreShouldRejectLegacyMetadataWithoutContentHashByDefault()
    {
        using var sandbox = TemporarySandbox.Create();
        var originalPath = System.IO.Path.Combine(sandbox.RootPath, "cache.tmp");
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        var metadata = CreateRestoreMetadata(originalPath, quarantineRoot, includeContentHash: false);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(metadata.QuarantinePath)!);
        File.WriteAllText(metadata.QuarantinePath, "cache");
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(metadata));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            [
                "restore",
                "--metadata", metadataFile.Path,
                "--manual-confirmation",
                "--i-understand-this-moves-files"
            ],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.False(File.Exists(originalPath));
        Assert.True(File.Exists(metadata.QuarantinePath));

        using var document = JsonDocument.Parse(stdout.ToString());
        Assert.False(document.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Contains("legacy", document.RootElement.GetProperty("errorMessage").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RestoreShouldAllowLegacyMetadataWithoutContentHashWhenExplicitlyRequested()
    {
        using var sandbox = TemporarySandbox.Create();
        var originalPath = System.IO.Path.Combine(sandbox.RootPath, "cache.tmp");
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        var metadata = CreateRestoreMetadata(originalPath, quarantineRoot, includeContentHash: false);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(metadata.QuarantinePath)!);
        File.WriteAllText(metadata.QuarantinePath, "cache");
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(metadata));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            [
                "restore",
                "--metadata", metadataFile.Path,
                "--manual-confirmation",
                "--i-understand-this-moves-files",
                "--allow-legacy-metadata-without-hash"
            ],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        Assert.True(File.Exists(originalPath));
        Assert.False(File.Exists(metadata.QuarantinePath));
    }

    [Fact]
    public void RestoreShouldAppendOperationLogWhenRequested()
    {
        using var sandbox = TemporarySandbox.Create();
        var originalPath = System.IO.Path.Combine(sandbox.RootPath, "cache.tmp");
        var quarantineRoot = sandbox.CreateDirectory("quarantine");
        var metadata = CreateRestoreMetadata(originalPath, quarantineRoot);
        var operationLogPath = System.IO.Path.Combine(quarantineRoot, "logs", "operations.jsonl");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(metadata.QuarantinePath)!);
        File.WriteAllText(metadata.QuarantinePath, "cache");
        using var metadataFile = TemporaryFile.Create(RestoreMetadataJsonSerializer.Serialize(metadata));
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            [
                "restore",
                "--metadata", metadataFile.Path,
                "--manual-confirmation",
                "--i-understand-this-moves-files",
                "--operation-log", operationLogPath
            ],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(operationLogPath));
        var log = File.ReadAllText(operationLogPath);
        Assert.Contains("RestoreStarted", log);
        Assert.Contains("RestoreCompleted", log);
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
    public void ScanShouldCalculateDirectorySizesWhenRequested()
    {
        using var sandbox = TemporarySandbox.Create();
        var nestedDirectory = sandbox.CreateDirectory("nested");
        sandbox.WriteFile(Path.Combine("nested", "alpha.bin"), "alpha");
        sandbox.WriteFile(Path.Combine("nested", "child", "beta.bin"), "beta");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();

        var exitCode = CommandLineApp.Run(
            ["scan", "--path", sandbox.RootPath, "--directory-sizes"],
            stdout,
            stderr,
            DateTimeOffset.UnixEpoch);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, stderr.ToString());
        using var document = JsonDocument.Parse(stdout.ToString());
        var item = Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(nestedDirectory, item.GetProperty("path").GetString());
        Assert.Equal("Directory", item.GetProperty("itemKind").GetString());
        Assert.Equal(9, item.GetProperty("sizeBytes").GetInt64());
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

    private static CleanupPlan CreatePreflightPlan()
    {
        const string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";
        return CreatePreflightPlan(@"C:\Users\Alice\AppData\Local\Example\cache.tmp", quarantineRoot);
    }

    private static CleanupPlan CreatePreflightPlan(string sourcePath, string quarantineRoot)
    {
        return new CleanupPlan(
            SchemaVersion: "0.2",
            CreatedAt: DateTimeOffset.UnixEpoch,
            QuarantineRoot: quarantineRoot,
            Items:
            [
                new CleanupPlanItem(
                    Path: sourcePath,
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
                    QuarantinePreview: new QuarantinePreview(
                        OriginalPath: sourcePath,
                        ProposedQuarantinePath: System.IO.Path.Combine(quarantineRoot, "items", "abcd-cache.tmp"),
                        RestoreMetadataPath: System.IO.Path.Combine(quarantineRoot, "restore", "abcd.restore.json"),
                        RestorePlanId: "abcd",
                        RequiresManualConfirmation: true,
                        Warnings: ["Quarantine preview only; no file operation has been executed."]))
            ]);
    }

    private static RestoreMetadata CreateRestoreMetadata()
    {
        const string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";
        return CreateRestoreMetadata(@"C:\Users\Alice\AppData\Local\Example\cache.tmp", quarantineRoot);
    }

    private static RestoreMetadata CreateRestoreMetadata(
        string sourcePath,
        string quarantineRoot,
        bool redacted = false,
        bool includeContentHash = true)
    {
        return new RestoreMetadata(
            SchemaVersion: includeContentHash ? "1.1" : "1.0",
            CreatedAt: DateTimeOffset.UnixEpoch,
            CleanupPlanSchemaVersion: "0.2",
            RestorePlanId: "abcd",
            OriginalPath: sourcePath,
            QuarantinePath: System.IO.Path.Combine(quarantineRoot, "items", "abcd-cache.tmp"),
            RestoreMetadataPath: System.IO.Path.Combine(quarantineRoot, "restore", "abcd.restore.json"),
            RiskLevel: RiskLevel.LowRisk,
            PlanAction: CleanupPlanAction.ReviewForQuarantine,
            Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
            Warnings: ["Quarantine preview only; no file operation has been executed."],
            RequiresManualConfirmation: true,
            Redacted: redacted,
            ContentHashAlgorithm: includeContentHash ? "SHA256" : null,
            ContentHash: includeContentHash ? "5e1ecee06a7fc06f305ae5c12acfe7a7f67b8ece7af76932ed3afab00c3c6921" : null);
    }
}
