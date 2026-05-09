using System.Security;

namespace WinSafeClean.Windows.Evidence;

public sealed class FileSystemWindowsStorePackageSource : IWindowsStorePackageSource
{
    private readonly IReadOnlyList<WindowsStorePackageRoot> packageRoots;

    public FileSystemWindowsStorePackageSource()
        : this(GetDefaultPackageRoots())
    {
    }

    public FileSystemWindowsStorePackageSource(IReadOnlyList<WindowsStorePackageRoot> packageRoots)
    {
        ArgumentNullException.ThrowIfNull(packageRoots);

        this.packageRoots = packageRoots;
    }

    public IReadOnlyList<WindowsStorePackageRecord> GetStorePackages()
    {
        var packages = new List<WindowsStorePackageRecord>();
        var seenPackageRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageRoot in packageRoots.Where(root => !string.IsNullOrWhiteSpace(root.RootPath)))
        {
            if (!Directory.Exists(packageRoot.RootPath))
            {
                continue;
            }

            foreach (var directoryPath in GetDirectories(packageRoot.RootPath))
            {
                if (!seenPackageRoots.Add(directoryPath) || IsReparsePoint(directoryPath))
                {
                    continue;
                }

                packages.Add(new WindowsStorePackageRecord(
                    PackageName: Path.GetFileName(directoryPath),
                    PackageRoot: directoryPath,
                    RootKind: packageRoot.RootKind));
            }
        }

        return packages;
    }

    private static IReadOnlyList<WindowsStorePackageRoot> GetDefaultPackageRoots()
    {
        var roots = new List<WindowsStorePackageRoot>();

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            roots.Add(new WindowsStorePackageRoot(
                RootPath: Path.Combine(programFiles, "WindowsApps"),
                RootKind: "Package install location"));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            roots.Add(new WindowsStorePackageRoot(
                RootPath: Path.Combine(localAppData, "Packages"),
                RootKind: "Package data root"));
        }

        return roots;
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
