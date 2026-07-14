package com.chromabrowser.app;

import org.junit.Test;

import java.util.Set;

import static org.junit.Assert.assertFalse;
import static org.junit.Assert.assertTrue;

public final class AdBlockerTest {
    private final AdBlocker blocker = new AdBlocker(Set.of("tracker.example", "ads.example"));

    @Test
    public void blocksExactAndSubdomainHosts() {
        assertTrue(blocker.shouldBlockHost("tracker.example"));
        assertTrue(blocker.shouldBlockHost("pixel.tracker.example"));
    }

    @Test
    public void doesNotBlockLookalikeHost() {
        assertFalse(blocker.shouldBlockHost("nottracker.example"));
        assertFalse(blocker.shouldBlockHost("example.org"));
    }
}
