using System.Globalization;

namespace WinSafeClean.Ui.ViewModels;

internal static class UtcTimestampFormatter
{
    public static string Format(DateTimeOffset? timestamp)
    {
        return timestamp is null
            ? "-"
            : timestamp.Value
                .ToUniversalTime()
                .ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);
    }
}
