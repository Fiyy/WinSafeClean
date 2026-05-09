using System.Security;

namespace WinSafeClean.Windows.Evidence;

public sealed class FileSystemWindowsShortcutSource : IWindowsShortcutSource
{
    private readonly IReadOnlyList<string> rootPaths;

    public FileSystemWindowsShortcutSource()
        : this(GetDefaultShortcutRootPaths())
    {
    }

    public FileSystemWindowsShortcutSource(IReadOnlyList<string> rootPaths)
    {
        ArgumentNullException.ThrowIfNull(rootPaths);

        this.rootPaths = rootPaths;
    }

    public IReadOnlyList<WindowsShortcutRecord> GetShortcuts()
    {
        var shortcuts = new List<WindowsShortcutRecord>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootPath in rootPaths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(rootPath))
            {
                continue;
            }

            foreach (var shortcutPath in EnumerateShortcutFiles(rootPath))
            {
                if (!seenPaths.Add(shortcutPath))
                {
                    continue;
                }

                var shortcut = WindowsShellLinkReader.TryRead(shortcutPath);
                if (shortcut is not null)
                {
                    shortcuts.Add(shortcut);
                }
            }
        }

        return shortcuts;
    }

    private static IReadOnlyList<string> GetDefaultShortcutRootPaths()
    {
        return
        [
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
            Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            Environment.GetFolderPath(Environment.SpecialFolder.Programs),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
        ];
    }

    private static IEnumerable<string> EnumerateShortcutFiles(string rootPath)
    {
        var directories = new Stack<string>();
        directories.Push(rootPath);

        while (directories.Count > 0)
        {
            var currentDirectory = directories.Pop();

            foreach (var filePath in GetShortcutFiles(currentDirectory))
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

    private static IReadOnlyList<string> GetShortcutFiles(string directoryPath)
    {
        try
        {
            return Directory.GetFiles(directoryPath, "*.lnk");
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
}
