using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security;

namespace WinSafeClean.Windows.Evidence;

[SupportedOSPlatform("windows")]
public sealed class RegistryWindowsFileAssociationSource : IWindowsFileAssociationSource
{
    private static readonly RegistryAssociationLocation[] AssociationLocations =
    [
        new(Registry.CurrentUser, "HKCU", @"Software\Classes"),
        new(Registry.LocalMachine, "HKLM", @"Software\Classes")
    ];

    public IReadOnlyList<WindowsFileAssociationRecord> GetFileAssociations()
    {
        var associations = new List<WindowsFileAssociationRecord>();

        foreach (var location in AssociationLocations)
        {
            associations.AddRange(ReadAssociationLocation(location));
        }

        return associations;
    }

    private static IReadOnlyList<WindowsFileAssociationRecord> ReadAssociationLocation(RegistryAssociationLocation location)
    {
        try
        {
            using var classesKey = location.Hive.OpenSubKey(location.SubKeyPath);
            if (classesKey is null)
            {
                return [];
            }

            var associations = new List<WindowsFileAssociationRecord>();
            var seenCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var extension in classesKey.GetSubKeyNames().Where(name => name.StartsWith(".", StringComparison.Ordinal)))
            {
                using var extensionKey = classesKey.OpenSubKey(extension);
                if (extensionKey is null)
                {
                    continue;
                }

                var progId = ReadDefaultString(extensionKey);
                AddCommandRecords(
                    associations,
                    seenCommands,
                    location,
                    extension,
                    progId: null,
                    classKeyName: extension,
                    classKey: extensionKey);

                if (!string.IsNullOrWhiteSpace(progId))
                {
                    using var progIdKey = classesKey.OpenSubKey(progId);
                    AddCommandRecords(
                        associations,
                        seenCommands,
                        location,
                        extension,
                        progId,
                        classKeyName: progId,
                        classKey: progIdKey);
                }
            }

            return associations;
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

    private static void AddCommandRecords(
        List<WindowsFileAssociationRecord> associations,
        HashSet<string> seenCommands,
        RegistryAssociationLocation location,
        string extension,
        string? progId,
        string classKeyName,
        RegistryKey? classKey)
    {
        using var shellKey = classKey?.OpenSubKey("shell");
        if (shellKey is null)
        {
            return;
        }

        foreach (var verb in shellKey.GetSubKeyNames())
        {
            using var commandKey = shellKey.OpenSubKey($@"{verb}\command");
            if (commandKey is null)
            {
                continue;
            }

            var command = ReadDefaultString(commandKey);
            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            var registryPath = $@"{location.SubKeyPath}\{classKeyName}\shell\{verb}\command";
            var identity = $@"{location.Scope}\{registryPath}|{extension}|{progId}|{command}";
            if (!seenCommands.Add(identity))
            {
                continue;
            }

            associations.Add(new WindowsFileAssociationRecord(
                Scope: location.Scope,
                Extension: extension,
                ProgId: progId,
                Verb: verb,
                Command: command,
                RegistryPath: registryPath));
        }
    }

    private static string? ReadDefaultString(RegistryKey key)
    {
        var value = key.GetValue(null, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return value is string text && !string.IsNullOrWhiteSpace(text) ? text : null;
    }

    private sealed record RegistryAssociationLocation(RegistryKey Hive, string Scope, string SubKeyPath);
}
