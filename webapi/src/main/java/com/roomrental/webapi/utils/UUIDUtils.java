package com.roomrental.webapi.utils;

import java.security.SecureRandom;
import java.util.UUID;

public final class UUIDUtils {
    private static final SecureRandom RANDOM = new SecureRandom();

    private UUIDUtils() {
    }

    /** Generates a UUIDv7 with a Unix-millisecond timestamp and random bits. */
    public static UUID generateV7() {
        long millis = System.currentTimeMillis() & 0xffffffffffffL;
        long most = (millis << 16) | 0x7000L | (RANDOM.nextInt() & 0xfffL);
        long least = (RANDOM.nextLong() & 0x3fffffffffffffffL) | 0x8000000000000000L;
        return new UUID(most, least);
    }
}
