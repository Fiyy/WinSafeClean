using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security;

namespace WinSafeClean.Windows.Evidence;

[SupportedOSPlatform("windows")]
public sealed class RegistryWindowsUninstallEntrySource : IWindowsUninstallEntrySource
{
    private static readonly RegistryUninstallLocation[] UninstallLocations =
    [
        new(Registry.CurrentUser, "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
        new(Registry.LocalMachine, "HKLM", @"Software\Microsoft\Windows\CurrentVersion\Uninstall"),
        new(Registry.LocalMachine, "HKLM", @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall")
    ];

    public IReadOnlyList<WindowsUninstallEntryRecord> GetUninstallEntries()
    {
        var entries = new List<WindowsUninstallEntryRecord>();

        foreach (var location in UninstallLocations)
        {
            entries.AddRange(ReadUninstallLocation(location));
        }

        return entries;
    }

    private static IReadOnlyList<WindowsUninstallEntryRecord> ReadUninstallLocation(RegistryUninstallLocation location)
    {
        try
        {
            using var uninstallKey = location.Hive.OpenSubKey(location.SubKeyPath);
            if (uninstallKey is null)
            {
                return [];
            }

            var entries = new List<WindowsUninstallEntryRecord>();
            foreach (var subKeyName in uninstallKey.GetSubKeyNames())
            {
                using var applicationKey = uninstallKey.OpenSubKey(subKeyName);
                if (applicationKey is null)
                {
                    continue;
                }

                entries.Add(new WindowsUninstallEntryRecord(
                    Scope: location.Scope,
                    Location: location.SubKeyPath,
                    KeyName: subKeyName,
                    DisplayName: ReadString(applicationKey, "DisplayName"),
                    InstallLocation: ReadString(applicationKey, "InstallLocation"),
                    UninstallString: ReadString(applicationKey, "UninstallString"),
                    QuietUninstallString: ReadString(applicationKey, "QuietUninstallString"),
                    DisplayIcon: ReadString(applicationKey, "DisplayIcon")));
            }

            return entries;
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

    private static string? ReadString(RegistryKey key, string valueName)
    {
        var value = key.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return value is string text && !string.IsNullOrWhiteSpace(text) ? text : null;
    }

    private sealed record RegistryUninstallLocation(RegistryKey Hive, string Scope, string SubKeyPath);
}
