package com.chromabrowser.app;

import android.Manifest;
import android.app.Activity;
import android.app.AlertDialog;
import android.app.DownloadManager;
import android.content.ClipData;
import android.content.ClipboardManager;
import android.content.Context;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.graphics.Color;
import android.graphics.drawable.GradientDrawable;
import android.net.Uri;
import android.net.http.SslError;
import android.os.Build;
import android.os.Bundle;
import android.os.Environment;
import android.view.Gravity;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.view.Window;
import android.view.WindowManager;
import android.view.inputmethod.EditorInfo;
import android.view.inputmethod.InputMethodManager;
import android.webkit.CookieManager;
import android.webkit.GeolocationPermissions;
import android.webkit.PermissionRequest;
import android.webkit.RenderProcessGoneDetail;
import android.webkit.SafeBrowsingResponse;
import android.webkit.SslErrorHandler;
import android.webkit.URLUtil;
import android.webkit.ValueCallback;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebResourceResponse;
import android.webkit.WebSettings;
import android.webkit.WebStorage;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.EditText;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.PopupMenu;
import android.widget.ProgressBar;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import org.json.JSONTokener;

import java.io.ByteArrayInputStream;
import java.io.UnsupportedEncodingException;
import java.net.URLEncoder;
import java.text.DateFormat;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.atomic.AtomicLong;

public class MainActivity extends Activity {
    private static final int FILE_CHOOSER_REQUEST = 7101;
    private static final int WEB_PERMISSION_REQUEST = 7102;
    private static final int GEOLOCATION_PERMISSION_REQUEST = 7103;
    private static final int STORAGE_PERMISSION_REQUEST = 7104;
    private static final int MAX_TABS = 20;
    private static final String INTERNAL_HOME = "https://chroma.internal/newtab";
    private static final AtomicLong TAB_IDS = new AtomicLong();
    private static boolean privateSuffixConfigured;

    private static final int MENU_NEW_TAB = 1;
    private static final int MENU_PRIVATE = 2;
    private static final int MENU_BOOKMARKS = 3;
    private static final int MENU_HISTORY = 4;
    private static final int MENU_DOWNLOADS = 5;
    private static final int MENU_FIND = 6;
    private static final int MENU_SHARE = 7;
    private static final int MENU_TRANSLATE = 8;
    private static final int MENU_DESKTOP = 9;
    private static final int MENU_SETTINGS = 10;
    private static final int MENU_CLEAR = 11;
    private static final int MENU_TOGGLE_BOOKMARK = 12;

    private final List<BrowserTab> tabs = new ArrayList<>();
    private final Map<String, String> aiProviders = new LinkedHashMap<>();
    private int selectedTab = -1;
    private boolean aiConfigured;
    private ValueCallback<Uri[]> fileChooserCallback;
    private PermissionRequest pendingWebPermission;
    private String pendingGeolocationOrigin;
    private GeolocationPermissions.Callback pendingGeolocationCallback;
    private PendingDownload pendingDownload;
    private View customView;
    private WebChromeClient.CustomViewCallback customViewCallback;

    private FrameLayout root;
    private FrameLayout webContainer;
    private LinearLayout tabBar;
    private LinearLayout aiSheet;
    private EditText addressBar;
    private ProgressBar progressBar;
    private Button backButton;
    private Button forwardButton;
    private Button reloadButton;
    private Button bookmarkButton;
    private Button homeButton;
    private Button tabsButton;
    private Button aiButton;
    private Button bottomMenuButton;
    private TextView privateBadge;
    private WebView aiWebView;
    private Spinner aiProvider;
    private TextView aiStatus;
    private BrowserStore store;
    private AdBlocker adBlocker;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        if (isPrivateMode() && !privateSuffixConfigured) {
            WebView.setDataDirectorySuffix("incognito");
            privateSuffixConfigured = true;
        }

        super.onCreate(savedInstanceState);
        requestWindowFeature(Window.FEATURE_NO_TITLE);
        getWindow().setStatusBarColor(Color.TRANSPARENT);
        getWindow().setNavigationBarColor(Color.rgb(17, 19, 24));
        getWindow().addFlags(WindowManager.LayoutParams.FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS);
        setContentView(R.layout.activity_main);

        bindViews();
        store = new BrowserStore(this, isPrivateMode());
        adBlocker = new AdBlocker(this);
        privateBadge.setVisibility(isPrivateMode() ? View.VISIBLE : View.GONE);
        setTitle(isPrivateMode() ? "Chroma Browser — Инкогнито" : "Chroma Browser");
        configureListeners();
        WebView.startSafeBrowsing(this, success -> {
            if (!success) {
                Toast.makeText(this, "Системная Safe Browsing недоступна", Toast.LENGTH_SHORT).show();
            }
        });

        Uri incoming = getIntent().getData();
        if (incoming != null && isWebScheme(incoming.getScheme())) {
            addTab(incoming.toString(), true);
        } else if (!isPrivateMode()) {
            for (String url : store.restoreSession()) {
                addTab(url, false);
            }
            if (!tabs.isEmpty()) {
                selectTab(0);
            }
        }
        if (tabs.isEmpty()) {
            addTab(UrlResolver.NEW_TAB, true);
        }
    }

    protected boolean isPrivateMode() {
        return false;
    }

    private void bindViews() {
        root = findViewById(R.id.root);
        webContainer = findViewById(R.id.webContainer);
        tabBar = findViewById(R.id.tabBar);
        aiSheet = findViewById(R.id.aiSheet);
        addressBar = findViewById(R.id.addressBar);
        progressBar = findViewById(R.id.progressBar);
        backButton = findViewById(R.id.backButton);
        forwardButton = findViewById(R.id.forwardButton);
        reloadButton = findViewById(R.id.reloadButton);
        bookmarkButton = findViewById(R.id.bookmarkButton);
        homeButton = findViewById(R.id.homeButton);
        tabsButton = findViewById(R.id.tabsButton);
        aiButton = findViewById(R.id.aiButton);
        bottomMenuButton = findViewById(R.id.bottomMenuButton);
        privateBadge = findViewById(R.id.privateBadge);
        aiWebView = findViewById(R.id.aiWebView);
        aiProvider = findViewById(R.id.aiProvider);
        aiStatus = findViewById(R.id.aiStatus);
    }

    private void configureListeners() {
        findViewById(R.id.newTabButton).setOnClickListener(view -> addTab(UrlResolver.NEW_TAB, true));
        backButton.setOnClickListener(view -> {
            BrowserTab tab = currentTab();
            if (tab != null && tab.webView.canGoBack()) {
                tab.webView.goBack();
            }
        });
        forwardButton.setOnClickListener(view -> {
            BrowserTab tab = currentTab();
            if (tab != null && tab.webView.canGoForward()) {
                tab.webView.goForward();
            }
        });
        reloadButton.setOnClickListener(view -> {
            BrowserTab tab = currentTab();
            if (tab != null) {
                tab.webView.reload();
            }
        });

        View.OnClickListener menuListener = this::showMainMenu;
        findViewById(R.id.menuButton).setOnClickListener(menuListener);
        bottomMenuButton.setOnClickListener(menuListener);
        homeButton.setOnClickListener(view -> loadAddress(UrlResolver.NEW_TAB));
        tabsButton.setOnClickListener(view -> showTabsDialog());
        bookmarkButton.setOnClickListener(view -> showEntries("Закладки", store.bookmarks(), false));
        bookmarkButton.setOnLongClickListener(view -> {
            toggleBookmark();
            return true;
        });
        aiButton.setOnClickListener(view -> showAiSheet());
        findViewById(R.id.aiCloseButton).setOnClickListener(view -> hideAiSheet());
        findViewById(R.id.aiSummarize).setOnClickListener(view -> prepareAiPrompt("Кратко перескажи эту страницу по-русски, выделив основные факты:"));
        findViewById(R.id.aiExplain).setOnClickListener(view -> prepareAiPrompt("Объясни содержимое простыми словами по-русски:"));
        findViewById(R.id.aiTranslate).setOnClickListener(view -> prepareAiPrompt("Переведи следующий текст на русский язык, сохранив смысл и структуру:"));
        findViewById(R.id.aiCode).setOnClickListener(view -> prepareAiPrompt("Проанализируй код или технический текст, найди ошибки и предложи исправление:"));

        addressBar.setOnEditorActionListener((view, actionId, event) -> {
            if (actionId == EditorInfo.IME_ACTION_GO || actionId == EditorInfo.IME_ACTION_SEARCH) {
                loadAddress(addressBar.getText().toString());
                addressBar.clearFocus();
                InputMethodManager keyboard = (InputMethodManager) getSystemService(INPUT_METHOD_SERVICE);
                keyboard.hideSoftInputFromWindow(addressBar.getWindowToken(), 0);
                return true;
            }
            return false;
        });
    }

    private void addTab(String address, boolean select) {
        if (tabs.size() >= MAX_TABS) {
            Toast.makeText(this, "Открыто максимальное количество вкладок", Toast.LENGTH_SHORT).show();
            return;
        }
        WebView webView = new WebView(this);
        webView.setLayoutParams(new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.MATCH_PARENT));
        BrowserTab tab = new BrowserTab(TAB_IDS.incrementAndGet(), webView);
        configureBrowserWebView(tab);
        tabs.add(tab);
        if (select || selectedTab < 0) {
            selectTab(tabs.size() - 1);
        } else {
            webView.onPause();
        }
        navigate(tab, address);
        rebuildTabBar();
    }

    private void configureBrowserWebView(BrowserTab tab) {
        WebView webView = tab.webView;
        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setLoadsImagesAutomatically(true);
        settings.setSupportZoom(true);
        settings.setBuiltInZoomControls(true);
        settings.setDisplayZoomControls(false);
        settings.setSupportMultipleWindows(true);
        settings.setJavaScriptCanOpenWindowsAutomatically(false);
        settings.setMediaPlaybackRequiresUserGesture(true);
        settings.setAllowFileAccess(false);
        settings.setAllowContentAccess(true);
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        settings.setSafeBrowsingEnabled(true);
        settings.setGeolocationEnabled(true);
        settings.setUserAgentString(WebSettings.getDefaultUserAgent(this) + " ChromaBrowser/0.3");
        CookieManager.getInstance().setAcceptThirdPartyCookies(webView,
            store != null && store.preferences().getBoolean("third_party_cookies", false));
        webView.setWebViewClient(new ChromaWebViewClient(tab));
        webView.setWebChromeClient(new ChromaChromeClient(tab));
        webView.setDownloadListener((url, userAgent, disposition, mimeType, length) ->
            requestDownload(url, userAgent, disposition, mimeType));
        webView.setBackgroundColor(Color.rgb(17, 19, 24));
        webView.setLayerType(View.LAYER_TYPE_HARDWARE, null);
    }

    private void navigate(BrowserTab tab, String value) {
        String resolved = UrlResolver.resolve(value, searchTemplate());
        if (UrlResolver.NEW_TAB.equals(resolved) || resolved.startsWith(INTERNAL_HOME)) {
            tab.webView.loadDataWithBaseURL(INTERNAL_HOME, newTabHtml(), "text/html", "UTF-8", null);
            return;
        }
        tab.webView.loadUrl(resolved);
    }

    private void loadAddress(String value) {
        BrowserTab tab = currentTab();
        if (tab != null) {
            navigate(tab, value);
        }
    }

    private void selectTab(int index) {
        if (index < 0 || index >= tabs.size()) {
            return;
        }
        BrowserTab previous = currentTab();
        if (previous != null) {
            previous.webView.onPause();
        }
        selectedTab = index;
        BrowserTab selected = tabs.get(index);
        webContainer.removeAllViews();
        if (selected.webView.getParent() instanceof ViewGroup parent) {
            parent.removeView(selected.webView);
        }
        webContainer.addView(selected.webView);
        selected.webView.onResume();
        selected.webView.resumeTimers();
        updateNavigationUi(selected);
        rebuildTabBar();
    }

    private void closeTab(int index) {
        if (index < 0 || index >= tabs.size()) {
            return;
        }
        BrowserTab tab = tabs.remove(index);
        if (tab.webView.getParent() instanceof ViewGroup parent) {
            parent.removeView(tab.webView);
        }
        tab.webView.stopLoading();
        tab.webView.removeAllViews();
        tab.webView.destroy();
        if (tabs.isEmpty()) {
            selectedTab = -1;
            addTab(UrlResolver.NEW_TAB, true);
            return;
        }
        selectedTab = Math.min(index, tabs.size() - 1);
        selectTab(selectedTab);
    }

    private BrowserTab currentTab() {
        return selectedTab >= 0 && selectedTab < tabs.size() ? tabs.get(selectedTab) : null;
    }

    private void rebuildTabBar() {
        tabBar.removeAllViews();
        for (int index = 0; index < tabs.size(); index++) {
            BrowserTab tab = tabs.get(index);
            TextView chip = new TextView(this);
            chip.setText(ellipsize(tab.title, 20));
            chip.setTextColor(index == selectedTab ? Color.rgb(245, 247, 250) : Color.rgb(173, 179, 189));
            chip.setTextSize(12);
            chip.setGravity(Gravity.CENTER_VERTICAL);
            chip.setSingleLine(true);
            chip.setPadding(dp(14), 0, dp(14), 0);
            GradientDrawable background = new GradientDrawable(
                GradientDrawable.Orientation.TL_BR,
                index == selectedTab
                    ? new int[]{Color.argb(235, 65, 70, 82), Color.argb(225, 48, 52, 62)}
                    : new int[]{Color.argb(110, 52, 56, 64), Color.argb(80, 38, 41, 48)});
            background.setCornerRadius(dp(19));
            background.setStroke(dp(1), index == selectedTab
                ? Color.argb(95, 255, 255, 255)
                : Color.argb(38, 255, 255, 255));
            chip.setBackground(background);
            LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(dp(150), dp(38));
            params.setMarginEnd(dp(5));
            chip.setLayoutParams(params);
            int capturedIndex = index;
            chip.setOnClickListener(view -> selectTab(capturedIndex));
            chip.setOnLongClickListener(view -> {
                PopupMenu menu = new PopupMenu(this, chip);
                menu.getMenu().add("Закрыть вкладку").setOnMenuItemClickListener(item -> {
                    closeTab(capturedIndex);
                    return true;
                });
                menu.getMenu().add("Дублировать").setOnMenuItemClickListener(item -> {
                    addTab(tab.webView.getUrl(), true);
                    return true;
                });
                menu.show();
                return true;
            });
            tabBar.addView(chip);
        }
    }

    private void updateNavigationUi(BrowserTab tab) {
        String url = tab.webView.getUrl();
        String shown = isInternalUrl(url) ? "" : url;
        addressBar.setText(shown == null ? "" : shown);
        backButton.setEnabled(tab.webView.canGoBack());
        forwardButton.setEnabled(tab.webView.canGoForward());
        boolean bookmarked = shown != null && !shown.trim().isEmpty() && store.isBookmarked(shown);
        bookmarkButton.setText(bookmarked ? "★\nЗакладки" : "☆\nЗакладки");
        setBottomNavigationState(isInternalUrl(url) ? homeButton : null);
    }

    private void setBottomNavigationState(Button active) {
        int normal = Color.rgb(245, 247, 250);
        int accent = Color.rgb(174, 184, 255);
        homeButton.setTextColor(active == homeButton ? accent : normal);
        tabsButton.setTextColor(active == tabsButton ? accent : normal);
        bookmarkButton.setTextColor(active == bookmarkButton ? accent : normal);
        aiButton.setTextColor(active == aiButton ? accent : normal);
        bottomMenuButton.setTextColor(active == bottomMenuButton ? accent : normal);
    }

    private void toggleBookmark() {
        BrowserTab tab = currentTab();
        if (tab == null || isPrivateMode()) {
            Toast.makeText(this, isPrivateMode() ? "Закладки не сохраняются в инкогнито" : "Нет активной страницы", Toast.LENGTH_SHORT).show();
            return;
        }
        String url = tab.webView.getUrl();
        if (isInternalUrl(url)) {
            Toast.makeText(this, "Откройте сайт, чтобы добавить его в закладки", Toast.LENGTH_SHORT).show();
            return;
        }
        boolean added = store.toggleBookmark(tab.title, url);
        Toast.makeText(this, added ? "Добавлено в закладки" : "Удалено из закладок", Toast.LENGTH_SHORT).show();
        updateNavigationUi(tab);
    }

    private void showMainMenu(View anchor) {
        setBottomNavigationState(bottomMenuButton);
        PopupMenu popup = new PopupMenu(this, anchor);
        popup.getMenu().add(0, MENU_NEW_TAB, 0, "Новая вкладка");
        popup.getMenu().add(0, MENU_PRIVATE, 1, "Новая вкладка инкогнито");
        popup.getMenu().add(0, MENU_TOGGLE_BOOKMARK, 2, "Добавить или убрать закладку");
        popup.getMenu().add(0, MENU_BOOKMARKS, 3, "Закладки");
        popup.getMenu().add(0, MENU_HISTORY, 4, "История");
        popup.getMenu().add(0, MENU_DOWNLOADS, 5, "Загрузки");
        popup.getMenu().add(0, MENU_FIND, 6, "Найти на странице");
        popup.getMenu().add(0, MENU_SHARE, 7, "Поделиться");
        popup.getMenu().add(0, MENU_TRANSLATE, 8, "Перевести страницу");
        BrowserTab current = currentTab();
        popup.getMenu().add(0, MENU_DESKTOP, 9,
            current != null && current.desktopMode ? "Мобильная версия сайта" : "Версия для ПК");
        popup.getMenu().add(0, MENU_SETTINGS, 10, "Настройки");
        popup.getMenu().add(0, MENU_CLEAR, 11, "Очистить данные просмотра");
        popup.setOnMenuItemClickListener(this::handleMenuItem);
        popup.setOnDismissListener(menu -> {
            BrowserTab tab = currentTab();
            if (tab != null) {
                updateNavigationUi(tab);
            }
        });
        popup.show();
    }

    private boolean handleMenuItem(MenuItem item) {
        switch (item.getItemId()) {
            case MENU_NEW_TAB -> addTab(UrlResolver.NEW_TAB, true);
            case MENU_PRIVATE -> startActivity(new Intent(this, PrivateActivity.class));
            case MENU_TOGGLE_BOOKMARK -> toggleBookmark();
            case MENU_BOOKMARKS -> showEntries("Закладки", store.bookmarks(), false);
            case MENU_HISTORY -> showEntries("История", store.history(), true);
            case MENU_DOWNLOADS -> startActivity(new Intent(DownloadManager.ACTION_VIEW_DOWNLOADS));
            case MENU_FIND -> showFindDialog();
            case MENU_SHARE -> sharePage();
            case MENU_TRANSLATE -> translatePage();
            case MENU_DESKTOP -> toggleDesktopMode();
            case MENU_SETTINGS -> showSettingsDialog();
            case MENU_CLEAR -> confirmClearData();
            default -> {
                return false;
            }
        }
        return true;
    }

    private void showTabsDialog() {
        setBottomNavigationState(tabsButton);
        String[] labels = new String[tabs.size()];
        for (int index = 0; index < tabs.size(); index++) {
            labels[index] = (index == selectedTab ? "● " : "○ ") + tabs.get(index).title;
        }
        AlertDialog dialog = new AlertDialog.Builder(this)
            .setTitle("Вкладки — " + tabs.size())
            .setItems(labels, (ignored, which) -> selectTab(which))
            .setNeutralButton("Закрыть текущую", (ignored, which) -> closeTab(selectedTab))
            .setPositiveButton("Новая", (ignored, which) -> addTab(UrlResolver.NEW_TAB, true))
            .setNegativeButton("Готово", null)
            .create();
        dialog.setOnDismissListener(ignored -> updateNavigationUi(currentTab()));
        dialog.show();
    }

    private void showEntries(String title, List<BrowserStore.Entry> entries, boolean history) {
        setBottomNavigationState(bookmarkButton);
        if (entries.isEmpty()) {
            Toast.makeText(this, title + ": пока пусто", Toast.LENGTH_SHORT).show();
            updateNavigationUi(currentTab());
            return;
        }
        String[] labels = new String[entries.size()];
        DateFormat dateFormat = DateFormat.getDateTimeInstance(DateFormat.SHORT, DateFormat.SHORT);
        for (int index = 0; index < entries.size(); index++) {
            BrowserStore.Entry entry = entries.get(index);
            labels[index] = entry.title + (history ? "\n" + dateFormat.format(entry.time) : "\n" + entry.url);
        }
        AlertDialog.Builder builder = new AlertDialog.Builder(this)
            .setTitle(title)
            .setItems(labels, (dialog, which) -> loadAddress(entries.get(which).url))
            .setNegativeButton("Закрыть", null);
        if (history) {
            builder.setNeutralButton("Очистить", (dialog, which) -> store.clearHistory());
        }
        AlertDialog dialog = builder.create();
        dialog.setOnDismissListener(ignored -> updateNavigationUi(currentTab()));
        dialog.show();
    }

    private void showFindDialog() {
        EditText query = new EditText(this);
        query.setHint("Текст на странице");
        query.setSingleLine(true);
        query.setPadding(dp(20), dp(8), dp(20), dp(8));
        new AlertDialog.Builder(this)
            .setTitle("Найти на странице")
            .setView(query)
            .setPositiveButton("Найти", (dialog, which) -> {
                BrowserTab tab = currentTab();
                if (tab != null) {
                    tab.webView.findAllAsync(query.getText().toString());
                    tab.webView.setFindListener((active, count, done) -> {
                        if (done) {
                            Toast.makeText(this, "Совпадений: " + count, Toast.LENGTH_SHORT).show();
                        }
                    });
                }
            })
            .setNegativeButton("Отмена", null)
            .show();
    }

    private void sharePage() {
        BrowserTab tab = currentTab();
        if (tab == null || isInternalUrl(tab.webView.getUrl())) {
            return;
        }
        Intent share = new Intent(Intent.ACTION_SEND);
        share.setType("text/plain");
        share.putExtra(Intent.EXTRA_SUBJECT, tab.title);
        share.putExtra(Intent.EXTRA_TEXT, tab.title + "\n" + tab.webView.getUrl());
        startActivity(Intent.createChooser(share, "Поделиться страницей"));
    }

    private void translatePage() {
        BrowserTab tab = currentTab();
        if (tab == null || isInternalUrl(tab.webView.getUrl())) {
            return;
        }
        try {
            String encoded = URLEncoder.encode(tab.webView.getUrl(), "UTF-8");
            navigate(tab, "https://translate.google.com/translate?sl=auto&tl=ru&u=" + encoded);
        } catch (UnsupportedEncodingException impossible) {
            navigate(tab, tab.webView.getUrl());
        }
    }

    private void toggleDesktopMode() {
        BrowserTab tab = currentTab();
        if (tab == null) {
            return;
        }
        tab.desktopMode = !tab.desktopMode;
        String agent = tab.desktopMode
            ? "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
              "(KHTML, like Gecko) Chrome/150.0.0.0 Safari/537.36 ChromaBrowser/0.3"
            : WebSettings.getDefaultUserAgent(this) + " ChromaBrowser/0.3";
        tab.webView.getSettings().setUserAgentString(agent);
        tab.webView.reload();
    }

    private void showSettingsDialog() {
        LinearLayout content = new LinearLayout(this);
        content.setOrientation(LinearLayout.VERTICAL);
        content.setPadding(dp(22), dp(8), dp(22), 0);
        CheckBox blockAds = new CheckBox(this);
        blockAds.setText("Блокировать рекламу и трекеры");
        blockAds.setChecked(store.preferences().getBoolean("adblock", true));
        CheckBox restore = new CheckBox(this);
        restore.setText("Восстанавливать вкладки");
        restore.setChecked(store.preferences().getBoolean("restore_session", true));
        CheckBox cookies = new CheckBox(this);
        cookies.setText("Разрешить сторонние cookies");
        cookies.setChecked(store.preferences().getBoolean("third_party_cookies", false));
        TextView searchLabel = new TextView(this);
        searchLabel.setText("Поисковая система");
        searchLabel.setTextColor(Color.WHITE);
        searchLabel.setPadding(0, dp(14), 0, dp(5));
        Spinner search = new Spinner(this);
        String[] engines = {"DuckDuckGo", "Google", "Яндекс", "Bing"};
        search.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, engines));
        search.setSelection(Math.max(0, store.preferences().getInt("search_engine", 0)));
        content.addView(blockAds);
        content.addView(restore);
        content.addView(cookies);
        content.addView(searchLabel);
        content.addView(search);
        new AlertDialog.Builder(this)
            .setTitle("Настройки Chroma")
            .setView(content)
            .setPositiveButton("Сохранить", (dialog, which) -> {
                store.preferences().edit()
                    .putBoolean("adblock", blockAds.isChecked())
                    .putBoolean("restore_session", restore.isChecked())
                    .putBoolean("third_party_cookies", cookies.isChecked())
                    .putInt("search_engine", search.getSelectedItemPosition())
                    .apply();
                for (BrowserTab tab : tabs) {
                    CookieManager.getInstance().setAcceptThirdPartyCookies(tab.webView, cookies.isChecked());
                }
                BrowserTab tab = currentTab();
                if (tab != null && isInternalUrl(tab.webView.getUrl())) {
                    navigate(tab, UrlResolver.NEW_TAB);
                }
            })
            .setNegativeButton("Отмена", null)
            .show();
    }

    private void confirmClearData() {
        new AlertDialog.Builder(this)
            .setTitle("Очистить данные просмотра?")
            .setMessage("Будут удалены история, cookies, кэш и хранилища сайтов. Закладки останутся.")
            .setPositiveButton("Очистить", (dialog, which) -> clearBrowsingData())
            .setNegativeButton("Отмена", null)
            .show();
    }

    private void clearBrowsingData() {
        store.clearHistory();
        CookieManager.getInstance().removeAllCookies(null);
        CookieManager.getInstance().flush();
        WebStorage.getInstance().deleteAllData();
        for (BrowserTab tab : tabs) {
            tab.webView.clearCache(true);
            tab.webView.clearHistory();
            tab.webView.clearFormData();
        }
        if (aiConfigured) {
            aiWebView.clearCache(true);
            aiWebView.clearHistory();
        }
        Toast.makeText(this, "Данные просмотра очищены", Toast.LENGTH_SHORT).show();
    }

    private void showAiSheet() {
        setBottomNavigationState(aiButton);
        if (!aiConfigured) {
            configureAi();
        }
        aiSheet.setVisibility(View.VISIBLE);
        ViewGroup.LayoutParams params = aiSheet.getLayoutParams();
        params.height = Math.max(dp(430), (int) (root.getHeight() * 0.72));
        aiSheet.setLayoutParams(params);
        aiSheet.setTranslationY(params.height);
        aiSheet.animate().translationY(0).setDuration(230).start();
    }

    private void hideAiSheet() {
        aiSheet.animate().translationY(aiSheet.getHeight()).setDuration(200)
            .withEndAction(() -> {
                aiSheet.setVisibility(View.GONE);
                updateNavigationUi(currentTab());
            }).start();
    }

    private void configureAi() {
        aiConfigured = true;
        aiProviders.put("Mistral", "https://chat.mistral.ai/chat");
        aiProviders.put("Qwen", "https://chat.qwen.ai/");
        aiProviders.put("DeepSeek", "https://chat.deepseek.com/");
        aiProviders.put("OpenRouter", "https://openrouter.ai/chat");
        List<String> names = new ArrayList<>(aiProviders.keySet());
        aiProvider.setAdapter(new ArrayAdapter<>(this, android.R.layout.simple_spinner_dropdown_item, names));
        aiProvider.setOnItemSelectedListener(new android.widget.AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(android.widget.AdapterView<?> parent, View view, int position, long id) {
                String url = aiProviders.get(names.get(position));
                if (url != null && (aiWebView.getUrl() == null || !aiWebView.getUrl().startsWith(origin(url)))) {
                    aiWebView.loadUrl(url);
                }
            }

            @Override
            public void onNothingSelected(android.widget.AdapterView<?> parent) {
            }
        });
        WebSettings settings = aiWebView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setDatabaseEnabled(true);
        settings.setSafeBrowsingEnabled(true);
        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_NEVER_ALLOW);
        settings.setMediaPlaybackRequiresUserGesture(true);
        settings.setUserAgentString(WebSettings.getDefaultUserAgent(this) + " ChromaBrowserAI/0.3");
        CookieManager.getInstance().setAcceptThirdPartyCookies(aiWebView, true);
        aiWebView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri uri = request.getUrl();
                return !isWebScheme(uri.getScheme()) && openExternal(uri);
            }
        });
        aiWebView.setWebChromeClient(new WebChromeClient());
        aiWebView.setDownloadListener((url, agent, disposition, type, length) ->
            requestDownload(url, agent, disposition, type));
        aiWebView.loadUrl(aiProviders.get(names.get(0)));
    }

    private void prepareAiPrompt(String instruction) {
        BrowserTab tab = currentTab();
        if (tab == null) {
            return;
        }
        String script = "(function(){const s=String(window.getSelection());" +
            "const t=s.trim()?s:document.body.innerText;return t.slice(0,12000);})()";
        tab.webView.evaluateJavascript(script, raw -> {
            String text;
            try {
                Object decoded = new JSONTokener(raw).nextValue();
                text = decoded == null ? "" : decoded.toString();
            } catch (Exception ignored) {
                text = "";
            }
            String prompt = instruction + "\n\nСтраница: " + tab.title + "\nURL: " +
                tab.webView.getUrl() + "\n\n" + text;
            ClipboardManager clipboard = (ClipboardManager) getSystemService(CLIPBOARD_SERVICE);
            clipboard.setPrimaryClip(ClipData.newPlainText("Chroma AI prompt", prompt));
            aiStatus.setText("Запрос скопирован — вставьте его в чат выбранного сервиса");
            Toast.makeText(this, "Запрос скопирован в буфер обмена", Toast.LENGTH_LONG).show();
        });
    }

    private void requestDownload(String url, String userAgent, String disposition, String mimeType) {
        PendingDownload request = new PendingDownload(url, userAgent, disposition, mimeType);
        if (Build.VERSION.SDK_INT == Build.VERSION_CODES.P &&
            checkSelfPermission(Manifest.permission.WRITE_EXTERNAL_STORAGE) != PackageManager.PERMISSION_GRANTED) {
            pendingDownload = request;
            requestPermissions(new String[]{Manifest.permission.WRITE_EXTERNAL_STORAGE}, STORAGE_PERMISSION_REQUEST);
            return;
        }
        startDownload(request);
    }

    private void startDownload(PendingDownload pending) {
        try {
            String name = URLUtil.guessFileName(pending.url, pending.disposition, pending.mimeType);
            DownloadManager.Request request = new DownloadManager.Request(Uri.parse(pending.url));
            request.setTitle(name);
            request.setDescription("Chroma Browser");
            request.setMimeType(pending.mimeType);
            request.setNotificationVisibility(DownloadManager.Request.VISIBILITY_VISIBLE_NOTIFY_COMPLETED);
            request.setDestinationInExternalPublicDir(Environment.DIRECTORY_DOWNLOADS, name);
            request.addRequestHeader("User-Agent", pending.userAgent == null ? "ChromaBrowser/0.3" : pending.userAgent);
            String cookies = CookieManager.getInstance().getCookie(pending.url);
            if (cookies != null) {
                request.addRequestHeader("Cookie", cookies);
            }
            ((DownloadManager) getSystemService(DOWNLOAD_SERVICE)).enqueue(request);
            Toast.makeText(this, "Загрузка началась", Toast.LENGTH_SHORT).show();
        } catch (Exception exception) {
            Toast.makeText(this, "Не удалось начать загрузку", Toast.LENGTH_LONG).show();
        }
    }

    private void grantWebPermission(PermissionRequest request) {
        List<String> androidPermissions = new ArrayList<>();
        for (String resource : request.getResources()) {
            if (PermissionRequest.RESOURCE_VIDEO_CAPTURE.equals(resource) &&
                checkSelfPermission(Manifest.permission.CAMERA) != PackageManager.PERMISSION_GRANTED) {
                androidPermissions.add(Manifest.permission.CAMERA);
            }
            if (PermissionRequest.RESOURCE_AUDIO_CAPTURE.equals(resource) &&
                checkSelfPermission(Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {
                androidPermissions.add(Manifest.permission.RECORD_AUDIO);
            }
        }
        if (androidPermissions.isEmpty()) {
            request.grant(request.getResources());
        } else {
            pendingWebPermission = request;
            requestPermissions(androidPermissions.toArray(new String[0]), WEB_PERMISSION_REQUEST);
        }
    }

    private void showCustomView(View view, WebChromeClient.CustomViewCallback callback) {
        if (customView != null) {
            callback.onCustomViewHidden();
            return;
        }
        customView = view;
        customViewCallback = callback;
        root.addView(view, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.MATCH_PARENT));
        getWindow().getDecorView().setSystemUiVisibility(
            View.SYSTEM_UI_FLAG_FULLSCREEN |
            View.SYSTEM_UI_FLAG_HIDE_NAVIGATION |
            View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY);
    }

    private void hideCustomView() {
        if (customView == null) {
            return;
        }
        root.removeView(customView);
        customView = null;
        getWindow().getDecorView().setSystemUiVisibility(View.SYSTEM_UI_FLAG_VISIBLE);
        if (customViewCallback != null) {
            customViewCallback.onCustomViewHidden();
            customViewCallback = null;
        }
    }

    private boolean openExternal(Uri uri) {
        try {
            Intent intent;
            if ("intent".equalsIgnoreCase(uri.getScheme())) {
                intent = Intent.parseUri(uri.toString(), Intent.URI_INTENT_SCHEME);
                String fallback = intent.getStringExtra("browser_fallback_url");
                if (intent.resolveActivity(getPackageManager()) == null && fallback != null) {
                    loadAddress(fallback);
                    return true;
                }
            } else {
                intent = new Intent(Intent.ACTION_VIEW, uri);
            }
            intent.addCategory(Intent.CATEGORY_BROWSABLE);
            intent.setComponent(null);
            startActivity(intent);
        } catch (Exception exception) {
            Toast.makeText(this, "Нет приложения для этой ссылки", Toast.LENGTH_SHORT).show();
        }
        return true;
    }

    private String searchTemplate() {
        return switch (store.preferences().getInt("search_engine", 0)) {
            case 1 -> "https://www.google.com/search?q=%s";
            case 2 -> "https://yandex.ru/search/?text=%s";
            case 3 -> "https://www.bing.com/search?q=%s";
            default -> "https://duckduckgo.com/?q=%s";
        };
    }

    private String newTabHtml() {
        String template = searchTemplate();
        int markerIndex = template.indexOf("%s");
        String prefix = markerIndex >= 0 ? template.substring(0, markerIndex) : template;
        int questionIndex = prefix.indexOf('?');
        String action = questionIndex >= 0 ? prefix.substring(0, questionIndex) : prefix;
        int parameterStart = Math.max(prefix.lastIndexOf('?'), prefix.lastIndexOf('&'));
        String parameter = parameterStart >= 0 ? prefix.substring(parameterStart + 1) : "q=";
        int equalsIndex = parameter.indexOf('=');
        String queryName = equalsIndex > 0 ? parameter.substring(0, equalsIndex) : "q";
        String subtitle = isPrivateMode()
            ? "Приватная сессия: история и данные просмотра не сохраняются."
            : "Введите запрос — результаты откроются в выбранной поисковой системе.";

        return String.format(java.util.Locale.ROOT, """
            <!doctype html><html lang="ru"><head><meta charset="utf-8">
            <meta name="viewport" content="width=device-width,initial-scale=1">
            <meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src 'unsafe-inline'; form-action https:; navigate-to https: http:">
            <style>
            :root{color-scheme:dark;font-family:system-ui,-apple-system,sans-serif}*{box-sizing:border-box}
            body{margin:0;min-height:100vh;overflow:hidden;color:#f7f8fb;background:radial-gradient(circle at 18% 18%,#aeb8ff30,transparent 31%),radial-gradient(circle at 82% 72%,#d7dbff1f,transparent 38%),linear-gradient(145deg,#101218,#252a35 52%,#111318)}
            main{min-height:100vh;width:min(92vw,760px);margin:auto;display:flex;flex-direction:column;align-items:center;justify-content:center;padding:34px 0 88px}
            h1{margin:0 0 28px;font-size:clamp(42px,13vw,72px);line-height:1;font-weight:750;letter-spacing:-.065em;text-shadow:0 16px 42px #000a}h1 span{color:#cbd1ff}
            form{width:100%;height:68px;display:flex;align-items:center;gap:10px;padding:0 10px 0 22px;border:1px solid #ffffff3d;border-radius:999px;background:linear-gradient(145deg,#ffffff24,#ffffff0d);box-shadow:0 28px 82px #0009,inset 0 1px 0 #ffffff42;backdrop-filter:blur(30px)}
            .icon{font-size:22px;color:#c7ccd6}input{min-width:0;flex:1;border:0;outline:0;background:transparent;color:#fff;font-size:17px;font-weight:500}input::placeholder{color:#b9bec8}
            button{width:48px;height:48px;flex:0 0 48px;border:0;border-radius:999px;background:#d9ddff;color:#17191f;font-size:22px;font-weight:800}p{max-width:620px;margin:17px 20px 0;text-align:center;color:#adb3be;font-size:13px;line-height:1.5}
            </style></head><body><main><h1>Chroma <span>Browser</span></h1>
            <form action="%s" method="get"><span class="icon">⌕</span><input name="%s" autofocus autocomplete="off" enterkeyhint="search" placeholder="Поиск в интернете"><button type="submit" aria-label="Найти">→</button></form>
            <p>%s</p></main></body></html>
            """, escapeHtml(action), escapeHtml(queryName), escapeHtml(subtitle));
    }

    private static String escapeHtml(String value) {
        return value.replace("&", "&amp;")
            .replace("\"", "&quot;")
            .replace("<", "&lt;")
            .replace(">", "&gt;");
    }

    private static boolean isInternalUrl(String url) {
        return url == null || url.startsWith(INTERNAL_HOME) || url.startsWith("data:text/html");
    }

    private static boolean isWebScheme(String scheme) {
        return "http".equalsIgnoreCase(scheme) || "https".equalsIgnoreCase(scheme);
    }

    private static String origin(String url) {
        Uri uri = Uri.parse(url);
        return uri.getScheme() + "://" + uri.getHost();
    }

    private static String ellipsize(String value, int max) {
        String text = value == null || value.trim().isEmpty() ? "Новая вкладка" : value.trim();
        return text.length() <= max ? text : text.substring(0, max - 1) + "…";
    }

    private int dp(int value) {
        return Math.round(value * getResources().getDisplayMetrics().density);
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        Uri uri = intent.getData();
        if (uri != null && isWebScheme(uri.getScheme())) {
            loadAddress(uri.toString());
        }
    }

    @Override
    protected void onPause() {
        super.onPause();
        if (!isPrivateMode()) {
            List<String> urls = new ArrayList<>();
            for (BrowserTab tab : tabs) {
                String url = tab.webView.getUrl();
                urls.add(isInternalUrl(url) ? UrlResolver.NEW_TAB : url);
            }
            store.saveSession(urls, selectedTab);
        }
    }

    @Override
    protected void onDestroy() {
        hideCustomView();
        for (BrowserTab tab : tabs) {
            tab.webView.stopLoading();
            tab.webView.destroy();
        }
        tabs.clear();
        if (aiConfigured) {
            aiWebView.stopLoading();
            aiWebView.destroy();
        }
        if (isPrivateMode()) {
            CookieManager.getInstance().removeAllCookies(null);
            WebStorage.getInstance().deleteAllData();
        }
        super.onDestroy();
    }

    @Override
    public void onBackPressed() {
        if (customView != null) {
            hideCustomView();
            return;
        }
        if (aiSheet.getVisibility() == View.VISIBLE) {
            hideAiSheet();
            return;
        }
        BrowserTab tab = currentTab();
        if (tab != null && tab.webView.canGoBack()) {
            tab.webView.goBack();
            return;
        }
        super.onBackPressed();
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        if (requestCode == FILE_CHOOSER_REQUEST && fileChooserCallback != null) {
            Uri[] result = WebChromeClient.FileChooserParams.parseResult(resultCode, data);
            fileChooserCallback.onReceiveValue(result);
            fileChooserCallback = null;
        }
    }

    @Override
    public void onRequestPermissionsResult(int requestCode, String[] permissions, int[] grantResults) {
        super.onRequestPermissionsResult(requestCode, permissions, grantResults);
        boolean granted = grantResults.length > 0;
        for (int result : grantResults) {
            granted &= result == PackageManager.PERMISSION_GRANTED;
        }
        if (requestCode == WEB_PERMISSION_REQUEST && pendingWebPermission != null) {
            if (granted) {
                pendingWebPermission.grant(pendingWebPermission.getResources());
            } else {
                pendingWebPermission.deny();
            }
            pendingWebPermission = null;
        } else if (requestCode == GEOLOCATION_PERMISSION_REQUEST && pendingGeolocationCallback != null) {
            pendingGeolocationCallback.invoke(pendingGeolocationOrigin, granted, false);
            pendingGeolocationCallback = null;
            pendingGeolocationOrigin = null;
        } else if (requestCode == STORAGE_PERMISSION_REQUEST && pendingDownload != null) {
            if (granted) {
                startDownload(pendingDownload);
            }
            pendingDownload = null;
        }
    }

    private final class ChromaWebViewClient extends WebViewClient {
        private final BrowserTab tab;

        ChromaWebViewClient(BrowserTab tab) {
            this.tab = tab;
        }

        @Override
        public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
            Uri uri = request.getUrl();
            return !isWebScheme(uri.getScheme()) && openExternal(uri);
        }

        @Override
        public WebResourceResponse shouldInterceptRequest(WebView view, WebResourceRequest request) {
            if (store.preferences().getBoolean("adblock", true) && adBlocker.shouldBlock(request.getUrl().toString())) {
                return new WebResourceResponse("text/plain", "UTF-8", new ByteArrayInputStream(new byte[0]));
            }
            return super.shouldInterceptRequest(view, request);
        }

        @Override
        public void onPageStarted(WebView view, String url, android.graphics.Bitmap favicon) {
            if (tab == currentTab()) {
                progressBar.setVisibility(View.VISIBLE);
                progressBar.setProgress(5);
                reloadButton.setText("×");
                reloadButton.setOnClickListener(button -> view.stopLoading());
            }
        }

        @Override
        public void onPageFinished(WebView view, String url) {
            tab.title = view.getTitle() == null ? "Новая вкладка" : view.getTitle();
            if (tab == currentTab()) {
                progressBar.setVisibility(View.GONE);
                reloadButton.setText("↻");
                reloadButton.setOnClickListener(button -> view.reload());
                updateNavigationUi(tab);
            }
            store.addHistory(tab.title, url);
            rebuildTabBar();
            if (store.preferences().getBoolean("adblock", true) && !isInternalUrl(url)) {
                view.evaluateJavascript("(function(){const s=document.createElement('style');s.textContent='" +
                    "[class*=\\\"ad-container\\\"],[id^=\\\"google_ads\\\"],.adsbygoogle{display:none!important}';" +
                    "document.documentElement.appendChild(s)})()", null);
            }
        }

        @Override
        public void onReceivedSslError(WebView view, SslErrorHandler handler, SslError error) {
            handler.cancel();
            new AlertDialog.Builder(MainActivity.this)
                .setTitle("Небезопасное соединение")
                .setMessage("Chroma остановил загрузку: сертификат сайта недействителен.")
                .setPositiveButton("Закрыть", null)
                .show();
        }

        @Override
        public void onSafeBrowsingHit(WebView view, WebResourceRequest request, int threatType, SafeBrowsingResponse callback) {
            callback.backToSafety(true);
            Toast.makeText(MainActivity.this, "Опасная страница заблокирована", Toast.LENGTH_LONG).show();
        }

        @Override
        public boolean onRenderProcessGone(WebView view, RenderProcessGoneDetail detail) {
            int index = tabs.indexOf(tab);
            String url = view.getUrl();
            if (index >= 0) {
                closeTab(index);
                addTab(url == null ? UrlResolver.NEW_TAB : url, true);
            }
            Toast.makeText(MainActivity.this, "Процесс страницы восстановлен", Toast.LENGTH_LONG).show();
            return true;
        }
    }

    private final class ChromaChromeClient extends WebChromeClient {
        private final BrowserTab tab;

        ChromaChromeClient(BrowserTab tab) {
            this.tab = tab;
        }

        @Override
        public void onProgressChanged(WebView view, int progress) {
            if (tab == currentTab()) {
                progressBar.setProgress(progress);
                progressBar.setVisibility(progress >= 100 ? View.GONE : View.VISIBLE);
            }
        }

        @Override
        public void onReceivedTitle(WebView view, String title) {
            tab.title = title == null || title.trim().isEmpty() ? "Новая вкладка" : title;
            rebuildTabBar();
        }

        @Override
        public boolean onShowFileChooser(WebView view, ValueCallback<Uri[]> callback, FileChooserParams params) {
            if (fileChooserCallback != null) {
                fileChooserCallback.onReceiveValue(null);
            }
            fileChooserCallback = callback;
            try {
                startActivityForResult(params.createIntent(), FILE_CHOOSER_REQUEST);
            } catch (Exception exception) {
                fileChooserCallback = null;
                Toast.makeText(MainActivity.this, "Не удалось открыть выбор файла", Toast.LENGTH_SHORT).show();
            }
            return true;
        }

        @Override
        public void onPermissionRequest(PermissionRequest request) {
            runOnUiThread(() -> new AlertDialog.Builder(MainActivity.this)
                .setTitle("Разрешение для сайта")
                .setMessage("Разрешить странице доступ к камере или микрофону?")
                .setPositiveButton("Разрешить", (dialog, which) -> grantWebPermission(request))
                .setNegativeButton("Запретить", (dialog, which) -> request.deny())
                .setOnCancelListener(dialog -> request.deny())
                .show());
        }

        @Override
        public void onGeolocationPermissionsShowPrompt(String origin, GeolocationPermissions.Callback callback) {
            new AlertDialog.Builder(MainActivity.this)
                .setTitle("Геолокация")
                .setMessage("Разрешить сайту определить местоположение?\n" + origin)
                .setPositiveButton("Разрешить", (dialog, which) -> {
                    if (checkSelfPermission(Manifest.permission.ACCESS_FINE_LOCATION) == PackageManager.PERMISSION_GRANTED) {
                        callback.invoke(origin, true, false);
                    } else {
                        pendingGeolocationOrigin = origin;
                        pendingGeolocationCallback = callback;
                        requestPermissions(new String[]{
                            Manifest.permission.ACCESS_COARSE_LOCATION,
                            Manifest.permission.ACCESS_FINE_LOCATION
                        }, GEOLOCATION_PERMISSION_REQUEST);
                    }
                })
                .setNegativeButton("Запретить", (dialog, which) -> callback.invoke(origin, false, false))
                .show();
        }

        @Override
        public boolean onCreateWindow(WebView view, boolean dialog, boolean userGesture, android.os.Message resultMsg) {
            addTab(UrlResolver.NEW_TAB, true);
            BrowserTab created = currentTab();
            if (created == null) {
                return false;
            }
            WebView.WebViewTransport transport = (WebView.WebViewTransport) resultMsg.obj;
            transport.setWebView(created.webView);
            resultMsg.sendToTarget();
            return true;
        }

        @Override
        public void onCloseWindow(WebView window) {
            for (int index = 0; index < tabs.size(); index++) {
                if (tabs.get(index).webView == window) {
                    closeTab(index);
                    return;
                }
            }
        }

        @Override
        public void onShowCustomView(View view, CustomViewCallback callback) {
            showCustomView(view, callback);
        }

        @Override
        public void onHideCustomView() {
            hideCustomView();
        }
    }

    private static final class PendingDownload {
        final String url;
        final String userAgent;
        final String disposition;
        final String mimeType;

        PendingDownload(String url, String userAgent, String disposition, String mimeType) {
            this.url = url;
            this.userAgent = userAgent;
            this.disposition = disposition;
            this.mimeType = mimeType;
        }
    }
}
