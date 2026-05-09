using System.Diagnostics;

namespace WinSafeClean.Ui.Operations;

public sealed record ReadOnlyOperationRunnerOptions(
    string DotNetPath,
    string CliProjectPath,
    string WorkingDirectory);

public sealed record ReadOnlyOperationProcessRequest(
    string FileName,
    string WorkingDirectory,
    IReadOnlyList<string> Arguments);

public sealed record ReadOnlyOperationProcessResult(
    int ExitCode,
    string StandardOutput,
    string StandardError);

public sealed record ReadOnlyOperationRunResult(
    int ExitCode,
    string StandardOutput,
    string StandardError,
    string CommandText)
{
    public bool Succeeded => ExitCode == 0;
}

public interface IReadOnlyOperationProcessRunner
{
    Task<ReadOnlyOperationProcessResult> RunAsync(
        ReadOnlyOperationProcessRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ReadOnlyOperationRunner
{
    private static readonly string[] AllowedCommands = ["scan", "plan", "preflight"];
    private static readonly string[] ExecutableCommands = ["quarantine", "restore", "delete", "clean"];
    private static readonly string[] ExecutableOptions = ["--delete", "--fix", "--quarantine", "--clean"];

    private readonly ReadOnlyOperationRunnerOptions _options;
    private readonly IReadOnlyOperationProcessRunner _processRunner;

    public ReadOnlyOperationRunner(
        ReadOnlyOperationRunnerOptions options,
        IReadOnlyOperationProcessRunner? processRunner = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _processRunner = processRunner ?? new SystemReadOnlyOperationProcessRunner();
    }

    public async Task<ReadOnlyOperationRunResult> RunAsync(
        IReadOnlyList<string> commandArguments,
        CancellationToken cancellationToken = default)
    {
        ValidateCommand(commandArguments);

        var processArguments = new List<string>
        {
            "run",
            "--project",
            _options.CliProjectPath,
            "--"
        };
        processArguments.AddRange(commandArguments);

        var request = new ReadOnlyOperationProcessRequest(
            _options.DotNetPath,
            _options.WorkingDirectory,
            processArguments);
        var result = await _processRunner.RunAsync(request, cancellationToken).ConfigureAwait(false);

        return new ReadOnlyOperationRunResult(
            result.ExitCode,
            result.StandardOutput,
            result.StandardError,
            FormatCommand(_options.DotNetPath, processArguments));
    }

    private static void ValidateCommand(IReadOnlyList<string> commandArguments)
    {
        if (commandArguments.Count == 0)
        {
            throw new InvalidOperationException("A read-only command is required.");
        }

        var command = commandArguments[0];
        if (ExecutableCommands.Contains(command, StringComparer.OrdinalIgnoreCase)
            || !AllowedCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("UI execution only supports read-only scan, plan, and preflight commands.");
        }

        if (commandArguments.Any(arg => ExecutableOptions.Contains(arg, StringComparer.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Executable cleanup options are not available from the UI runner.");
        }
    }

    private static string FormatCommand(string fileName, IReadOnlyList<string> arguments)
    {
        return QuoteIfNeeded(fileName) + " " + string.Join(" ", arguments.Select(QuoteIfNeeded));
    }

    private static string QuoteIfNeeded(string value)
    {
        return value.Contains(' ', StringComparison.Ordinal)
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }
}

public sealed class SystemReadOnlyOperationProcessRunner : IReadOnlyOperationProcessRunner
{
    public async Task<ReadOnlyOperationProcessResult> RunAsync(
        ReadOnlyOperationProcessRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var startInfo = new ProcessStartInfo(request.FileName)
        {
            WorkingDirectory = request.WorkingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Read-only command process could not be started.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            throw;
        }

        return new ReadOnlyOperationProcessResult(
            process.ExitCode,
            await stdoutTask.ConfigureAwait(false),
            await stderrTask.ConfigureAwait(false));
    }
}
