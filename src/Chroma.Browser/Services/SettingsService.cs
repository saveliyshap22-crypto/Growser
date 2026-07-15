using Chroma.Browser.Models;

namespace Chroma.Browser.Services;

public sealed class SettingsService
{
    private readonly JsonStore<AppSettings> _store = new(AppPaths.SettingsFile);

    public AppSettings Current { get; private set; } = new();

    public async Task InitializeAsync()
    {
        Current = await _store.LoadAsync().ConfigureAwait(false);
        Current.FilterSubscriptions ??= FilterSubscription.CreateDefaults();
        if (string.IsNullOrWhiteSpace(Current.DownloadFolder))
        {
            Current.DownloadFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }

    public Task SaveAsync() => _store.SaveAsync(Current);
}

