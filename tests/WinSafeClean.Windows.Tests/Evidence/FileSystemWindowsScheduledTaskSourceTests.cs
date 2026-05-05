using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class FileSystemWindowsScheduledTaskSourceTests
{
    [Fact]
    public void ShouldReadExecActionsFromScheduledTaskXml()
    {
        using var sandbox = TemporarySandbox.Create();
        sandbox.WriteText(
            "Microsoft\\Windows\\ExampleTask",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
              <RegistrationInfo>
                <URI>\Microsoft\Windows\ExampleTask</URI>
              </RegistrationInfo>
              <Actions Context="Author">
                <Exec>
                  <Command>C:\Tools\example.exe</Command>
                  <Arguments>--run</Arguments>
                  <WorkingDirectory>C:\Tools</WorkingDirectory>
                </Exec>
              </Actions>
            </Task>
            """);
        var source = new FileSystemWindowsScheduledTaskSource(sandbox.RootPath);

        var tasks = source.GetScheduledTasks();

        var task = Assert.Single(tasks);
        Assert.Equal(@"\Microsoft\Windows\ExampleTask", task.Path);
        Assert.Equal(@"\Microsoft\Windows\ExampleTask", task.Uri);
        var action = Assert.Single(task.Actions);
        Assert.Equal(@"C:\Tools\example.exe", action.Command);
        Assert.Equal("--run", action.Arguments);
        Assert.Equal(@"C:\Tools", action.WorkingDirectory);
    }

    [Fact]
    public void ShouldSkipMalformedTaskXml()
    {
        using var sandbox = TemporarySandbox.Create();
        sandbox.WriteText("BrokenTask", "<Task>");
        var source = new FileSystemWindowsScheduledTaskSource(sandbox.RootPath);

        var tasks = source.GetScheduledTasks();

        Assert.Empty(tasks);
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
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
        }

        public void WriteText(string relativePath, string contents)
        {
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
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
