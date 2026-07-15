package com.chromabrowser.app;

import android.content.Context;
import android.net.Uri;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.Collections;
import java.util.HashSet;
import java.util.Locale;
import java.util.Set;

public final class AdBlocker {
    private final Set<String> blockedHosts;

    public AdBlocker(Context context) {
        Set<String> hosts = new HashSet<>();
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(
            context.getAssets().open("adblock_hosts.txt")))) {
            String line;
            while ((line = reader.readLine()) != null) {
                String host = line.trim().toLowerCase(Locale.ROOT);
                if (!host.isEmpty() && !host.startsWith("#")) {
                    hosts.add(host);
                }
            }
        } catch (IOException ignored) {
            Collections.addAll(hosts, "doubleclick.net", "googlesyndication.com", "google-analytics.com");
        }
        blockedHosts = Collections.unmodifiableSet(hosts);
    }

    AdBlocker(Set<String> hosts) {
        blockedHosts = Collections.unmodifiableSet(new HashSet<>(hosts));
    }

    public boolean shouldBlock(String url) {
        if (url == null || url.trim().isEmpty()) {
            return false;
        }
        String host = Uri.parse(url).getHost();
        return shouldBlockHost(host);
    }

    boolean shouldBlockHost(String value) {
        if (value == null) {
            return false;
        }
        String host = value.toLowerCase(Locale.ROOT);
        for (String blocked : blockedHosts) {
            if (host.equals(blocked) || host.endsWith("." + blocked)) {
                return true;
            }
        }
        return false;
    }
}
