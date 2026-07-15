using Chroma.Browser.Models;
using Chroma.Browser.Services;

namespace Chroma.Browser.Tests;

public sealed class NewTabPageServiceTests
{
    [Fact]
    public void HomePageDoesNotExposeBookmarkShortcuts()
    {
        var page = NewTabPageService.Build(
            new AppSettings(),
            [new BookmarkEntry { Title = "Private bookmark", Url = "https://example.com/private" }],
            false);

        Assert.DoesNotContain("Private bookmark", page, StringComparison.Ordinal);
        Assert.DoesNotContain("https://example.com/private", page, StringComparison.Ordinal);
        Assert.Contains("Поиск в интернете", page, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchFormUsesConfiguredQueryParameter()
    {
        var page = NewTabPageService.Build(
            new AppSettings { SearchUrlTemplate = "https://yandex.ru/search/?text={0}" },
            [],
            false);

        Assert.Contains("action=\"https://yandex.ru/search/\"", page, StringComparison.Ordinal);
        Assert.Contains("name=\"text\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void PrivatePageIncludesPrivateNotice()
    {
        var page = NewTabPageService.Build(new AppSettings(), [], true);
        Assert.Contains("Инкогнито", page, StringComparison.Ordinal);
    }
}
