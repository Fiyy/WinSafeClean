using Microsoft.Win32;
using System.Runtime.Versioning;

namespace WinSafeClean.Windows.Evidence;

[SupportedOSPlatform("windows")]
public sealed class RegistryWindowsServiceSource : IWindowsServiceSource
{
    private const string ServicesKeyPath = @"SYSTEM\CurrentControlSet\Services";

    public IReadOnlyList<WindowsServiceRecord> GetServices()
    {
        using var servicesKey = Registry.LocalMachine.OpenSubKey(ServicesKeyPath);
        if (servicesKey is null)
        {
            return [];
        }

        var services = new List<WindowsServiceRecord>();

        foreach (var serviceName in servicesKey.GetSubKeyNames())
        {
            using var serviceKey = servicesKey.OpenSubKey(serviceName);
            if (serviceKey is null)
            {
                continue;
            }

            services.Add(new WindowsServiceRecord(
                Name: serviceName,
                DisplayName: serviceKey.GetValue("DisplayName") as string,
                ImagePath: serviceKey.GetValue("ImagePath") as string));
        }

        return services;
    }
}
