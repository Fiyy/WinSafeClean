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
    public void ShouldBuildPreflightArgumentsWithoutExecutableCleanupCommands()
    {
        var args = ReadOnlyOperationCommandBuilder.BuildPreflight(
            planPath: @".\plan.json",
            metadataPath: @".\abcd.restore.json",
            manualConfirmation: true);

        Assert.Equal(["preflight", "--plan", @".\plan.json", "--metadata", @".\abcd.restore.json", "--manual-confirmation"], args);
        Assert.DoesNotContain(args, arg => arg is "quarantine" or "restore" or "delete" or "clean");
    }
}
