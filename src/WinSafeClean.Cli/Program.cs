using WinSafeClean.Cli;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

return CommandLineApp.Run(
    args,
    Console.Out,
    Console.Error,
    evidenceProvider: CommandLineApp.CreateDefaultEvidenceProvider(),
    cancellationToken: cancellation.Token);
