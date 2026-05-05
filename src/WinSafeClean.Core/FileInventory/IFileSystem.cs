namespace WinSafeClean.Core.FileInventory;

public interface IFileSystem
{
    string GetFullPath(string path);

    bool FileExists(string path);

    bool DirectoryExists(string path);

    IEnumerable<string> EnumerateFileSystemEntries(string path);

    long GetFileLength(string path);

    DateTimeOffset GetFileLastWriteTimeUtc(string path);

    DateTimeOffset GetDirectoryLastWriteTimeUtc(string path);
}
