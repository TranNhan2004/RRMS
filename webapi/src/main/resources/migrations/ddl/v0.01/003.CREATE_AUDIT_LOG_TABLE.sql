CREATE TABLE `audit_logs` (
    `id`                    CHAR(36)        NOT NULL PRIMARY KEY,
    `action`                VARCHAR(20)     NOT NULL,
    `trace_id`              VARCHAR(64)     NULL,
    `request_id`            VARCHAR(255)    NULL,
    `transaction_id`        CHAR(36)        NULL,
    `client_action_id`      VARCHAR(255)    NULL,
    `client_request_id`     VARCHAR(255)    NULL,
    `ip_address`            VARCHAR(45)     NULL,
    `entity_name`           VARCHAR(255)    NOT NULL,
    `entity_id`             VARCHAR(255)    NOT NULL,
    `old_values`            JSON            NULL,
    `new_values`            JSON            NULL,
    `actor_id`              VARCHAR(255)    NOT NULL,
    `created_at`            DATETIME(6)     NOT NULL,
);

CREATE INDEX `idx_audit_logs_transaction_id` 
ON `audit_logs` (`transaction_id`);

CREATE INDEX `idx_audit_logs_entity_name_entity_id` 
ON `audit_logs` (`entity_name`, `entity_id`);

CREATE INDEX `idx_audit_logs_actor_id` 
ON `audit_logs` (`actor_id`);

CREATE INDEX `idx_audit_logs_created_at` 
ON `audit_logs` (`created_at`);
