using System.Collections;
using System.Resources;
using System.Text.RegularExpressions;
using Chroma.Browser;

namespace Chroma.Browser.Tests;

public sealed class WindowResourceTests
{
    private static readonly Regex HexColorPattern = new(
        @"(?<![A-Za-z0-9_])#(?<hex>[0-9A-Fa-f]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void MainWindowAndWindowIconAreEmbeddedInWpfResources()
    {
        var assembly = typeof(MainWindow).Assembly;
        var resourceName = Assert.Single(
            assembly.GetManifestResourceNames(),
            name => name.EndsWith(".g.resources", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        Assert.NotNull(stream);

        using var reader = new ResourceReader(stream!);
        var keys = reader.Cast<DictionaryEntry>()
            .Select(entry => entry.Key?.ToString() ?? string.Empty)
            .ToArray();

        Assert.Contains(keys, key => string.Equals(key, "mainwindow.baml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(keys, key => string.Equals(key, "resources/chroma.png", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AllXamlHexColorsUseSupportedWpfLengths()
    {
        var xamlDirectory = Path.Combine(AppContext.BaseDirectory, "Xaml");
        Assert.True(Directory.Exists(xamlDirectory), $"XAML test directory was not copied: {xamlDirectory}");

        var xamlFiles = Directory.GetFiles(xamlDirectory, "*.xaml", SearchOption.AllDirectories);
        Assert.NotEmpty(xamlFiles);

        var supportedLengths = new HashSet<int> { 3, 4, 6, 8 };
        var invalidColors = xamlFiles
            .SelectMany(file => HexColorPattern.Matches(File.ReadAllText(file))
                .Select(match => new
                {
                    File = Path.GetRelativePath(xamlDirectory, file),
                    Token = match.Value,
                    Length = match.Groups["hex"].Value.Length
                }))
            .Where(color => !supportedLengths.Contains(color.Length))
            .Select(color => $"{color.File}: {color.Token} ({color.Length} hex digits)")
            .ToArray();

        Assert.True(
            invalidColors.Length == 0,
            "Invalid WPF hexadecimal color values:" + Environment.NewLine + string.Join(Environment.NewLine, invalidColors));
    }
}
