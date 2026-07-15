package com.chromabrowser.app;

import java.net.IDN;
import java.net.URLEncoder;
import java.io.UnsupportedEncodingException;
import java.util.Locale;
import java.util.regex.Pattern;

public final class UrlResolver {
    public static final String NEW_TAB = "chroma://newtab";
    private static final Pattern IPV4 = Pattern.compile("^(?:\\d{1,3}\\.){3}\\d{1,3}(?::\\d+)?(?:/.*)?$");
    private static final Pattern DOMAIN = Pattern.compile("^(?:[\\p{L}0-9](?:[\\p{L}0-9-]{0,61}[\\p{L}0-9])?\\.)+[\\p{L}]{2,63}(?::\\d+)?(?:/.*)?$");

    private UrlResolver() {
    }

    public static String resolve(String value, String searchTemplate) {
        String input = value == null ? "" : value.trim();
        if (input.isEmpty() || NEW_TAB.equalsIgnoreCase(input)) {
            return NEW_TAB;
        }

        String lower = input.toLowerCase(Locale.ROOT);
        if (lower.startsWith("https://") || lower.startsWith("http://")) {
            return input;
        }
        if (lower.startsWith("about:")) {
            return input;
        }

        if (lower.startsWith("localhost") || lower.startsWith("127.0.0.1") || lower.startsWith("[::1]")) {
            return "http://" + input;
        }

        if (!input.contains(" ") && (IPV4.matcher(input).matches() || DOMAIN.matcher(input).matches())) {
            int slash = input.indexOf('/');
            String host = slash >= 0 ? input.substring(0, slash) : input;
            String path = slash >= 0 ? input.substring(slash) : "";
            int colon = host.lastIndexOf(':');
            String port = colon > 0 ? host.substring(colon) : "";
            String bareHost = colon > 0 ? host.substring(0, colon) : host;
            try {
                bareHost = IDN.toASCII(bareHost);
            } catch (IllegalArgumentException ignored) {
                return search(input, searchTemplate);
            }
            return "https://" + bareHost + port + path;
        }

        return search(input, searchTemplate);
    }

    private static String search(String query, String template) {
        String selected = template == null || !template.contains("%s")
            ? "https://duckduckgo.com/?q=%s"
            : template;
        try {
            return selected.replace("%s", URLEncoder.encode(query, "UTF-8"));
        } catch (UnsupportedEncodingException impossible) {
            return selected.replace("%s", query.replace(" ", "+"));
        }
    }
}
