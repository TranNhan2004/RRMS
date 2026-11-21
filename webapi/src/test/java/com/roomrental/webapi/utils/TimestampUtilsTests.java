package com.roomrental.webapi.utils;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.catchThrowable;

import java.time.Instant;
import java.util.stream.IntStream;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;

class TimestampUtilsTests {
    @ParameterizedTest
    @CsvSource({
            "500, 5000, 2.0, 1000",
            "3000, 5000, 2.0, 5000",
            "1, 100, 1.5, 1",
            "0, 5000, 2.0, 0",
            "9223372036854775806, 9223372036854775807, 2.0, 9223372036854775807"
    })
    void calculateExponentialAttempt_ShouldReturnCappedNextDelay_WhenSettingsAreValid(
            long current, long max, double multiplier, long expected) {
        // Arrange
        // The parameterized inputs supply the delay settings and expected result.

        // Act
        long result = TimestampUtils.calculateExponentialAttempt(current, max, multiplier);

        // Assert
        assertThat(result).isEqualTo(expected);
    }

    @Test
    void calculateExponentialAttempt_ShouldRejectInvalidSettings_WhenInputsAreOutOfRange() {
        // Arrange
        long max = 100;

        // Act
        Throwable negative = catchThrowable(() -> TimestampUtils.calculateExponentialAttempt(-1, max, 2));
        Throwable aboveMax = catchThrowable(() -> TimestampUtils.calculateExponentialAttempt(101, max, 2));
        Throwable smallMultiplier = catchThrowable(() -> TimestampUtils.calculateExponentialAttempt(1, max, 0.5));
        Throwable nanMultiplier = catchThrowable(() -> TimestampUtils.calculateExponentialAttempt(1, max, Double.NaN));
        Throwable infiniteMultiplier = catchThrowable(
                () -> TimestampUtils.calculateExponentialAttempt(1, max, Double.POSITIVE_INFINITY));

        // Assert
        assertThat(negative).isInstanceOf(IllegalArgumentException.class);
        assertThat(aboveMax).isInstanceOf(IllegalArgumentException.class);
        assertThat(smallMultiplier).isInstanceOf(IllegalArgumentException.class);
        assertThat(nanMultiplier).isInstanceOf(IllegalArgumentException.class);
        assertThat(infiniteMultiplier).isInstanceOf(IllegalArgumentException.class);
    }

    @Test
    void calculateJitterExpiration_ShouldStayWithinBounds_WhenJitterIsPositive() {
        // Arrange
        var expiration = Instant.parse("2026-01-01T00:00:00Z");

        // Act
        var results = IntStream.range(0, 100)
                .mapToObj(i -> TimestampUtils.calculateJitterExpiration(expiration, 1000)).toList();

        // Assert
        assertThat(results).allSatisfy(result -> assertThat(result).isBetween(expiration, expiration.plusMillis(1000)));
    }

    @Test
    void calculateJitterExpiration_ShouldReturnExpiration_WhenJitterIsZero() {
        // Arrange
        var expiration = Instant.parse("2026-01-01T00:00:00Z");

        // Act
        Instant result = TimestampUtils.calculateJitterExpiration(expiration, 0);

        // Assert
        assertThat(result).isEqualTo(expiration);
    }

    @Test
    void calculateJitterExpiration_ShouldRejectInvalidInputs_WhenExpirationIsNullOrJitterIsNegative() {
        // Arrange
        Instant expiration = Instant.EPOCH;

        // Act
        Throwable nullExpiration = catchThrowable(() -> TimestampUtils.calculateJitterExpiration(null, 0));
        Throwable negativeJitter = catchThrowable(() -> TimestampUtils.calculateJitterExpiration(expiration, -1));

        // Assert
        assertThat(nullExpiration).isInstanceOf(NullPointerException.class);
        assertThat(negativeJitter).isInstanceOf(IllegalArgumentException.class);
    }
}
