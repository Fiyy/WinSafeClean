namespace WinSafeClean.Core.Quarantine;

public interface IQuarantineFileSystem
{
    bool FileExists(string path);

    bool DirectoryExists(string path);

    void CreateDirectory(string path);

    void MoveFile(string sourcePath, string targetPath);

    void WriteNewTextFile(string path, string contents);

    void AppendTextFile(string path, string contents);

    void DeleteFileIfExists(string path);
}
