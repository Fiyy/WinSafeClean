using System.Runtime.InteropServices;
using System.Text;

namespace WinSafeClean.Windows.Evidence;

internal static class WindowsShellLinkReader
{
    private const uint StgmRead = 0x00000000;
    private const uint SlgpRawPath = 0x00000004;
    private const int BufferLength = 32768;
    private static readonly Guid ShellLinkClassId = new("00021401-0000-0000-C000-000000000046");

    public static WindowsShortcutRecord? TryRead(string shortcutPath)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath) || !OperatingSystem.IsWindows())
        {
            return null;
        }

        object? link = null;
        try
        {
            var shellLinkType = Type.GetTypeFromCLSID(ShellLinkClassId);
            if (shellLinkType is null)
            {
                return null;
            }

            link = Activator.CreateInstance(shellLinkType);
            if (link is null)
            {
                return null;
            }

            var persistFile = (IPersistFile)link;
            if (persistFile.Load(shortcutPath, StgmRead) != 0)
            {
                return null;
            }

            var shellLink = (IShellLinkW)link;
            var targetPath = ReadPath(shellLink);
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return null;
            }

            return new WindowsShortcutRecord(
                ShortcutPath: shortcutPath,
                TargetPath: targetPath,
                Arguments: ReadString(shellLink.GetArguments),
                WorkingDirectory: ReadString(shellLink.GetWorkingDirectory));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        finally
        {
            if (link is not null && Marshal.IsComObject(link))
            {
                Marshal.FinalReleaseComObject(link);
            }
        }
    }

    private static string? ReadPath(IShellLinkW shellLink)
    {
        var buffer = new StringBuilder(BufferLength);
        var result = shellLink.GetPath(buffer, buffer.Capacity, IntPtr.Zero, SlgpRawPath);
        return result == 0 ? NormalizeEmpty(buffer.ToString()) : null;
    }

    private static string? ReadString(Func<StringBuilder, int, int> reader)
    {
        var buffer = new StringBuilder(BufferLength);
        var result = reader(buffer, buffer.Capacity);
        return result == 0 ? NormalizeEmpty(buffer.ToString()) : null;
    }

    private static string? NormalizeEmpty(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        [PreserveSig]
        int GetClassID(out Guid classId);

        [PreserveSig]
        int IsDirty();

        [PreserveSig]
        int Load([MarshalAs(UnmanagedType.LPWStr)] string fileName, uint mode);

        [PreserveSig]
        int Save([MarshalAs(UnmanagedType.LPWStr)] string fileName, bool remember);

        [PreserveSig]
        int SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string fileName);

        [PreserveSig]
        int GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string fileName);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        [PreserveSig]
        int GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder file, int maxPath, IntPtr findData, uint flags);

        [PreserveSig]
        int GetIDList(out IntPtr idList);

        [PreserveSig]
        int SetIDList(IntPtr idList);

        [PreserveSig]
        int GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int maxName);

        [PreserveSig]
        int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);

        [PreserveSig]
        int GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder directory, int maxPath);

        [PreserveSig]
        int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);

        [PreserveSig]
        int GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int maxPath);

        [PreserveSig]
        int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);

        [PreserveSig]
        int GetHotkey(out short hotkey);

        [PreserveSig]
        int SetHotkey(short hotkey);

        [PreserveSig]
        int GetShowCmd(out int showCommand);

        [PreserveSig]
        int SetShowCmd(int showCommand);

        [PreserveSig]
        int GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder iconPath, int maxIconPath, out int iconIndex);

        [PreserveSig]
        int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string iconPath, int iconIndex);

        [PreserveSig]
        int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, uint reserved);

        [PreserveSig]
        int Resolve(IntPtr windowHandle, uint flags);

        [PreserveSig]
        int SetPath([MarshalAs(UnmanagedType.LPWStr)] string path);
    }
}
