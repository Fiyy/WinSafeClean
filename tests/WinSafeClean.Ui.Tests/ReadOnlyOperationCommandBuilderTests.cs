using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class ReadOnlyOperationCommandBuilderTests
{
    [Fact]
    public void ShouldBuildScanArguments()
    {
        var args = ReadOnlyOperationCommandBuilder.BuildScan(path: @"C:\Temp", recursive: true, maxItems: 200);

        Assert.Equal(["scan", "--path", @"C:\Temp", "--recursive", "--max-items", "200"], args);
    }

    [Fact]
    public void ShouldBuildScanArgumentsWithOptionalOutputPrivacyFormatAndCleanerMl()
    {
        var args = ReadOnlyOperationCommandBuilder.BuildScan(new ReadOnlyScanCommandOptions(
            Path: @"C:\Temp",
            Recursive: true,
            MaxItems: 50,
            Format: "markdown",
            Privacy: "redacted",
            OutputPath: @".\scan.md",
            CleanerMlPath: @".\rules",
            IncludeDirectorySizes: true));

        Assert.Equal(
            [
                "scan",
                "--path",
                @"C:\Temp",
                "--recursive",
                "--max-items",
                "50",
                "--cleanerml",
                @".\rules",
                "--directory-sizes",
                "--format",
                "markdown",
                "--privacy",
                "redacted",
                "--output",
                @".\scan.md"
            ],
            args);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("  ", null)]
    [InlineData("200", 200)]
    public void ShouldParseOptionalMaxItems(string value, int? expected)
    {
        Assert.Equal(expected, ReadOnlyOperationCommandBuilder.ParseMaxItems(value));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("many")]
    public void ShouldRejectInvalidMaxItemsWithReadableMessage(string value)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ReadOnlyOperationCommandBuilder.ParseMaxItems(value));

        Assert.Equal("Max items must be a positive integer.", exception.Message);
    }

    [Fact]
    public void ShouldBuildPlanArgumentsWithCleanerMl()
    {
        var args = ReadOnlyOperationCommandBuilder.BuildPlan(
            path: @"C:\Temp",
            cleanerMlPath: @".\rules\example.xml");

        Assert.Equal(["plan", "--path", @"C:\Temp", "--cleanerml", @".\rules\example.xml"], args);
    }

    [Fact]
    public void ShouldBuildPlanArgumentsWithReadOnlyScanOptions()
    {
        var args = ReadOnlyOperationCommandBuilder.BuildPlan(new ReadOnlyPlanCommandOptions(
            Path: @"C:\Temp",
            Recursive: true,
            MaxItems: 75,
            CleanerMlPath: @".\rules\example.xml",
            Format: "markdown",
            Privacy: "redacted",
            OutputPath: @".\plan.md",
            IncludeDirectorySizes: true));

        Assert.Equal(
            [
                "plan",
                "--path",
                @"C:\Temp",
                "--recursive",
                "--max-items",
                "75",
                "--cleanerml",
                @".\rules\example.xml",
                "--directory-sizes",
                "--format",
                "markdown",
                "--privacy",
                "redacted",
                "--output",
                @".\plan.md"
            ],
            args);
    }

    [Fact]
    public void ShouldBuildPreflightArgumentsWithoutExecutableCleanupCommands()
    {
        var args = ReadOnlyOperationCommandBuilder.BuildPreflight(
            planPath: @".\plan.json",
            metadataPath: @".\abcd.restore.json",
            manualConfirmation: true);

        Assert.Equal(["preflight", "--plan", @".\plan.json", "--metadata", @".\abcd.restore.json", "--manual-confirmation"], args);
        Assert.DoesNotContain(args, arg => arg is "quarantine" or "restore" or "delete" or "clean");
    }

    [Fact]
    public void ShouldBuildPreflightArgumentsWithFormatAndOutput()
    {
        var args = ReadOnlyOperationCommandBuilder.BuildPreflight(new ReadOnlyPreflightCommandOptions(
            PlanPath: @".\plan.json",
            MetadataPath: @".\abcd.restore.json",
            ManualConfirmation: true,
            Format: "markdown",
            OutputPath: @".\preflight.md"));

        Assert.Equal(
            [
                "preflight",
                "--plan",
                @".\plan.json",
                "--metadata",
                @".\abcd.restore.json",
                "--manual-confirmation",
                "--format",
                "markdown",
                "--output",
                @".\preflight.md"
            ],
            args);
        Assert.DoesNotContain(args, arg => arg is "quarantine" or "restore" or "delete" or "clean");
    }

    [Theory]
    [InlineData("html")]
    [InlineData("xml")]
    public void ShouldRejectUnsupportedOutputFormats(string format)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ReadOnlyOperationCommandBuilder.BuildScan(new ReadOnlyScanCommandOptions(
                Path: @"C:\Temp",
                Format: format)));

        Assert.Equal("Format must be either 'json' or 'markdown'.", exception.Message);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    public void ShouldRejectUnsupportedPrivacyModes(string privacy)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ReadOnlyOperationCommandBuilder.BuildPlan(new ReadOnlyPlanCommandOptions(
                Path: @"C:\Temp",
                Privacy: privacy)));

        Assert.Equal("Privacy must be either 'full' or 'redacted'.", exception.Message);
    }
}
