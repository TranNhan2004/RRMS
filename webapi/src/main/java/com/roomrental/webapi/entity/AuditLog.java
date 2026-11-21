package com.roomrental.webapi.entity;

import java.time.Instant;
import java.util.UUID;

import org.hibernate.annotations.JdbcTypeCode;
import org.hibernate.type.SqlTypes;

import com.roomrental.webapi.enums.common.AuditAction;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Table;
import lombok.Getter;
import lombok.NoArgsConstructor;
import lombok.Setter;

/** Audit records are deliberately not annotated with Auditable. */
@Getter
@Setter
@NoArgsConstructor
@Entity
@Table(name = "audit_logs")
public class AuditLog extends BaseEntity {
    @Enumerated(EnumType.STRING)
    @Column(name = "action", nullable = false, length = 20)
    private AuditAction action;

    @Column(name = "trace_id", length = 64)
    private String traceId;

    @Column(name = "request_id", length = 255)
    private String requestId;

    @JdbcTypeCode(SqlTypes.CHAR)
    @Column(name = "transaction_id", length = 36)
    private UUID transactionId;

    @Column(name = "client_action_id", length = 255)
    private String clientActionId;

    @Column(name = "client_request_id", length = 255)
    private String clientRequestId;

    @Column(name = "ip_address", length = 45)
    private String ipAddress;

    @Column(name = "entity_name", nullable = false, length = 255)
    private String entityName;

    @Column(name = "entity_id", nullable = false, length = 255)
    private String entityId;

    @JdbcTypeCode(SqlTypes.JSON)
    @Column(name = "old_values", columnDefinition = "json")
    private String oldValues;

    @JdbcTypeCode(SqlTypes.JSON)
    @Column(name = "new_values", columnDefinition = "json")
    private String newValues;

    @Column(name = "actor_id", nullable = false, length = 255)
    private String actorId;

    @Column(name = "created_at", nullable = false, columnDefinition = "datetime(6)")
    private Instant createdAt;
}
