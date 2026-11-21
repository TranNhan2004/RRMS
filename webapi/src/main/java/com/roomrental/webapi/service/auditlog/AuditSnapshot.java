package com.roomrental.webapi.service.auditlog;

import java.time.Instant;
import java.util.UUID;

import com.roomrental.webapi.enums.common.AuditAction;

/**
 * Detached values only; no managed entities or request objects cross threads.
 */
public record AuditSnapshot(
        AuditAction action,
        String entityName,
        String entityId,
        String oldValues,
        String newValues,
        AuditContext context,
        UUID transactionId,
        Instant createdAt) {
}
