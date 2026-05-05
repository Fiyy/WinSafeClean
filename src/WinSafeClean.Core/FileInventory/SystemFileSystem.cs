namespace WinSafeClean.Core.FileInventory;

internal sealed class SystemFileSystem : IFileSystem
{
    public static SystemFileSystem Instance { get; } = new();

    private SystemFileSystem()
    {
    }

    public string GetFullPath(string path)
    {
        return Path.GetFullPath(path);
    }

    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public IEnumerable<string> EnumerateFileSystemEntries(string path)
    {
        return Directory.EnumerateFileSystemEntries(path);
    }

    public long GetFileLength(string path)
    {
        return new FileInfo(path).Length;
    }

    public DateTimeOffset GetFileLastWriteTimeUtc(string path)
    {
        return new DateTimeOffset(new FileInfo(path).LastWriteTimeUtc);
    }

    public DateTimeOffset GetDirectoryLastWriteTimeUtc(string path)
    {
        return new DateTimeOffset(new DirectoryInfo(path).LastWriteTimeUtc);
    }

    public bool IsReparsePoint(string path)
    {
        return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    }
}
