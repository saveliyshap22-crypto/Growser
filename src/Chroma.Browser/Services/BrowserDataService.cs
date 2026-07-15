using Chroma.Browser.Models;

namespace Chroma.Browser.Services;

public sealed class BrowserDataService
{
    private readonly JsonStore<BrowserData> _store = new(AppPaths.BrowserDataFile);
    private readonly SemaphoreSlim _mutationGate = new(1, 1);

    public BrowserData Data { get; private set; } = new();
    public event EventHandler? Changed;

    public async Task InitializeAsync()
    {
        Data = await _store.LoadAsync().ConfigureAwait(false);
    }

    public async Task AddHistoryAsync(string title, string url, bool isPrivate)
    {
        if (isPrivate || string.IsNullOrWhiteSpace(url) || url.StartsWith("chroma://", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _mutationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var recent = Data.History.FirstOrDefault(entry =>
                entry.Url.Equals(url, StringComparison.OrdinalIgnoreCase) &&
                DateTimeOffset.Now - entry.VisitedAt < TimeSpan.FromMinutes(2));

            if (recent is not null)
            {
                recent.Title = title;
                recent.VisitedAt = DateTimeOffset.Now;
            }
            else
            {
                Data.History.Insert(0, new HistoryEntry { Title = title, Url = url });
            }

            if (Data.History.Count > 5000)
            {
                Data.History.RemoveRange(5000, Data.History.Count - 5000);
            }

            await _store.SaveAsync(Data).ConfigureAwait(false);
        }
        finally
        {
            _mutationGate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task ToggleBookmarkAsync(string title, string url)
    {
        await _mutationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            var existing = Data.Bookmarks.FirstOrDefault(entry => entry.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                Data.Bookmarks.Insert(0, new BookmarkEntry { Title = title, Url = url });
            }
            else
            {
                Data.Bookmarks.Remove(existing);
            }

            await _store.SaveAsync(Data).ConfigureAwait(false);
        }
        finally
        {
            _mutationGate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsBookmarked(string url) =>
        Data.Bookmarks.Any(entry => entry.Url.Equals(url, StringComparison.OrdinalIgnoreCase));

    public async Task AddDownloadAsync(DownloadRecord download)
    {
        await _mutationGate.WaitAsync().ConfigureAwait(false);
        try
        {
            Data.Downloads.Insert(0, download);
            if (Data.Downloads.Count > 1000)
            {
                Data.Downloads.RemoveRange(1000, Data.Downloads.Count - 1000);
            }

            await _store.SaveAsync(Data).ConfigureAwait(false);
        }
        finally
        {
            _mutationGate.Release();
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveAsync()
    {
        await _store.SaveAsync(Data).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task ClearHistoryAsync()
    {
        Data.History.Clear();
        await SaveAsync().ConfigureAwait(false);
    }
}

