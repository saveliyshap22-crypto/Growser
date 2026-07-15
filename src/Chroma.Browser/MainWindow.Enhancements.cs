using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Chroma.Browser.Models;

namespace Chroma.Browser;

public partial class MainWindow
{
    private bool _enhancementsInitialized;
    private bool _tabLayoutQueued;
    private int _lastResponsiveTabCount = -1;
    private double _lastResponsiveWidth = -1;

    private void Window_Activated(object? sender, EventArgs e)
    {
        if (!_enhancementsInitialized)
        {
            _enhancementsInitialized = true;
            TabStrip.ItemContainerGenerator.StatusChanged += (_, _) => QueueResponsiveLayout();
            TabStrip.LayoutUpdated += (_, _) =>
            {
                if (_tabs.Count != _lastResponsiveTabCount || Math.Abs(TabStrip.ActualWidth - _lastResponsiveWidth) > 2)
                {
                    QueueResponsiveLayout();
                }
            };
        }

        ApplyDynamicAppearance();
        AiIncludeContextCheck.IsChecked = _settings.Current.AiIncludePageContext;
        QueueResponsiveLayout();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => QueueResponsiveLayout();

    private void QueueResponsiveLayout()
    {
        if (_tabLayoutQueued)
        {
            return;
        }

        _tabLayoutQueued = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            () =>
            {
                _tabLayoutQueued = false;
                UpdateResponsiveLayout();
            });
    }

    private void UpdateResponsiveLayout()
    {
        _lastResponsiveTabCount = _tabs.Count;
        _lastResponsiveWidth = TabStrip.ActualWidth;
        TabCountText.Text = _tabs.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        TabOverviewButton.ToolTip = _tabs.Count switch
        {
            1 => "1 открытая вкладка",
            >= 2 and <= 4 => $"{_tabs.Count} открытые вкладки",
            _ => $"{_tabs.Count} открытых вкладок"
        };

        var compactDensity = _settings.Current.InterfaceDensity == InterfaceDensity.Compact;
        TitleRow.Height = new GridLength(compactDensity ? 44 : 50);
        ToolbarRow.Height = new GridLength(compactDensity ? 60 : 68);
        AddressBar.Height = compactDensity ? 42 : 48;

        if (_tabs.Count == 0 || TabStrip.ActualWidth <= 0)
        {
            return;
        }

        var gap = 5d;
        var available = Math.Max(200, TabStrip.ActualWidth - 8);
        var desired = _settings.Current.TabWidthMode switch
        {
            TabWidthMode.Compact => 104d,
            TabWidthMode.Wide => 210d,
            _ => Math.Clamp((available - ((_tabs.Count - 1) * gap)) / _tabs.Count, 82d, 220d)
        };
        if (compactDensity)
        {
            desired = Math.Max(76, desired - 12);
        }

        foreach (var header in _headers.Values)
        {
            header.Item.Width = desired;
            header.Item.MinWidth = compactDensity ? 72 : 78;
            header.Title.FontSize = compactDensity ? 11.5 : 12.5;
            header.Title.MaxWidth = Math.Max(24, desired - 58);
            header.Title.TextTrimming = TextTrimming.CharacterEllipsis;
            header.Title.ToolTip = header.Title.Text;
            header.Close.Width = compactDensity ? 24 : 27;
            header.Close.Height = compactDensity ? 24 : 27;
            header.Close.Visibility = header.Item.Tag is Views.BrowserTabView view &&
                                      !view.IsPinned &&
                                      _settings.Current.ShowTabCloseButtons &&
                                      desired >= 98
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (TabStrip.SelectedItem is not null)
        {
            TabStrip.ScrollIntoView(TabStrip.SelectedItem);
        }

        BlockedCountText.Visibility = ActualWidth < 1030 ? Visibility.Collapsed : Visibility.Visible;
    }

    private void TabStrip_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var viewer = FindVisualChild<ScrollViewer>(TabStrip);
        if (viewer is null || viewer.ScrollableWidth <= 0)
        {
            return;
        }

        viewer.ScrollToHorizontalOffset(viewer.HorizontalOffset - e.Delta);
        e.Handled = true;
    }

    private void TabOverview_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        if (_tabs.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "Нет открытых вкладок", IsEnabled = false });
        }
        else
        {
            for (var index = 0; index < _tabs.Count; index++)
            {
                var tab = _tabs[index];
                var item = new MenuItem
                {
                    Header = $"{index + 1}. {TrimForMenu(tab.Title, 48)}",
                    ToolTip = tab.Url,
                    IsCheckable = true,
                    IsChecked = tab == CurrentTab
                };
                item.Click += (_, _) =>
                {
                    if (_headers.TryGetValue(tab, out var header))
                    {
                        TabStrip.SelectedItem = header.Item;
                        TabStrip.ScrollIntoView(header.Item);
                    }
                };
                menu.Items.Add(item);
            }
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(CreateMenuItem("Новая вкладка", async () => await AddTabAsync(Services.NavigationService.NewTab, true), "Ctrl+T"));
        OpenContextMenu(menu, sender as FrameworkElement);
    }

    private void ApplyDynamicAppearance()
    {
        // MainWindow.Experience owns all visual resources so activation cannot
        // accidentally restore an old glass palette over the selected mode.
        QueueResponsiveLayout();
    }

    private async void AiQuickPrompt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string prompt })
        {
            AiPromptBox.Text = prompt;
            AiPromptBox.CaretIndex = AiPromptBox.Text.Length;
            AiPromptBox.Focus();
            AiStatus.Text = "Шаблон добавлен — при необходимости измените текст";
        }

        await Task.CompletedTask;
    }

    private async void AiFillPage_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentTab is null)
        {
            return;
        }

        AiIncludeContextCheck.IsChecked = true;
        if (string.IsNullOrWhiteSpace(AiPromptBox.Text))
        {
            AiPromptBox.Text = "Проанализируй текущую страницу. Кратко изложи содержание, выдели важные факты, возможные ошибки и практические выводы.";
        }

        AiPromptBox.Focus();
        AiPromptBox.CaretIndex = AiPromptBox.Text.Length;
        AiStatus.Text = "Контекст страницы будет добавлен при вставке запроса";
        await Task.CompletedTask;
    }

    private async void AiPasteToChat_Click(object sender, RoutedEventArgs e)
    {
        await ShowAiPanelAsync(true);
        var prompt = await BuildAiPromptAsync();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            AiStatus.Text = "Введите запрос для AI";
            AiPromptBox.Focus();
            return;
        }

        if (AiWebView.CoreWebView2 is null)
        {
            AiStatus.Text = "AI-сервис ещё загружается";
            return;
        }

        var serialized = JsonSerializer.Serialize(prompt);
        var script = $$"""
            (() => {
              const prompt = {{serialized}};
              const candidates = [...document.querySelectorAll('textarea,[contenteditable="true"],[role="textbox"]')];
              const element = candidates.find(x => x.offsetParent !== null && !x.disabled) || candidates[0];
              if (!element) return false;
              element.focus();
              if ('value' in element) {
                const proto = element.tagName === 'TEXTAREA' ? HTMLTextAreaElement.prototype : HTMLInputElement.prototype;
                const setter = Object.getOwnPropertyDescriptor(proto, 'value')?.set;
                if (setter) setter.call(element, prompt); else element.value = prompt;
                element.dispatchEvent(new Event('input', { bubbles: true }));
                element.dispatchEvent(new Event('change', { bubbles: true }));
              } else {
                element.textContent = prompt;
                element.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: prompt }));
              }
              return true;
            })()
            """;

        try
        {
            var result = await AiWebView.CoreWebView2.ExecuteScriptAsync(script);
            var inserted = bool.TryParse(result, out var value) && value;
            if (inserted)
            {
                AiStatus.Text = "Запрос вставлен в чат — проверьте его и отправьте";
                AiWebView.Focus();
            }
            else
            {
                Clipboard.SetText(prompt);
                AiStatus.Text = "Поле чата не найдено — запрос скопирован, вставьте Ctrl+V";
            }
        }
        catch (Exception exception)
        {
            Clipboard.SetText(prompt);
            AiStatus.Text = $"Автовставка недоступна: {exception.Message}. Запрос скопирован.";
        }
    }

    private async void AiCopyPrompt_Click(object sender, RoutedEventArgs e)
    {
        var prompt = await BuildAiPromptAsync();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            AiStatus.Text = "Введите запрос для AI";
            return;
        }

        Clipboard.SetText(prompt);
        AiStatus.Text = "Запрос скопирован в буфер обмена";
    }

    private async void AiOpenFull_Click(object sender, RoutedEventArgs e)
    {
        await ShowAiPanelAsync(true);
        if (AiProviderBox.SelectedItem is AiProvider provider)
        {
            await AddTabAsync(provider.Url, true);
        }
    }

    private async void AiContext_Changed(object sender, RoutedEventArgs e)
    {
        _settings.Current.AiIncludePageContext = AiIncludeContextCheck.IsChecked == true;
        await _settings.SaveAsync();
    }

    private void AiPromptBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            AiPasteToChat_Click(sender, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private async Task<string> BuildAiPromptAsync()
    {
        var prompt = AiPromptBox.Text.Trim();
        if (AiIncludeContextCheck.IsChecked != true || CurrentTab is null)
        {
            return prompt;
        }

        var pageText = await CurrentTab.GetPageTextAsync(false);
        var contextLimit = Math.Clamp(_settings.Current.AiContextCharacterLimit, 3_000, 30_000);
        if (pageText.Length > contextLimit)
        {
            pageText = pageText[..contextLimit];
            AiStatus.Text = $"Контекст сокращён до {contextLimit:N0} символов";
        }

        if (string.IsNullOrWhiteSpace(pageText))
        {
            return prompt;
        }

        return $"{prompt}\n\nКонтекст текущей страницы:\nНазвание: {CurrentTab.Title}\nURL: {CurrentTab.Url}\n\n{pageText}".Trim();
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var nested = FindVisualChild<T>(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static string TrimForMenu(string value, int length) =>
        string.IsNullOrWhiteSpace(value)
            ? "Новая вкладка"
            : value.Length <= length ? value : value[..(length - 1)] + "…";

    private static Color ParseAccent(string value)
    {
        try
        {
            if (ColorConverter.ConvertFromString(value) is Color color)
            {
                return Color.FromRgb(color.R, color.G, color.B);
            }
        }
        catch (FormatException)
        {
        }

        return Color.FromRgb(255, 138, 61);
    }

    private static void SetApplicationBrush(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }

        Application.Current.Resources[key] = brush;
    }

    private static Color Blend(Color first, Color second, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromRgb(
            (byte)(first.R + ((second.R - first.R) * amount)),
            (byte)(first.G + ((second.G - first.G) * amount)),
            (byte)(first.B + ((second.B - first.B) * amount)));
    }

    private static bool IsBright(Color color) =>
        ((color.R * 299) + (color.G * 587) + (color.B * 114)) / 1000 >= 150;
}
