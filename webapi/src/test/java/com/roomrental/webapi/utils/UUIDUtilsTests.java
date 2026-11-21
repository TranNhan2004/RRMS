package com.roomrental.webapi.utils;

import static org.assertj.core.api.Assertions.assertThat;

import org.junit.jupiter.api.Test;

class UUIDUtilsTests {
    @Test
    void generateV7_ShouldProduceUuidWithTimestampVersionAndVariant_WhenCalled() {
        // Arrange
        long before = System.currentTimeMillis();

        // Act
        var id = UUIDUtils.generateV7();
        long after = System.currentTimeMillis();

        // Assert
        assertThat(id.version()).isEqualTo(7);
        assertThat(id.variant()).isEqualTo(2);
        assertThat(id.getMostSignificantBits() >>> 16).isBetween(before, after);
    }

    @Test
    void generateV7_ShouldProduceDistinctValues_WhenCalledRepeatedly() {
        // Arrange
        var first = UUIDUtils.generateV7();

        // Act
        var second = UUIDUtils.generateV7();

        // Assert
        assertThat(first).isNotEqualTo(second);
    }
}
