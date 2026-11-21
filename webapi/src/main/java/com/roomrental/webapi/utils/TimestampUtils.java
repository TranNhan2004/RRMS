package com.roomrental.webapi.utils;

import java.time.Instant;
import java.util.Objects;
import java.util.concurrent.ThreadLocalRandom;

public final class TimestampUtils {
    private TimestampUtils() {
    }

    /**
     * Calculates the delay in milliseconds after an attempt, capped at the maximum.
     * Fractional milliseconds are rounded down.
     */
    public static long calculateExponentialAttempt(long currentDelayMs, long maxDelayMs, double multiplier) {
        if (currentDelayMs < 0 || maxDelayMs < 0 || currentDelayMs > maxDelayMs
                || !Double.isFinite(multiplier) || multiplier < 1.0) {
            throw new IllegalArgumentException("Invalid exponential delay settings");
        }
        return (long) Math.min(maxDelayMs, currentDelayMs * multiplier);
    }

    /**
     * Adds a uniformly chosen jitter between zero and {@code maxJitterMs}, inclusive,
     * to the supplied expiration. A zero jitter leaves the expiration unchanged.
     *
     * @throws NullPointerException if expiration is null
     * @throws IllegalArgumentException if maxJitterMs is negative
     * @throws java.time.DateTimeException if the jittered expiration exceeds the supported range
     */
    public static Instant calculateJitterExpiration(Instant expiration, long maxJitterMs) {
        Objects.requireNonNull(expiration, "expiration must not be null");
        if (maxJitterMs < 0) {
            throw new IllegalArgumentException("maxJitterMs must not be negative");
        }
        if (maxJitterMs == 0) {
            return expiration;
        }
        long jitter = maxJitterMs == Long.MAX_VALUE
                ? ThreadLocalRandom.current().nextLong() & Long.MAX_VALUE
                : ThreadLocalRandom.current().nextLong(maxJitterMs + 1);
        return expiration.plusMillis(jitter);
    }
}
