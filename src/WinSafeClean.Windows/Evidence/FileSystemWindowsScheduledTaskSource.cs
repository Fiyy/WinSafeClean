using System.Security;
using System.Xml;
using System.Xml.Linq;

namespace WinSafeClean.Windows.Evidence;

public sealed class FileSystemWindowsScheduledTaskSource : IWindowsScheduledTaskSource
{
    private readonly string rootPath;

    public FileSystemWindowsScheduledTaskSource()
        : this(GetDefaultTasksRootPath())
    {
    }

    public FileSystemWindowsScheduledTaskSource(string rootPath)
    {
        ArgumentNullException.ThrowIfNull(rootPath);

        this.rootPath = rootPath;
    }

    public IReadOnlyList<WindowsScheduledTaskRecord> GetScheduledTasks()
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return [];
        }

        var tasks = new List<WindowsScheduledTaskRecord>();
        foreach (var taskFilePath in EnumerateTaskFiles(rootPath))
        {
            var task = TryReadTask(rootPath, taskFilePath);
            if (task is not null)
            {
                tasks.Add(task);
            }
        }

        return tasks;
    }

    private static string GetDefaultTasksRootPath()
    {
        var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
        return string.IsNullOrWhiteSpace(systemRoot)
            ? string.Empty
            : Path.Combine(systemRoot, "System32", "Tasks");
    }

    private static WindowsScheduledTaskRecord? TryReadTask(string rootPath, string taskFilePath)
    {
        try
        {
            var document = XDocument.Load(taskFilePath);
            var actions = ReadExecActions(document).ToArray();
            if (actions.Length == 0)
            {
                return null;
            }

            return new WindowsScheduledTaskRecord(
                Path: FormatTaskPath(rootPath, taskFilePath),
                Uri: ReadFirstDescendantValue(document, "URI"),
                Actions: actions);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static IEnumerable<WindowsScheduledTaskActionRecord> ReadExecActions(XDocument document)
    {
        foreach (var execElement in document.Descendants().Where(element => element.Name.LocalName == "Exec"))
        {
            var command = ReadChildValue(execElement, "Command");
            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            yield return new WindowsScheduledTaskActionRecord(
                Command: command,
                Arguments: ReadChildValue(execElement, "Arguments"),
                WorkingDirectory: ReadChildValue(execElement, "WorkingDirectory"));
        }
    }

    private static string? ReadFirstDescendantValue(XDocument document, string localName)
    {
        return document
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == localName)
            ?.Value
            .Trim();
    }

    private static string? ReadChildValue(XElement element, string localName)
    {
        return element
            .Elements()
            .FirstOrDefault(child => child.Name.LocalName == localName)
            ?.Value
            .Trim();
    }

    private static IEnumerable<string> EnumerateTaskFiles(string rootPath)
    {
        var directories = new Stack<string>();
        directories.Push(rootPath);

        while (directories.Count > 0)
        {
            var currentDirectory = directories.Pop();

            foreach (var filePath in GetFiles(currentDirectory))
            {
                yield return filePath;
            }

            foreach (var directoryPath in GetDirectories(currentDirectory))
            {
                if (!IsReparsePoint(directoryPath))
                {
                    directories.Push(directoryPath);
                }
            }
        }
    }

    private static IReadOnlyList<string> GetFiles(string directoryPath)
    {
        try
        {
            return Directory.GetFiles(directoryPath);
        }
        catch (IOException)
        {
            return [];
        }
        catch (SecurityException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> GetDirectories(string directoryPath)
    {
        try
        {
            return Directory.GetDirectories(directoryPath);
        }
        catch (IOException)
        {
            return [];
        }
        catch (SecurityException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static bool IsReparsePoint(string directoryPath)
    {
        try
        {
            return new DirectoryInfo(directoryPath).Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
        catch (IOException)
        {
            return true;
        }
        catch (SecurityException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static string FormatTaskPath(string rootPath, string taskFilePath)
    {
        var relativePath = Path.GetRelativePath(rootPath, taskFilePath)
            .Replace(Path.AltDirectorySeparatorChar, '\\')
            .Replace(Path.DirectorySeparatorChar, '\\');

        return relativePath.StartsWith('\\')
            ? relativePath
            : $@"\{relativePath}";
    }
}
