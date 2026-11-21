package com.roomrental.webapi.utils;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.catchThrowable;

import org.junit.jupiter.api.Test;

class EnumUtilsTest {
    private enum Plain { FIRST, SECOND }

    private enum Custom {
        FIRST;

        @Override
        public String toString() { return "custom value"; }
    }

    @Test
    void parse_ShouldReturnConstant_WhenValueMatches() {
        // Arrange
        String plainName = "SECOND";
        String customName = "FIRST";

        // Act
        Plain plain = EnumUtils.parse(Plain.class, plainName);
        Custom custom = EnumUtils.parse(Custom.class, customName);

        // Assert
        assertThat(plain).isEqualTo(Plain.SECOND);
        assertThat(custom).isEqualTo(Custom.FIRST);
    }

    @Test
    void toString_ShouldReturnEnumRepresentation_WhenValueIsProvided() {
        // Arrange
        Plain plain = Plain.FIRST;
        Custom custom = Custom.FIRST;

        // Act
        String plainValue = EnumUtils.toString(plain);
        String customValue = EnumUtils.toString(custom);

        // Assert
        assertThat(plainValue).isEqualTo("FIRST");
        assertThat(customValue).isEqualTo("custom value");
    }

    @Test
    void parse_ShouldThrow_WhenValueIsUnknown() {
        // Arrange
        String name = "first";

        // Act
        Throwable error = catchThrowable(() -> EnumUtils.parse(Plain.class, name));

        // Assert
        assertThat(error).isInstanceOf(IllegalArgumentException.class);
    }

    @Test
    void methods_ShouldThrow_WhenArgumentsAreNull() {
        // Arrange
        String name = "FIRST";

        // Act
        Throwable missingType = catchThrowable(() -> EnumUtils.parse(null, name));
        Throwable missingName = catchThrowable(() -> EnumUtils.parse(Plain.class, null));
        Throwable missingValue = catchThrowable(() -> EnumUtils.toString(null));

        // Assert
        assertThat(missingType).isInstanceOf(NullPointerException.class);
        assertThat(missingName).isInstanceOf(NullPointerException.class);
        assertThat(missingValue).isInstanceOf(NullPointerException.class);
    }

    @Test
    void parse_ShouldUseConstantName_WhenStringRepresentationIsOverridden() {
        // Arrange
        String name = "FIRST";
        String representation = "custom value";

        // Act
        Custom result = EnumUtils.parse(Custom.class, name);
        Throwable error = catchThrowable(() -> EnumUtils.parse(Custom.class, representation));

        // Assert
        assertThat(result).isEqualTo(Custom.FIRST);
        assertThat(error).isInstanceOf(IllegalArgumentException.class);
    }
}
