package com.chromabrowser.app;

import android.content.Context;
import android.content.SharedPreferences;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;

final class BrowserStore {
    static final class Entry {
        final String title;
        final String url;
        final long time;

        Entry(String title, String url, long time) {
            this.title = title;
            this.url = url;
            this.time = time;
        }

        @Override
        public String toString() {
            return title + "\n" + url;
        }
    }

    private static final String PREFS = "chroma_browser";
    private static final String HISTORY = "history";
    private static final String BOOKMARKS = "bookmarks";
    private static final String SESSION = "session";
    private final SharedPreferences preferences;
    private final boolean privateMode;

    BrowserStore(Context context, boolean privateMode) {
        preferences = context.getSharedPreferences(privateMode ? PREFS + "_private" : PREFS, Context.MODE_PRIVATE);
        this.privateMode = privateMode;
    }

    void addHistory(String title, String url) {
        if (privateMode || url == null || url.startsWith("chroma://") || url.startsWith("data:") ||
            url.startsWith("https://chroma.internal/")) {
            return;
        }
        List<Entry> entries = readEntries(HISTORY);
        entries.removeIf(entry -> entry.url.equals(url));
        entries.add(0, new Entry(cleanTitle(title, url), url, System.currentTimeMillis()));
        if (entries.size() > 500) {
            entries = new ArrayList<>(entries.subList(0, 500));
        }
        writeEntries(HISTORY, entries);
    }

    List<Entry> history() {
        return readEntries(HISTORY);
    }

    List<Entry> bookmarks() {
        return readEntries(BOOKMARKS);
    }

    boolean isBookmarked(String url) {
        for (Entry entry : bookmarks()) {
            if (entry.url.equals(url)) {
                return true;
            }
        }
        return false;
    }

    boolean toggleBookmark(String title, String url) {
        if (privateMode || url == null || url.startsWith("chroma://")) {
            return false;
        }
        List<Entry> entries = readEntries(BOOKMARKS);
        boolean removed = entries.removeIf(entry -> entry.url.equals(url));
        if (!removed) {
            entries.add(0, new Entry(cleanTitle(title, url), url, System.currentTimeMillis()));
        }
        writeEntries(BOOKMARKS, entries);
        return !removed;
    }

    void clearHistory() {
        if (!privateMode) {
            preferences.edit().remove(HISTORY).apply();
        }
    }

    void saveSession(List<String> urls, int selectedIndex) {
        if (privateMode) {
            return;
        }
        JSONArray array = new JSONArray();
        for (String url : urls) {
            if (url != null && !url.startsWith("data:")) {
                array.put(url);
            }
        }
        JSONObject root = new JSONObject();
        try {
            root.put("selected", selectedIndex);
            root.put("tabs", array);
            preferences.edit().putString(SESSION, root.toString()).apply();
        } catch (JSONException ignored) {
            preferences.edit().remove(SESSION).apply();
        }
    }

    List<String> restoreSession() {
        List<String> result = new ArrayList<>();
        if (privateMode || !preferences.getBoolean("restore_session", true)) {
            return result;
        }
        try {
            JSONObject root = new JSONObject(preferences.getString(SESSION, "{}"));
            JSONArray tabs = root.optJSONArray("tabs");
            if (tabs != null) {
                for (int index = 0; index < Math.min(tabs.length(), 12); index++) {
                    String url = tabs.optString(index);
                    if (!url.trim().isEmpty()) {
                        result.add(url);
                    }
                }
            }
        } catch (JSONException ignored) {
            preferences.edit().remove(SESSION).apply();
        }
        return result;
    }

    SharedPreferences preferences() {
        return preferences;
    }

    private List<Entry> readEntries(String key) {
        List<Entry> result = new ArrayList<>();
        try {
            JSONArray array = new JSONArray(preferences.getString(key, "[]"));
            for (int index = 0; index < array.length(); index++) {
                JSONObject item = array.optJSONObject(index);
                if (item == null) {
                    continue;
                }
                String url = item.optString("url");
                if (!url.trim().isEmpty()) {
                    result.add(new Entry(item.optString("title", url), url, item.optLong("time")));
                }
            }
        } catch (JSONException ignored) {
            preferences.edit().remove(key).apply();
        }
        return result;
    }

    private void writeEntries(String key, List<Entry> entries) {
        JSONArray array = new JSONArray();
        for (Entry entry : entries) {
            JSONObject item = new JSONObject();
            try {
                item.put("title", entry.title);
                item.put("url", entry.url);
                item.put("time", entry.time);
                array.put(item);
            } catch (JSONException ignored) {
                // JSONObject with primitive values is not expected to fail.
            }
        }
        preferences.edit().putString(key, array.toString()).apply();
    }

    private static String cleanTitle(String title, String fallback) {
        return title == null || title.trim().isEmpty() ? fallback : title.trim();
    }
}
