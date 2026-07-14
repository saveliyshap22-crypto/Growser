using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Chroma.Browser.Models;
using Chroma.Browser.Services;

namespace Chroma.Browser.Views;

public partial class VaultWindow : Window
{
    private readonly SecureVaultService _vault;
    private readonly ObservableCollection<VaultEntry> _entries;

    public VaultWindow(SecureVaultService vault)
    {
        _vault = vault;
        _entries = new ObservableCollection<VaultEntry>(vault.Load());
        InitializeComponent();
        VaultGrid.ItemsSource = _entries;
    }

    private void VaultGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (VaultGrid.SelectedItem is VaultEntry entry)
        {
            OriginBox.Text = entry.Origin;
            UsernameBox.Text = entry.Username;
            PasswordBox.Password = entry.Password;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(OriginBox.Text.Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            MessageBox.Show("Укажите полный адрес сайта, например https://example.com.", "Пароли", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var entry = VaultGrid.SelectedItem as VaultEntry;
        if (entry is null)
        {
            entry = new VaultEntry();
            _entries.Add(entry);
        }

        entry.Origin = uri.GetLeftPart(UriPartial.Authority);
        entry.Username = UsernameBox.Text;
        entry.Password = PasswordBox.Password;
        entry.UpdatedAt = DateTimeOffset.Now;
        _vault.Save(_entries);
        VaultGrid.Items.Refresh();
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (VaultGrid.SelectedItem is VaultEntry entry)
        {
            Clipboard.SetText(entry.Password);
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (VaultGrid.SelectedItem is VaultEntry entry &&
            MessageBox.Show($"Удалить пароль для {entry.Origin}?", "Пароли", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _entries.Remove(entry);
            _vault.Save(_entries);
            OriginBox.Clear();
            UsernameBox.Clear();
            PasswordBox.Clear();
        }
    }
}
