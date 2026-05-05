using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security;

namespace WinSafeClean.Windows.Evidence;

[SupportedOSPlatform("windows")]
public sealed class RegistryWindowsStartupEntrySource : IWindowsStartupEntrySource
{
    private static readonly RegistryStartupLocation[] StartupLocations =
    [
        new(Registry.CurrentUser, "HKCU", @"Software\Microsoft\Windows\CurrentVersion\Run"),
        new(Registry.CurrentUser, "HKCU", @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
        new(Registry.LocalMachine, "HKLM", @"Software\Microsoft\Windows\CurrentVersion\Run"),
        new(Registry.LocalMachine, "HKLM", @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
        new(Registry.LocalMachine, "HKLM", @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"),
        new(Registry.LocalMachine, "HKLM", @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce")
    ];

    public IReadOnlyList<WindowsStartupEntryRecord> GetStartupEntries()
    {
        var entries = new List<WindowsStartupEntryRecord>();

        foreach (var location in StartupLocations)
        {
            entries.AddRange(ReadStartupLocation(location));
        }

        return entries;
    }

    private static IReadOnlyList<WindowsStartupEntryRecord> ReadStartupLocation(RegistryStartupLocation location)
    {
        try
        {
            using var startupKey = location.Hive.OpenSubKey(location.SubKeyPath);
            if (startupKey is null)
            {
                return [];
            }

            var entries = new List<WindowsStartupEntryRecord>();
            foreach (var valueName in startupKey.GetValueNames())
            {
                var value = startupKey.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (value is string command && !string.IsNullOrWhiteSpace(command))
                {
                    entries.Add(new WindowsStartupEntryRecord(
                        Scope: location.Scope,
                        Location: location.SubKeyPath,
                        Name: valueName,
                        Command: command));
                }
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

    private sealed record RegistryStartupLocation(RegistryKey Hive, string Scope, string SubKeyPath);
}
