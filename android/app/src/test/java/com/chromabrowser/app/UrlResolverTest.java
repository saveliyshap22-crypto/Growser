package com.chromabrowser.app;

import org.junit.Test;

import static org.junit.Assert.assertEquals;

public final class UrlResolverTest {
    private static final String SEARCH = "https://duckduckgo.com/?q=%s";

    @Test
    public void emptyInputOpensNewTab() {
        assertEquals(UrlResolver.NEW_TAB, UrlResolver.resolve("  ", SEARCH));
    }

    @Test
    public void preservesHttpsUrl() {
        assertEquals("https://example.com/a", UrlResolver.resolve("https://example.com/a", SEARCH));
    }

    @Test
    public void upgradesDomainToHttps() {
        assertEquals("https://example.com/test", UrlResolver.resolve("example.com/test", SEARCH));
    }

    @Test
    public void searchesPlainText() {
        assertEquals("https://duckduckgo.com/?q=chroma+browser", UrlResolver.resolve("chroma browser", SEARCH));
    }
}
