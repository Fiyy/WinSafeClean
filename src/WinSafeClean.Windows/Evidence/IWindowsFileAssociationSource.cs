namespace WinSafeClean.Windows.Evidence;

public interface IWindowsFileAssociationSource
{
    IReadOnlyList<WindowsFileAssociationRecord> GetFileAssociations();
}
