using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Chroma.Browser.Models;
using Chroma.Browser.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace Chroma.Browser.Views;

public partial class BrowserTabView : UserControl
{
    private readonly CoreWebView2Environment _environment;
    private readonly AppSettings _settings;
    private readonly BrowserDataService _browserData;
    private readonly SettingsService _settingsService;
    private readonly AdBlockService _adBlock;
    private readonly bool _isPrivate;
    private bool _initialized;
    private bool _showingNewTab;
    private bool _isSuspended;
    private string _lastRequestedUrl = NavigationService.NewTab;

    public BrowserTabView(
        CoreWebView2Environment environment,
        AppSettings settings,
        BrowserDataService browserData,
        SettingsService settingsService,
        AdBlockService adBlock,
        bool isPrivate)
    {
        _environment = environment;
        _settings = settings;
        _browserData = browserData;
        _settingsService = settingsService;
        _adBlock = adBlock;
        _isPrivate = isPrivate;
        InitializeComponent();
    }

    public string Title { get; private set; } = "Новая вкладка";
    public string Url { get; private set; } = NavigationService.NewTab;
    public bool IsPrivate => _isPrivate;
    public bool IsPinned { get; set; }
    public string Group { get; set; } = string.Empty;
    public int BlockedCount { get; private set; }
    public bool IsSuspended => _isSuspended;
    public CoreWebView2Profile? Profile => WebView.CoreWebView2?.Profile;
    public bool CanGoBack => WebView.CoreWebView2?.CanGoBack == true;
    public bool CanGoForward => WebView.CoreWebView2?.CanGoForward == true;
    public bool IsMuted => WebView.CoreWebView2?.IsMuted == true;
    public bool IsPlayingAudio => WebView.CoreWebView2?.IsDocumentPlayingAudio == true;

    public event EventHandler? TitleChanged;
    public event EventHandler? AddressChanged;
    public event EventHandler? NavigationStateChanged;
    public event EventHandler? BlockedCountChanged;
    public event EventHandler<string>? NewTabRequested;
    public event EventHandler<DownloadRecord>? DownloadStarted;
    public event EventHandler<DownloadRecord>? DownloadChanged;
    public event EventHandler<bool>? FullScreenChanged;

    public async Task InitializeAsync(string initialUrl)
    {
        if (_initialized)
        {
            Navigate(initialUrl);
            return;
        }

        LoadingSurface.Visibility = Visibility.Visible;
        await WebView.EnsureCoreWebView2Async(_environment);
        ConfigureCore();
        _initialized = true;
        Navigate(initialUrl);
    }

    public void Navigate(string value)
    {
        var resolved = NavigationService.Resolve(value, _settings);
        _lastRequestedUrl = resolved;
        ErrorSurface.Visibility = Visibility.Collapsed;

        if (resolved.Equals(NavigationService.NewTab, StringComparison.OrdinalIgnoreCase))
        {
            ShowNewTab();
            return;
        }

        _showingNewTab = false;
        Url = resolved;
        AddressChanged?.Invoke(this, EventArgs.Empty);
        if (_initialized)
        {
            WebView.CoreWebView2.Navigate(resolved);
        }
    }

    public void GoBack()
    {
        if (CanGoBack)
        {
            WebView.CoreWebView2.GoBack();
        }
    }

    public void GoForward()
    {
        if (CanGoForward)
        {
            WebView.CoreWebView2.GoForward();
        }
    }

    public void ReloadOrStop()
    {
        if (LoadingSurface.Visibility == Visibility.Visible)
        {
            WebView.CoreWebView2?.Stop();
        }
        else
        {
            WebView.CoreWebView2?.Reload();
        }
    }

    public void FocusPage() => WebView.Focus();

    public void ToggleMute()
    {
        if (WebView.CoreWebView2 is not null)
        {
            WebView.CoreWebView2.IsMuted = !WebView.CoreWebView2.IsMuted;
            NavigationStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task ToggleSuspendAsync()
    {
        if (WebView.CoreWebView2 is null)
        {
            return;
        }

        if (_isSuspended)
        {
            WebView.CoreWebView2.Resume();
            _isSuspended = false;
        }
        else
        {
            _isSuspended = await WebView.CoreWebView2.TrySuspendAsync();
        }

        NavigationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void OpenDevTools() => WebView.CoreWebView2?.OpenDevToolsWindow();

    public async Task<string> GetPageTextAsync(bool selectionOnly = false)
    {
        if (WebView.CoreWebView2 is null || _showingNewTab)
        {
            return string.Empty;
        }

        var script = selectionOnly
            ? "(() => (window.getSelection()?.toString() || '').slice(0, 30000))()"
            : "(() => { const selected=window.getSelection()?.toString()||''; if(selected.trim()) return selected.slice(0,30000); const clone=document.body?.cloneNode(true); if(!clone) return ''; clone.querySelectorAll('script,style,noscript,input,textarea,select').forEach(x=>x.remove()); return (clone.innerText||'').replace(/\\s+/g,' ').trim().slice(0,30000); })()";
        var json = await WebView.CoreWebView2.ExecuteScriptAsync(script);
        return JsonSerializer.Deserialize<string>(json) ?? string.Empty;
    }

    public async Task ClearPrivateDataAsync()
    {
        if (_isPrivate && WebView.CoreWebView2 is not null)
        {
            await WebView.CoreWebView2.Profile.ClearBrowsingDataAsync();
        }
    }

    public Task ClearBrowsingDataAsync() => WebView.CoreWebView2?.Profile.ClearBrowsingDataAsync() ?? Task.CompletedTask;

    public void ApplySettings()
    {
        if (WebView.CoreWebView2 is null)
        {
            return;
        }

        WebView.CoreWebView2.Profile.IsPasswordAutosaveEnabled = !_isPrivate && _settings.PasswordAutosave;
        WebView.CoreWebView2.Profile.IsGeneralAutofillEnabled = !_isPrivate && _settings.GeneralAutofill;
        WebView.CoreWebView2.Profile.PreferredTrackingPreventionLevel = _settings.TrackingPrevention
            ? CoreWebView2TrackingPreventionLevel.Strict
            : CoreWebView2TrackingPreventionLevel.None;
        ApplyEnhancedSecurity(WebView.CoreWebView2.Profile, _settings.EnhancedSecurity);
        WebView.CoreWebView2.Profile.DefaultDownloadFolderPath = _settings.DownloadFolder;
    }

    public void Close()
    {
        WebView.Dispose();
    }

    public async Task ToggleSiteAdBlockAsync()
    {
        if (!Uri.TryCreate(Url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            return;
        }

        var host = uri.IdnHost;
        var existing = _settings.AdBlockAllowlist.FirstOrDefault(item => item.Equals(host, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            _settings.AdBlockAllowlist.Add(host);
        }
        else
        {
            _settings.AdBlockAllowlist.Remove(existing);
        }

        await _settingsService.SaveAsync();
        WebView.CoreWebView2?.Reload();
    }

    public bool IsAdBlockDisabledForSite()
    {
        return Uri.TryCreate(Url, UriKind.Absolute, out var uri) &&
               _settings.AdBlockAllowlist.Any(item => item.Equals(uri.IdnHost, StringComparison.OrdinalIgnoreCase));
    }

    private void ConfigureCore()
    {
        var core = WebView.CoreWebView2;
        core.Settings.AreDefaultContextMenusEnabled = true;
        core.Settings.AreDevToolsEnabled = true;
        core.Settings.AreBrowserAcceleratorKeysEnabled = true;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = true;
        core.Settings.IsReputationCheckingRequired = true;
        core.Profile.IsPasswordAutosaveEnabled = !_isPrivate && _settings.PasswordAutosave;
        core.Profile.IsGeneralAutofillEnabled = !_isPrivate && _settings.GeneralAutofill;
        core.Profile.PreferredTrackingPreventionLevel = _settings.TrackingPrevention
            ? CoreWebView2TrackingPreventionLevel.Strict
            : CoreWebView2TrackingPreventionLevel.None;
        ApplyEnhancedSecurity(core.Profile, _settings.EnhancedSecurity);
        core.Profile.DefaultDownloadFolderPath = _settings.DownloadFolder;

        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.AddScriptToExecuteOnDocumentCreatedAsync(_adBlock.GetCosmeticScript()).ContinueWith(
            task =>
            {
                if (task.Exception is not null)
                {
                    LogService.Instance.Error("Cosmetic filter injection failed", task.Exception);
                }
            },
            TaskScheduler.Default);

        core.NavigationStarting += Core_NavigationStarting;
        core.NavigationCompleted += Core_NavigationCompleted;
        core.SourceChanged += Core_SourceChanged;
        core.DocumentTitleChanged += Core_DocumentTitleChanged;
        core.HistoryChanged += (_, _) => NavigationStateChanged?.Invoke(this, EventArgs.Empty);
        core.NewWindowRequested += Core_NewWindowRequested;
        core.WebResourceRequested += Core_WebResourceRequested;
        core.DownloadStarting += Core_DownloadStarting;
        core.PermissionRequested += Core_PermissionRequested;
        core.ContainsFullScreenElementChanged += (_, _) =>
            FullScreenChanged?.Invoke(this, core.ContainsFullScreenElement);
        core.ProcessFailed += (_, args) =>
            LogService.Instance.Error($"Chromium process failed: {args.ProcessFailedKind}");
    }

    private void Core_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        LoadingSurface.Visibility = Visibility.Visible;
        ErrorSurface.Visibility = Visibility.Collapsed;
        BlockedCount = 0;
        BlockedCountChanged?.Invoke(this, EventArgs.Empty);
    }

    private async void Core_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        LoadingSurface.Visibility = Visibility.Collapsed;
        NavigationStateChanged?.Invoke(this, EventArgs.Empty);

        if (!args.IsSuccess)
        {
            ErrorText.Text = $"{args.WebErrorStatus}\n{_lastRequestedUrl}";
            ErrorSurface.Visibility = Visibility.Visible;
            return;
        }

        if (!_showingNewTab)
        {
            await _browserData.AddHistoryAsync(Title, Url, _isPrivate);
        }
    }

    private void Core_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs args)
    {
        if (_showingNewTab && WebView.Source?.ToString() == "about:blank")
        {
            return;
        }

        var source = WebView.Source?.ToString();
        if (!string.IsNullOrWhiteSpace(source))
        {
            _showingNewTab = false;
            Url = NavigationService.Display(source);
            _lastRequestedUrl = Url;
            AddressChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Core_DocumentTitleChanged(object? sender, object args)
    {
        Title = _showingNewTab
            ? (_isPrivate ? "Инкогнито" : "Новая вкладка")
            : string.IsNullOrWhiteSpace(WebView.CoreWebView2.DocumentTitle)
                ? Url
                : WebView.CoreWebView2.DocumentTitle;
        TitleChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Core_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        args.Handled = true;
        if (_settings.BlockPopups && !args.IsUserInitiated)
        {
            return;
        }

        NewTabRequested?.Invoke(this, args.Uri);
    }

    private void Core_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs args)
    {
        if (!_settings.AdBlockEnabled || IsAdBlockDisabledForSite() ||
            !Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out var request))
        {
            return;
        }

        Uri.TryCreate(Url, UriKind.Absolute, out var document);
        if (!_adBlock.ShouldBlock(request, document, args.ResourceContext, out var reason))
        {
            return;
        }

        args.Response = _environment.CreateWebResourceResponse(
            Stream.Null,
            403,
            "Blocked by Chroma",
            "Content-Type: text/plain; charset=utf-8\r\nCache-Control: no-store");

        var isDocumentThreat = args.ResourceContext == CoreWebView2WebResourceContext.Document &&
                               reason.Contains("Вредоносный", StringComparison.Ordinal);
        Dispatcher.BeginInvoke(() =>
        {
            BlockedCount++;
            BlockedCountChanged?.Invoke(this, EventArgs.Empty);
            if (isDocumentThreat)
            {
                ErrorText.Text = $"Chroma заблокировал опасный адрес:\n{request.Host}";
                ErrorSurface.Visibility = Visibility.Visible;
            }
        });
    }

    private void Core_DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs args)
    {
        var suggestedName = Path.GetFileName(args.ResultFilePath);
        var targetPath = Path.Combine(_settings.DownloadFolder, suggestedName);
        if (_settings.AskDownloadLocation)
        {
            var dialog = new SaveFileDialog
            {
                FileName = suggestedName,
                InitialDirectory = _settings.DownloadFolder,
                Title = "Сохранить файл"
            };
            if (dialog.ShowDialog() != true)
            {
                args.Cancel = true;
                return;
            }

            targetPath = dialog.FileName;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        args.ResultFilePath = MakeUniquePath(targetPath);
        args.Handled = true;

        var operation = args.DownloadOperation;
        var record = new DownloadRecord
        {
            FileName = Path.GetFileName(args.ResultFilePath),
            SourceUrl = operation.Uri,
            TargetPath = args.ResultFilePath,
            TotalBytes = operation.TotalBytesToReceive ?? 0,
            ReceivedBytes = ToLong(operation.BytesReceived),
            State = DownloadState.InProgress,
            Operation = operation
        };

        operation.BytesReceivedChanged += (_, _) =>
        {
            record.ReceivedBytes = ToLong(operation.BytesReceived);
            DownloadChanged?.Invoke(this, record);
        };
        operation.StateChanged += (_, _) =>
        {
            record.State = operation.State switch
            {
                CoreWebView2DownloadState.InProgress => DownloadState.InProgress,
                CoreWebView2DownloadState.Completed => DownloadState.Completed,
                CoreWebView2DownloadState.Interrupted when operation.CanResume => DownloadState.Paused,
                CoreWebView2DownloadState.Interrupted when operation.InterruptReason == CoreWebView2DownloadInterruptReason.UserCanceled => DownloadState.Cancelled,
                CoreWebView2DownloadState.Interrupted => DownloadState.Interrupted,
                _ => DownloadState.Interrupted
            };
            DownloadChanged?.Invoke(this, record);
        };

        DownloadStarted?.Invoke(this, record);
    }

    private void Core_PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs args)
    {
        var sensitive = args.PermissionKind is CoreWebView2PermissionKind.Camera or
            CoreWebView2PermissionKind.Microphone or
            CoreWebView2PermissionKind.Geolocation or
            CoreWebView2PermissionKind.Notifications;

        if (!sensitive)
        {
            args.State = CoreWebView2PermissionState.Default;
            return;
        }

        var result = MessageBox.Show(
            $"Разрешить сайту доступ к функции «{args.PermissionKind}»?\n\n{args.Uri}",
            "Разрешение сайта",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        args.State = result == MessageBoxResult.Yes
            ? CoreWebView2PermissionState.Allow
            : CoreWebView2PermissionState.Deny;
    }

    private void ShowNewTab()
    {
        _showingNewTab = true;
        Url = NavigationService.NewTab;
        Title = _isPrivate ? "Инкогнито" : "Новая вкладка";
        AddressChanged?.Invoke(this, EventArgs.Empty);
        TitleChanged?.Invoke(this, EventArgs.Empty);

        if (_initialized)
        {
            WebView.NavigateToString(NewTabPageService.Build(_settings, _browserData.Data.Bookmarks, _isPrivate));
        }
    }

    private void Retry_Click(object sender, RoutedEventArgs e) => Navigate(_lastRequestedUrl);

    private static string MakeUniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path)!;
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var index = 1; index < 10_000; index++)
        {
            var candidate = Path.Combine(directory, $"{name} ({index}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(directory, $"{name}-{Guid.NewGuid():N}{extension}");
    }

    private static long ToLong(ulong value) => value > long.MaxValue ? long.MaxValue : (long)value;

    private static void ApplyEnhancedSecurity(CoreWebView2Profile profile, bool enabled)
    {
        try
        {
            var property = profile.GetType().GetProperty("EnhancedSecurityModeLevel");
            if (property is { CanWrite: true } && property.PropertyType.IsEnum)
            {
                property.SetValue(profile, Enum.Parse(property.PropertyType, enabled ? "Strict" : "Off"));
            }
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Enhanced security mode is unavailable in this WebView2 Runtime: {exception.Message}");
        }
    }
}
