using Chroma.Browser.Models;
using Chroma.Browser.Services;

namespace Chroma.Browser.Tests;

public sealed class NavigationServiceTests
{
    private readonly AppSettings _settings = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("chroma://newtab")]
    public void EmptyInputOpensNewTab(string input)
    {
        Assert.Equal(NavigationService.NewTab, NavigationService.Resolve(input, _settings));
    }

    [Fact]
    public void HostUsesHttpsFirst()
    {
        Assert.Equal("https://example.com/", NavigationService.Resolve("example.com", _settings));
    }

    [Fact]
    public void AbsoluteUrlIsPreserved()
    {
        Assert.Equal("https://example.com/path?q=1", NavigationService.Resolve("https://example.com/path?q=1", _settings));
    }

    [Fact]
    public void TextBecomesEscapedSearch()
    {
        Assert.Equal(
            "https://www.google.com/search?q=chroma%20browser",
            NavigationService.Resolve("chroma browser", _settings));
    }

    [Theory]
    [InlineData("!g liquid glass", "https://www.google.com/search?q=liquid%20glass")]
    [InlineData("!ddg privacy", "https://duckduckgo.com/?q=privacy")]
    [InlineData("!w Chromium", "https://ru.wikipedia.org/w/index.php?search=Chromium")]
    [InlineData("!yt browser", "https://www.youtube.com/results?search_query=browser")]
    public void BangCommandsUseExpectedService(string input, string expected)
    {
        Assert.Equal(expected, NavigationService.Resolve(input, _settings));
    }
}

