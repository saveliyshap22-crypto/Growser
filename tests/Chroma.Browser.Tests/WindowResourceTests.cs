using System.Collections;
using System.Resources;
using Chroma.Browser;

namespace Chroma.Browser.Tests;

public sealed class WindowResourceTests
{
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
}
