using Chroma.Browser.Models;
using Chroma.Browser.Services;

namespace Chroma.Browser.Tests;

public sealed class ExperienceSettingsTests
{
    [Fact]
    public void SquareAndFlatModeReachTheNewTabPage()
    {
        var page = NewTabPageService.Build(
            new AppSettings
            {
                SurfaceShape = SurfaceShapeMode.Square,
                GlassMode = GlassMode.Off,
                AccentColor = "#FF8A3D"
            },
            [],
            false);

        Assert.Contains("--control-radius:4px", page, StringComparison.Ordinal);
        Assert.Contains("--panel-radius:7px", page, StringComparison.Ordinal);
        Assert.Contains("class=\" flat\"", page, StringComparison.Ordinal);
        Assert.Contains("body.flat", page, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(CursorStyleMode.WhiteDot, "fill%3D%27white%27")]
    [InlineData(CursorStyleMode.BlackDot, "fill%3D%27black%27")]
    [InlineData(CursorStyleMode.RainbowDot, "linearGradient")]
    public void CustomWebCursorScriptContainsSelectedVisual(CursorStyleMode style, string expected)
    {
        var script = CustomCursorService.BuildWebCursorScript(new AppSettings
        {
            CursorStyle = style,
            CursorSize = 18
        });

        Assert.Contains("chroma-custom-cursor", script, StringComparison.Ordinal);
        Assert.Contains("data:image/svg+xml", script, StringComparison.Ordinal);
        Assert.Contains(expected, script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SystemCursorScriptRemovesInjectedCursor()
    {
        var script = CustomCursorService.BuildWebCursorScript(new AppSettings
        {
            CursorStyle = CursorStyleMode.System
        });

        Assert.Contains("remove", script, StringComparison.Ordinal);
        Assert.Contains("chroma-custom-cursor", script, StringComparison.Ordinal);
    }

    [Fact]
    public void PrivateNewTabNeverContainsBookmarkCards()
    {
        var page = NewTabPageService.Build(
            new AppSettings { SurfaceShape = SurfaceShapeMode.Balanced },
            [new BookmarkEntry { Title = "Secret", Url = "https://example.com/private" }],
            true);

        Assert.DoesNotContain("Secret", page, StringComparison.Ordinal);
        Assert.DoesNotContain("example.com/private", page, StringComparison.Ordinal);
        Assert.Contains("Инкогнито", page, StringComparison.Ordinal);
    }
}
