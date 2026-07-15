using System.Windows;
using Chroma.Browser.Services;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace Chroma.Browser.Views;

public partial class ExtensionsWindow : Window
{
    private readonly CoreWebView2Profile _profile;

    public ExtensionsWindow(CoreWebView2Profile profile)
    {
        _profile = profile;
        InitializeComponent();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            ExtensionGrid.ItemsSource = await _profile.GetBrowserExtensionsAsync();
        }
        catch (Exception exception)
        {
            LogService.Instance.Error("Could not enumerate browser extensions", exception);
            MessageBox.Show($"Не удалось получить список расширений:\n{exception.Message}", "Расширения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Папка распакованного расширения с manifest.json" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (!File.Exists(Path.Combine(dialog.FolderName, "manifest.json")))
        {
            MessageBox.Show("В выбранной папке нет manifest.json.", "Расширения", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _profile.AddBrowserExtensionAsync(dialog.FolderName);
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            LogService.Instance.Error("Extension installation failed", exception);
            MessageBox.Show($"Не удалось установить расширение:\n{exception.Message}", "Расширения", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (ExtensionGrid.SelectedItem is CoreWebView2BrowserExtension extension)
        {
            await extension.EnableAsync(!extension.IsEnabled);
            await RefreshAsync();
        }
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (ExtensionGrid.SelectedItem is not CoreWebView2BrowserExtension extension ||
            MessageBox.Show($"Удалить «{extension.Name}»?", "Расширения", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        await extension.RemoveAsync();
        await RefreshAsync();
    }
}

