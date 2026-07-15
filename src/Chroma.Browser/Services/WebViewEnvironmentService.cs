using Microsoft.Web.WebView2.Core;

namespace Chroma.Browser.Services;

public sealed class WebViewEnvironmentService
{
    private readonly SemaphoreSlim _normalGate = new(1, 1);
    private readonly SemaphoreSlim _privateGate = new(1, 1);
    private CoreWebView2Environment? _normal;
    private CoreWebView2Environment? _private;
    private string? _privatePath;

    public async Task<CoreWebView2Environment> GetAsync(bool isPrivate)
    {
        return isPrivate
            ? await GetPrivateAsync().ConfigureAwait(true)
            : await GetNormalAsync().ConfigureAwait(true);
    }

    public void CleanupPrivateData()
    {
        if (string.IsNullOrWhiteSpace(_privatePath))
        {
            return;
        }

        TryDeleteDirectory(_privatePath);
    }

    public static void CleanupAbandonedPrivateData()
    {
        if (!Directory.Exists(AppPaths.Temp))
        {
            return;
        }

        foreach (var directory in Directory.EnumerateDirectories(AppPaths.Temp, "Private-*"))
        {
            try
            {
                if (Directory.GetCreationTimeUtc(directory) < DateTime.UtcNow.AddHours(-12))
                {
                    TryDeleteDirectory(directory);
                }
            }
            catch (Exception exception)
            {
                LogService.Instance.Warn($"Could not inspect private profile: {exception.Message}");
            }
        }
    }

    private async Task<CoreWebView2Environment> GetNormalAsync()
    {
        if (_normal is not null)
        {
            return _normal;
        }

        await _normalGate.WaitAsync().ConfigureAwait(true);
        try
        {
            _normal ??= await CreateAsync(AppPaths.UserData, true).ConfigureAwait(true);
            return _normal;
        }
        finally
        {
            _normalGate.Release();
        }
    }

    private async Task<CoreWebView2Environment> GetPrivateAsync()
    {
        if (_private is not null)
        {
            return _private;
        }

        await _privateGate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (_private is null)
            {
                _privatePath = Path.Combine(AppPaths.Temp, $"Private-{Environment.ProcessId}-{Guid.NewGuid():N}");
                _private = await CreateAsync(_privatePath, false).ConfigureAwait(true);
            }

            return _private;
        }
        finally
        {
            _privateGate.Release();
        }
    }

    private static Task<CoreWebView2Environment> CreateAsync(string userDataFolder, bool extensionsEnabled)
    {
        Directory.CreateDirectory(userDataFolder);
        var options = new CoreWebView2EnvironmentOptions
        {
            AreBrowserExtensionsEnabled = extensionsEnabled,
            EnableTrackingPrevention = true,
            ExclusiveUserDataFolderAccess = false,
            IsCustomCrashReportingEnabled = true,
            Language = System.Globalization.CultureInfo.CurrentUICulture.Name
        };

        return CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Private data cleanup will be retried later: {exception.Message}");
        }
    }
}

