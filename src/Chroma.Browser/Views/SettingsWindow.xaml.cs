using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Chroma.Browser.Models;
using Microsoft.Win32;

namespace Chroma.Browser.Views;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
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

        ThemeBox.ItemsSource = Enum.GetValues<ThemeMode>();
        ThemeBox.SelectedItem = settings.Theme;
        AccentBox.Text = settings.AccentColor;
        OpacitySlider.Value = settings.InterfaceOpacity;
        MicaCheck.IsChecked = settings.UseMica;
        ReduceMotionCheck.IsChecked = settings.ReduceMotion;
        WallpaperBox.Text = settings.NewTabWallpaperPath;
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
        DownloadFolderBox.Text = settings.DownloadFolder;
        AskDownloadCheck.IsChecked = settings.AskDownloadLocation;
        AutoUpdateCheck.IsChecked = settings.AutoCheckUpdates;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ColorConverter.ConvertFromString(AccentBox.Text) is not Color)
            {
                throw new FormatException("Invalid color");
            }
        }
        catch (FormatException)
        {
            MessageBox.Show("Цвет акцента должен быть записан как #RRGGBB.", "Настройки", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(SuspendMinutesBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes))
        {
            MessageBox.Show("Укажите корректное время приостановки вкладок.", "Настройки", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _settings.Theme = ThemeBox.SelectedItem is ThemeMode theme ? theme : ThemeMode.System;
        _settings.AccentColor = AccentBox.Text.Trim();
        _settings.InterfaceOpacity = OpacitySlider.Value;
        _settings.UseMica = MicaCheck.IsChecked == true;
        _settings.ReduceMotion = ReduceMotionCheck.IsChecked == true;
        _settings.NewTabWallpaperPath = WallpaperBox.Text.Trim();
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

    private sealed record SearchEngineOption(string Name, string Template);
}
