package com.chromabrowser.app;

import android.webkit.WebView;

final class BrowserTab {
    final long id;
    final WebView webView;
    String title = "Новая вкладка";
    boolean desktopMode;

    BrowserTab(long id, WebView webView) {
        this.id = id;
        this.webView = webView;
    }
}
