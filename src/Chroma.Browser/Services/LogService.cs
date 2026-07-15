using System.Globalization;

namespace Chroma.Browser.Services;

public sealed class LogService
{
    private readonly object _gate = new();
    private readonly string _path;

    public static LogService Instance { get; } = new();

    private LogService()
    {
        AppPaths.EnsureCreated();
        _path = Path.Combine(AppPaths.Logs, $"chroma-{DateTime.UtcNow:yyyyMMdd}.log");
        RemoveExpiredLogs();
    }

    public void Info(string message) => Write("INF", message, null);
    public void Warn(string message) => Write("WRN", message, null);
    public void Error(string message, Exception? exception = null) => Write("ERR", message, exception);

    private void Write(string level, string message, Exception? exception)
    {
        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTimeOffset.Now:O} [{level}] {message}{(exception is null ? string.Empty : Environment.NewLine + exception)}{Environment.NewLine}");

        lock (_gate)
        {
            try
            {
                File.AppendAllText(_path, line);
            }
            catch
            {
                // Logging must never terminate the browser.
            }
        }
    }

    private static void RemoveExpiredLogs()
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(AppPaths.Logs, "chroma-*.log"))
            {
                if (File.GetLastWriteTimeUtc(file) < DateTime.UtcNow.AddDays(-14))
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Cleanup is best effort.
        }
    }
}

