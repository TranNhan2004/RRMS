package com.roomrental.webapi.config.auditlog;

import java.util.Set;

import org.springframework.boot.context.properties.ConfigurationProperties;
import org.springframework.boot.context.properties.bind.DefaultValue;
import org.springframework.validation.annotation.Validated;

import jakarta.validation.Valid;
import jakarta.validation.constraints.DecimalMin;
import jakarta.validation.constraints.Min;
import jakarta.validation.constraints.NotNull;

@Validated
@ConfigurationProperties(prefix = "audit")
public record AuditProperties(
        @DefaultValue("true") boolean enabled,
        @Valid @NotNull @DefaultValue Queue queue,
        @Valid @NotNull @DefaultValue Batch batch,
        @Valid @NotNull @DefaultValue Retry retry,
        @Valid @NotNull @DefaultValue Shutdown shutdown,
        @DefaultValue({ "createdAt", "updatedAt", "createdBy", "updatedBy" }) Set<String> excludedFields) {

    public record Queue(
            @DefaultValue("5000") @Min(1) int capacity,
            @DefaultValue("2") @Min(1) int workerCount,
            @DefaultValue("0") @Min(0) long enqueueTimeoutMs) {
    }

    public record Batch(
            @DefaultValue("true") boolean enabled,
            @DefaultValue("100") @Min(1) int size,
            @DefaultValue("1000") @Min(1) long flushIntervalMs) {
    }

    public record Retry(
            @DefaultValue("3") @Min(1) int maxAttempts,
            @DefaultValue("500") @Min(0) long initialDelayMs,
            @DefaultValue("5000") @Min(0) long maxDelayMs,
            @DefaultValue("2.0") @DecimalMin("1.0") double multiplier) {

        public Retry {
            if (initialDelayMs > maxDelayMs || !Double.isFinite(multiplier)) {
                throw new IllegalArgumentException("Invalid audit retry settings");
            }
        }
    }

    public record Shutdown(@DefaultValue("30") @Min(1) long timeoutSeconds) {
    }
}
