using System.Text.Json.Serialization;
using Microsoft.Web.WebView2.Core;

namespace Chroma.Browser.Models;

public sealed class BrowserData
{
    public List<BookmarkEntry> Bookmarks { get; set; } = [];
    public List<HistoryEntry> History { get; set; } = [];
    public List<DownloadRecord> Downloads { get; set; } = [];
}

public sealed class BookmarkEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Folder { get; set; } = "Избранное";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}

public sealed class HistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTimeOffset VisitedAt { get; set; } = DateTimeOffset.Now;
}

public enum DownloadState
{
    Starting,
    InProgress,
    Paused,
    Completed,
    Cancelled,
    Interrupted
}

public sealed class DownloadRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long ReceivedBytes { get; set; }
    public DownloadState State { get; set; } = DownloadState.Starting;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.Now;

    [JsonIgnore]
    public CoreWebView2DownloadOperation? Operation { get; set; }
}

public sealed class BrowserTabState
{
    public string Url { get; set; } = "chroma://newtab";
    public string Title { get; set; } = "Новая вкладка";
    public bool IsPinned { get; set; }
    public string Group { get; set; } = string.Empty;
}

public sealed class BrowserSessionState
{
    public List<BrowserTabState> Tabs { get; set; } = [];
    public int SelectedIndex { get; set; }
}

public sealed class VaultEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Origin { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
}

