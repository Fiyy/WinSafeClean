namespace WinSafeClean.Core.Quarantine;

public sealed class SystemQuarantineFileSystem : IQuarantineFileSystem
{
    public bool FileExists(string path)
    {
        return File.Exists(path);
    }

    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public void MoveFile(string sourcePath, string targetPath)
    {
        File.Move(sourcePath, targetPath, overwrite: false);
    }

    public void WriteNewTextFile(string path, string contents)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream);
        writer.Write(contents);
    }

    public void AppendTextFile(string path, string contents)
    {
        File.AppendAllText(path, contents);
    }

    public void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
