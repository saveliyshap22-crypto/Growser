using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Chroma.Browser.Models;
using Chroma.Browser.Services;
using Microsoft.Win32;

namespace Chroma.Browser.Views;

public partial class DataWindow : Window
{
    private readonly BrowserDataService _data;

    public DataWindow(BrowserDataService data, int tabIndex)
    {
        _data = data;
        InitializeComponent();
        Sections.SelectedIndex = Math.Clamp(tabIndex, 0, 2);
        _data.Changed += Data_Changed;
        Refresh();
    }

    public event EventHandler<string>? UrlOpened;

    private void Data_Changed(object? sender, EventArgs e) => Dispatcher.BeginInvoke(Refresh);
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Refresh();
    private void Sections_SelectionChanged(object sender, SelectionChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        if (!IsLoaded && BookmarkGrid is null)
        {
            return;
        }

        var query = SearchBox?.Text?.Trim() ?? string.Empty;
        BookmarkGrid.ItemsSource = _data.Data.Bookmarks
            .Where(item => Matches(query, item.Title, item.Url, item.Folder))
            .ToArray();
        HistoryGrid.ItemsSource = _data.Data.History
            .Where(item => Matches(query, item.Title, item.Url))
            .ToArray();
        DownloadGrid.ItemsSource = _data.Data.Downloads
            .Where(item => Matches(query, item.FileName, item.SourceUrl, item.TargetPath))
            .ToArray();
    }

    private static bool Matches(string query, params string[] values) =>
        string.IsNullOrWhiteSpace(query) || values.Any(value => value.Contains(query, StringComparison.CurrentCultureIgnoreCase));

    private void OpenBookmark_Click(object sender, RoutedEventArgs e) => OpenBookmark();
    private void BookmarkGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenBookmark();
    private void OpenBookmark()
    {
        if (BookmarkGrid.SelectedItem is BookmarkEntry entry)
        {
            UrlOpened?.Invoke(this, entry.Url);
        }
    }

    private async void DeleteBookmark_Click(object sender, RoutedEventArgs e)
    {
        if (BookmarkGrid.SelectedItem is BookmarkEntry entry)
        {
            _data.Data.Bookmarks.Remove(entry);
            await _data.SaveAsync();
            Refresh();
        }
    }

    private void OpenHistory_Click(object sender, RoutedEventArgs e) => OpenHistory();
    private void HistoryGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenHistory();
    private void OpenHistory()
    {
        if (HistoryGrid.SelectedItem is HistoryEntry entry)
        {
            UrlOpened?.Invoke(this, entry.Url);
        }
    }

    private async void DeleteHistory_Click(object sender, RoutedEventArgs e)
    {
        if (HistoryGrid.SelectedItem is HistoryEntry entry)
        {
            _data.Data.History.Remove(entry);
            await _data.SaveAsync();
            Refresh();
        }
    }

    private async void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Удалить всю историю?", "Chroma Browser", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _data.ClearHistoryAsync();
            Refresh();
        }
    }

    private async void ImportBookmarks_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Импорт закладок",
            Filter = "Закладки Chrome/HTML|*.html;*.htm|Chroma JSON|*.json|Все файлы|*.*"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            List<BookmarkEntry> imported;
            if (Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                await using var stream = File.OpenRead(dialog.FileName);
                imported = await JsonSerializer.DeserializeAsync<List<BookmarkEntry>>(stream) ?? [];
            }
            else
            {
                var html = await File.ReadAllTextAsync(dialog.FileName);
                imported = Regex.Matches(
                        html,
                        "<a[^>]+href=[\\\"'](?<url>[^\\\"']+)[\\\"'][^>]*>(?<title>.*?)</a>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline)
                    .Cast<Match>()
                    .Select(match => new BookmarkEntry
                    {
                        Url = WebUtility.HtmlDecode(match.Groups["url"].Value),
                        Title = WebUtility.HtmlDecode(Regex.Replace(match.Groups["title"].Value, "<.*?>", string.Empty)),
                        Folder = "Импорт"
                    })
                    .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out _))
                    .ToList();
            }

            foreach (var entry in imported)
            {
                if (!_data.Data.Bookmarks.Any(item => item.Url.Equals(entry.Url, StringComparison.OrdinalIgnoreCase)))
                {
                    _data.Data.Bookmarks.Add(entry);
                }
            }

            await _data.SaveAsync();
            Refresh();
            MessageBox.Show($"Импортировано: {imported.Count}", "Закладки");
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Не удалось импортировать закладки:\n{exception.Message}", "Закладки", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExportBookmarks_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Экспорт закладок",
            FileName = "chroma-bookmarks.html",
            Filter = "Chrome HTML|*.html|Chroma JSON|*.json"
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (Path.GetExtension(dialog.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            await using var stream = File.Create(dialog.FileName);
            await JsonSerializer.SerializeAsync(stream, _data.Data.Bookmarks, new JsonSerializerOptions { WriteIndented = true });
        }
        else
        {
            var builder = new StringBuilder("<!DOCTYPE NETSCAPE-Bookmark-file-1>\n<meta charset=\"UTF-8\"><title>Chroma Bookmarks</title>\n<dl><p>\n");
            foreach (var bookmark in _data.Data.Bookmarks)
            {
                builder.Append("<dt><a href=\"")
                    .Append(WebUtility.HtmlEncode(bookmark.Url))
                    .Append("\">")
                    .Append(WebUtility.HtmlEncode(bookmark.Title))
                    .AppendLine("</a>");
            }

            builder.AppendLine("</dl><p>");
            await File.WriteAllTextAsync(dialog.FileName, builder.ToString(), Encoding.UTF8);
        }
    }

    private void OpenDownload_Click(object sender, RoutedEventArgs e) => OpenDownload();
    private void DownloadGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenDownload();
    private void OpenDownload()
    {
        if (DownloadGrid.SelectedItem is DownloadRecord entry && File.Exists(entry.TargetPath))
        {
            Process.Start(new ProcessStartInfo(entry.TargetPath) { UseShellExecute = true });
        }
    }

    private void OpenDownloadFolder_Click(object sender, RoutedEventArgs e)
    {
        if (DownloadGrid.SelectedItem is DownloadRecord entry)
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{entry.TargetPath}\"") { UseShellExecute = true });
        }
    }

    private void PauseDownload_Click(object sender, RoutedEventArgs e)
    {
        if (DownloadGrid.SelectedItem is not DownloadRecord { Operation: { } operation })
        {
            return;
        }

        if (operation.IsPaused)
        {
            operation.Resume();
        }
        else
        {
            operation.Pause();
        }

        Refresh();
    }

    private void CancelDownload_Click(object sender, RoutedEventArgs e)
    {
        if (DownloadGrid.SelectedItem is DownloadRecord { Operation: { } operation })
        {
            operation.Cancel();
            Refresh();
        }
    }

    private void Window_Closed(object? sender, EventArgs e) => _data.Changed -= Data_Changed;
}
