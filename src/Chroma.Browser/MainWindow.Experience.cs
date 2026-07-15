using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Chroma.Browser.Interop;
using Chroma.Browser.Models;
using Chroma.Browser.Services;
using Chroma.Browser.Views;

namespace Chroma.Browser;

public partial class MainWindow
{
    private readonly HashSet<BrowserTabView> _experienceTabs = [];
    private readonly Dictionary<BrowserTabView, TextBlock> _audioIndicators = [];
    private DispatcherTimer? _experienceTimer;
    private BrowserTabView? _mediaTab;
    private DateTimeOffset _mediaLastSeen;
    private DateTimeOffset _lastMediaMetadataRefresh;
    private string _experienceSignature = string.Empty;
    private bool _experienceTickRunning;
    private bool _lastAiPanelVisible;

    private void Experience_Initialized(object? sender, EventArgs e)
    {
        Loaded += (_, _) =>
        {
            _experienceTimer ??= new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _experienceTimer.Tick -= ExperienceTimer_Tick;
            _experienceTimer.Tick += ExperienceTimer_Tick;
            _experienceTimer.Start();
            _ = RunExperienceTickAsync();
        };
        Closed += (_, _) => _experienceTimer?.Stop();
    }

    private async void ExperienceTimer_Tick(object? sender, EventArgs e) => await RunExperienceTickAsync();

    private async Task RunExperienceTickAsync()
    {
        if (_experienceTickRunning || !_initialized)
        {
            return;
        }

        _experienceTickRunning = true;
        try
        {
            var settings = _settings.Current;
            var signature = string.Join(':',
                settings.Theme,
                settings.AccentColor,
                settings.GlassMode,
                settings.GlassIntensity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                settings.SurfaceShape,
                settings.CornerRadius.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture),
                settings.CursorStyle,
                settings.CursorSize,
                settings.MediaPillEnabled,
                settings.ReduceMotion,
                settings.UseMica,
                settings.InterfaceOpacity.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            var experienceChanged = !string.Equals(signature, _experienceSignature, StringComparison.Ordinal);
            if (experienceChanged)
            {
                _experienceSignature = signature;
                ApplyExperienceAppearance(settings);
            }

            foreach (var tab in _tabs.ToArray())
            {
                EnsureAudioIndicator(tab);
                if (experienceChanged || _experienceTabs.Add(tab))
                {
                    await tab.ApplyExperienceSettingsAsync();
                    tab.AudioStateChanged -= Tab_AudioStateChanged;
                    tab.AudioStateChanged += Tab_AudioStateChanged;
                }
                UpdateAudioIndicator(tab);
            }

            foreach (var stale in _experienceTabs.Where(tab => !_tabs.Contains(tab)).ToArray())
            {
                _experienceTabs.Remove(stale);
                _audioIndicators.Remove(stale);
            }

            if (_aiInitialized && AiWebView.CoreWebView2 is not null && experienceChanged)
            {
                await AiWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    CustomCursorService.BuildWebCursorScript(settings));
                await CustomCursorService.ApplyWebCursorAsync(AiWebView.CoreWebView2, settings);
            }

            await RefreshMediaPillAsync(settings);
            AnimateAiPanelIfNeeded(settings);
        }
        catch (Exception exception)
        {
            LogService.Instance.Warn($"Experience refresh failed: {exception.Message}");
        }
        finally
        {
            _experienceTickRunning = false;
        }
    }

    private void ApplyExperienceAppearance(AppSettings settings)
    {
        var cursor = CustomCursorService.GetCursor(settings);
        Application.Current.Resources["AppCursor"] = cursor;
        Resources["AppCursor"] = cursor;
        Cursor = cursor;

        var accent = ParseAccent(settings.AccentColor);
        var light = settings.Theme == ThemeMode.Light;
        var glassAmount = Math.Clamp(settings.GlassIntensity, 0.35, 1);
        var baseSurface = light ? Color.FromRgb(245, 247, 250) : Color.FromRgb(31, 34, 40);
        var strongSurface = light ? Color.FromRgb(250, 251, 253) : Color.FromRgb(25, 27, 32);
        var hoverSurface = light ? Color.FromRgb(230, 234, 240) : Color.FromRgb(48, 52, 61);

        var (glassAlpha, strongAlpha, hoverAlpha, highlightAlpha) = settings.GlassMode switch
        {
            GlassMode.Off => ((byte)255, (byte)255, (byte)255, (byte)0),
            GlassMode.Soft => ((byte)(150 + (glassAmount * 48)), (byte)(205 + (glassAmount * 38)), (byte)(220 + (glassAmount * 25)), (byte)20),
            _ => ((byte)(100 + (glassAmount * 78)), (byte)(178 + (glassAmount * 67)), (byte)(190 + (glassAmount * 55)), (byte)(22 + (glassAmount * 28)))
        };

        SetExperienceBrush("WindowBrush", light ? Color.FromRgb(239, 242, 247) : Color.FromRgb(16, 18, 22));
        SetExperienceBrush("PanelBrush", light ? Color.FromRgb(247, 249, 252) : Color.FromRgb(25, 28, 34));
        SetExperienceBrush("TextBrush", light ? Color.FromRgb(27, 31, 38) : Color.FromRgb(247, 248, 250));
        SetExperienceBrush("MutedTextBrush", light ? Color.FromRgb(86, 94, 106) : Color.FromRgb(176, 182, 193));
        SetExperienceBrush("GlassBrush", Color.FromArgb(glassAlpha, baseSurface.R, baseSurface.G, baseSurface.B));
        SetExperienceBrush("GlassStrongBrush", Color.FromArgb(strongAlpha, strongSurface.R, strongSurface.G, strongSurface.B));
        SetExperienceBrush("GlassHoverBrush", Color.FromArgb(hoverAlpha, hoverSurface.R, hoverSurface.G, hoverSurface.B));
        SetExperienceBrush("GlassHighlightBrush", Color.FromArgb(highlightAlpha, 255, 255, 255));
        SetExperienceBrush("MenuSurfaceBrush", Color.FromArgb(settings.GlassMode == GlassMode.Off ? (byte)255 : (byte)248,
            strongSurface.R, strongSurface.G, strongSurface.B));
        SetExperienceBrush("DividerBrush", Color.FromArgb(light ? (byte)32 : (byte)42, accent.R, accent.G, accent.B));

        SetExperienceBrush("AccentBrush", Color.FromArgb(255, accent.R, accent.G, accent.B));
        SetExperienceBrush("AccentSoftBrush", Color.FromArgb((byte)(34 + (glassAmount * 36)), accent.R, accent.G, accent.B));
        SetExperienceBrush("AccentMediumBrush", Color.FromArgb((byte)(88 + (glassAmount * 52)), accent.R, accent.G, accent.B));
        SetExperienceBrush("AccentStrongBrush", Color.FromArgb(235, accent.R, accent.G, accent.B));
        SetExperienceBrush("AccentTextBrush", Blend(accent, Colors.White, 0.46));
        SetExperienceBrush("AccentForegroundBrush", IsBright(accent) ? Color.FromRgb(20, 22, 26) : Colors.White);

        var userRadius = Math.Clamp(settings.CornerRadius, 0, 28);
        var (control, panel, tab, pill) = settings.SurfaceShape switch
        {
            SurfaceShapeMode.Square => (new CornerRadius(4), new CornerRadius(7), new CornerRadius(4), new CornerRadius(6)),
            SurfaceShapeMode.Balanced => (new CornerRadius(10), new CornerRadius(16), new CornerRadius(10), new CornerRadius(15)),
            _ => (new CornerRadius(Math.Max(12, userRadius)),
                new CornerRadius(Math.Max(20, userRadius + 10)),
                new CornerRadius(Math.Max(12, userRadius - 1)),
                new CornerRadius(999))
        };
        SetExperienceResource("ControlCornerRadius", control);
        SetExperienceResource("PanelCornerRadius", panel);
        SetExperienceResource("TabCornerRadius", tab);
        SetExperienceResource("PillCornerRadius", pill);

        Opacity = settings.GlassMode == GlassMode.Off ? 1 : Math.Clamp(settings.InterfaceOpacity, 0.84, 1);
        ToolbarSurface.Effect = settings.GlassMode == GlassMode.Off
            ? null
            : new DropShadowEffect
            {
                BlurRadius = settings.GlassMode == GlassMode.Strong ? 28 : 17,
                ShadowDepth = 5,
                Opacity = settings.GlassMode == GlassMode.Strong ? 0.26 : 0.15,
                Color = Colors.Black
            };
        WindowsBackdrop.Apply(this, settings.UseMica && settings.GlassMode != GlassMode.Off, settings.Theme != ThemeMode.Light);
        QueueResponsiveLayout();
    }

    private void SetExperienceBrush(string key, Color color)
    {
        var brush = new SolidColorBrush(color);
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }
        SetExperienceResource(key, brush);
    }

    private void SetExperienceResource(string key, object value)
    {
        Application.Current.Resources[key] = value;
        Resources[key] = value;
    }

    private void EnsureAudioIndicator(BrowserTabView tab)
    {
        if (_audioIndicators.ContainsKey(tab) || !_headers.TryGetValue(tab, out var header) || header.Item.Content is not Grid grid)
        {
            return;
        }

        grid.ColumnDefinitions.Insert(2, new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(header.Close, 3);
        var indicator = new TextBlock
        {
            Width = 19,
            Margin = new Thickness(0, 0, 1, 0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            FontFamily = new FontFamily("Segoe Fluent Icons"),
            FontSize = 11,
            Visibility = Visibility.Collapsed,
            ToolTip = "Вкладка воспроизводит звук"
        };
        indicator.SetResourceReference(TextBlock.ForegroundProperty, "AccentTextBrush");
        Grid.SetColumn(indicator, 2);
        grid.Children.Add(indicator);
        _audioIndicators[tab] = indicator;
    }

    private void UpdateAudioIndicator(BrowserTabView tab)
    {
        if (!_audioIndicators.TryGetValue(tab, out var indicator))
        {
            return;
        }

        indicator.Visibility = tab.IsPlayingAudio || tab.IsMuted ? Visibility.Visible : Visibility.Collapsed;
        indicator.Text = tab.IsMuted ? "\uE74F" : "\uE767";
        indicator.ToolTip = tab.IsMuted ? "Звук выключен" : "Вкладка воспроизводит звук";
    }

    private void Tab_AudioStateChanged(object? sender, EventArgs e)
    {
        if (sender is BrowserTabView tab)
        {
            UpdateAudioIndicator(tab);
            if (tab.IsPlayingAudio)
            {
                _mediaTab = tab;
                _mediaLastSeen = DateTimeOffset.Now;
            }
        }
    }

    private async Task RefreshMediaPillAsync(AppSettings settings)
    {
        if (!settings.MediaPillEnabled)
        {
            MediaPill.Visibility = Visibility.Collapsed;
            AppLogoSurface.Visibility = Visibility.Visible;
            return;
        }

        var playing = _tabs.FirstOrDefault(tab => tab == CurrentTab && tab.IsPlayingAudio) ??
                      _tabs.FirstOrDefault(tab => tab.IsPlayingAudio);
        if (playing is not null)
        {
            _mediaTab = playing;
            _mediaLastSeen = DateTimeOffset.Now;
        }
        else if (_mediaTab is not null && (!_tabs.Contains(_mediaTab) || DateTimeOffset.Now - _mediaLastSeen > TimeSpan.FromSeconds(12)))
        {
            _mediaTab = null;
        }

        if (_mediaTab is null)
        {
            MediaPill.Visibility = Visibility.Collapsed;
            AppLogoSurface.Visibility = Visibility.Visible;
            return;
        }

        MediaPill.Visibility = Visibility.Visible;
        AppLogoSurface.Visibility = Visibility.Collapsed;
        MediaPill.Width = ActualWidth < 1080 ? 174 : 248;
        MediaArtistText.Visibility = ActualWidth < 1080 ? Visibility.Collapsed : Visibility.Visible;

        if (DateTimeOffset.Now - _lastMediaMetadataRefresh > TimeSpan.FromMilliseconds(900))
        {
            _lastMediaMetadataRefresh = DateTimeOffset.Now;
            var metadata = await _mediaTab.GetMediaMetadataAsync();
            MediaTitleText.Text = TrimForMenu(metadata.Title, ActualWidth < 1080 ? 22 : 38);
            MediaTitleText.ToolTip = metadata.Title;
            MediaArtistText.Text = TrimForMenu(metadata.Artist, 40);
            MediaPlayPauseButton.Content = metadata.IsPlaying || _mediaTab.IsPlayingAudio ? "\uE769" : "\uE768";
            MediaPlayPauseButton.ToolTip = metadata.IsPlaying || _mediaTab.IsPlayingAudio ? "Пауза" : "Продолжить";
            MediaMuteButton.Content = metadata.IsMuted || _mediaTab.IsMuted ? "\uE74F" : "\uE767";
            MediaMuteButton.ToolTip = metadata.IsMuted || _mediaTab.IsMuted ? "Включить звук" : "Выключить звук";
        }
    }

    private void MediaPill_Click(object sender, MouseButtonEventArgs e)
    {
        if (_mediaTab is null || FindAncestor<Button>(e.OriginalSource as DependencyObject) is not null)
        {
            return;
        }

        if (_headers.TryGetValue(_mediaTab, out var header))
        {
            TabStrip.SelectedItem = header.Item;
            TabStrip.ScrollIntoView(header.Item);
        }
        e.Handled = true;
    }

    private async void MediaPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_mediaTab is not null)
        {
            var changed = await _mediaTab.ToggleMediaPlaybackAsync();
            if (!changed && _headers.TryGetValue(_mediaTab, out var header))
            {
                TabStrip.SelectedItem = header.Item;
                _mediaTab.FocusPage();
            }
            _mediaLastSeen = DateTimeOffset.Now;
            await RefreshMediaPillAsync(_settings.Current);
        }
        e.Handled = true;
    }

    private async void MediaMute_Click(object sender, RoutedEventArgs e)
    {
        if (_mediaTab is not null)
        {
            _mediaTab.ToggleMute();
            _mediaLastSeen = DateTimeOffset.Now;
            await RefreshMediaPillAsync(_settings.Current);
        }
        e.Handled = true;
    }

    private void AnimateAiPanelIfNeeded(AppSettings settings)
    {
        var visible = AiPanel.Visibility == Visibility.Visible;
        if (visible == _lastAiPanelVisible)
        {
            return;
        }
        _lastAiPanelVisible = visible;
        if (!visible || settings.ReduceMotion)
        {
            AiPanel.Opacity = 1;
            return;
        }

        AiPanel.Opacity = 0;
        var animation = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(190))
        {
            EasingFunction = new System.Windows.Media.Animation.CubicEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            }
        };
        AiPanel.BeginAnimation(OpacityProperty, animation);
    }
}
