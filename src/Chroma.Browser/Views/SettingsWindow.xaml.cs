using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chroma.Browser.Models;
using Chroma.Browser.Services;
using Microsoft.Win32;

namespace Chroma.Browser.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private readonly IReadOnlyList<Choice<ThemeMode>> _themes =
    [
        new("Как в Windows", ThemeMode.System),
        new("Тёмная", ThemeMode.Dark),
        new("Светлая", ThemeMode.Light)
    ];
    private readonly IReadOnlyList<Choice<InterfaceDensity>> _densities =
    [
        new("Удобная", InterfaceDensity.Comfortable),
        new("Компактная", InterfaceDensity.Compact)
    ];
    private readonly IReadOnlyList<Choice<TabWidthMode>> _tabWidths =
    [
        new("Автоматически по ширине окна", TabWidthMode.Adaptive),
        new("Компактные вкладки", TabWidthMode.Compact),
        new("Широкие вкладки", TabWidthMode.Wide)
    ];
    private readonly IReadOnlyList<Choice<SurfaceShapeMode>> _shapes =
    [
        new("Сильно скруглённая", SurfaceShapeMode.Rounded),
        new("Сбалансированная", SurfaceShapeMode.Balanced),
        new("Почти квадратная", SurfaceShapeMode.Square)
    ];
    private readonly IReadOnlyList<Choice<GlassMode>> _glassModes =
    [
        new("Усиленное жидкое стекло", GlassMode.Strong),
        new("Мягкое стекло", GlassMode.Soft),
        new("Без стекла", GlassMode.Off)
    ];
    private readonly IReadOnlyList<Choice<CursorStyleMode>> _cursorStyles =
    [
        new("Системный курсор", CursorStyleMode.System),
        new("Белая точка с чёрной обводкой", CursorStyleMode.WhiteDot),
        new("Чёрная точка с белой обводкой", CursorStyleMode.BlackDot),
        new("Радужная точка без обводки", CursorStyleMode.RainbowDot)
    ];
    private readonly IReadOnlyList<Choice<int>> _contextLimits =
    [
        new("Короткий — 6 000 символов", 6000),
        new("Обычный — 12 000 символов", 12000),
        new("Большой — 18 000 символов", 18000)
    ];
    private readonly IReadOnlyList<SearchEngineOption> _searchEngines =
    [
        new("Google", "https://www.google.com/search?q={0}"),
        new("DuckDuckGo", "https://duckduckgo.com/?q={0}"),
        new("Bing", "https://www.bing.com/search?q={0}"),
        new("Brave Search", "https://search.brave.com/search?q={0}")
    ];

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        EnsureCursorResource(settings);
        InitializeComponent();

        ThemeBox.ItemsSource = _themes;
        ThemeBox.SelectedItem = _themes.First(item => item.Value == settings.Theme);
        DensityBox.ItemsSource = _densities;
        DensityBox.SelectedItem = _densities.First(item => item.Value == settings.InterfaceDensity);
        TabWidthBox.ItemsSource = _tabWidths;
        TabWidthBox.SelectedItem = _tabWidths.First(item => item.Value == settings.TabWidthMode);
        ShapeBox.ItemsSource = _shapes;
        ShapeBox.SelectedItem = _shapes.First(item => item.Value == settings.SurfaceShape);
        GlassModeBox.ItemsSource = _glassModes;
        GlassModeBox.SelectedItem = _glassModes.First(item => item.Value == settings.GlassMode);
        CursorStyleBox.ItemsSource = _cursorStyles;
        CursorStyleBox.SelectedItem = _cursorStyles.First(item => item.Value == settings.CursorStyle);
        AiContextLimitBox.ItemsSource = _contextLimits;
        AiContextLimitBox.SelectedItem = _contextLimits
            .OrderBy(item => Math.Abs(item.Value - settings.AiContextCharacterLimit))
            .First();

        AccentPalette.SelectedItem = AccentPalette.Items
            .OfType<ListBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, settings.AccentColor, StringComparison.OrdinalIgnoreCase))
            ?? AccentPalette.Items.OfType<ListBoxItem>().First();

        OpacitySlider.Value = Math.Clamp(settings.InterfaceOpacity, OpacitySlider.Minimum, OpacitySlider.Maximum);
        GlassSlider.Value = Math.Clamp(settings.GlassIntensity, GlassSlider.Minimum, GlassSlider.Maximum);
        CornerSlider.Value = Math.Clamp(settings.CornerRadius, CornerSlider.Minimum, CornerSlider.Maximum);
        CursorSizeSlider.Value = Math.Clamp(settings.CursorSize, (int)CursorSizeSlider.Minimum, (int)CursorSizeSlider.Maximum);
        MicaCheck.IsChecked = settings.UseMica;
        AmbientGlowCheck.IsChecked = settings.ShowAmbientGlow;
        ReduceMotionCheck.IsChecked = settings.ReduceMotion;
        MediaPillCheck.IsChecked = settings.MediaPillEnabled;
        WallpaperBox.Text = settings.NewTabWallpaperPath;
        ShowTabCloseButtonsCheck.IsChecked = settings.ShowTabCloseButtons;
        RestoreSessionCheck.IsChecked = settings.RestoreSession;
        SuspendTabsCheck.IsChecked = settings.SuspendBackgroundTabs;
        SuspendMinutesBox.Text = settings.SuspendAfterMinutes.ToString(CultureInfo.InvariantCulture);
        HttpsFirstCheck.IsChecked = settings.HttpsFirst;
        AdBlockCheck.IsChecked = settings.AdBlockEnabled;
        TrackingCheck.IsChecked = settings.TrackingPrevention;
        EnhancedSecurityCheck.IsChecked = settings.EnhancedSecurity;
        ClearOnExitCheck.IsChecked = settings.ClearDataOnExit;
        PasswordAutosaveCheck.IsChecked = settings.PasswordAutosave;
        AutofillCheck.IsChecked = settings.GeneralAutofill;
        SearchEngineBox.ItemsSource = _searchEngines;
        SearchEngineBox.SelectedItem = _searchEngines.FirstOrDefault(item => item.Name == settings.SearchEngineName) ?? _searchEngines[0];
        AiProviderBox.ItemsSource = AiProvider.BuiltIn;
        AiProviderBox.SelectedItem = AiProvider.BuiltIn.FirstOrDefault(item => item.Id == settings.AiProviderId) ?? AiProvider.BuiltIn[0];
        AiIncludePageContextCheck.IsChecked = settings.AiIncludePageContext;
        DownloadFolderBox.Text = settings.DownloadFolder;
        AskDownloadCheck.IsChecked = settings.AskDownloadLocation;
        AutoUpdateCheck.IsChecked = settings.AutoCheckUpdates;

        UpdateAppearanceLabels();
        UpdateAppearanceModeState();
        UpdateAccentPreview();
        UpdateCursorPreview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (AccentPalette.SelectedItem is not ListBoxItem { Tag: string accent })
        {
            MessageBox.Show("Выберите цвет акцента из палитры.", "Настройки", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(SuspendMinutesBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
        {
            MessageBox.Show("Укажите корректное время приостановки вкладок.", "Настройки", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.Theme = SelectedValue(ThemeBox, _themes, ThemeMode.System);
        _settings.InterfaceDensity = SelectedValue(DensityBox, _densities, InterfaceDensity.Comfortable);
        _settings.TabWidthMode = SelectedValue(TabWidthBox, _tabWidths, TabWidthMode.Adaptive);
        _settings.SurfaceShape = SelectedValue(ShapeBox, _shapes, SurfaceShapeMode.Rounded);
        _settings.GlassMode = SelectedValue(GlassModeBox, _glassModes, GlassMode.Strong);
        _settings.CursorStyle = SelectedValue(CursorStyleBox, _cursorStyles, CursorStyleMode.System);
        _settings.AiContextCharacterLimit = SelectedValue(AiContextLimitBox, _contextLimits, 12000);
        _settings.AccentColor = accent;
        _settings.InterfaceOpacity = OpacitySlider.Value;
        _settings.GlassIntensity = GlassSlider.Value;
        _settings.CornerRadius = CornerSlider.Value;
        _settings.CursorSize = (int)Math.Round(CursorSizeSlider.Value);
        _settings.UseMica = MicaCheck.IsChecked == true;
        _settings.ShowAmbientGlow = AmbientGlowCheck.IsChecked == true;
        _settings.ReduceMotion = ReduceMotionCheck.IsChecked == true;
        _settings.MediaPillEnabled = MediaPillCheck.IsChecked == true;
        _settings.NewTabWallpaperPath = WallpaperBox.Text.Trim();
        _settings.ShowTabCloseButtons = ShowTabCloseButtonsCheck.IsChecked == true;
        _settings.RestoreSession = RestoreSessionCheck.IsChecked == true;
        _settings.SuspendBackgroundTabs = SuspendTabsCheck.IsChecked == true;
        _settings.SuspendAfterMinutes = Math.Clamp(minutes, 1, 1440);
        _settings.HttpsFirst = HttpsFirstCheck.IsChecked == true;
        _settings.AdBlockEnabled = AdBlockCheck.IsChecked == true;
        _settings.TrackingPrevention = TrackingCheck.IsChecked == true;
        _settings.EnhancedSecurity = EnhancedSecurityCheck.IsChecked == true;
        _settings.ClearDataOnExit = ClearOnExitCheck.IsChecked == true;
        _settings.PasswordAutosave = PasswordAutosaveCheck.IsChecked == true;
        _settings.GeneralAutofill = AutofillCheck.IsChecked == true;
        _settings.AiIncludePageContext = AiIncludePageContextCheck.IsChecked == true;

        if (SearchEngineBox.SelectedItem is SearchEngineOption search)
        {
            _settings.SearchEngineName = search.Name;
            _settings.SearchUrlTemplate = search.Template;
        }

        if (AiProviderBox.SelectedItem is AiProvider provider)
        {
            _settings.AiProviderId = provider.Id;
        }

        _settings.DownloadFolder = DownloadFolderBox.Text.Trim();
        _settings.AskDownloadLocation = AskDownloadCheck.IsChecked == true;
        _settings.AutoCheckUpdates = AutoUpdateCheck.IsChecked == true;
        EnsureCursorResource(_settings);
        DialogResult = true;
    }

    private void AccentPalette_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateAccentPreview();

    private void AppearanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateAppearanceLabels();

    private void AppearanceMode_Changed(object sender, SelectionChangedEventArgs e) => UpdateAppearanceModeState();

    private void CursorStyle_Changed(object sender, SelectionChangedEventArgs e) => UpdateCursorPreview();

    private void CursorSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateAppearanceLabels();
        UpdateCursorPreview();
    }

    private void UpdateAppearanceLabels()
    {
        if (GlassValueText is null || OpacityValueText is null || CornerValueText is null || CursorSizeValueText is null)
        {
            return;
        }

        GlassValueText.Text = $"{GlassSlider.Value:P0}";
        OpacityValueText.Text = $"{OpacitySlider.Value:P0}";
        CornerValueText.Text = $"{CornerSlider.Value:0} px";
        CursorSizeValueText.Text = $"{CursorSizeSlider.Value:0} px";
    }

    private void UpdateAppearanceModeState()
    {
        if (GlassModeBox is null || ShapeBox is null || GlassSlider is null || OpacitySlider is null || MicaCheck is null || CornerSlider is null)
        {
            return;
        }

        var glassMode = SelectedValue(GlassModeBox, _glassModes, GlassMode.Strong);
        var shape = SelectedValue(ShapeBox, _shapes, SurfaceShapeMode.Rounded);
        var glassEnabled = glassMode != GlassMode.Off;
        GlassSlider.IsEnabled = glassEnabled;
        OpacitySlider.IsEnabled = glassEnabled;
        MicaCheck.IsEnabled = glassEnabled;
        CornerSlider.IsEnabled = shape == SurfaceShapeMode.Rounded;
    }

    private void UpdateAccentPreview()
    {
        if (AccentPreview is null || AccentPalette.SelectedItem is not ListBoxItem { Tag: string accent })
        {
            return;
        }

        try
        {
            AccentPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent));
        }
        catch (FormatException)
        {
            AccentPreview.Background = new SolidColorBrush(Color.FromRgb(255, 138, 61));
        }
    }

    private void UpdateCursorPreview()
    {
        if (CursorStyleBox is null || CursorPreviewDot is null || CursorSizeSlider is null)
        {
            return;
        }

        var style = SelectedValue(CursorStyleBox, _cursorStyles, CursorStyleMode.System);
        var size = Math.Clamp(CursorSizeSlider.Value, 10, 24);
        CursorPreviewDot.Width = size;
        CursorPreviewDot.Height = size;
        CursorPreviewDot.Visibility = style == CursorStyleMode.System ? Visibility.Collapsed : Visibility.Visible;
        CursorSizeSlider.IsEnabled = style != CursorStyleMode.System;
        Cursor = CustomCursorService.GetCursor(style, (int)Math.Round(size));

        switch (style)
        {
            case CursorStyleMode.BlackDot:
                CursorPreviewDot.Fill = Brushes.Black;
                CursorPreviewDot.Stroke = Brushes.White;
                CursorPreviewDot.StrokeThickness = 2;
                break;
            case CursorStyleMode.RainbowDot:
                CursorPreviewDot.Fill = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new(Colors.Red, 0),
                        new(Colors.Orange, 0.2),
                        new(Colors.Yellow, 0.38),
                        new(Colors.LimeGreen, 0.56),
                        new(Colors.DeepSkyBlue, 0.75),
                        new(Colors.MediumPurple, 1)
                    },
                    new Point(0, 0),
                    new Point(1, 1));
                CursorPreviewDot.Stroke = Brushes.Transparent;
                CursorPreviewDot.StrokeThickness = 0;
                break;
            default:
                CursorPreviewDot.Fill = Brushes.White;
                CursorPreviewDot.Stroke = Brushes.Black;
                CursorPreviewDot.StrokeThickness = 2;
                break;
        }
    }

    private void ResetAppearance_Click(object sender, RoutedEventArgs e)
    {
        AccentPalette.SelectedItem = AccentPalette.Items.OfType<ListBoxItem>().First();
        DensityBox.SelectedItem = _densities[0];
        TabWidthBox.SelectedItem = _tabWidths[0];
        ShapeBox.SelectedItem = _shapes[0];
        GlassModeBox.SelectedItem = _glassModes[0];
        CursorStyleBox.SelectedItem = _cursorStyles[0];
        OpacitySlider.Value = 0.96;
        GlassSlider.Value = 0.78;
        CornerSlider.Value = 18;
        CursorSizeSlider.Value = 16;
        MicaCheck.IsChecked = true;
        AmbientGlowCheck.IsChecked = true;
        ReduceMotionCheck.IsChecked = false;
        MediaPillCheck.IsChecked = true;
        ShowTabCloseButtonsCheck.IsChecked = true;
        UpdateAppearanceModeState();
        UpdateCursorPreview();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void BrowseWallpaper_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите обои",
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.webp|Все файлы|*.*"
        };
        if (dialog.ShowDialog(this) == true)
        {
            WallpaperBox.Text = dialog.FileName;
        }
    }

    private void BrowseDownloads_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Папка загрузок",
            InitialDirectory = Directory.Exists(DownloadFolderBox.Text) ? DownloadFolderBox.Text : null
        };
        if (dialog.ShowDialog(this) == true)
        {
            DownloadFolderBox.Text = dialog.FolderName;
        }
    }

    private static T SelectedValue<T>(ComboBox box, IReadOnlyList<Choice<T>> choices, T fallback) =>
        box.SelectedItem is Choice<T> selected ? selected.Value : choices.FirstOrDefault()?.Value ?? fallback;

    private static void EnsureCursorResource(AppSettings settings)
    {
        var cursor = CustomCursorService.GetCursor(settings);
        Application.Current.Resources["AppCursor"] = cursor;
    }

    private sealed record Choice<T>(string Name, T Value);
    private sealed record SearchEngineOption(string Name, string Template);
}
