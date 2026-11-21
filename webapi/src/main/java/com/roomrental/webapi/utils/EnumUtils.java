package com.roomrental.webapi.utils;

import java.util.Objects;

public final class EnumUtils {

    private EnumUtils() {
    }

    /**
     * Parses the exact Java name of an enum constant.
     *
     * @throws NullPointerException if either argument is null
     * @throws IllegalArgumentException if no constant matches
     */
    public static <E extends Enum<E>> E parse(Class<E> enumType, String value) {
        Objects.requireNonNull(enumType, "enumType must not be null");
        Objects.requireNonNull(value, "value must not be null");

        return Enum.valueOf(enumType, value);
    }

    /** Returns the enum constant's string representation. */
    public static String toString(Enum<?> value) {
        return Objects.requireNonNull(value, "value must not be null").toString();
    }
}
