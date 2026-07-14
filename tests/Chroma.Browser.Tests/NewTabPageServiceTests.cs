using Chroma.Browser.Models;
using Chroma.Browser.Services;

namespace Chroma.Browser.Tests;

public sealed class NewTabPageServiceTests
{
    [Fact]
    public void BookmarkTextIsHtmlEncoded()
    {
        var page = NewTabPageService.Build(
            new AppSettings(),
            [new BookmarkEntry { Title = "<script>alert(1)</script>", Url = "https://example.com/?a=1&b=2" }],
            false);

        Assert.DoesNotContain("<script>alert(1)</script>", page, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", page, StringComparison.Ordinal);
        Assert.Contains("https://example.com/?a=1&amp;b=2", page, StringComparison.Ordinal);
    }

    [Fact]
    public void PrivatePageIncludesPrivateNotice()
    {
        var page = NewTabPageService.Build(new AppSettings(), [], true);
        Assert.Contains("Инкогнито", page, StringComparison.Ordinal);
    }
}
