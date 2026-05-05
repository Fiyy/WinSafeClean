namespace WinSafeClean.Core.Reporting;

public enum EvidenceType
{
    Unknown = 0,
    ServiceReference,
    ScheduledTaskReference,
    StartupReference,
    UninstallRegistryReference,
    RunningProcessReference,
    PathEnvironmentReference,
    ShortcutReference,
    FileSignature,
    InstalledApplication,
    MicrosoftStorePackage,
    WindowsComponent,
    KnownCleanupRule,
    ProtectedPathRule,
    Metadata,
    CollectionFailure
}
