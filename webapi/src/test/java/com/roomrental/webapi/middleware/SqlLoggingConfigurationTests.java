package com.roomrental.webapi.middleware;

import static org.assertj.core.api.Assertions.assertThat;

import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;
import org.springframework.boot.context.config.ConfigDataEnvironmentPostProcessor;
import org.springframework.core.env.StandardEnvironment;

class SqlLoggingConfigurationTests {
    @ParameterizedTest
    @CsvSource({"default", "local", "dev", "prod", "prod+dev"})
    void configuration_ShouldUseConfiguredSqlLogLevel_WhenProfilesChange(String profile) {
        // Arrange
        var environment = new StandardEnvironment();
        environment.getPropertySources().remove(StandardEnvironment.SYSTEM_ENVIRONMENT_PROPERTY_SOURCE_NAME);
        environment.getPropertySources().remove(StandardEnvironment.SYSTEM_PROPERTIES_PROPERTY_SOURCE_NAME);
        if (!profile.equals("default")) environment.setActiveProfiles(profile.split("\\+"));

        // Act
        ConfigDataEnvironmentPostProcessor.applyTo(environment);

        // Assert
        assertThat(environment.getProperty("logging.level.org.hibernate.SQL")).isEqualTo("OFF");
        assertThat(environment.getProperty("logging.level.org.hibernate.orm.jdbc.bind")).isEqualTo("OFF");
        assertThat(environment.getProperty("spring.jpa.show-sql")).isEqualTo("false");
    }
}
