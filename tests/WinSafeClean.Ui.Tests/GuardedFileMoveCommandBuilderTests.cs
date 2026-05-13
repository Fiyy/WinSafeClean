using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class GuardedFileMoveCommandBuilderTests
{
    [Fact]
    public void ShouldBuildQuarantineArgumentsOnlyAfterDoubleConfirmation()
    {
        var args = GuardedFileMoveCommandBuilder.BuildQuarantine(new GuardedQuarantineCommandOptions(
            PlanPath: @".\plan.json",
            MetadataPath: @".\abcd.restore.json",
            ManualConfirmation: true,
            UnderstandsFileMoves: true,
            OperationLogPath: @".\operations.jsonl"));

        Assert.Equal(
            [
                "quarantine",
                "--plan",
                @".\plan.json",
                "--metadata",
                @".\abcd.restore.json",
                "--manual-confirmation",
                "--i-understand-this-moves-files",
                "--operation-log",
                @".\operations.jsonl"
            ],
            args);
    }

    [Fact]
    public void ShouldBuildRestoreArgumentsOnlyAfterDoubleConfirmation()
    {
        var args = GuardedFileMoveCommandBuilder.BuildRestore(new GuardedRestoreCommandOptions(
            MetadataPath: @".\abcd.restore.json",
            ManualConfirmation: true,
            UnderstandsFileMoves: true,
            OperationLogPath: @".\operations.jsonl",
            AllowLegacyMetadataWithoutHash: true));

        Assert.Equal(
            [
                "restore",
                "--metadata",
                @".\abcd.restore.json",
                "--manual-confirmation",
                "--i-understand-this-moves-files",
                "--allow-legacy-metadata-without-hash",
                "--operation-log",
                @".\operations.jsonl"
            ],
            args);
    }

    [Theory]
    [InlineData(false, true, "Manual confirmation must be checked before building a file-moving CLI command.")]
    [InlineData(true, false, "File move acknowledgement must be checked before building a file-moving CLI command.")]
    public void ShouldRejectQuarantineWithoutDoubleConfirmation(
        bool manualConfirmation,
        bool understandsFileMoves,
        string expectedMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GuardedFileMoveCommandBuilder.BuildQuarantine(new GuardedQuarantineCommandOptions(
                PlanPath: @".\plan.json",
                MetadataPath: @".\abcd.restore.json",
                ManualConfirmation: manualConfirmation,
                UnderstandsFileMoves: understandsFileMoves)));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData(false, true, "Manual confirmation must be checked before building a file-moving CLI command.")]
    [InlineData(true, false, "File move acknowledgement must be checked before building a file-moving CLI command.")]
    public void ShouldRejectRestoreWithoutDoubleConfirmation(
        bool manualConfirmation,
        bool understandsFileMoves,
        string expectedMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            GuardedFileMoveCommandBuilder.BuildRestore(new GuardedRestoreCommandOptions(
                MetadataPath: @".\abcd.restore.json",
                ManualConfirmation: manualConfirmation,
                UnderstandsFileMoves: understandsFileMoves)));

        Assert.Equal(expectedMessage, exception.Message);
    }
}
