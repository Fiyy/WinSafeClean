using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class ReadOnlyOperationRunnerTests
{
    [Fact]
    public async Task ShouldRunReadOnlyCommandThroughDotnetCliProject()
    {
        var processRunner = new RecordingProcessRunner(new ReadOnlyOperationProcessResult(0, "{}", string.Empty));
        var runner = new ReadOnlyOperationRunner(
            new ReadOnlyOperationRunnerOptions(
                DotNetPath: @".\.tools\dotnet\dotnet.exe",
                CliProjectPath: @".\src\WinSafeClean.Cli",
                WorkingDirectory: @"C:\repo"),
            processRunner);

        var result = await runner.RunAsync(["scan", "--path", @"C:\Temp"]);

        Assert.True(result.Succeeded);
        Assert.Equal("{}", result.StandardOutput);
        Assert.Equal(@".\.tools\dotnet\dotnet.exe", processRunner.Request!.FileName);
        Assert.Equal(@"C:\repo", processRunner.Request.WorkingDirectory);
        Assert.Equal(
            ["run", "--project", @".\src\WinSafeClean.Cli", "--", "scan", "--path", @"C:\Temp"],
            processRunner.Request.Arguments);
    }

    [Theory]
    [InlineData("quarantine")]
    [InlineData("restore")]
    [InlineData("delete")]
    [InlineData("clean")]
    public async Task ShouldRejectExecutableCommands(string command)
    {
        var runner = CreateRunner();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.RunAsync([command, "--metadata", @".\metadata.json"]));

        Assert.Equal("UI execution only supports read-only scan, plan, and preflight commands.", exception.Message);
    }

    [Theory]
    [InlineData("--delete")]
    [InlineData("--fix")]
    [InlineData("--quarantine")]
    [InlineData("--clean")]
    public async Task ShouldRejectExecutableOptions(string option)
    {
        var runner = CreateRunner();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runner.RunAsync(["scan", "--path", ".", option]));

        Assert.Equal("Executable cleanup options are not available from the UI runner.", exception.Message);
    }

    [Fact]
    public async Task ShouldExposeFailedCommandOutput()
    {
        var processRunner = new RecordingProcessRunner(new ReadOnlyOperationProcessResult(2, string.Empty, "--output must not overwrite existing files."));
        var runner = new ReadOnlyOperationRunner(
            new ReadOnlyOperationRunnerOptions(
                DotNetPath: @".\.tools\dotnet\dotnet.exe",
                CliProjectPath: @".\src\WinSafeClean.Cli",
                WorkingDirectory: @"C:\repo"),
            processRunner);

        var result = await runner.RunAsync(["plan", "--path", ".", "--output", @".\plan.json"]);

        Assert.False(result.Succeeded);
        Assert.Equal(2, result.ExitCode);
        Assert.Equal("--output must not overwrite existing files.", result.StandardError);
    }

    private static ReadOnlyOperationRunner CreateRunner()
    {
        return new ReadOnlyOperationRunner(
            new ReadOnlyOperationRunnerOptions(
                DotNetPath: @".\.tools\dotnet\dotnet.exe",
                CliProjectPath: @".\src\WinSafeClean.Cli",
                WorkingDirectory: @"C:\repo"),
            new RecordingProcessRunner(new ReadOnlyOperationProcessResult(0, string.Empty, string.Empty)));
    }

    private sealed class RecordingProcessRunner : IReadOnlyOperationProcessRunner
    {
        private readonly ReadOnlyOperationProcessResult _result;

        public RecordingProcessRunner(ReadOnlyOperationProcessResult result)
        {
            _result = result;
        }

        public ReadOnlyOperationProcessRequest? Request { get; private set; }

        public Task<ReadOnlyOperationProcessResult> RunAsync(
            ReadOnlyOperationProcessRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(_result);
        }
    }
}
