using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Chroma.Browser.Models;
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
        InitializeComponent();

        ThemeBox.ItemsSource = _themes;
        ThemeBox.SelectedItem = _themes.First(item => item.Value == settings.Theme);
        DensityBox.ItemsSource = _densities;
        DensityBox.SelectedItem = _densities.First(item => item.Value == settings.InterfaceDensity);
        TabWidthBox.ItemsSource = _tabWidths;
        TabWidthBox.SelectedItem = _tabWidths.First(item => item.Value == settings.TabWidthMode);

        AccentPalette.SelectedItem = AccentPalette.Items
            .OfType<ListBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag as string, settings.AccentColor, StringComparison.OrdinalIgnoreCase))
            ?? AccentPalette.Items.OfType<ListBoxItem>().First();

        OpacitySlider.Value = settings.InterfaceOpacity;
        GlassSlider.Value = settings.GlassIntensity;
        CornerSlider.Value = settings.CornerRadius;
        MicaCheck.IsChecked = settings.UseMica;
        AmbientGlowCheck.IsChecked = settings.ShowAmbientGlow;
        ReduceMotionCheck.IsChecked = settings.ReduceMotion;
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
        UpdateAccentPreview();
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

        _settings.Theme = ThemeBox.SelectedItem is Choice<ThemeMode> theme ? theme.Value : ThemeMode.System;
        _settings.InterfaceDensity = DensityBox.SelectedItem is Choice<InterfaceDensity> density ? density.Value : InterfaceDensity.Comfortable;
        _settings.TabWidthMode = TabWidthBox.SelectedItem is Choice<TabWidthMode> tabs ? tabs.Value : TabWidthMode.Adaptive;
        _settings.AccentColor = accent;
        _settings.InterfaceOpacity = OpacitySlider.Value;
        _settings.GlassIntensity = GlassSlider.Value;
        _settings.CornerRadius = CornerSlider.Value;
        _settings.UseMica = MicaCheck.IsChecked == true;
        _settings.ShowAmbientGlow = AmbientGlowCheck.IsChecked == true;
        _settings.ReduceMotion = ReduceMotionCheck.IsChecked == true;
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
        DialogResult = true;
    }

    private void AccentPalette_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateAccentPreview();

    private void AppearanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdateAppearanceLabels();

    private void UpdateAppearanceLabels()
    {
        if (GlassValueText is null || OpacityValueText is null || CornerValueText is null)
        {
            return;
        }

        GlassValueText.Text = $"{GlassSlider.Value:P0}";
        OpacityValueText.Text = $"{OpacitySlider.Value:P0}";
        CornerValueText.Text = $"{CornerSlider.Value:0} px";
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

    private void ResetAppearance_Click(object sender, RoutedEventArgs e)
    {
        AccentPalette.SelectedItem = AccentPalette.Items.OfType<ListBoxItem>().First();
        DensityBox.SelectedItem = _densities[0];
        TabWidthBox.SelectedItem = _tabWidths[0];
        OpacitySlider.Value = 0.94;
        GlassSlider.Value = 0.78;
        CornerSlider.Value = 18;
        MicaCheck.IsChecked = true;
        AmbientGlowCheck.IsChecked = true;
        ReduceMotionCheck.IsChecked = false;
        ShowTabCloseButtonsCheck.IsChecked = true;
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

    private sealed record Choice<T>(string Name, T Value);
    private sealed record SearchEngineOption(string Name, string Template);
}
