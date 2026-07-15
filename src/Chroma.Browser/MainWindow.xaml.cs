using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Chroma.Browser.Interop;
using Chroma.Browser.Models;
using Chroma.Browser.Services;
using Chroma.Browser.Views;
using Microsoft.Web.WebView2.Core;

namespace Chroma.Browser;

public partial class MainWindow : Window
{
    private readonly bool _privateWindow;
    private readonly string? _startupUrl;
    private readonly SettingsService _settings = new();
    private readonly BrowserDataService _browserData = new();
    private readonly WebViewEnvironmentService _environment = new();
    private readonly AdBlockService _adBlock = new();
    private readonly SessionService _session = new();
    private readonly UpdateService _updates = new();
    private readonly List<BrowserTabView> _tabs = [];
    private readonly Dictionary<BrowserTabView, TabHeaderElements> _headers = [];
    private readonly Stack<BrowserTabState> _closedTabs = [];
    private readonly DispatcherTimer _downloadSaveTimer;
    private readonly DispatcherTimer _memoryTimer;
    private readonly Dictionary<BrowserTabView, DateTimeOffset> _lastActivated = [];
    private Point _dragStart;
    private bool _updatingAddress;
    private bool _closing;
    private bool _initialized;
    private bool _fullScreen;
    private bool _aiInitialized;
    private bool _changingAiProvider;
    private WindowState _stateBeforeFullScreen;

    public MainWindow(bool isPrivateWindow = false, string? startupUrl = null)
    {
        _privateWindow = isPrivateWindow;
        _startupUrl = startupUrl;
        InitializeComponent();

        _downloadSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _downloadSaveTimer.Tick += async (_, _) =>
        {
            _downloadSaveTimer.Stop();
            await _browserData.SaveAsync();
        };

        _memoryTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        _memoryTimer.Tick += MemoryTimer_Tick;
    }

    private BrowserTabView? CurrentTab => (TabStrip.SelectedItem as ListBoxItem)?.Tag as BrowserTabView;

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        try
        {
            AppPaths.EnsureCreated();
            WebViewEnvironmentService.CleanupAbandonedPrivateData();
            await Task.WhenAll(_settings.InitializeAsync(), _browserData.InitializeAsync());
            ApplyTheme();

            PrivateBadge.Visibility = _privateWindow ? Visibility.Visible : Visibility.Collapsed;
            Title = _privateWindow ? "Chroma Browser — Инкогнито" : "Chroma Browser";

            _ = _adBlock.InitializeAsync(_settings.Current).ContinueWith(
                task =>
                {
                    if (task.Exception is not null)
                    {
                        LogService.Instance.Error("AdBlock initialization failed", task.Exception);
                    }
                },
                TaskScheduler.Default);

            if (!_privateWindow && _settings.Current.RestoreSession)
            {
                var restored = await _session.LoadAsync();
                foreach (var state in restored.Tabs.Take(30))
                {
                    await AddTabAsync(state.Url, false, state);
                }

                if (_tabs.Count > 0)
                {
                    TabStrip.SelectedIndex = Math.Clamp(restored.SelectedIndex, 0, _tabs.Count - 1);
                }
            }

            if (_tabs.Count == 0)
            {
                await AddTabAsync(_startupUrl ?? NavigationService.NewTab, true);
            }
            else if (!string.IsNullOrWhiteSpace(_startupUrl))
            {
                await AddTabAsync(_startupUrl, true);
            }

            _memoryTimer.Start();

            if (_settings.Current.AiPanelVisible)
            {
                await ShowAiPanelAsync(true);
            }

            if (!_privateWindow && _settings.Current.AutoCheckUpdates)
            {
                _ = CheckForUpdatesAsync(false);
            }
        }
        catch (Exception exception)
        {
            LogService.Instance.Error("Window initialization failed", exception);
            MessageBox.Show(
                $"Не удалось запустить браузер:\n{exception.Message}",
                "Chroma Browser",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Close();
        }
    }

    private void Window_SourceInitialized(object? sender, EventArgs e) => ApplyBackdrop();

    private async Task AddTabAsync(string url, bool select, BrowserTabState? restored = null)
    {
        var environment = await _environment.GetAsync(_privateWindow);
        var view = new BrowserTabView(
            environment,
            _settings.Current,
            _browserData,
            _settings,
            _adBlock,
            _privateWindow)
        {
            IsPinned = restored?.IsPinned == true,
            Group = restored?.Group ?? string.Empty
        };

        view.TitleChanged += Tab_TitleChanged;
        view.AddressChanged += Tab_AddressChanged;
        view.NavigationStateChanged += Tab_NavigationStateChanged;
        view.BlockedCountChanged += Tab_BlockedCountChanged;
        view.NewTabRequested += async (_, target) => await AddTabAsync(target, true);
        view.DownloadStarted += Tab_DownloadStarted;
        view.DownloadChanged += Tab_DownloadChanged;
        view.FullScreenChanged += (_, enabled) => SetFullScreen(enabled);

        var item = CreateTabItem(view);
        _tabs.Add(view);
        _lastActivated[view] = DateTimeOffset.Now;
        TabStrip.Items.Add(item);
        if (select)
        {
            TabStrip.SelectedItem = item;
        }

        await view.InitializeAsync(url);
        UpdateTabHeader(view);
    }

    private ListBoxItem CreateTabItem(BrowserTabView view)
    {
        var pin = new TextBlock
        {
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 11,
            Text = "\uE718",
            Margin = new Thickness(7, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = view.IsPinned ? Visibility.Visible : Visibility.Collapsed
        };
        var title = new TextBlock
        {
            Margin = new Thickness(5, 0, 5, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Text = view.Title
        };
        var close = new Button
        {
            Width = 27,
            Height = 27,
            Margin = new Thickness(0, 0, 3, 0),
            Padding = new Thickness(0),
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 11,
            Content = "\uE8BB",
            Style = (Style)FindResource("GlassButton"),
            Tag = view,
            ToolTip = "Закрыть"
        };
        close.Click += TabClose_Click;

        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pin, 0);
        Grid.SetColumn(title, 1);
        Grid.SetColumn(close, 2);
        header.Children.Add(pin);
        header.Children.Add(title);
        header.Children.Add(close);

        var item = new ListBoxItem
        {
            Content = header,
            Tag = view,
            ToolTip = view.Url,
            ContextMenu = CreateTabContextMenu(view)
        };
        item.PreviewMouseUp += (_, args) =>
        {
            if (args.ChangedButton == MouseButton.Middle)
            {
                CloseTab(view);
                args.Handled = true;
            }
        };
        _headers[view] = new TabHeaderElements(item, title, pin, close);
        return item;
    }

    private ContextMenu CreateTabContextMenu(BrowserTabView view)
    {
        var menu = new ContextMenu();
        var pin = new MenuItem { Header = view.IsPinned ? "Открепить вкладку" : "Закрепить вкладку" };
        pin.Click += (_, _) =>
        {
            view.IsPinned = !view.IsPinned;
            pin.Header = view.IsPinned ? "Открепить вкладку" : "Закрепить вкладку";
            UpdateTabHeader(view);
            MovePinnedTab(view);
        };
        var mute = new MenuItem { Header = "Включить/выключить звук" };
        mute.Click += (_, _) => view.ToggleMute();
        var suspend = new MenuItem { Header = "Приостановить/возобновить" };
        suspend.Click += async (_, _) => await view.ToggleSuspendAsync();
        var duplicate = new MenuItem { Header = "Дублировать" };
        duplicate.Click += async (_, _) => await AddTabAsync(view.Url, true);
        var group = new MenuItem { Header = "Группа" };
        foreach (var groupName in new[] { string.Empty, "Работа", "Личное", "Покупки" })
        {
            var groupItem = new MenuItem { Header = string.IsNullOrEmpty(groupName) ? "Без группы" : groupName };
            groupItem.Click += (_, _) =>
            {
                view.Group = groupName;
                UpdateTabHeader(view);
            };
            group.Items.Add(groupItem);
        }

        var close = new MenuItem { Header = "Закрыть" };
        close.Click += (_, _) => CloseTab(view);
        menu.Items.Add(pin);
        menu.Items.Add(mute);
        menu.Items.Add(suspend);
        menu.Items.Add(duplicate);
        menu.Items.Add(group);
        menu.Items.Add(new Separator());
        menu.Items.Add(close);
        return menu;
    }

    private void UpdateTabHeader(BrowserTabView view)
    {
        if (!_headers.TryGetValue(view, out var header))
        {
            return;
        }

        header.Title.Text = string.IsNullOrWhiteSpace(view.Title) ? "Новая вкладка" : view.Title;
        header.Pin.Visibility = view.IsPinned ? Visibility.Visible : Visibility.Collapsed;
        header.Close.Visibility = view.IsPinned ? Visibility.Collapsed : Visibility.Visible;
        header.Item.ToolTip = $"{view.Title}\n{view.Url}";
        header.Item.BorderBrush = string.IsNullOrWhiteSpace(view.Group)
            ? Brushes.Transparent
            : view.Group switch
            {
                "Работа" => new SolidColorBrush(Color.FromRgb(132, 151, 255)),
                "Личное" => new SolidColorBrush(Color.FromRgb(224, 132, 255)),
                _ => new SolidColorBrush(Color.FromRgb(119, 220, 191))
            };
    }

    private void MovePinnedTab(BrowserTabView view)
    {
        if (!_headers.TryGetValue(view, out var header))
        {
            return;
        }

        var oldIndex = TabStrip.Items.IndexOf(header.Item);
        var newIndex = view.IsPinned
            ? _tabs.TakeWhile(tab => tab.IsPinned && tab != view).Count()
            : _tabs.Count(tab => tab.IsPinned);
        if (oldIndex == newIndex)
        {
            return;
        }

        TabStrip.Items.Remove(header.Item);
        _tabs.Remove(view);
        _lastActivated.Remove(view);
        newIndex = Math.Clamp(newIndex, 0, _tabs.Count);
        TabStrip.Items.Insert(newIndex, header.Item);
        _tabs.Insert(newIndex, view);
        TabStrip.SelectedItem = header.Item;
    }

    private void CloseTab(BrowserTabView view)
    {
        if (!_headers.TryGetValue(view, out var header))
        {
            return;
        }

        if (!_privateWindow)
        {
            _closedTabs.Push(new BrowserTabState
            {
                Url = view.Url,
                Title = view.Title,
                IsPinned = view.IsPinned,
                Group = view.Group
            });
        }

        var index = TabStrip.Items.IndexOf(header.Item);
        TabStrip.Items.Remove(header.Item);
        _tabs.Remove(view);
        _headers.Remove(view);
        view.Close();

        if (_tabs.Count == 0)
        {
            Close();
            return;
        }

        TabStrip.SelectedIndex = Math.Clamp(index, 0, _tabs.Count - 1);
    }

    private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var current = CurrentTab;
        if (current is null)
        {
            BrowserContent.Content = null;
            return;
        }

        BrowserContent.Content = current;
        _lastActivated[current] = DateTimeOffset.Now;
        if (current.IsSuspended)
        {
            _ = current.ToggleSuspendAsync();
        }

        UpdateChrome();
        AnimateBrowserContent();
    }

    private void Tab_TitleChanged(object? sender, EventArgs e)
    {
        if (sender is BrowserTabView view)
        {
            UpdateTabHeader(view);
            if (view == CurrentTab)
            {
                Title = _privateWindow ? $"{view.Title} — Инкогнито — Chroma" : $"{view.Title} — Chroma";
            }
        }
    }

    private void Tab_AddressChanged(object? sender, EventArgs e)
    {
        if (sender == CurrentTab)
        {
            UpdateChrome();
        }
    }

    private void Tab_NavigationStateChanged(object? sender, EventArgs e)
    {
        if (sender == CurrentTab)
        {
            UpdateChrome();
        }
    }

    private void Tab_BlockedCountChanged(object? sender, EventArgs e)
    {
        if (sender == CurrentTab)
        {
            BlockedCountText.Text = CurrentTab?.BlockedCount.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "0";
        }
    }

    private async void Tab_DownloadStarted(object? sender, DownloadRecord download)
    {
        await _browserData.AddDownloadAsync(download);
    }

    private void Tab_DownloadChanged(object? sender, DownloadRecord download)
    {
        _downloadSaveTimer.Stop();
        _downloadSaveTimer.Start();
    }

    private async void MemoryTimer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.Current.SuspendBackgroundTabs)
        {
            return;
        }

        var threshold = DateTimeOffset.Now.AddMinutes(-Math.Clamp(_settings.Current.SuspendAfterMinutes, 1, 1440));
        foreach (var tab in _tabs.Where(tab => tab != CurrentTab && !tab.IsPinned && !tab.IsPlayingAudio && !tab.IsSuspended).ToArray())
        {
            if (_lastActivated.TryGetValue(tab, out var activated) && activated < threshold)
            {
                await tab.ToggleSuspendAsync();
            }
        }
    }

    private void UpdateChrome()
    {
        var tab = CurrentTab;
        if (tab is null)
        {
            return;
        }

        _updatingAddress = true;
        AddressBar.Text = tab.Url;
        _updatingAddress = false;
        BackButton.IsEnabled = tab.CanGoBack;
        ForwardButton.IsEnabled = tab.CanGoForward;
        BookmarkButton.Content = _browserData.IsBookmarked(tab.Url) ? "\uE735" : "\uE734";
        BlockedCountText.Text = tab.BlockedCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Title = _privateWindow ? $"{tab.Title} — Инкогнито — Chroma" : $"{tab.Title} — Chroma";
    }

    private void NewTab_Click(object sender, RoutedEventArgs e) => _ = AddTabAsync(NavigationService.NewTab, true);
    private void Back_Click(object sender, RoutedEventArgs e) => CurrentTab?.GoBack();
    private void Forward_Click(object sender, RoutedEventArgs e) => CurrentTab?.GoForward();
    private void Reload_Click(object sender, RoutedEventArgs e) => CurrentTab?.ReloadOrStop();

    private async void Bookmark_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentTab is null || CurrentTab.Url.StartsWith("chroma://", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await _browserData.ToggleBookmarkAsync(CurrentTab.Title, CurrentTab.Url);
        UpdateChrome();
    }

    private void AddressBar_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        Dispatcher.BeginInvoke(AddressBar.SelectAll);

    private void AddressBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingAddress || !AddressBar.IsKeyboardFocusWithin)
        {
            return;
        }

        var query = AddressBar.Text.Trim();
        if (query.Length < 2)
        {
            SuggestionsPopup.IsOpen = false;
            return;
        }

        var entries = _browserData.Data.Bookmarks
            .Select(item => new SuggestionItem($"★  {item.Title}   {item.Url}", item.Url))
            .Concat(_browserData.Data.History.Select(item => new SuggestionItem($"{item.Title}   {item.Url}", item.Url)))
            .Where(item => item.Display.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
        SuggestionsList.ItemsSource = entries;
        SuggestionsPopup.IsOpen = entries.Length > 0;
    }

    private void AddressBar_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && SuggestionsPopup.IsOpen && SuggestionsList.Items.Count > 0)
        {
            SuggestionsList.SelectedIndex = Math.Min(SuggestionsList.SelectedIndex + 1, SuggestionsList.Items.Count - 1);
            SuggestionsList.ScrollIntoView(SuggestionsList.SelectedItem);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            SuggestionsPopup.IsOpen = false;
            CurrentTab?.FocusPage();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter)
        {
            return;
        }

        var target = SuggestionsPopup.IsOpen && SuggestionsList.SelectedItem is SuggestionItem suggestion
            ? suggestion.Url
            : AddressBar.Text;
        SuggestionsPopup.IsOpen = false;
        CurrentTab?.Navigate(target);
        CurrentTab?.FocusPage();
        e.Handled = true;
    }

    private void SuggestionsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SuggestionsList.SelectedItem is SuggestionItem suggestion)
        {
            SuggestionsPopup.IsOpen = false;
            CurrentTab?.Navigate(suggestion.Url);
        }
    }

    private async void Shield_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentTab is null)
        {
            return;
        }

        var menu = new ContextMenu();
        var status = new MenuItem
        {
            Header = $"{_adBlock.RuleCount:N0} правил · заблокировано {CurrentTab.BlockedCount}",
            IsEnabled = false
        };
        var site = new MenuItem
        {
            Header = CurrentTab.IsAdBlockDisabledForSite() ? "Включить на этом сайте" : "Отключить на этом сайте"
        };
        site.Click += async (_, _) => await CurrentTab.ToggleSiteAdBlockAsync();
        var global = new MenuItem
        {
            Header = "Встроенная блокировка",
            IsCheckable = true,
            IsChecked = _settings.Current.AdBlockEnabled
        };
        global.Click += async (_, _) =>
        {
            _settings.Current.AdBlockEnabled = global.IsChecked;
            await _settings.SaveAsync();
            CurrentTab.ReloadOrStop();
        };
        var update = new MenuItem { Header = "Обновить фильтры" };
        update.Click += async (_, _) =>
        {
            ShieldButton.IsEnabled = false;
            try
            {
                await _adBlock.UpdateAsync(_settings.Current);
                MessageBox.Show($"Фильтры обновлены: {_adBlock.RuleCount:N0} правил.", "Chroma AdBlock");
            }
            finally
            {
                ShieldButton.IsEnabled = true;
            }
        };
        menu.Items.Add(status);
        menu.Items.Add(new Separator());
        menu.Items.Add(site);
        menu.Items.Add(global);
        menu.Items.Add(update);
        OpenContextMenu(menu, sender as FrameworkElement);
        await Task.CompletedTask;
    }

    private async void Ai_Click(object sender, RoutedEventArgs e) => await ShowAiPanelAsync(AiPanel.Visibility != Visibility.Visible);
    private async void AiClose_Click(object sender, RoutedEventArgs e) => await ShowAiPanelAsync(false);

    private async Task ShowAiPanelAsync(bool show)
    {
        AiPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        AiColumn.Width = show
            ? new GridLength(Math.Clamp(_settings.Current.AiPanelWidth, 320, Math.Max(320, ActualWidth * 0.58)))
            : new GridLength(0);
        _settings.Current.AiPanelVisible = show;
        if (show)
        {
            await EnsureAiInitializedAsync();
        }

        await _settings.SaveAsync();
    }

    private async Task EnsureAiInitializedAsync()
    {
        if (_aiInitialized)
        {
            return;
        }

        var environment = await _environment.GetAsync(_privateWindow);
        await AiWebView.EnsureCoreWebView2Async(environment);
        AiWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
        AiWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        AiWebView.CoreWebView2.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Balanced;
        AiProviderBox.ItemsSource = AiProvider.BuiltIn;
        _changingAiProvider = true;
        AiProviderBox.SelectedItem = AiProvider.BuiltIn.FirstOrDefault(item => item.Id == _settings.Current.AiProviderId)
            ?? AiProvider.BuiltIn[0];
        _changingAiProvider = false;
        _aiInitialized = true;
        NavigateAiProvider();
    }

    private async void AiProviderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingAiProvider || AiProviderBox.SelectedItem is not AiProvider provider)
        {
            return;
        }

        _settings.Current.AiProviderId = provider.Id;
        await _settings.SaveAsync();
        if (_aiInitialized)
        {
            NavigateAiProvider();
        }
    }

    private void NavigateAiProvider()
    {
        if (AiWebView.CoreWebView2 is not null && AiProviderBox.SelectedItem is AiProvider provider)
        {
            AiWebView.CoreWebView2.Navigate(provider.Url);
            AiStatus.Text = $"{provider.Name}: вход и лимиты управляются самим сервисом";
        }
    }

    private async void AiSummarize_Click(object sender, RoutedEventArgs e) =>
        await PrepareAiPromptAsync("Кратко и структурированно перескажи эту страницу на русском языке. Выдели главные факты и выводы.", false);

    private async void AiExplain_Click(object sender, RoutedEventArgs e) =>
        await PrepareAiPromptAsync("Объясни содержание этой страницы простыми словами. Укажи, что важно и где могут быть спорные утверждения.", false);

    private async void AiTranslate_Click(object sender, RoutedEventArgs e) =>
        await PrepareAiPromptAsync("Переведи текст на русский язык, сохранив смысл, структуру и термины. Не добавляй комментариев.", false);

    private async void AiSelection_Click(object sender, RoutedEventArgs e) =>
        await PrepareAiPromptAsync("Ответь на вопрос или объясни выделенный фрагмент в контексте страницы.", true);

    private async Task PrepareAiPromptAsync(string instruction, bool selectionOnly)
    {
        if (CurrentTab is null)
        {
            return;
        }

        await ShowAiPanelAsync(true);
        var pageText = await CurrentTab.GetPageTextAsync(selectionOnly);
        if (string.IsNullOrWhiteSpace(pageText))
        {
            AiStatus.Text = selectionOnly ? "Сначала выделите текст на странице" : "На странице не найден текст";
            return;
        }

        var prompt = $"{instruction}\n\nИсточник: {CurrentTab.Title}\nURL: {CurrentTab.Url}\n\nТекст:\n{pageText}";
        try
        {
            Clipboard.SetText(prompt);
            AiStatus.Text = "Промпт скопирован — вставьте его в чат клавишами Ctrl+V";
            AiWebView.Focus();
        }
        catch (Exception exception)
        {
            AiStatus.Text = $"Не удалось скопировать текст: {exception.Message}";
        }
    }

    private void AiSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
        if (AiPanel.Visibility == Visibility.Visible)
        {
            AiColumn.Width = new GridLength(Math.Clamp(AiColumn.ActualWidth - e.HorizontalChange, 320, Math.Max(320, ActualWidth * 0.7)));
        }
    }

    private void AiSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        _settings.Current.AiPanelWidth = AiColumn.ActualWidth;
        _ = _settings.SaveAsync();
    }

    private void Downloads_Click(object sender, RoutedEventArgs e) => OpenDataWindow(2);

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        menu.Items.Add(CreateMenuItem("Новая вкладка", async () => await AddTabAsync(NavigationService.NewTab, true), "Ctrl+T"));
        menu.Items.Add(CreateMenuItem("Новое окно инкогнито", () => new MainWindow(true).Show(), "Ctrl+Shift+N"));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Закладки", () => OpenDataWindow(0), "Ctrl+Shift+O"));
        menu.Items.Add(CreateMenuItem("История", () => OpenDataWindow(1), "Ctrl+H"));
        menu.Items.Add(CreateMenuItem("Загрузки", () => OpenDataWindow(2), "Ctrl+J"));
        menu.Items.Add(CreateMenuItem("Расширения", OpenExtensions));
        menu.Items.Add(CreateMenuItem("Пароли", OpenVault));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Инструменты разработчика", () => CurrentTab?.OpenDevTools(), "Ctrl+Shift+I"));
        menu.Items.Add(CreateMenuItem("Настройки", OpenSettings));
        menu.Items.Add(CreateMenuItem("Проверить обновления", async () => await CheckForUpdatesAsync(true)));
        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Выход", Close));
        OpenContextMenu(menu, sender as FrameworkElement);
    }

    private void OpenDataWindow(int tabIndex)
    {
        var window = new DataWindow(_browserData, tabIndex) { Owner = this };
        window.UrlOpened += async (_, url) => await AddTabAsync(url, true);
        window.Show();
    }

    private async void OpenSettings()
    {
        var dialog = new SettingsWindow(_settings.Current) { Owner = this };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        await _settings.SaveAsync();
        ApplyTheme();
        foreach (var tab in _tabs)
        {
            tab.ApplySettings();
        }
    }

    private void OpenExtensions()
    {
        if (_privateWindow)
        {
            MessageBox.Show("Расширения отключены в окне инкогнито.", "Chroma Browser");
            return;
        }

        if (CurrentTab?.Profile is { } profile)
        {
            new ExtensionsWindow(profile) { Owner = this }.ShowDialog();
        }
    }

    private async void OpenVault()
    {
        var vault = new SecureVaultService();
        if (!await vault.RequestUnlockAsync())
        {
            MessageBox.Show("Windows Hello недоступен или проверка отменена.", "Пароли Chroma");
            return;
        }

        new VaultWindow(vault) { Owner = this }.ShowDialog();
    }

    private async Task CheckForUpdatesAsync(bool interactive)
    {
        var update = await _updates.CheckAsync();
        if (update is null)
        {
            if (interactive)
            {
                MessageBox.Show("Установлена последняя версия Chroma Browser.", "Обновления");
            }

            return;
        }

        var result = MessageBox.Show(
            $"Доступно обновление {update.Version}. Скачать и установить?",
            "Обновление Chroma Browser",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (update.ChecksumUrl is null)
        {
            Process.Start(new ProcessStartInfo(update.WebUrl) { UseShellExecute = true });
            return;
        }

        try
        {
            var installer = await _updates.DownloadAsync(update);
            UpdateService.LaunchInstaller(installer);
            Close();
        }
        catch (Exception exception)
        {
            LogService.Instance.Error("Update installation failed", exception);
            MessageBox.Show($"Не удалось установить обновление:\n{exception.Message}", "Обновления", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static MenuItem CreateMenuItem(string header, Action action, string? gesture = null)
    {
        var item = new MenuItem { Header = header, InputGestureText = gesture ?? string.Empty };
        item.Click += (_, _) => action();
        return item;
    }

    private static void OpenContextMenu(ContextMenu menu, FrameworkElement? target)
    {
        menu.PlacementTarget = target;
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var control = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
        var alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt);

        if (e.Key == Key.F11)
        {
            SetFullScreen(!_fullScreen);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && _fullScreen)
        {
            SetFullScreen(false);
            e.Handled = true;
        }
        else if (control && shift && e.Key == Key.T)
        {
            RestoreClosedTab();
            e.Handled = true;
        }
        else if (control && shift && e.Key == Key.N)
        {
            new MainWindow(true).Show();
            e.Handled = true;
        }
        else if (control && shift && e.Key == Key.I)
        {
            CurrentTab?.OpenDevTools();
            e.Handled = true;
        }
        else if (control && e.Key == Key.T)
        {
            _ = AddTabAsync(NavigationService.NewTab, true);
            e.Handled = true;
        }
        else if (control && e.Key == Key.W)
        {
            if (CurrentTab is not null)
            {
                CloseTab(CurrentTab);
            }
            e.Handled = true;
        }
        else if (control && e.Key == Key.L)
        {
            AddressBar.Focus();
            AddressBar.SelectAll();
            e.Handled = true;
        }
        else if (control && e.Key == Key.R)
        {
            CurrentTab?.ReloadOrStop();
            e.Handled = true;
        }
        else if (control && e.Key == Key.D)
        {
            Bookmark_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (control && e.Key == Key.H)
        {
            OpenDataWindow(1);
            e.Handled = true;
        }
        else if (control && e.Key == Key.J)
        {
            OpenDataWindow(2);
            e.Handled = true;
        }
        else if (control && e.Key == Key.Tab)
        {
            CycleTab(shift ? -1 : 1);
            e.Handled = true;
        }
        else if (alt && e.SystemKey == Key.Left)
        {
            CurrentTab?.GoBack();
            e.Handled = true;
        }
        else if (alt && e.SystemKey == Key.Right)
        {
            CurrentTab?.GoForward();
            e.Handled = true;
        }
    }

    private void RestoreClosedTab()
    {
        if (_closedTabs.TryPop(out var state))
        {
            _ = AddTabAsync(state.Url, true, state);
        }
    }

    private void CycleTab(int direction)
    {
        if (TabStrip.Items.Count < 2)
        {
            return;
        }

        var index = (TabStrip.SelectedIndex + direction + TabStrip.Items.Count) % TabStrip.Items.Count;
        TabStrip.SelectedIndex = index;
    }

    private void SetFullScreen(bool enabled)
    {
        if (_fullScreen == enabled)
        {
            return;
        }

        _fullScreen = enabled;
        if (enabled)
        {
            _stateBeforeFullScreen = WindowState;
            TitleRow.Height = new GridLength(0);
            ToolbarRow.Height = new GridLength(0);
            WindowState = WindowState.Maximized;
        }
        else
        {
            TitleRow.Height = new GridLength(45);
            ToolbarRow.Height = new GridLength(56);
            WindowState = _stateBeforeFullScreen;
        }
    }

    private void ApplyTheme()
    {
        var light = _settings.Current.Theme == ThemeMode.Light;
        Resources["WindowBrush"] = new SolidColorBrush(light ? Color.FromArgb(235, 241, 243, 247) : Color.FromArgb(230, 21, 23, 27));
        Resources["PanelBrush"] = new SolidColorBrush(light ? Color.FromArgb(245, 247, 248, 251) : Color.FromArgb(242, 34, 37, 43));
        Resources["TextBrush"] = new SolidColorBrush(light ? Color.FromRgb(31, 34, 40) : Color.FromRgb(245, 247, 250));
        Resources["MutedTextBrush"] = new SolidColorBrush(light ? Color.FromRgb(91, 97, 108) : Color.FromRgb(173, 179, 189));
        try
        {
            var converted = ColorConverter.ConvertFromString(_settings.Current.AccentColor);
            var accent = converted is Color color ? color : Color.FromRgb(174, 184, 255);
            Resources["AccentBrush"] = new SolidColorBrush(accent);
        }
        catch (FormatException)
        {
            Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(174, 184, 255));
        }

        Opacity = Math.Clamp(_settings.Current.InterfaceOpacity, 0.78, 1.0);
        ApplyBackdrop();
    }

    private void ApplyBackdrop() => WindowsBackdrop.Apply(
        this,
        _settings.Current.UseMica,
        _settings.Current.Theme != ThemeMode.Light);

    private void AnimateBrowserContent()
    {
        if (_settings.Current.ReduceMotion)
        {
            BrowserContent.Opacity = 1;
            return;
        }

        BrowserContent.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(0.74, 1, TimeSpan.FromMilliseconds(130))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null ||
            FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximize();
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void TabStrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        _dragStart = e.GetPosition(TabStrip);

    private void TabStrip_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        var position = e.GetPosition(TabStrip);
        if (Math.Abs(position.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(position.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var item = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (item is not null)
        {
            DragDrop.DoDragDrop(item, item, DragDropEffects.Move);
        }
    }

    private void TabStrip_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(ListBoxItem)) is not ListBoxItem source)
        {
            return;
        }

        var target = FindAncestor<ListBoxItem>(TabStrip.InputHitTest(e.GetPosition(TabStrip)) as DependencyObject);
        if (target is null || target == source || source.Tag is not BrowserTabView sourceView)
        {
            return;
        }

        var sourceIndex = TabStrip.Items.IndexOf(source);
        var targetIndex = TabStrip.Items.IndexOf(target);
        TabStrip.Items.RemoveAt(sourceIndex);
        _tabs.RemoveAt(sourceIndex);
        targetIndex = Math.Clamp(targetIndex, 0, TabStrip.Items.Count);
        TabStrip.Items.Insert(targetIndex, source);
        _tabs.Insert(targetIndex, sourceView);
        TabStrip.SelectedItem = source;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void ToggleMaximize() => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BrowserTabView view })
        {
            CloseTab(view);
        }
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_closing)
        {
            return;
        }

        e.Cancel = true;
        try
        {
            _settings.Current.AiPanelWidth = AiColumn.ActualWidth > 0 ? AiColumn.ActualWidth : _settings.Current.AiPanelWidth;
            await _settings.SaveAsync();

            if (!_privateWindow && _settings.Current.RestoreSession)
            {
                await _session.SaveAsync(new BrowserSessionState
                {
                    Tabs = _tabs.Select(tab => new BrowserTabState
                    {
                        Url = tab.Url,
                        Title = tab.Title,
                        IsPinned = tab.IsPinned,
                        Group = tab.Group
                    }).ToList(),
                    SelectedIndex = Math.Max(0, TabStrip.SelectedIndex)
                });
            }

            if (_privateWindow || _settings.Current.ClearDataOnExit)
            {
                var profileTab = _tabs.FirstOrDefault();
                if (profileTab is not null)
                {
                    await profileTab.ClearBrowsingDataAsync();
                }
            }

            foreach (var tab in _tabs)
            {
                tab.Close();
            }

            _memoryTimer.Stop();
            _downloadSaveTimer.Stop();
            AiWebView.Dispose();
            if (_privateWindow)
            {
                _environment.CleanupPrivateData();
            }
        }
        catch (Exception exception)
        {
            LogService.Instance.Error("Window shutdown failed", exception);
        }
        finally
        {
            _closing = true;
            Close();
        }
    }

    private sealed record TabHeaderElements(ListBoxItem Item, TextBlock Title, TextBlock Pin, Button Close);
    private sealed record SuggestionItem(string Display, string Url);
}
