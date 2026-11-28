CREATE TABLE `file_storage` (
    `id`             CHAR(36)       NOT NULL PRIMARY KEY,
    `reference_id`   VARCHAR(255)   NULL,
    `object_key`     VARCHAR(4096)  NOT NULL,
    `file_name`      VARCHAR(2048)  NOT NULL,
    `file_type`      VARCHAR(20)    NOT NULL,
    `content_type`   VARCHAR(255)   NOT NULL,
    `file_size`      BIGINT         NOT NULL,
    `expired_at`     TIMESTAMP(6)   NULL,
    `is_deleted`     BOOLEAN        NOT NULL DEFAULT FALSE,
    `created_at`     TIMESTAMP(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `created_by_id`  CHAR(36)       NULL,
    `updated_at`     TIMESTAMP(6)   NULL,
    `updated_by_id`  CHAR(36)       NULL
);

ALTER TABLE `file_storage`
    ADD CONSTRAINT `fk_file_storage_created_by_id`
    FOREIGN KEY (`created_by_id`) REFERENCES `users` (`id`);

ALTER TABLE `file_storage`
    ADD CONSTRAINT `fk_file_storage_updated_by_id`
    FOREIGN KEY (`updated_by_id`) REFERENCES `users` (`id`);

CREATE INDEX `idx_file_storage_reference_id` ON `file_storage` (`reference_id`);
CREATE INDEX `idx_file_storage_file_type` ON `file_storage` (`file_type`);
